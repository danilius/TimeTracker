using System;
using TimeTracker.Models;
using TimeTracker.Utils;

namespace TimeTracker.Models
{
  public class Client : TTDataObject
  {
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

    private string _address = string.Empty;
    public string Address
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

    private string _companyName = string.Empty;
    public string CompanyName
    {
      get { return _companyName; }
      set
      {
        if (_companyName != value)
        {
          _companyName = value;
          OnPropertyChanged();
        }
      }
    }

    private string _email = string.Empty;
    public string Email
    {
      get { return _email; }
      set
      {
        if (_email != value)
        {
          _email = value;
          OnPropertyChanged();
        }
      }
    }

    private string _phone = string.Empty;
    public string Phone
    {
      get { return _phone; }
      set
      {
        if (_phone != value)
        {
          _phone = value;
          OnPropertyChanged();
        }
      }
    }

    private string _invoiceNumberPrefix = string.Empty;
    public string InvoiceNumberPrefix
    {
      get { return _invoiceNumberPrefix; }
      set
      {
        if (_invoiceNumberPrefix != value)
        {
          _invoiceNumberPrefix = value;
          OnPropertyChanged();
        }
      }
    }

    private int _currentInvoiceNumber = 1;
    public int CurrentInvoiceNumber
    {
      get { return _currentInvoiceNumber; }
      set
      {
        if (_currentInvoiceNumber != value)
        {
          _currentInvoiceNumber = value;
          OnPropertyChanged();
        }
      }
    }

    private decimal _defaultHourlyRate;
    public decimal DefaultHourlyRate
    {
      get { return _defaultHourlyRate; }
      set
      {
        if (_defaultHourlyRate != value)
        {
          _defaultHourlyRate = value;
          OnPropertyChanged();
        }
      }
    }

    private string _defaultCurrency = string.Empty;
    public string DefaultCurrency
    {
      get { return _defaultCurrency; }
      set
      {
        if (_defaultCurrency != value)
        {
          _defaultCurrency = value;
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

    private TrulyObservableCollection<Project> _projects = new();
    public TrulyObservableCollection<Project> Projects
    {
      get { return _projects; }
      set
      {
        if (_projects != value)
        {
          _projects = value;
          OnPropertyChanged();
        }
      }
    }
  }
}
