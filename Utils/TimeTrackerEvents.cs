using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeTracker.Models;

namespace TimeTracker.Utils
{
  public class ClientEventArgs : EventArgs
  {
    public ClientEventArgs(Client client)
    {
      Client = client;
    }

    public Client? Client { get; set; }
  }

  public class ProjectEventArgs : EventArgs
  {
    public ProjectEventArgs(Project project)
    {
      Project = project;
    }

    public Project? Project { get; set; }
  }

  public class WorkEntryEventArgs : EventArgs
  {
    public WorkEntryEventArgs(WorkEntry workEntry)
    {
      WorkEntry = workEntry;
    }
    public WorkEntry? WorkEntry { get; set; }
  }

}
