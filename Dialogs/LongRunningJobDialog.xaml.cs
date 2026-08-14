using System;
using System.Globalization;
using System.Windows;
using TimeTracker.Models;
using TimeTracker.Utils;

namespace TimeTracker.Dialogs
{
  /// <summary>
  /// Interaction logic for LongRunningJobDialog.xaml
  /// </summary>
  public partial class LongRunningJobDialog : Window
  {
    /// <summary>
    /// Warns that <paramref name="workEntry" /> has passed the long running threshold.
    /// </summary>
    /// <param name="cutoff">
    /// When the job will be recorded as having ended if the user stops it — the moment the
    /// threshold was reached, not the moment the user answers the prompt.
    /// </param>
    /// <param name="allowContinue">
    /// False when the configured policy stops the job regardless, which leaves Stop as the only
    /// button.
    /// </param>
    public LongRunningJobDialog(WorkEntry workEntry, DateTime cutoff, bool allowContinue)
    {
      InitializeComponent();

      int thresholdMinutes = TTAppSettings.Instance.LongRunningJobThresholdMinutes;
      string timePattern = TTAppSettings.Instance.ShortTimePattern;

      MessageTextBlock.Text = allowContinue
        ? $"This job has been running for more than {FormatThreshold(thresholdMinutes)}."
        : $"This job has been running for more than {FormatThreshold(thresholdMinutes)} and will be stopped.";

      JobTextBlock.Text = string.IsNullOrWhiteSpace(workEntry.ClientName)
        ? workEntry.ProjectName
        : $"{workEntry.ClientName} — {workEntry.ProjectName}";

      DetailTextBlock.Text = string.Format(
        CultureInfo.CurrentCulture,
        "Started {0}, running for {1}. Stopping records it as ending at {2}.",
        workEntry.StartTime.ToString(timePattern, CultureInfo.CurrentCulture),
        FormatElapsed(DateTime.Now - workEntry.StartTime),
        cutoff.ToString(timePattern, CultureInfo.CurrentCulture));

      if (!allowContinue)
      {
        ContinueButton.Visibility = Visibility.Collapsed;
        StopButton.IsDefault = true;
      }

      // Closing the window with the title bar leaves the job running, which is the safe default.
      ContinueRequested = allowContinue;
    }

    public bool ContinueRequested { get; private set; }

    private static string FormatThreshold(int minutes)
    {
      if (minutes == 60)
      {
        return "an hour";
      }

      return minutes % 60 == 0
        ? $"{minutes / 60} hours"
        : $"{minutes} minutes";
    }

    private static string FormatElapsed(TimeSpan elapsed)
    {
      if (elapsed < TimeSpan.Zero)
      {
        elapsed = TimeSpan.Zero;
      }

      return elapsed.TotalHours >= 1
        ? $"{(int)elapsed.TotalHours}h {elapsed.Minutes}m"
        : $"{elapsed.Minutes}m";
    }

    private void ContinueButton_Click(object sender, RoutedEventArgs e)
    {
      ContinueRequested = true;
      DialogResult = true;
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
      ContinueRequested = false;
      DialogResult = true;
    }
  }
}
