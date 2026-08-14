using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace TimeTracker.Utils
{
  /// <summary>
  /// Reading and writing the durations people type into the work entry dialog: "1h 30m", "90m",
  /// "1:30" or a bare number of hours.
  /// </summary>
  public static class DurationText
  {
    // The unit must not be followed by another letter, so "h" cannot swallow the start of "hrs".
    // A following digit is fine, which is what lets "1h30m" parse as well as "1h 30m".
    private const string UnitPattern = @"(?<value>\d+(?:[\.,]\d+)?)\s*(?<unit>h|hr|hrs|hour|hours|m|min|mins|minute|minutes)(?![\p{L}])";

    public static string Format(TimeSpan duration)
    {
      int hours = (int)duration.TotalHours;
      int minutes = duration.Minutes;

      if (hours > 0 && minutes > 0)
      {
        return $"{hours}h {minutes}m";
      }

      return hours > 0 ? $"{hours}h" : $"{minutes}m";
    }

    public static bool TryParse(string text, out TimeSpan duration)
    {
      duration = TimeSpan.Zero;

      if (string.IsNullOrWhiteSpace(text))
      {
        return false;
      }

      text = text.Trim();

      if (text.Contains(':') && TimeSpan.TryParse(text, CultureInfo.CurrentCulture, out duration))
      {
        return duration >= TimeSpan.Zero;
      }

      MatchCollection matches = Regex.Matches(text, UnitPattern, RegexOptions.IgnoreCase);

      if (matches.Count > 0)
      {
        // Anything left over means the text was not purely a duration, so reject it rather than
        // silently reading "1h of admin" as an hour.
        string remainder = Regex.Replace(text, UnitPattern, string.Empty, RegexOptions.IgnoreCase);

        if (!string.IsNullOrWhiteSpace(remainder))
        {
          return false;
        }

        double totalMinutes = 0;
        foreach (Match match in matches.Cast<Match>())
        {
          if (!TryParseFlexibleDouble(match.Groups["value"].Value, out double value))
          {
            return false;
          }

          string unit = match.Groups["unit"].Value.ToLowerInvariant();
          totalMinutes += unit.StartsWith("h", StringComparison.Ordinal)
            ? value * 60
            : value;
        }

        duration = TimeSpan.FromMinutes(totalMinutes);
        return duration >= TimeSpan.Zero;
      }

      if (TryParseFlexibleDouble(text, out double hours))
      {
        duration = TimeSpan.FromHours(hours);
        return duration >= TimeSpan.Zero;
      }

      return false;
    }

    /// <summary>
    /// Accepts both the current culture's decimal separator and a full stop, because people type
    /// "1.5" on a keyboard regardless of their regional settings.
    /// </summary>
    private static bool TryParseFlexibleDouble(string text, out double value)
    {
      return double.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value)
        || double.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }
  }
}
