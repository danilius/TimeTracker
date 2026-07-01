using System;
using TimeTracker.Models;
using TimeTracker.Utils;

namespace TimeTracker.ViewModels
{
  public class WorkEntriesViewModel : TTDataObject
  {
    public WorkEntriesViewModel()
    {
      TimeTrackerModel timeTracker = TimeTrackerModel.Instance;

      timeTracker.WorkEntryAdded += WorkEntryAdded;
      timeTracker.WorkEntryRemoved += WorkEntryRemoved;
      
      LoadWorkEntries();
    }

    private void WorkEntryRemoved(object? sender, WorkEntryEventArgs e)
    {
      if (e.WorkEntry == null) throw new ArgumentNullException(nameof(e.WorkEntry));

      _workEntries?.Remove(e.WorkEntry);
    }

    private void LoadWorkEntries()
    {
      foreach (WorkEntry workEntry in TimeTrackerModel.Instance.WorkEntries)
      {
        _workEntries.Add(workEntry);
      }
    }

    private void WorkEntryAdded(object? sender, WorkEntryEventArgs e)
    {
      if (e.WorkEntry == null) throw new ArgumentNullException(nameof(e.WorkEntry));

      _workEntries?.Add(e.WorkEntry);
    }

    private TrulyObservableCollection<WorkEntry> _workEntries = new();
    public TrulyObservableCollection<WorkEntry> WorkEntries
    {
      get => _workEntries;
    }

    public void Delete(WorkEntry workEntry)
    {
      TimeTrackerModel.Instance.DeleteWorkEntry(workEntry);
    }
  }
}
