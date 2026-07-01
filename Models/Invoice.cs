using System;
using System.Collections.Generic;

namespace TimeTracker.Models
{
  public class Invoice : TTDataObject
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

    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid ClientID { get; set; }
    public DateTime IssueDate { get; set; } = DateTime.Today;
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime EndDate { get; set; } = DateTime.Today;
    public List<Guid> WorkEntryIDs { get; set; } = new();
    public double HoursTotal { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
  }
}
