using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using TimeTracker.Models;
using TimeTracker.Utils;

namespace TimeTracker.Services
{
  public static class CsvExport
  {
    private const string LineEnding = "\r\n";

    public static string BuildWorkEntryCsv(IEnumerable<WorkEntry> workEntries)
    {
      StringBuilder csv = new();
      csv.Append("Date,Start,End,Client,Project,Description,Hours,Rate,Currency,Total");
      csv.Append(LineEnding);

      if (workEntries == null)
      {
        return csv.ToString();
      }

      string timePattern = TTAppSettings.Instance.ShortTimePattern;

      foreach (WorkEntry entry in workEntries)
      {
        decimal hours = (decimal)entry.Duration.TotalHours;
        decimal total = hours * entry.HourlyRate;

        csv.Append(EscapeField(entry.StartTime.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture)));
        csv.Append(',');
        csv.Append(EscapeField(entry.StartTime.ToString(timePattern, CultureInfo.CurrentCulture)));
        csv.Append(',');
        csv.Append(EscapeField(entry.EndTime.ToString(timePattern, CultureInfo.CurrentCulture)));
        csv.Append(',');
        csv.Append(EscapeField(entry.ClientName));
        csv.Append(',');
        csv.Append(EscapeField(entry.ProjectName));
        csv.Append(',');
        csv.Append(EscapeField(entry.Description));
        csv.Append(',');
        csv.Append(EscapeField(hours.ToString("0.00", CultureInfo.InvariantCulture)));
        csv.Append(',');
        csv.Append(EscapeField(entry.HourlyRate.ToString("0.00", CultureInfo.InvariantCulture)));
        csv.Append(',');
        csv.Append(EscapeField(entry.Currency));
        csv.Append(',');
        csv.Append(EscapeField(total.ToString("0.00", CultureInfo.InvariantCulture)));
        csv.Append(LineEnding);
      }

      return csv.ToString();
    }

    /// <summary>
    /// Writes the CSV as UTF-8 with a byte order mark. Excel reads a BOM-less file as ANSI,
    /// which mangles the currency symbols the entries carry.
    /// </summary>
    public static void WriteWorkEntryCsv(string filePath, IEnumerable<WorkEntry> workEntries)
    {
      File.WriteAllText(filePath, BuildWorkEntryCsv(workEntries), new UTF8Encoding(true));
    }

    private static string EscapeField(string? value)
    {
      if (string.IsNullOrEmpty(value))
      {
        return string.Empty;
      }

      if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
      {
        return value;
      }

      return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
  }
}
