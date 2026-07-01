using TimeTracker.Utils;

namespace TimeTracker.Models
{
  public class TimeTrackerData
  {
    public TrulyObservableCollection<Client> Clients { get; set; } = new();
    public TrulyObservableCollection<Report> Reports { get; set; } = new();
    public TrulyObservableCollection<Invoice> Invoices { get; set; } = new();
  }
}
