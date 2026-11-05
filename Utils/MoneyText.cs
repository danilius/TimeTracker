using System.Globalization;

namespace TimeTracker.Utils
{
  /// <summary>
  /// Money as text. Every figure the app shows or exports comes through here, so the currency sits
  /// against its number the same way in the tables, the dashboard, the reports and the invoices.
  /// The currency is whatever the entry carries - a symbol, a prefixed symbol like A$, or a letter
  /// code like CHF - and is written with no space before the amount.
  /// </summary>
  public static class MoneyText
  {
    /// <summary>
    /// An amount to the penny, as totals and line items are quoted.
    /// </summary>
    public static string Format(decimal amount, string? currency)
    {
      return Format(amount, currency, "0.00");
    }

    /// <summary>
    /// An hourly rate, which drops trailing zeroes because a rate is usually a round figure.
    /// </summary>
    public static string FormatRate(decimal rate, string? currency)
    {
      return Format(rate, currency, "0.##");
    }

    /// <summary>
    /// An hourly rate with its unit, for the places that label a rate rather than tabulate it.
    /// </summary>
    public static string FormatRatePerHour(decimal rate, string? currency)
    {
      return FormatRate(rate, currency) + "/hr";
    }

    /// <summary>
    /// An already-formatted amount joined to its currency. This is for the figures that are summed
    /// elsewhere and arrive as text.
    /// </summary>
    public static string Join(string? currency, string amount)
    {
      return string.IsNullOrWhiteSpace(currency)
        ? amount
        : currency.Trim() + amount;
    }

    private static string Format(decimal amount, string? currency, string numberFormat)
    {
      return Join(currency, amount.ToString(numberFormat, CultureInfo.CurrentCulture));
    }
  }
}
