using System;

namespace TimeTracker.Utils
{
  /// <summary>
  /// When a running job should ask about idle time, and what end time discarding that idle time
  /// should record. All times are parameters so the rules can be exercised directly.
  /// </summary>
  public static class IdlePolicy
  {
    /// <summary>
    /// Idle that accrued before the job started belongs to whatever the user was doing beforehand,
    /// so only a quiet stretch that began at or after the start counts against this job.
    /// </summary>
    public static bool ShouldPrompt(TimeSpan idleTime, DateTime jobStartTime, DateTime now, int idleThresholdMinutes)
    {
      if (idleTime.TotalMinutes < idleThresholdMinutes)
      {
        return false;
      }

      return IdleStartedAt(idleTime, now) >= jobStartTime;
    }

    public static DateTime IdleStartedAt(TimeSpan idleTime, DateTime now)
    {
      return now - idleTime;
    }

    /// <summary>
    /// The end time to record when the user discards the idle stretch: the moment input stopped,
    /// so neither the idle time nor the time spent answering the prompt is kept.
    /// </summary>
    public static DateTime Cutoff(TimeSpan idleTime, DateTime jobStartTime, DateTime now)
    {
      DateTime cutoff = IdleStartedAt(idleTime, now);

      if (cutoff < jobStartTime)
      {
        cutoff = jobStartTime;
      }

      return cutoff > now ? now : cutoff;
    }
  }
}
