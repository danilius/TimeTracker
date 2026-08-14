using System;

namespace TimeTracker.Utils
{
  /// <summary>
  /// Date arithmetic for the dashboard periods.
  /// </summary>
  public static class DateRanges
  {
    /// <summary>
    /// The first day of the week containing <paramref name="date" />, for a week that starts on
    /// <paramref name="weekStartsOn" />.
    /// </summary>
    public static DateTime GetStartOfWeek(DateTime date, DayOfWeek weekStartsOn)
    {
      int daysSinceWeekStart = (7 + (date.DayOfWeek - weekStartsOn)) % 7;

      return date.Date.AddDays(-daysSinceWeekStart);
    }
  }
}
