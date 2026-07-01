using System;
using System.Text.Json.Serialization;

namespace TimeTracker.Models
{
  
  public class WorkEntry : TTDataObject
  {
  
    public WorkEntry()
    {
    }

    private Project? _project;
    [JsonIgnore]
    public Project? Project
    {
      get { return _project; }
      set
      {
        if (_project != value)
        {
          _project = value;
          OnPropertyChanged();
        }
      }
    }

    private DateTime _startTime;
    public DateTime StartTime
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

    private DateTime _endTime;
    public DateTime EndTime
    {
      get { return _endTime; }
      set
      {
        if (_endTime != value)
        {
          _endTime = value;
          OnPropertyChanged();
          OnPropertyChanged(nameof(Duration));
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

    private bool _isRunning;
    [JsonIgnore]
    public bool IsRunning
    {
      get { return _isRunning; }
      set
      {
        if (_isRunning != value)
        {
          _isRunning = value;
          OnPropertyChanged();
        }
      }
    }

    [JsonIgnore]
    public string ClientName => Project?.Client?.Name ?? string.Empty;

    [JsonIgnore]
    public string ProjectName => Project?.Name ?? string.Empty;

    private bool _isBillable = true;
    public bool IsBillable
    {
      get { return _isBillable; }
      set
      {
        if (_isBillable != value)
        {
          _isBillable = value;
          OnPropertyChanged();
        }
      }
    }

    private decimal _hourlyRate;
    public decimal HourlyRate
    {
      get { return _hourlyRate; }
      set
      {
        if (_hourlyRate != value)
        {
          _hourlyRate = value;
          OnPropertyChanged();
        }
      }
    }

    private string _currency = string.Empty;
    public string Currency
    {
      get { return _currency; }
      set
      {
        if (_currency != value)
        {
          _currency = value;
          OnPropertyChanged();
        }
      }
    }

    public TimeSpan Duration
    {
      get
      {
        if (IsRunning)
        {
          return DateTime.Now - StartTime;
        }

        if (EndTime == default)
        {
          return TimeSpan.Zero;
        }

        return EndTime - StartTime;
      }
    }

  }
}
