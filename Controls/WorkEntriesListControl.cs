using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using TimeTracker.Models;
using TimeTracker.Utils;
using TimeTracker.ViewModels;

namespace TimeTracker.Controls
{

  public class WorkEntriesListControl : Control
  {
    private WorkEntriesViewModel _workEntriesViewModel = new WorkEntriesViewModel();
    private ItemsControl? _listPanel;
    private CollectionViewSource _workEntriesCVS = new();

    static WorkEntriesListControl()
    {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(WorkEntriesListControl), new FrameworkPropertyMetadata(typeof(WorkEntriesListControl)));
    }


    public WorkEntriesListControl()
    {
      Loaded += WorkEntriesListControl_Loaded;

      _workEntriesCVS.Source = _workEntriesViewModel.WorkEntries;
      _workEntriesCVS.SortDescriptions.Add(new System.ComponentModel.SortDescription("StartTime", System.ComponentModel.ListSortDirection.Descending));
    }

    private void WorkEntriesListControl_Loaded(object sender, RoutedEventArgs e)
    {
      _listPanel = (ItemsControl)GetTemplateChild("WorkStack");
      _listPanel.ItemsSource = _workEntriesCVS.View;
    }
    
  }
}
