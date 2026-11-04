using System;
using TimeTracker.Models;
using Xunit;

namespace TimeTracker.Tests
{
  /// <summary>
  /// Loading repairs paused totals that no running job could have produced. An older build let a
  /// typed duration overwrite the end time while leaving the paused total in place, so a job
  /// edited after its idle time had been discarded read as zero billable minutes.
  /// </summary>
  public class PausedTotalRepairTests
  {
    private static readonly DateTime Start = new(2026, 11, 4, 11, 13, 48);

    private static WorkEntry FinishedEntry(TimeSpan elapsed)
    {
      return new WorkEntry
      {
        ID = Guid.NewGuid(),
        StartTime = Start,
        EndTime = Start + elapsed
      };
    }

    [Fact]
    public void APausedTotalLongerThanTheJobIsCleared()
    {
      WorkEntry entry = FinishedEntry(TimeSpan.FromMinutes(34));
      entry.PausedDuration = TimeSpan.FromMinutes(73);

      TimeTrackerModel.RepairPausedTotal(entry);

      Assert.Equal(TimeSpan.Zero, entry.PausedDuration);
      Assert.Equal(TimeSpan.FromMinutes(34), entry.Duration);
    }

    [Fact]
    public void APlausiblePausedTotalIsLeftAlone()
    {
      WorkEntry entry = FinishedEntry(TimeSpan.FromHours(2));
      entry.PausedDuration = TimeSpan.FromMinutes(30);

      TimeTrackerModel.RepairPausedTotal(entry);

      Assert.Equal(TimeSpan.FromMinutes(30), entry.PausedDuration);
      Assert.Equal(TimeSpan.FromMinutes(90), entry.Duration);
    }

    [Fact]
    public void APauseLeftOpenIsClosedAtTheEndTime()
    {
      WorkEntry entry = FinishedEntry(TimeSpan.FromHours(2));
      entry.PausedAt = Start.AddMinutes(90);

      TimeTrackerModel.RepairPausedTotal(entry);

      Assert.False(entry.IsPaused);
      Assert.Equal(TimeSpan.FromMinutes(30), entry.PausedDuration);
      Assert.Equal(TimeSpan.FromMinutes(90), entry.Duration);
    }

    [Fact]
    public void AnEntryThatNeverEndedIsLeftAlone()
    {
      WorkEntry entry = new() { ID = Guid.NewGuid(), StartTime = Start };
      entry.PausedAt = Start.AddMinutes(10);

      TimeTrackerModel.RepairPausedTotal(entry);

      Assert.True(entry.IsPaused);
      Assert.Equal(TimeSpan.Zero, entry.PausedDuration);
    }
  }
}
