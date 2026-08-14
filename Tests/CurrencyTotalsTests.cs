using System;
using System.Collections.Generic;
using TimeTracker.Models;
using Xunit;

namespace TimeTracker.Tests
{
  public class CurrencyTotalsTests
  {
    private static WorkEntry Entry(double hours, decimal rate, string currency)
    {
      DateTime start = new(2026, 8, 10, 9, 0, 0);

      return new WorkEntry
      {
        ID = Guid.NewGuid(),
        StartTime = start,
        EndTime = start.AddHours(hours),
        HourlyRate = rate,
        Currency = currency
      };
    }

    [Fact]
    public void OneCurrencyGivesOneTotal()
    {
      List<WorkEntry> entries = new()
      {
        Entry(2, 50, "£"),
        Entry(3, 45, "£")
      };

      IReadOnlyList<(string Currency, decimal Total)> totals = TimeTrackerModel.CalculateTotalsByCurrency(entries);

      (string Currency, decimal Total) only = Assert.Single(totals);
      Assert.Equal("£", only.Currency);
      Assert.Equal(235m, only.Total);
    }

    [Fact]
    public void TwoCurrenciesStaySeparateAndOrderByDescendingTotal()
    {
      List<WorkEntry> entries = new()
      {
        Entry(1, 130.01m, "€"),
        Entry(2, 50, "£"),
        Entry(4, 200, "£")
      };

      IReadOnlyList<(string Currency, decimal Total)> totals = TimeTrackerModel.CalculateTotalsByCurrency(entries);

      Assert.Equal(2, totals.Count);
      Assert.Equal("£", totals[0].Currency);
      Assert.Equal(900m, totals[0].Total);
      Assert.Equal("€", totals[1].Currency);
      Assert.Equal(130.01m, totals[1].Total);
    }

    [Fact]
    public void EmptyInputHasNoTotals()
    {
      Assert.Empty(TimeTrackerModel.CalculateTotalsByCurrency(new List<WorkEntry>()));
    }

    [Fact]
    public void BlankCurrencyGroupsUnderNoCurrency()
    {
      List<WorkEntry> entries = new()
      {
        Entry(2, 10, string.Empty),
        Entry(1, 10, "   "),
        Entry(1, 10, "£")
      };

      IReadOnlyList<(string Currency, decimal Total)> totals = TimeTrackerModel.CalculateTotalsByCurrency(entries);

      Assert.Equal(2, totals.Count);
      Assert.Equal(string.Empty, totals[0].Currency);
      Assert.Equal(30m, totals[0].Total);
      Assert.Equal("£", totals[1].Currency);
      Assert.Equal(10m, totals[1].Total);
    }
  }
}
