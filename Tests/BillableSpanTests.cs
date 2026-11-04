using System;
using TimeTracker.Models;
using TimeTracker.Utils;
using Xunit;

namespace TimeTracker.Tests
{
  /// <summary>
  /// The entry dialog shows and edits billable time while the start and end times record when
  /// the work happened, so the two readings have to agree: a duration typed into the dialog has
  /// to come back out of <see cref="WorkEntry.Duration" /> unchanged, however much of the job
  /// was paused.
  /// </summary>
  public class BillableSpanTests
  {
    private static readonly DateTime Start = new(2026, 11, 4, 11, 13, 48);

    [Fact]
    public void PausedTimeComesOffTheElapsedSpan()
    {
      Assert.Equal(
        TimeSpan.FromMinutes(90),
        BillableSpan.BillableOf(TimeSpan.FromHours(2), TimeSpan.FromMinutes(30)));
    }

    [Fact]
    public void BillableTimeNeverGoesNegative()
    {
      Assert.Equal(
        TimeSpan.Zero,
        BillableSpan.BillableOf(TimeSpan.FromMinutes(34), TimeSpan.FromMinutes(73)));
    }

    [Fact]
    public void TheElapsedSpanHasToCoverThePause()
    {
      Assert.Equal(
        TimeSpan.FromMinutes(107),
        BillableSpan.ElapsedFor(TimeSpan.FromMinutes(34), TimeSpan.FromMinutes(73)));
    }

    [Theory]
    [InlineData(34, 0)]
    [InlineData(34, 73)]
    [InlineData(120, 15)]
    [InlineData(1, 600)]
    public void ATypedDurationSurvivesBeingStoredAsAnEndTime(int billableMinutes, int pausedMinutes)
    {
      TimeSpan billable = TimeSpan.FromMinutes(billableMinutes);
      TimeSpan paused = TimeSpan.FromMinutes(pausedMinutes);

      WorkEntry entry = new()
      {
        ID = Guid.NewGuid(),
        StartTime = Start,
        PausedDuration = paused,
        EndTime = BillableSpan.EndTimeFor(Start, billable, paused)
      };

      Assert.Equal(billable, entry.Duration);
    }

    [Fact]
    public void ARunningJobsStartTimeIsWoundBackPastThePause()
    {
      DateTime now = Start.AddHours(5);
      TimeSpan paused = TimeSpan.FromMinutes(73);

      WorkEntry entry = new()
      {
        ID = Guid.NewGuid(),
        StartTime = BillableSpan.StartTimeFor(now, TimeSpan.FromMinutes(34), paused),
        EndTime = now,
        PausedDuration = paused
      };

      Assert.Equal(now.AddMinutes(-107), entry.StartTime);
      Assert.Equal(TimeSpan.FromMinutes(34), entry.Duration);
    }
  }
}
