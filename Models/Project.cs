using System;
using TimeTracker.Models;
using TimeTracker.Utils;

namespace TimeTracker.Models
{
  public class Project : TTDataObject
  {
    private string? _name;
    public string? Name
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

    private Client? _client;
    public Client? Client
    {
      get { return _client; }
      set
      {
        if (_client != value)
        {
          _client = value;
          OnPropertyChanged();
        }
      }
    }

    private Guid _id;
    public Guid Id
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

    private double _rate;
    public double Rate
    {
      get { return _rate; }
      set
      {
        if (_rate != value)
        {
          _rate = value;
          OnPropertyChanged();
        }
      }
    }

    private TrulyObservableCollection<WorkEntry> _workEntries = new();
    public TrulyObservableCollection<WorkEntry> WorkEntries
    {
      get { return _workEntries; }
      set
      {
        if (_workEntries != value)
        {
          _workEntries = value;
          OnPropertyChanged();
        }
      }
    }
  }
}
