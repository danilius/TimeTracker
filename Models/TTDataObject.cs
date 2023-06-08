using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace TimeTracker.Models
{
  public class TTDataObject : INotifyPropertyChanged
  {
    public TTDataObject()
    {
      _dateCreated = DateTime.Now;
      _dateModified = DateTime.Now;
    }

    private bool _dataIsDirty = false;
    [JsonIgnore]
    public virtual bool DataIsDirty
    {
      get => _dataIsDirty;
      set
      {
        if (_dataIsDirty != value)
        {
          _dataIsDirty = value;

          OnPropertyChanged();
        }
      }
    }

    private DateTime _dateCreated;
    public DateTime DateCreated
    {
      get { return _dateCreated; }
      set
      {
        if (_dateCreated != value)
        {
          _dateCreated = value;
          OnPropertyChanged();
        }
      }
    }

    private DateTime _dateModified;
    public DateTime DateModified
    {
      get { return _dateModified; }
      set
      {
        if (_dateModified != value)
        {
          _dateModified = value;
          OnPropertyChanged();
        }
      }
    }

    private bool _isSelected = false;
    [JsonIgnore]
    public bool IsSelected
    {
      get => _isSelected;
      set
      {
        if (_isSelected != value)
        {
          _isSelected = value;
          OnPropertyChanged();
        }
      }
    }

    [JsonIgnore]
    private TTDataObject? _parent;
    public TTDataObject? Parent
    {
      get => _parent;
      set => _parent = value;
    }

    private bool _isArchived = false;
    public virtual bool IsArchived
    {
      get => _isArchived;
      set
      {
        _isArchived = value;
        OnPropertyChanged();
      }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public virtual void OnPropertyChanged([CallerMemberName] string? name = null)
    {
      // change DataIsDirty if some other property has been altered
      // ignore IsEditing, which should not affect DataIsDirty
      if (name != nameof(DataIsDirty))
      {
        bool wasDirty = false;

        if (_dataIsDirty == true)
        {
          wasDirty = true;
        }

        _dataIsDirty = true;

        // only trigger event if data was not previously dirty
        if (!wasDirty)
        {
          PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DataIsDirty)));
        }
      }

      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
  }
}
