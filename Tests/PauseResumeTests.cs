using System;
using TimeTracker.Models;
using Xunit;

namespace TimeTracker.Tests
{
  public class PauseResumeTests
  {
    private static readonly DateTime Start = new(2026, 8, 14, 9, 0, 0);

    private static WorkEntry FinishedEntry(double elapsedHours)
    {
      return new WorkEntry
      {
        ID = Guid.NewGuid(),
        StartTime = Start,
        EndTime = Start.AddHours(elapsedHours)
      };
    }

    [Fact]
    public void DurationIsTheElapsedTimeWhenNothingWasPaused()
    {
      Assert.Equal(TimeSpan.FromHours(2), FinishedEntry(2).Duration);
    }

    [Fact]
    public void PausedTimeIsNotBilled()
    {
      WorkEntry entry = FinishedEntry(2);
      entry.PausedDuration = TimeSpan.FromMinutes(30);

      Assert.Equal(TimeSpan.FromMinutes(90), entry.Duration);
    }

    [Fact]
    public void DurationNeverGoesNegative()
    {
      WorkEntry entry = FinishedEntry(1);
      entry.PausedDuration = TimeSpan.FromHours(3);

      Assert.Equal(TimeSpan.Zero, entry.Duration);
    }

    [Fact]
    public void AnOpenPauseCountsAgainstTheDuration()
    {
      WorkEntry entry = FinishedEntry(2);
      entry.PausedAt = DateTime.Now.AddMinutes(-10);

      Assert.True(entry.IsPaused);
      // Ten minutes of the two hours are currently paused, allowing for the clock moving on.
      Assert.InRange(entry.Duration.TotalMinutes, 109, 110.5);
    }

    [Fact]
    public void ClearingThePauseMakesTheEntryRunAgain()
    {
      WorkEntry entry = FinishedEntry(2);
      entry.PausedAt = Start;
      entry.PausedAt = null;

      Assert.False(entry.IsPaused);
      Assert.Equal(TimeSpan.FromHours(2), entry.Duration);
    }

    [Fact]
    public void AnEntryWithNoEndTimeHasNoDuration()
    {
      WorkEntry entry = new() { ID = Guid.NewGuid(), StartTime = Start };

      Assert.Equal(TimeSpan.Zero, entry.Duration);
    }
  }
}
