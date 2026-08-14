using System;

namespace TimeTracker.Utils
{
  // TODO: this enum was specified as already living in TTAppSettings.cs, but it is not present on
  // this branch. Delete it from here as soon as the settings copy lands, or the two will collide.
  public enum BillingRoundingMode
  {
    None,
    Nearest,
    Up
  }

  /// <summary>
  /// Rounding a tracked duration to a billing granularity. Kept free of settings and storage types
  /// so the rules can be exercised directly.
  /// </summary>
  public static class BillingRounding
  {
    public static TimeSpan Round(TimeSpan duration, BillingRoundingMode mode, int minutes)
    {
      if (mode == BillingRoundingMode.None || minutes <= 0 || duration <= TimeSpan.Zero)
      {
        return duration;
      }

      long unit = TimeSpan.FromMinutes(minutes).Ticks;
      long remainder = duration.Ticks % unit;

      if (remainder == 0)
      {
        return duration;
      }

      if (mode == BillingRoundingMode.Up)
      {
        return TimeSpan.FromTicks(duration.Ticks - remainder + unit);
      }

      // An exact half rounds up, which is the direction people expect when the result is money.
      return remainder * 2 >= unit
        ? TimeSpan.FromTicks(duration.Ticks - remainder + unit)
        : TimeSpan.FromTicks(duration.Ticks - remainder);
    }

    /// <summary>
    /// A client's own rounding wins over the application default, and each part falls back on its
    /// own: a client may set the mode while leaving the granularity to the default, or the reverse.
    /// </summary>
    public static TimeSpan RoundForClient(TimeSpan duration, BillingRoundingMode? clientMode, int? clientMinutes, BillingRoundingMode defaultMode, int defaultMinutes)
    {
      return Round(duration, clientMode ?? defaultMode, clientMinutes ?? defaultMinutes);
    }
  }
}
