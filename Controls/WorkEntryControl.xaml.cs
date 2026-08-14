using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TimeTracker.Dialogs;
using TimeTracker.Models;
using TimeTracker.Utils;

namespace TimeTracker.Controls
{
  /// <summary>
  /// Interaction logic for WorkEntryControl.xaml
  /// </summary>
  public partial class WorkEntryControl : UserControl
  {
    public WorkEntryControl()
    {
      InitializeComponent();
    }

    // event handler for the delete button
    //public event EventHandler<WorkEntryEventArgs>? Delete;
    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
      var workEntry = (WorkEntry)DataContext;
      TimeTrackerModel.Instance.DeleteWorkEntry(workEntry);
    }

    private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
      projectNameLabel.Foreground = new SolidColorBrush(((WorkEntry)DataContext).Project!.ProjectColour);
    }

    private void UserControl_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
      e.Handled = false;
    }

    private void RunButton_Click(object sender, RoutedEventArgs e)
    {
      var workEntry = (WorkEntry)DataContext;
      TimeTrackerModel.Instance.StartNewWorkEntryBasedOn(workEntry);
    }

    private void UserControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (DataContext is not WorkEntry workEntry)
      {
        return;
      }

      NewWorkEntryDialog dialog = new(workEntry)
      {
        Owner = Window.GetWindow(this),
        WindowStartupLocation = WindowStartupLocation.CenterOwner
      };

      if (dialog.ShowDialog() == true)
      {
        TimeTrackerModel.Instance.UpdateWorkEntry(
          workEntry,
          dialog.ClientName,
          dialog.ProjectName,
          dialog.StartTime,
          dialog.Description,
          dialog.HourlyRate,
          dialog.Currency,
          dialog.Duration);
      }
    }
  }
}
