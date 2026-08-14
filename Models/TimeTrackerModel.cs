using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using TimeTracker.Services;
using TimeTracker.Utils;

namespace TimeTracker.Models
{


  public class TimeTrackerModel
  {
    private DispatcherTimer _workTimer = new() { Interval = TimeSpan.FromSeconds(5) };
    private static TimeTrackerModel? _instance = null;
    private static readonly Random _random = new();
    private static readonly object _objlocker = new object();
    private readonly LiteDbTimeTrackerStore _store = new();
    private TimeTrackerData _data = new();
    private WorkEntry? _currentWorkEntry = null;
    private DateTime? _lastLongRunningWarningTime;
    private bool _isShowingLongRunningPrompt;
    private DateTime _lastRunningWorkSaveTime = DateTime.MinValue;

    /// <summary>
    /// How often a running job is written to disk by the timer. Starting, stopping and editing
    /// all save immediately, so this only bounds what an unclean shutdown can lose.
    /// </summary>
    private static readonly TimeSpan RunningWorkSaveInterval = TimeSpan.FromSeconds(60);

    private TimeTrackerModel()
    {
      _workTimer = new() { Interval = TimeSpan.FromSeconds(TTAppSettings.Instance.TimerInterval) };
      _workTimer.Tick += WorkTimer_Tick;

      LiteDbTimeTrackerStore.CreateDailyBackup();
      LoadData();

      //_projects.CollectionChanged += Projects_CollectionChanged;
      //_workEntries.CollectionChanged += WorkEntries_CollectionChanged;
    }

    private void WorkTimer_Tick(object? sender, EventArgs e)
    {
      // set the current duration of the current work entry
      if (_currentWorkEntry == null)
      {
        return;
      }

      _currentWorkEntry.EndTime = DateTime.Now;
      CheckLongRunningJobPolicy();

      if (_currentWorkEntry == null)
      {
        // The policy stopped the job, which saved on its way out.
        return;
      }

      // The tick exists to drive the on-screen clock. Saving here rewrites the entire dataset
      // and reopens the database, so it runs on its own slower schedule: a crash costs at most
      // RunningWorkSaveInterval of the running job, rather than the file being rewritten every
      // second the timer is going.
      if (DateTime.Now - _lastRunningWorkSaveTime >= RunningWorkSaveInterval)
      {
        _lastRunningWorkSaveTime = DateTime.Now;
        SaveData();
      }
    }

    private void SaveData()
    {
      _data.Clients = _clients;
      _data.Reports = _reports;
      _data.Invoices = _invoices;
      _store.SaveData(_data);
    }

    public void SaveChanges()
    {
      SaveData();
    }

    private void LoadData()
    {
      _data = _store.LoadData();
      _clients = _data.Clients;
      _reports = _data.Reports;
      _invoices = _data.Invoices;

      // Reconnect parent references that are intentionally omitted from JSON.
      foreach (Client client in _clients)
      {
        if (client.CurrentInvoiceNumber <= 0)
        {
          client.CurrentInvoiceNumber = 1;
        }

        if (string.IsNullOrWhiteSpace(client.DefaultCurrency))
        {
          client.DefaultCurrency = TTAppSettings.Instance.DefaultCurrency;
        }

        foreach (Project project in client.Projects)
        {
          project.Client = client;
          if (project.Rate <= 0)
          {
            project.UseClientDefaultRate = true;
          }
        }
      }

      foreach (Client client in _clients)
      {
        foreach (Project project in client.Projects)
        {
          foreach (WorkEntry workEntry in project.WorkEntries)
          {
            workEntry.Project = project;
            if (workEntry.ID == Guid.Empty)
            {
              workEntry.ID = Guid.NewGuid();
            }
          }
        }
      }

      foreach (Report report in _reports)
      {
        if (report.ID == Guid.Empty)
        {
          report.ID = Guid.NewGuid();
        }
      }

      foreach (Invoice invoice in _invoices)
      {
        if (invoice.ID == Guid.Empty)
        {
          invoice.ID = Guid.NewGuid();
        }
      }

      SaveData();
    }

    //private void Projects_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    //{
    //  switch (e.Action)
    //  {
    //    // if a project is added, then add the prject's WorkEntries to the WorkEntries collection
    //    case NotifyCollectionChangedAction.Add:
    //      {
    //        // check if e.NewItems is null
    //        if (e.NewItems == null)
    //        {
    //          return;
    //        }

    //        foreach (Project project in e.NewItems)
    //        {
    //          foreach (WorkEntry workEntry in project.WorkEntries)
    //          {
    //            WorkEntries.Add(workEntry);
    //          }
    //        }

    //        break;
    //      }

    //    // if a project is removed, then remove the project's WorkEntries from the WorkEntries collection
    //    case NotifyCollectionChangedAction.Remove:
    //      {

    //        // check if e.OldItems is null
    //        if (e.OldItems == null)
    //        {
    //          return;
    //        }

    //        foreach (Project project in e.OldItems)
    //        {
    //          foreach (WorkEntry workEntry in project.WorkEntries)
    //          {
    //            WorkEntries.Remove(workEntry);
    //          }
    //        }

    //        break;
    //      }
    //  }
    //}

    public static TimeTrackerModel Instance
    {
      get
      {
        if (_instance == null)
        {
          lock (_objlocker)
          {
            if (_instance == null)
            {
              _instance = new TimeTrackerModel();
            }
          }
        }

        return _instance;
      }
    }


    private TrulyObservableCollection<Client> _clients = new();
    public TrulyObservableCollection<Client> Clients
    {
      get { return _clients; }
    }

    private TrulyObservableCollection<Report> _reports = new();
    public TrulyObservableCollection<Report> Reports
    {
      get { return _reports; }
    }

    private TrulyObservableCollection<Invoice> _invoices = new();
    public TrulyObservableCollection<Invoice> Invoices
    {
      get { return _invoices; }
    }

    public TrulyObservableCollection<Client> ActiveClients
    {
      get
      {
        TrulyObservableCollection<Client> result = new();
        foreach (Client client in _clients.Where(client => !client.IsArchived))
        {
          result.Add(client);
        }

        return result;
      }
    }

    public TrulyObservableCollection<Project> Projects
    {
      get
      {
        TrulyObservableCollection<Project> result = new();
        foreach (Client client in _clients)
        {
          foreach (Project project in client.Projects)
          {
            if (!project.IsArchived)
            {
              result.Add(project);
            }
          }
        }

        return result;
      }
    }

    public TrulyObservableCollection<WorkEntry> WorkEntries
    {
      get
      {
        TrulyObservableCollection<WorkEntry> result = new();

        foreach (Client client in _clients)
        {
          foreach (Project project in client.Projects)
          {
            foreach (WorkEntry workEntry in project.WorkEntries)
            {
              if (!workEntry.IsArchived)
              {
                result.Add(workEntry);
              }
            }
          }
        }

        return result;
      }
    }

    public TrulyObservableCollection<TTDataObject> ArchivedItems
    {
      get
      {
        TrulyObservableCollection<TTDataObject> result = new();

        foreach (Client client in _clients)
        {
          if (client.IsArchived)
          {
            result.Add(client);
          }

          foreach (Project project in client.Projects)
          {
            if (project.IsArchived)
            {
              result.Add(project);
            }

            foreach (WorkEntry workEntry in project.WorkEntries)
            {
              if (workEntry.IsArchived)
              {
                result.Add(workEntry);
              }
            }
          }
        }

        return result;
      }
    }

    private IEnumerable<WorkEntry> AllWorkEntries
    {
      get
      {
        foreach (Client client in _clients)
        {
          foreach (Project project in client.Projects)
          {
            foreach (WorkEntry workEntry in project.WorkEntries)
            {
              yield return workEntry;
            }
          }
        }
      }
    }

    public WorkEntry? CurrentWorkEntry => _currentWorkEntry;

    public bool HasRunningWork => _currentWorkEntry != null;

    public event EventHandler? RunningWorkChanged;

    /// <summary>
    /// Raised when a running job passes the long running threshold. The UI handles this by
    /// prompting; with no handler the configured policy applies on its own.
    /// </summary>
    public event EventHandler<LongRunningJobEventArgs>? LongRunningJobDetected;

    public void StartWork(WorkEntry workEntry)
    {
      if (_currentWorkEntry != null)
      {
        StopWork();
      }

      _currentWorkEntry = workEntry;
      _currentWorkEntry.IsRunning = true;
      _lastLongRunningWarningTime = null;
      _lastRunningWorkSaveTime = DateTime.Now;

      _workTimer.Start();

      SaveData();
      RunningWorkChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool HasPausedWork => _currentWorkEntry?.IsPaused == true;

    /// <summary>
    /// Suspends the running job. The clock keeps moving, but the paused span is subtracted from
    /// the entry's billable duration.
    /// </summary>
    public void PauseWork()
    {
      if (_currentWorkEntry == null || _currentWorkEntry.IsPaused)
      {
        return;
      }

      _currentWorkEntry.PausedAt = DateTime.Now;

      SaveData();
      RunningWorkChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ResumeWork()
    {
      if (_currentWorkEntry == null || !_currentWorkEntry.IsPaused)
      {
        return;
      }

      ClosePause(_currentWorkEntry, DateTime.Now);

      SaveData();
      RunningWorkChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Folds an open pause into the total. Used when resuming and when stopping a paused job, so
    /// a job can never be stopped with a pause left hanging.
    /// </summary>
    private static void ClosePause(WorkEntry workEntry, DateTime until)
    {
      if (!workEntry.PausedAt.HasValue)
      {
        return;
      }

      TimeSpan pause = until - workEntry.PausedAt.Value;

      if (pause > TimeSpan.Zero)
      {
        workEntry.PausedDuration += pause;
      }

      workEntry.PausedAt = null;
    }

    public void StopWork()
    {
      StopWork(DateTime.Now);
    }

    /// <summary>
    /// Stops the running job and records it as having ended at <paramref name="endTime" />, which
    /// lets the long running prompt backdate a stop to the moment the threshold was reached.
    /// </summary>
    public void StopWork(DateTime endTime)
    {
      if (_currentWorkEntry != null)
      {
        DateTime end = endTime < _currentWorkEntry.StartTime
          ? _currentWorkEntry.StartTime
          : endTime;

        ClosePause(_currentWorkEntry, end);
        _currentWorkEntry.EndTime = end;
        _currentWorkEntry.IsRunning = false;
      }

      _workTimer.Stop();
      _currentWorkEntry = null;
      _lastLongRunningWarningTime = null;
      _lastRunningWorkSaveTime = DateTime.MinValue;

      SaveData();
      RunningWorkChanged?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler<WorkEntryEventArgs>? WorkEntryAdded;

    public WorkEntry CreateWorkEntry(string clientName, string projectName, DateTime startTime, string? description, decimal? hourlyRate = null, string? currency = null, TimeSpan? duration = null)
    {
      Client client = FindOrCreateClient(clientName);
      Project project = FindOrCreateProject(client, projectName, description);

      return CreateWorkEntry(project, startTime, description, hourlyRate, currency, duration);
    }

    public WorkEntry CreateWorkEntry(Project project, DateTime startTime, string? description, decimal? hourlyRate = null, string? currency = null, TimeSpan? duration = null)
    {
      TTAppSettings settings = TTAppSettings.Instance;
      Client? client = project.Client;
      decimal defaultHourlyRate = GetProjectHourlyRate(project);
      WorkEntry workEntry = new()
      {
        ID = Guid.NewGuid(),
        Project = project,
        StartTime = startTime,
        EndTime = startTime + (duration ?? TimeSpan.Zero),
        Description = description,
        HourlyRate = hourlyRate ?? defaultHourlyRate,
        Currency = string.IsNullOrWhiteSpace(currency)
          ? (string.IsNullOrWhiteSpace(client?.DefaultCurrency) ? settings.DefaultCurrency : client.DefaultCurrency)
          : TTAppSettings.NormalizeCurrency(currency)
      };

      project.WorkEntries.Add(workEntry);

      WorkEntryAdded?.Invoke(this, new WorkEntryEventArgs(workEntry));

      SaveData();

      return workEntry;
    }

    public IEnumerable<WorkEntry> GetReportJobs(Report report)
    {
      return AllWorkEntries
        .Where(workEntry => report.IncludeArchivedJobs || !workEntry.IsArchived)
        .Where(workEntry => workEntry.StartTime.Date >= report.StartDate.Date && workEntry.StartTime.Date <= report.EndDate.Date)
        .Where(workEntry => report.ClientID == null || workEntry.Project?.Client?.ID == report.ClientID)
        .Where(workEntry => report.ProjectID == null || workEntry.Project?.ID == report.ProjectID)
        .OrderBy(workEntry => workEntry.StartTime);
    }

    public Report CreateReport(string name, DateTime startDate, DateTime endDate, Client? client, Project? project, bool includeArchivedJobs)
    {
      Report report = new()
      {
        ID = Guid.NewGuid(),
        Name = name,
        StartDate = startDate.Date,
        EndDate = endDate.Date,
        ClientID = client?.ID,
        ProjectID = project?.ID,
        IncludeArchivedJobs = includeArchivedJobs,
        CreatedAt = DateTime.Now,
        ModifiedAt = DateTime.Now
      };

      _reports.Add(report);
      SaveData();

      return report;
    }

    public void UpdateReport(Report report, string name, DateTime startDate, DateTime endDate, Client? client, Project? project, bool includeArchivedJobs)
    {
      report.Name = name;
      report.StartDate = startDate.Date;
      report.EndDate = endDate.Date;
      report.ClientID = client?.ID;
      report.ProjectID = project?.ID;
      report.IncludeArchivedJobs = includeArchivedJobs;
      report.ModifiedAt = DateTime.Now;
      SaveData();
    }

    public void DeleteReport(Report report)
    {
      _reports.Remove(report);
      SaveData();
    }

    public DateTime GetDefaultInvoiceStartDate(Client client)
    {
      Invoice? lastInvoice = _invoices
        .Where(invoice => invoice.ClientID == client.ID)
        .OrderByDescending(invoice => invoice.EndDate)
        .ThenByDescending(invoice => invoice.IssueDate)
        .FirstOrDefault();

      return lastInvoice == null ? DateTime.Today : lastInvoice.EndDate.Date.AddDays(1);
    }

    public IEnumerable<WorkEntry> GetInvoiceCandidateJobs(Client client, DateTime startDate, DateTime endDate)
    {
      return WorkEntries
        .Where(workEntry => !workEntry.IsArchived)
        .Where(workEntry => workEntry.Project?.Client?.ID == client.ID)
        .Where(workEntry => workEntry.StartTime.Date >= startDate.Date && workEntry.StartTime.Date <= endDate.Date)
        .OrderBy(workEntry => workEntry.StartTime);
    }

    public Invoice CreateInvoice(Client client, DateTime startDate, DateTime endDate, IEnumerable<WorkEntry> workEntries)
    {
      List<WorkEntry> selectedJobs = workEntries.ToList();
      Invoice invoice = new()
      {
        ID = Guid.NewGuid(),
        InvoiceNumber = CreateInvoiceNumber(client),
        ClientID = client.ID,
        IssueDate = DateTime.Today,
        StartDate = startDate.Date,
        EndDate = endDate.Date,
        WorkEntryIDs = selectedJobs.Select(workEntry => workEntry.ID).ToList(),
        HoursTotal = CalculateHoursTotal(selectedJobs),
        Total = CalculateTotal(selectedJobs),
        Currency = GetCurrencySummary(selectedJobs),
        CreatedAt = DateTime.Now
      };

      _invoices.Add(invoice);
      client.CurrentInvoiceNumber += 1;
      SaveData();

      return invoice;
    }

    public void DeleteInvoice(Invoice invoice)
    {
      _invoices.Remove(invoice);
      SaveData();
    }

    public IEnumerable<WorkEntry> GetInvoiceJobs(Invoice invoice)
    {
      HashSet<Guid> jobIDs = invoice.WorkEntryIDs.ToHashSet();
      return WorkEntries
        .Where(workEntry => jobIDs.Contains(workEntry.ID))
        .OrderBy(workEntry => workEntry.StartTime);
    }

    public Client? FindClient(Guid clientID)
    {
      return _clients.FirstOrDefault(client => client.ID == clientID);
    }

    public Project? FindProject(Guid projectID)
    {
      return Projects.FirstOrDefault(project => project.ID == projectID);
    }

    public static decimal CalculateTotal(IEnumerable<WorkEntry> workEntries)
    {
      return workEntries.Sum(workEntry => Convert.ToDecimal(workEntry.Duration.TotalHours) * workEntry.HourlyRate);
    }

    public static double CalculateHoursTotal(IEnumerable<WorkEntry> workEntries)
    {
      return workEntries.Sum(workEntry => workEntry.Duration.TotalHours);
    }

    public static string GetCurrencySummary(IEnumerable<WorkEntry> workEntries)
    {
      List<string> currencies = workEntries
        .Select(workEntry => workEntry.Currency)
        .Where(currency => !string.IsNullOrWhiteSpace(currency))
        .Distinct()
        .ToList();

      return currencies.Count == 1 ? currencies[0] : string.Join(", ", currencies);
    }

    public static string CreateInvoiceNumber(Client client)
    {
      return FormatInvoiceNumber(client.InvoiceNumberPrefix, client.CurrentInvoiceNumber);
    }

    /// <summary>
    /// A client's invoice number: their prefix, a slash, then the running number. A missing prefix
    /// leaves the bare number rather than a leading slash.
    /// </summary>
    public static string FormatInvoiceNumber(string? prefix, int number)
    {
      prefix = (prefix ?? string.Empty).Trim().TrimEnd('/');

      return string.IsNullOrWhiteSpace(prefix)
        ? number.ToString(CultureInfo.InvariantCulture)
        : $"{prefix}/{number}";
    }

    public WorkEntry StartNewWorkEntryBasedOn(WorkEntry sourceWorkEntry)
    {
      if (sourceWorkEntry.Project == null)
      {
        throw new InvalidOperationException("Cannot start a job without a project.");
      }

      WorkEntry workEntry = CreateWorkEntry(
        sourceWorkEntry.Project,
        DateTime.Now,
        sourceWorkEntry.Description,
        sourceWorkEntry.HourlyRate,
        sourceWorkEntry.Currency);
      StartWork(workEntry);

      return workEntry;
    }

    // remove a work entry from the model
    public event EventHandler<WorkEntryEventArgs>? WorkEntryRemoved;
    public void DeleteWorkEntry(WorkEntry workEntry)
    {
      if (_currentWorkEntry == workEntry)
      {
        _workTimer.Stop();
        _currentWorkEntry = null;
        _lastLongRunningWarningTime = null;
        RunningWorkChanged?.Invoke(this, EventArgs.Empty);
      }

      workEntry.IsRunning = false;
      workEntry.IsArchived = true;
      workEntry.DateArchived = DateTime.Now;

      WorkEntryRemoved?.Invoke(this, new WorkEntryEventArgs(workEntry));

      SaveData();
    }

    public void UpdateWorkEntry(WorkEntry workEntry, Project project, DateTime startTime, string? description, decimal hourlyRate, string currency, TimeSpan? duration = null)
    {
      if (workEntry.Project != project)
      {
        workEntry.Project?.WorkEntries.Remove(workEntry);
        project.WorkEntries.Add(workEntry);
        workEntry.Project = project;
      }

      if (duration != null && workEntry.IsRunning)
      {
        workEntry.StartTime = DateTime.Now - duration.Value;
        workEntry.EndTime = DateTime.Now;
      }
      else
      {
        workEntry.StartTime = startTime;
        if (duration != null)
        {
          workEntry.EndTime = startTime + duration.Value;
        }
      }

      workEntry.Description = description;
      workEntry.HourlyRate = hourlyRate;
      workEntry.Currency = TTAppSettings.NormalizeCurrency(currency);

      if (!workEntry.IsRunning && workEntry.EndTime < workEntry.StartTime)
      {
        workEntry.EndTime = workEntry.StartTime;
      }

      SaveData();
    }

    public void UpdateWorkEntry(WorkEntry workEntry, string clientName, string projectName, DateTime startTime, string? description, decimal hourlyRate, string currency, TimeSpan? duration = null)
    {
      Client client = FindOrCreateClient(clientName);
      Project project = FindOrCreateProject(client, projectName, description);

      UpdateWorkEntry(workEntry, project, startTime, description, hourlyRate, currency, duration);
    }

    public void UpdateClient(Client client, string name, string address, string companyName = "", string email = "", string phone = "", string invoiceNumberPrefix = "", int currentInvoiceNumber = 1, decimal defaultHourlyRate = 0, string defaultCurrency = "")
    {
      client.Name = name;
      client.Address = address;
      client.CompanyName = companyName;
      client.Email = email;
      client.Phone = phone;
      client.InvoiceNumberPrefix = invoiceNumberPrefix;
      client.CurrentInvoiceNumber = currentInvoiceNumber;
      client.DefaultHourlyRate = defaultHourlyRate;
      client.DefaultCurrency = string.IsNullOrWhiteSpace(defaultCurrency)
        ? TTAppSettings.Instance.DefaultCurrency
        : TTAppSettings.NormalizeCurrency(defaultCurrency);

      SaveData();
    }

    public void UpdateProject(Project project, Client client, string name, string? description, double rate, bool useClientDefaultRate)
    {
      if (project.Client != client)
      {
        project.Client?.Projects.Remove(project);
        client.Projects.Add(project);
        project.Client = client;
      }

      project.Name = name;
      project.Description = description;
      project.UseClientDefaultRate = useClientDefaultRate;
      project.Rate = useClientDefaultRate ? 0 : rate;
      SaveData();
    }

    public void ApplyProjectRateToJobs(Project project)
    {
      decimal hourlyRate = GetProjectHourlyRate(project);

      foreach (WorkEntry workEntry in project.WorkEntries)
      {
        if (!workEntry.IsArchived)
        {
          workEntry.HourlyRate = hourlyRate;
        }
      }

      SaveData();
    }

    public void ArchiveClient(Client client)
    {
      client.IsArchived = true;
      client.DateArchived = DateTime.Now;

      foreach (Project project in client.Projects)
      {
        ArchiveProject(project, false);
      }

      SaveData();
    }

    public void ArchiveProject(Project project, bool save = true)
    {
      project.IsArchived = true;
      project.DateArchived = DateTime.Now;

      foreach (WorkEntry workEntry in project.WorkEntries)
      {
        if (_currentWorkEntry == workEntry)
        {
          _workTimer.Stop();
          _currentWorkEntry = null;
          _lastLongRunningWarningTime = null;
          RunningWorkChanged?.Invoke(this, EventArgs.Empty);
        }

        workEntry.IsRunning = false;
        workEntry.IsArchived = true;
        workEntry.DateArchived = DateTime.Now;
      }

      if (save)
      {
        SaveData();
      }
    }

    public void PermanentDelete(TTDataObject item)
    {
      switch (item)
      {
        case Client client:
          _clients.Remove(client);
          break;

        case Project project:
          project.Client?.Projects.Remove(project);
          break;

        case WorkEntry workEntry:
          workEntry.Project?.WorkEntries.Remove(workEntry);
          break;
      }

      SaveData();
    }

    public void Restore(TTDataObject item)
    {
      switch (item)
      {
        case Client client:
          RestoreClient(client);
          break;

        case Project project:
          RestoreProject(project);
          break;

        case WorkEntry workEntry:
          RestoreWorkEntry(workEntry);
          break;
      }

      SaveData();
    }

    private void RestoreClient(Client client)
    {
      client.IsArchived = false;
      client.DateArchived = null;

      foreach (Project project in client.Projects)
      {
        RestoreProject(project);
      }
    }

    private void RestoreProject(Project project)
    {
      if (project.Client != null)
      {
        project.Client.IsArchived = false;
        project.Client.DateArchived = null;
      }

      project.IsArchived = false;
      project.DateArchived = null;

      foreach (WorkEntry workEntry in project.WorkEntries)
      {
        RestoreWorkEntry(workEntry);
      }
    }

    private void RestoreWorkEntry(WorkEntry workEntry)
    {
      if (workEntry.Project != null)
      {
        workEntry.Project.IsArchived = false;
        workEntry.Project.DateArchived = null;

        if (workEntry.Project.Client != null)
        {
          workEntry.Project.Client.IsArchived = false;
          workEntry.Project.Client.DateArchived = null;
        }
      }

      workEntry.IsArchived = false;
      workEntry.DateArchived = null;
    }

    public event EventHandler<ProjectEventArgs>? ProjectAdded;
    public Project CreateProject(Client client, string name, string? description = null, double rate = 0, bool useClientDefaultRate = true)
    {
      Project project = new()
      {
        ID = Guid.NewGuid(),
        Name = name,
        Client = client,
        Description = description,
        Rate = useClientDefaultRate ? 0 : rate,
        UseClientDefaultRate = useClientDefaultRate,
        ProjectColour = GenerateRandomPastelColor()
      };

      client.Projects.Add(project);

      ProjectAdded?.Invoke(this, new ProjectEventArgs(project));

      SaveData();

      return project;
    }

    public static decimal GetProjectHourlyRate(Project project)
    {
      if (!project.UseClientDefaultRate)
      {
        return Convert.ToDecimal(project.Rate);
      }

      return project.Client?.DefaultHourlyRate ?? TTAppSettings.Instance.DefaultHourlyRate;
    }

    public event EventHandler<ClientEventArgs>? ClientAdded;
    public Client CreateClient(string clientName)
    {
      Client client = new()
      {
        ID = Guid.NewGuid(),
        Name = clientName
      };

      _clients.Add(client);

      ClientAdded?.Invoke(this, new ClientEventArgs(client));

      SaveData();

      return client;
    }

    private Client FindOrCreateClient(string clientName)
    {
      Client? client = _clients.FirstOrDefault(c => string.Equals(c.Name, clientName, StringComparison.OrdinalIgnoreCase));

      return client ?? CreateClient(clientName);
    }

    private Project FindOrCreateProject(Client client, string projectName, string? description)
    {
      Project? project = client.Projects.FirstOrDefault(p => string.Equals(p.Name, projectName, StringComparison.OrdinalIgnoreCase));

      return project ?? CreateProject(client, projectName, description);
    }

    private static Color GenerateRandomPastelColor()
    {
      int blue = _random.Next(128, 200);
      int red = _random.Next(128, 200);
      int green = _random.Next(128, 200);

      return Color.FromRgb((byte)red, (byte)green, (byte)blue);
    }

    private void CheckLongRunningJobPolicy()
    {
      // A paused job is not running away with the clock, so it is not worth warning about.
      if (_currentWorkEntry == null || _isShowingLongRunningPrompt || _currentWorkEntry.IsPaused)
      {
        return;
      }

      TTAppSettings settings = TTAppSettings.Instance;
      DateTime now = DateTime.Now;
      int intervalMinutes = LongRunningJobPolicy.IntervalMinutes(
        settings.LongRunningJobBehavior,
        settings.LongRunningJobThresholdMinutes,
        settings.LongRunningJobReminderMinutes);

      if (!LongRunningJobPolicy.ShouldWarn(
        _currentWorkEntry.StartTime,
        now,
        _lastLongRunningWarningTime,
        settings.LongRunningJobThresholdMinutes,
        intervalMinutes))
      {
        return;
      }

      DateTime cutoff = LongRunningJobPolicy.Cutoff(
        _currentWorkEntry.StartTime,
        now,
        _lastLongRunningWarningTime,
        settings.LongRunningJobThresholdMinutes,
        intervalMinutes);
      _lastLongRunningWarningTime = now;

      bool allowContinue = settings.LongRunningJobBehavior != LongRunningJobBehavior.PromptAndStop;

      // Asking the user is the UI's job. With no listener the default stands, which keeps the
      // policy usable — and testable — without a window.
      LongRunningJobEventArgs args = new(_currentWorkEntry, cutoff, allowContinue)
      {
        ContinueRunning = allowContinue
      };

      // The listener is expected to block on a modal prompt while the work timer keeps ticking,
      // so re-entry is blocked until it returns.
      _isShowingLongRunningPrompt = true;
      try
      {
        LongRunningJobDetected?.Invoke(this, args);
      }
      finally
      {
        _isShowingLongRunningPrompt = false;
      }

      if (!args.ContinueRunning)
      {
        StopWork(cutoff);
      }
    }

  }
}
