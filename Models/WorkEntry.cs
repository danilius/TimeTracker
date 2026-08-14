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

    /// <summary>
    /// Time this job spent paused. Kept separate from the start and end times so those stay a
    /// plain record of when the work began and finished, which is what invoices and sorting use.
    /// </summary>
    private TimeSpan _pausedDuration;
    public TimeSpan PausedDuration
    {
      get { return _pausedDuration; }
      set
      {
        if (_pausedDuration != value)
        {
          _pausedDuration = value;
          OnPropertyChanged();
          OnPropertyChanged(nameof(Duration));
        }
      }
    }

    /// <summary>
    /// When the current pause began, or null when the job is not paused. Persisted so a pause
    /// survives closing the application.
    /// </summary>
    private DateTime? _pausedAt;
    public DateTime? PausedAt
    {
      get { return _pausedAt; }
      set
      {
        if (_pausedAt != value)
        {
          _pausedAt = value;
          OnPropertyChanged();
          OnPropertyChanged(nameof(IsPaused));
          OnPropertyChanged(nameof(Duration));
        }
      }
    }

    [JsonIgnore]
    public bool IsPaused => _pausedAt.HasValue;

    /// <summary>
    /// Billable time: the elapsed span less anything spent paused, including a pause still open.
    /// </summary>
    public TimeSpan Duration
    {
      get
      {
        TimeSpan elapsed;

        if (IsRunning)
        {
          elapsed = DateTime.Now - StartTime;
        }
        else if (EndTime == default)
        {
          return TimeSpan.Zero;
        }
        else
        {
          elapsed = EndTime - StartTime;
        }

        TimeSpan paused = _pausedDuration;

        if (_pausedAt.HasValue)
        {
          paused += DateTime.Now - _pausedAt.Value;
        }

        TimeSpan worked = elapsed - paused;

        return worked < TimeSpan.Zero ? TimeSpan.Zero : worked;
      }
    }

  }
}
