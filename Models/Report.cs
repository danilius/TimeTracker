using System;

namespace TimeTracker.Models
{
  public class Report : TTDataObject
  {
    private Guid _id;
    public Guid ID
    {
      get { return _id; }
      set
      {
        if (_id != value)
        {
          _id = value;
          OnPropertyChanged();
        }
      }
    }

    private string _name = string.Empty;
    public string Name
    {
      get { return _name; }
      set
      {
        if (_name != value)
        {
          _name = value;
          OnPropertyChanged();
        }
      }
    }

    private DateTime _startDate = DateTime.Today;
    public DateTime StartDate
    {
      get { return _startDate; }
      set
      {
        if (_startDate != value)
        {
          _startDate = value;
          OnPropertyChanged();
        }
      }
    }

    private DateTime _endDate = DateTime.Today;
    public DateTime EndDate
    {
      get { return _endDate; }
      set
      {
        if (_endDate != value)
        {
          _endDate = value;
          OnPropertyChanged();
        }
      }
    }

    private Guid? _clientID;
    public Guid? ClientID
    {
      get { return _clientID; }
      set
      {
        if (_clientID != value)
        {
          _clientID = value;
          OnPropertyChanged();
        }
      }
    }

    private Guid? _projectID;
    public Guid? ProjectID
    {
      get { return _projectID; }
      set
      {
        if (_projectID != value)
        {
          _projectID = value;
          OnPropertyChanged();
        }
      }
    }

    private bool _includeArchivedJobs;
    public bool IncludeArchivedJobs
    {
      get { return _includeArchivedJobs; }
      set
      {
        if (_includeArchivedJobs != value)
        {
          _includeArchivedJobs = value;
          OnPropertyChanged();
        }
      }
    }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime ModifiedAt { get; set; } = DateTime.Now;
  }
}
