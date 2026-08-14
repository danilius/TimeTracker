using System;
using System.Globalization;
using System.Windows;
using TimeTracker.Models;
using TimeTracker.Utils;

namespace TimeTracker.Dialogs
{
  /// <summary>
  /// Interaction logic for IdleTimeDialog.xaml
  /// </summary>
  public partial class IdleTimeDialog : Window
  {
    /// <summary>
    /// Asks whether the idle stretch that <paramref name="workEntry" /> has been running through
    /// should be billed.
    /// </summary>
    /// <param name="idleDuration">How long the machine has been quiet.</param>
    /// <param name="cutoff">The moment input stopped, which is where discarding starts from.</param>
    public IdleTimeDialog(WorkEntry workEntry, TimeSpan idleDuration, DateTime cutoff)
    {
      InitializeComponent();

      string timePattern = TTAppSettings.Instance.ShortTimePattern;

      MessageTextBlock.Text = string.Format(
        CultureInfo.CurrentCulture,
        "This job has been running, but the machine has been idle since {0}.",
        cutoff.ToString(timePattern, CultureInfo.CurrentCulture));

      JobTextBlock.Text = string.IsNullOrWhiteSpace(workEntry.ClientName)
        ? workEntry.ProjectName
        : $"{workEntry.ClientName} — {workEntry.ProjectName}";

      DetailTextBlock.Text = string.Format(
        CultureInfo.CurrentCulture,
        "That is {0} of idle time. Discarding it takes it off the job's billable duration; the job carries on running either way.",
        FormatElapsed(idleDuration));
    }

    /// <summary>
    /// Closing the window with the title bar keeps the time, which is the answer that cannot lose
    /// anything.
    /// </summary>
    public bool DiscardRequested { get; private set; }

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

    private void KeepButton_Click(object sender, RoutedEventArgs e)
    {
      DiscardRequested = false;
      DialogResult = true;
    }

    private void DiscardButton_Click(object sender, RoutedEventArgs e)
    {
      DiscardRequested = true;
      DialogResult = true;
    }
  }
}
