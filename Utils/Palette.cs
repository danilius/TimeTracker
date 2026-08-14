using System.Windows.Media;

namespace TimeTracker.Utils
{
  /// <summary>
  /// The single definition of every colour in the app. XAML resources take their colours from the
  /// <c>*Color</c> fields via <c>x:Static</c> and code-behind uses the matching brushes, so a
  /// colour is written down exactly once.
  /// </summary>
  public static class Palette
  {
    public static readonly Color AccentColor = FromHex("#0090EE");
    public static readonly Color AccentDeepColor = FromHex("#0079E7");
    public static readonly Color AccentHoverColor = FromHex("#0B66BE");
    public static readonly Color AccentPressedColor = FromHex("#0B7FD0");
    public static readonly Color AlternatingRowColor = FromHex("#FAFBFC");
    public static readonly Color AppBackgroundColor = FromHex("#F3F5F7");
    public static readonly Color PanelColor = FromHex("#FFFFFF");
    public static readonly Color PanelBorderColor = FromHex("#D9E0E7");
    public static readonly Color PrimaryTextColor = FromHex("#17212B");
    public static readonly Color SecondaryTextColor = FromHex("#64707D");
    public static readonly Color NavColor = FromHex("#0B0F14");
    public static readonly Color NavHoverColor = FromHex("#121A22");
    public static readonly Color NavSelectedColor = FromHex("#122232");
    public static readonly Color NavMutedColor = FromHex("#C8D1DC");
    public static readonly Color SuccessColor = FromHex("#1F9D61");
    public static readonly Color WarningColor = FromHex("#D98B16");
    public static readonly Color DangerColor = FromHex("#C22F2F");
    public static readonly Color DangerHoverColor = FromHex("#A92828");
    public static readonly Color InputBorderColor = FromHex("#CBD5DF");
    public static readonly Color LockedFieldColor = FromHex("#EEF2F6");
    public static readonly Color HighlightColor = FromHex("#EAF4FF");
    public static readonly Color HighlightBorderColor = FromHex("#8EC7F7");
    public static readonly Color TableHeaderColor = FromHex("#F8FAFC");
    public static readonly Color RowDividerColor = FromHex("#E7ECF1");
    public static readonly Color RowSelectedColor = FromHex("#F4FAFF");
    public static readonly Color ChartSeriesColor = FromHex("#5E6AD2");
    public static readonly Color DialogRuleColor = FromHex("#E4E9EF");
    public static readonly Color StepperColor = FromHex("#F5F7FA");
    public static readonly Color RunningRowColor = FromHex("#DFF5DF");

    public static readonly SolidColorBrush Accent = Frozen(AccentColor);
    public static readonly SolidColorBrush AccentDeep = Frozen(AccentDeepColor);
    public static readonly SolidColorBrush AppBackground = Frozen(AppBackgroundColor);
    public static readonly SolidColorBrush Panel = Frozen(PanelColor);
    public static readonly SolidColorBrush PanelBorder = Frozen(PanelBorderColor);
    public static readonly SolidColorBrush PrimaryText = Frozen(PrimaryTextColor);
    public static readonly SolidColorBrush SecondaryText = Frozen(SecondaryTextColor);
    public static readonly SolidColorBrush NavSelected = Frozen(NavSelectedColor);
    public static readonly SolidColorBrush NavMuted = Frozen(NavMutedColor);
    public static readonly SolidColorBrush Success = Frozen(SuccessColor);
    public static readonly SolidColorBrush Warning = Frozen(WarningColor);
    public static readonly SolidColorBrush Danger = Frozen(DangerColor);
    public static readonly SolidColorBrush LockedField = Frozen(LockedFieldColor);
    public static readonly SolidColorBrush Highlight = Frozen(HighlightColor);
    public static readonly SolidColorBrush HighlightBorder = Frozen(HighlightBorderColor);
    public static readonly SolidColorBrush TableHeader = Frozen(TableHeaderColor);
    public static readonly SolidColorBrush RowDivider = Frozen(RowDividerColor);
    public static readonly SolidColorBrush RowSelected = Frozen(RowSelectedColor);
    public static readonly SolidColorBrush ChartSeries = Frozen(ChartSeriesColor);
    public static readonly SolidColorBrush AccentHover = Frozen(AccentHoverColor);
    public static readonly SolidColorBrush DangerHover = Frozen(DangerHoverColor);
    public static readonly SolidColorBrush InputBorder = Frozen(InputBorderColor);
    public static readonly SolidColorBrush DialogRule = Frozen(DialogRuleColor);
    public static readonly SolidColorBrush Stepper = Frozen(StepperColor);
    public static readonly SolidColorBrush RunningRow = Frozen(RunningRowColor);

    private static Color FromHex(string hex)
    {
      return (Color)ColorConverter.ConvertFromString(hex);
    }

    /// <summary>
    /// Frozen brushes are shared safely and skip change tracking, which matters because these are
    /// handed to thousands of elements as tables are rebuilt.
    /// </summary>
    private static SolidColorBrush Frozen(Color color)
    {
      SolidColorBrush brush = new(color);
      brush.Freeze();

      return brush;
    }
  }
}
