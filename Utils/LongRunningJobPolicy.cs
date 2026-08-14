using System;

namespace TimeTracker.Utils
{
  /// <summary>
  /// When a running job should be warned about, and what end time a stop from that warning should
  /// record. Kept free of UI and storage so the rules can be exercised directly.
  /// </summary>
  public static class LongRunningJobPolicy
  {
    /// <summary>
    /// The gap between warnings: the reminder interval when repeating, otherwise the threshold.
    /// </summary>
    public static int IntervalMinutes(LongRunningJobBehavior behavior, int thresholdMinutes, int reminderMinutes)
    {
      return behavior == LongRunningJobBehavior.RepeatReminder
        ? reminderMinutes
        : thresholdMinutes;
    }

    public static bool ShouldWarn(DateTime startTime, DateTime now, DateTime? lastWarningTime, int thresholdMinutes, int intervalMinutes)
    {
      if ((now - startTime).TotalMinutes < thresholdMinutes)
      {
        return false;
      }

      if (lastWarningTime == null)
      {
        return true;
      }

      return (now - lastWarningTime.Value).TotalMinutes >= intervalMinutes;
    }

    /// <summary>
    /// When the current warning became due: the threshold itself for the first warning, and the
    /// end of the latest reminder interval for repeats. Stopping from the warning records the job
    /// as ending here, so time spent deciding is not billed — and time already acknowledged by an
    /// earlier "continue" is not thrown away.
    /// </summary>
    public static DateTime Cutoff(DateTime startTime, DateTime now, DateTime? lastWarningTime, int thresholdMinutes, int intervalMinutes)
    {
      DateTime cutoff = lastWarningTime == null
        ? startTime.AddMinutes(thresholdMinutes)
        : lastWarningTime.Value.AddMinutes(intervalMinutes);

      if (cutoff < startTime)
      {
        cutoff = startTime;
      }

      return cutoff > now ? now : cutoff;
    }
  }
}
