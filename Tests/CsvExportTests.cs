using System;
using System.Collections.Generic;
using System.Globalization;
using TimeTracker.Models;
using TimeTracker.Services;
using TimeTracker.Utils;
using Xunit;

namespace TimeTracker.Tests
{
  public class CsvExportTests
  {
    private const string HeaderRow = "Date,Start,End,Client,Project,Description,Hours,Rate,Currency,Total";

    private static WorkEntry Entry(string? description)
    {
      DateTime start = new(2026, 8, 10, 9, 0, 0);

      return new WorkEntry
      {
        ID = Guid.NewGuid(),
        StartTime = start,
        EndTime = start.AddHours(2.5),
        Description = description,
        HourlyRate = 50m,
        Currency = "£",
        Project = new Project
        {
          Name = "Website",
          Client = new Client { Name = "Acme" }
        }
      };
    }

    private static string[] Lines(string csv)
    {
      return csv.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
    }

    [Fact]
    public void StartsWithTheHeaderRow()
    {
      string csv = CsvExport.BuildWorkEntryCsv(new List<WorkEntry> { Entry("Design work") });

      Assert.StartsWith(HeaderRow + "\r\n", csv);
    }

    [Fact]
    public void EmptyInputStillYieldsTheHeader()
    {
      Assert.Equal(HeaderRow + "\r\n", CsvExport.BuildWorkEntryCsv(new List<WorkEntry>()));
    }

    [Fact]
    public void WritesOneRowPerEntry()
    {
      string timePattern = TTAppSettings.Instance.ShortTimePattern;
      string expectedStart = new DateTime(2026, 8, 10, 9, 0, 0).ToString(timePattern, CultureInfo.CurrentCulture);
      string expectedEnd = new DateTime(2026, 8, 10, 11, 30, 0).ToString(timePattern, CultureInfo.CurrentCulture);

      string[] lines = Lines(CsvExport.BuildWorkEntryCsv(new List<WorkEntry> { Entry("Design work") }));

      Assert.Equal(2, lines.Length);
      Assert.Equal(
        $"10/08/2026,{expectedStart},{expectedEnd},Acme,Website,Design work,2.50,50.00,£,125.00",
        lines[1]);
    }

    [Fact]
    public void QuotesADescriptionContainingAComma()
    {
      string[] lines = Lines(CsvExport.BuildWorkEntryCsv(new List<WorkEntry> { Entry("Design, build") }));

      Assert.Contains("\"Design, build\"", lines[1]);
    }

    [Fact]
    public void DoublesEmbeddedQuotes()
    {
      string[] lines = Lines(CsvExport.BuildWorkEntryCsv(new List<WorkEntry> { Entry("Fixed the \"login\" bug") }));

      Assert.Contains("\"Fixed the \"\"login\"\" bug\"", lines[1]);
    }

    [Fact]
    public void QuotesADescriptionContainingANewline()
    {
      string csv = CsvExport.BuildWorkEntryCsv(new List<WorkEntry> { Entry("First line\r\nSecond line") });

      Assert.Contains("\"First line\r\nSecond line\"", csv);
      // The quoted newline must not end the record: header plus the two physical lines of the row.
      Assert.Equal(3, csv.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).Length);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void EmptyDescriptionBecomesAnEmptyField(string? description)
    {
      string[] lines = Lines(CsvExport.BuildWorkEntryCsv(new List<WorkEntry> { Entry(description) }));

      Assert.Contains(",Website,,2.50,", lines[1]);
    }
  }
}
