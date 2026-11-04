using System;

namespace TimeTracker.Utils
{
  /// <summary>
  /// The one relationship between the three figures a work entry carries: the span between its
  /// start and end times, the time paused or idle inside that span, and the billable duration
  /// left over. The entry reads it one way and an edit writes it the other, so both go through
  /// here instead of each doing the arithmetic -- the two drifting apart is what let an edited
  /// job subtract its paused time twice and report nothing billable at all.
  /// </summary>
  public static class BillableSpan
  {
    /// <summary>
    /// What is billable out of an elapsed span. Never negative: a paused total larger than the
    /// span it sits in means there is nothing left to bill, not that time was owed back.
    /// </summary>
    public static TimeSpan BillableOf(TimeSpan elapsed, TimeSpan paused)
    {
      TimeSpan billable = elapsed - Floor(paused);

      return Floor(billable);
    }

    /// <summary>
    /// The span a job has to cover for <paramref name="billable" /> to survive the paused time
    /// being taken off it again.
    /// </summary>
    public static TimeSpan ElapsedFor(TimeSpan billable, TimeSpan paused)
    {
      return Floor(billable) + Floor(paused);
    }

    /// <summary>
    /// Where a job that started at <paramref name="startTime" /> has to end to be worth
    /// <paramref name="billable" />.
    /// </summary>
    public static DateTime EndTimeFor(DateTime startTime, TimeSpan billable, TimeSpan paused)
    {
      return startTime + ElapsedFor(billable, paused);
    }

    /// <summary>
    /// Where a job has to have started to be worth <paramref name="billable" /> by
    /// <paramref name="endTime" />. Used while a job runs, where the end is simply now.
    /// </summary>
    public static DateTime StartTimeFor(DateTime endTime, TimeSpan billable, TimeSpan paused)
    {
      return endTime - ElapsedFor(billable, paused);
    }

    private static TimeSpan Floor(TimeSpan value)
    {
      return value < TimeSpan.Zero ? TimeSpan.Zero : value;
    }
  }
}
