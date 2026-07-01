using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TimeTracker.ViewModels
{
  internal class NewWorkEntryModel :INotifyPropertyChanged
  {
    private string _clientName = "";
    public string ClientName
    {
      get { return _clientName; }
      set
      {
        if (_clientName != value)
        {
          _clientName = value;
          OnPropertyChanged();
        }
      }
    }

    private string _projectName = "";
    public string ProjectName
    {
      get { return _projectName; }
      set
      {
        if (_projectName != value)
        {
          _projectName = value;
          OnPropertyChanged();
        }
      }
    }

    private DateTime _startTime = DateTime.Now;
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

    private DateTime _endTime = DateTime.Now;
    public DateTime EndTime
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

    private string _description = "";
    public string Description
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

    public event PropertyChangedEventHandler? PropertyChanged;
    public virtual void OnPropertyChanged([CallerMemberName] string? name = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
  }
}
