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
      //_dateCreated = DateTime.Now;
      //_dateModified = DateTime.Now;
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

    private DateTime? _dateArchived;
    public DateTime? DateArchived
    {
      get => _dateArchived;
      set
      {
        if (_dateArchived != value)
        {
          _dateArchived = value;
          OnPropertyChanged();
        }
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
