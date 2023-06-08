

using System;
using TimeTracker.Models;

namespace Time_Tracker.Models
{
  public class Client : TTDataObject
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

    private string? _address;
    public string? Address
    {
      get { return _address; }
      set
      {
        if (_address != value)
        {
          _address = value;
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
  }
}
