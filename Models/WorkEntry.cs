using System;
using FastEnumUtility;

namespace TimeTracker.Models
{
  public enum WorkEntryTypes
  {
    [Label("Work")]
    Work,
    [Label("Purchase")]
    Purchase
  }

  public class WorkEntry : TTDataObject
  {
    private DateTime? _startTime;
    public DateTime? StartTime
    {
      get { return _startTime; }
      set
      {
        if (_startTime != value)
        {
          _startTime = value;
          OnPropertyChanged();
        }
      }
    }

    private DateTime? _endTime;
    public DateTime? EndTime
    {
      get { return _endTime; }
      set
      {
        if (_endTime != value)
        {
          _endTime = value;
          OnPropertyChanged();
        }
      }
    }

    private string? _description;
    public string? Description
    {
      get { return _description; }
      set
      {
        if (_description != value)
        {
          _description = value;
          OnPropertyChanged();
        }
      }
    }

    private WorkEntryTypes _workEntryType;
    public WorkEntryTypes WorkEntryType
    {
      get { return _workEntryType; }
      set
      {
        if (_workEntryType != value)
        {
          _workEntryType = value;
          OnPropertyChanged();
        }
      }
    }

    public TimeSpan Duration
    {
      get
      {
        if (StartTime == null || EndTime == null)
        {
          return TimeSpan.Zero;
        }
        else
        {
          return EndTime.Value - StartTime.Value;
        }
      }
    }
  }
}
