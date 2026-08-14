using System;
using TimeTracker.Utils;
using Xunit;

namespace TimeTracker.Tests
{
  public class LongRunningJobPolicyTests
  {
    private static readonly DateTime Start = new(2026, 8, 14, 9, 0, 0);

    [Fact]
    public void DoesNotWarnBeforeTheThreshold()
    {
      Assert.False(LongRunningJobPolicy.ShouldWarn(Start, Start.AddMinutes(59), null, 60, 60));
    }

    [Fact]
    public void WarnsOnceTheThresholdIsPassed()
    {
      Assert.True(LongRunningJobPolicy.ShouldWarn(Start, Start.AddMinutes(60), null, 60, 60));
    }

    [Fact]
    public void DoesNotWarnAgainWithinTheInterval()
    {
      DateTime firstWarning = Start.AddMinutes(60);

      Assert.False(LongRunningJobPolicy.ShouldWarn(Start, firstWarning.AddMinutes(14), firstWarning, 60, 15));
      Assert.True(LongRunningJobPolicy.ShouldWarn(Start, firstWarning.AddMinutes(15), firstWarning, 60, 15));
    }

    [Fact]
    public void FirstCutoffIsTheHourMarkNotTheMomentTheUserAnswers()
    {
      // The prompt sat on screen for 80 minutes; the job still ends when the hour ended.
      DateTime cutoff = LongRunningJobPolicy.Cutoff(Start, Start.AddMinutes(140), null, 60, 60);

      Assert.Equal(Start.AddHours(1), cutoff);
    }

    [Fact]
    public void RepeatCutoffKeepsTimeTheUserAlreadyAcknowledged()
    {
      // Continued at the hour, warned again 15 minutes later: stopping keeps that quarter hour
      // rather than throwing it away by falling back to the original threshold.
      DateTime firstWarning = Start.AddMinutes(60);
      DateTime cutoff = LongRunningJobPolicy.Cutoff(Start, firstWarning.AddMinutes(20), firstWarning, 60, 15);

      Assert.Equal(firstWarning.AddMinutes(15), cutoff);
    }

    [Fact]
    public void CutoffNeverRunsPastNow()
    {
      DateTime now = Start.AddMinutes(61);
      DateTime cutoff = LongRunningJobPolicy.Cutoff(Start, now, null, 90, 90);

      Assert.Equal(now, cutoff);
    }

    [Fact]
    public void CutoffNeverPrecedesTheStart()
    {
      DateTime cutoff = LongRunningJobPolicy.Cutoff(Start, Start.AddMinutes(60), Start.AddMinutes(-120), 60, 30);

      Assert.Equal(Start, cutoff);
    }

    [Theory]
    [InlineData(LongRunningJobBehavior.RepeatReminder, 15)]
    [InlineData(LongRunningJobBehavior.PromptAndContinue, 60)]
    [InlineData(LongRunningJobBehavior.PromptAndStop, 60)]
    public void OnlyTheRepeatPolicyUsesTheReminderInterval(LongRunningJobBehavior behavior, int expected)
    {
      Assert.Equal(expected, LongRunningJobPolicy.IntervalMinutes(behavior, 60, 15));
    }
  }
}
