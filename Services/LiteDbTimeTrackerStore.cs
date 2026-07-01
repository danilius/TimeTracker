using System;
using System.IO;
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

    private sealed class AppDataRecord
    {
      public string ID { get; set; } = DataRecordID;
      public string Payload { get; set; } = string.Empty;
      public DateTime SavedAt { get; set; }
    }
  }
}
