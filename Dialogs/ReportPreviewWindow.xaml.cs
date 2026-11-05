using System;
using System.Windows;
using System.Windows.Documents;

namespace TimeTracker.Dialogs
{
  /// <summary>
  /// Interaction logic for ReportPreviewWindow.xaml — a read-only look at a saved report, opened by
  /// double clicking its row. It owns no data: the caller supplies the rendered document and the
  /// print and save actions, which are the same ones the row's buttons run.
  /// </summary>
  public partial class ReportPreviewWindow : Window
  {
    private readonly Action printReport;
    private readonly Action saveReport;

    /// <param name="document">
    /// The rendered report. The window shows it as it stands; printing builds a fresh document,
    /// because printing resizes one to the printable area.
    /// </param>
    public ReportPreviewWindow(string reportName, string range, FlowDocument document, Action printReport, Action saveReport)
    {
      InitializeComponent();

      this.printReport = printReport;
      this.saveReport = saveReport;

      Title = $"Report - {reportName}";
      TitleTextBlock.Text = reportName;
      SubtitleTextBlock.Text = range;
      DocumentViewer.Document = document;
    }

    private void PrintButton_Click(object sender, RoutedEventArgs e)
    {
      printReport();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
      saveReport();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
      Close();
    }
  }
}
