using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using LiteDB;
using TimeTracker.Models;
using TimeTracker.Utils;

namespace TimeTracker.Services
{
  public class LiteDbTimeTrackerStore
  {
    private const string DataCollectionName = "app_data";
    private const string DataRecordID = "main";
    private const string DailyBackupPrefix = "tt_data-";
    private const string BeforeRestoreBackupPrefix = "tt_data-before-restore-";

    public TimeTrackerData LoadData()
    {
      using LiteDatabase database = new(GetDatabaseFilePath());
      ILiteCollection<AppDataRecord> collection = database.GetCollection<AppDataRecord>(DataCollectionName);
      AppDataRecord? record = collection.FindById(DataRecordID);

      if (record != null && !string.IsNullOrWhiteSpace(record.Payload))
      {
        return System.Text.Json.JsonSerializer.Deserialize<TimeTrackerData>(record.Payload) ?? new TimeTrackerData();
      }

      return LoadLegacyJsonData();
    }

    public void SaveData(TimeTrackerData data)
    {
      using LiteDatabase database = new(GetDatabaseFilePath());
      ILiteCollection<AppDataRecord> collection = database.GetCollection<AppDataRecord>(DataCollectionName);
      collection.Upsert(new AppDataRecord
      {
        ID = DataRecordID,
        Payload = System.Text.Json.JsonSerializer.Serialize(data),
        SavedAt = DateTime.Now
      });
    }

    public static string GetDatabaseFilePath()
    {
      string dataFolder = GetDataFolderPath();
      return Path.Combine(dataFolder, "tt_data.db");
    }

    /// <summary>
    /// Copies the data file into the backup folder once per day, keeping as many copies as the
    /// user has asked for. Everything lives in a single record that is rewritten on every save, so
    /// one bad write would otherwise take the whole billing history with it.
    /// </summary>
    /// <returns>The backup written, or null when nothing needed doing or the copy failed.</returns>
    public static string? CreateDailyBackup()
    {
      try
      {
        string databaseFile = GetDatabaseFilePath();

        if (!File.Exists(databaseFile))
        {
          return null;
        }

        string backupFolder = GetBackupFolderPath();
        Directory.CreateDirectory(backupFolder);

        string backupPath = Path.Combine(backupFolder, $"tt_data-{DateTime.Now:yyyyMMdd}.db");

        if (File.Exists(backupPath))
        {
          return null;
        }

        File.Copy(databaseFile, backupPath);
        PruneBackups(backupFolder);

        return backupPath;
      }
      catch (IOException)
      {
        return null;
      }
      catch (UnauthorizedAccessException)
      {
        return null;
      }
    }

    /// <summary>
    /// The backups on disk, newest first. Both the daily copies and the safety copies taken before
    /// a restore are listed: either is something the user may want back.
    /// </summary>
    public static IReadOnlyList<BackupFile> GetBackups()
    {
      string backupFolder = GetBackupFolderPath();

      if (!Directory.Exists(backupFolder))
      {
        return Array.Empty<BackupFile>();
      }

      List<BackupFile> backups = new();

      foreach (string backupPath in Directory.GetFiles(backupFolder, "tt_data-*.db"))
      {
        FileInfo fileInfo = new(backupPath);
        backups.Add(new BackupFile(backupPath, GetBackupTimestamp(fileInfo), fileInfo.Length));
      }

      backups.Sort((left, right) => right.TakenAt.CompareTo(left.TakenAt));

      return backups;
    }

    /// <summary>
    /// Puts <paramref name="backupPath" /> back in place of the live database, after copying the
    /// current database aside so the restore itself can be undone. The caller is expected to close
    /// the application afterwards: the running instance holds the old data in memory and would
    /// write it straight back over the restored file.
    /// </summary>
    /// <returns>The safety copy written, or null when there was no database to copy aside.</returns>
    public static string? RestoreFromBackup(string backupPath)
    {
      if (!File.Exists(backupPath))
      {
        throw new FileNotFoundException("The backup file no longer exists.", backupPath);
      }

      string databaseFile = GetDatabaseFilePath();
      string backupFolder = GetBackupFolderPath();
      Directory.CreateDirectory(backupFolder);

      string? safetyCopyPath = null;

      if (File.Exists(databaseFile))
      {
        safetyCopyPath = Path.Combine(backupFolder, $"{BeforeRestoreBackupPrefix}{DateTime.Now:yyyyMMdd-HHmmss}.db");
        File.Copy(databaseFile, safetyCopyPath, true);
      }

      File.Copy(backupPath, databaseFile, true);
      PruneBackups(backupFolder);

      return safetyCopyPath;
    }

    /// <summary>
    /// When a backup was taken, read from its name where possible so that copying files around
    /// cannot rewrite the history. File.Copy carries the source's write time over, so the file
    /// system alone would date a daily backup by when the data was last saved.
    /// </summary>
    private static DateTime GetBackupTimestamp(FileInfo fileInfo)
    {
      string name = Path.GetFileNameWithoutExtension(fileInfo.Name);

      if (name.StartsWith(BeforeRestoreBackupPrefix, StringComparison.OrdinalIgnoreCase)
        && DateTime.TryParseExact(name[BeforeRestoreBackupPrefix.Length..], "yyyyMMdd-HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime takenAt))
      {
        return takenAt;
      }

      if (name.StartsWith(DailyBackupPrefix, StringComparison.OrdinalIgnoreCase)
        && DateTime.TryParseExact(name[DailyBackupPrefix.Length..], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime takenOn))
      {
        return takenOn;
      }

      return fileInfo.LastWriteTime;
    }

    /// <summary>
    /// Trims both kinds of copy to the configured limit, counted separately so a run of restores
    /// cannot push the daily history out and vice versa.
    /// </summary>
    private static void PruneBackups(string backupFolder)
    {
      int keep = TTAppSettings.Instance.BackupsToKeep;

      if (keep < 1)
      {
        keep = 1;
      }

      string[] allBackups = Directory.GetFiles(backupFolder, "tt_data-*.db");

      PruneGroup(allBackups.Where(IsDailyBackup).ToArray(), keep);
      PruneGroup(allBackups.Where(IsBeforeRestoreBackup).ToArray(), keep);
    }

    private static void PruneGroup(string[] backups, int keep)
    {
      // Dated names sort chronologically, so the newest are simply the last by name.
      Array.Sort(backups, StringComparer.OrdinalIgnoreCase);

      for (int index = 0; index < backups.Length - keep; index++)
      {
        try
        {
          File.Delete(backups[index]);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
      }
    }

    private static bool IsBeforeRestoreBackup(string backupPath)
    {
      return Path.GetFileNameWithoutExtension(backupPath)
        .StartsWith(BeforeRestoreBackupPrefix, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// A daily copy, whose name is the prefix followed by a plain date. Anything else in the folder
    /// — the copies taken before a restore — is not part of the rota.
    /// </summary>
    private static bool IsDailyBackup(string backupPath)
    {
      string name = Path.GetFileNameWithoutExtension(backupPath);

      return name.StartsWith(DailyBackupPrefix, StringComparison.OrdinalIgnoreCase)
        && DateTime.TryParseExact(name[DailyBackupPrefix.Length..], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
    }

    public static string GetBackupFolderPath()
    {
      return Path.Combine(GetDataFolderPath(), "backups");
    }

    public static string GetLegacyJsonDataFilePath()
    {
      string dataFolder = GetDataFolderPath();
      return Path.Combine(dataFolder, "tt_data.json");
    }

    private static TimeTrackerData LoadLegacyJsonData()
    {
      string dataFile = GetLegacyJsonDataFilePath();

      if (!File.Exists(dataFile))
      {
        return new TimeTrackerData();
      }

      string? json = File.ReadAllText(dataFile);

      if (string.IsNullOrWhiteSpace(json))
      {
        return new TimeTrackerData();
      }

      TimeTrackerData? data = null;
      try
      {
        data = System.Text.Json.JsonSerializer.Deserialize<TimeTrackerData>(json);
      }
      catch (JsonException)
      {
        data = null;
      }

      if (data != null && (data.Clients.Count > 0 || data.Reports.Count > 0 || data.Invoices.Count > 0 || json.Contains("\"Clients\"")))
      {
        return data;
      }

      TrulyObservableCollection<Client>? legacyClients = System.Text.Json.JsonSerializer.Deserialize<TrulyObservableCollection<Client>>(json);
      return new TimeTrackerData
      {
        Clients = legacyClients ?? new TrulyObservableCollection<Client>()
      };
    }

    private static string GetDataFolderPath()
    {
      string roamingFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
      string dataFolder = Path.Combine(roamingFolder, "TimeTrackerApp");

      if (!Directory.Exists(dataFolder))
      {
        Directory.CreateDirectory(dataFolder);
      }

      return dataFolder;
    }

    /// <summary>
    /// One file in the backup folder, as the settings page needs to describe it.
    /// </summary>
    public sealed class BackupFile
    {
      public BackupFile(string filePath, DateTime takenAt, long sizeBytes)
      {
        FilePath = filePath;
        TakenAt = takenAt;
        SizeBytes = sizeBytes;
      }

      public string FilePath { get; }

      public string FileName => Path.GetFileName(FilePath);

      public DateTime TakenAt { get; }

      public long SizeBytes { get; }
    }

    private sealed class AppDataRecord
    {
      public string ID { get; set; } = DataRecordID;
      public string Payload { get; set; } = string.Empty;
      public DateTime SavedAt { get; set; }
    }
  }
}
