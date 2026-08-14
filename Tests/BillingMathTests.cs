using System;
using System.Collections.Generic;
using TimeTracker.Models;
using TimeTracker.Utils;
using Xunit;

namespace TimeTracker.Tests
{
  public class BillingMathTests
  {
    private static WorkEntry Entry(DateTime start, double hours, decimal rate)
    {
      return new WorkEntry
      {
        ID = Guid.NewGuid(),
        StartTime = start,
        EndTime = start.AddHours(hours),
        HourlyRate = rate,
        Currency = "£"
      };
    }

    [Fact]
    public void TotalsHoursAcrossEntries()
    {
      List<WorkEntry> entries = new()
      {
        Entry(new DateTime(2026, 8, 10, 9, 0, 0), 2.5, 50),
        Entry(new DateTime(2026, 8, 11, 9, 0, 0), 1.5, 50)
      };

      Assert.Equal(4, TimeTrackerModel.CalculateHoursTotal(entries));
    }

    [Fact]
    public void ChargesEachEntryAtItsOwnRate()
    {
      List<WorkEntry> entries = new()
      {
        Entry(new DateTime(2026, 8, 10, 9, 0, 0), 2, 50),
        Entry(new DateTime(2026, 8, 11, 9, 0, 0), 3, 45)
      };

      Assert.Equal(235m, TimeTrackerModel.CalculateTotal(entries));
    }

    [Fact]
    public void EmptyPeriodCostsNothing()
    {
      Assert.Equal(0m, TimeTrackerModel.CalculateTotal(new List<WorkEntry>()));
      Assert.Equal(0, TimeTrackerModel.CalculateHoursTotal(new List<WorkEntry>()));
    }

    [Theory]
    [InlineData("INV", 7, "INV/7")]
    [InlineData("INV/", 7, "INV/7")]
    [InlineData("  INV  ", 7, "INV/7")]
    [InlineData("", 7, "7")]
    [InlineData(null, 7, "7")]
    public void InvoiceNumberJoinsPrefixAndCounter(string? prefix, int number, string expected)
    {
      Assert.Equal(expected, TimeTrackerModel.FormatInvoiceNumber(prefix, number));
    }

    [Theory]
    // 2026-08-14 is a Friday.
    [InlineData(DayOfWeek.Monday, "2026-08-10")]
    [InlineData(DayOfWeek.Sunday, "2026-08-09")]
    [InlineData(DayOfWeek.Friday, "2026-08-14")]
    public void WeekStartsOnTheConfiguredDay(DayOfWeek weekStartsOn, string expected)
    {
      DateTime start = DateRanges.GetStartOfWeek(new DateTime(2026, 8, 14, 16, 30, 0), weekStartsOn);

      Assert.Equal(DateTime.Parse(expected), start);
    }
  }
}
