using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using TimeTracker.Dialogs;
using TimeTracker.Models;
using TimeTracker.Utils;

namespace TimeTracker
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    private readonly TimeTrackerModel timeTracker = TimeTrackerModel.Instance;
    private bool isNavCollapsed;
    private const string JobsPage = "Jobs";
    private const string ClientsPage = "Clients";
    private const string ProjectsPage = "Projects";
    private const string TimesheetsPage = "Time sheets";
    private const string ReportsPage = "Reports";
    private const string InvoicesPage = "Invoices";
    private const string ArchivePage = "Archive";
    private const string SettingsPage = "Settings";

    public MainWindow()
    {
      InitializeComponent();

      timeTracker.RunningWorkChanged += TimeTracker_RunningWorkChanged;

      RestoreWindowState();
      RestoreNavigationState();
      ShowSavedPage();
      UpdateStartStopButton();
    }

    private void HamburgerButton_Click(object sender, RoutedEventArgs e)
    {
      ToggleNavigation();
    }

    private void StartStopButton_Click(object sender, RoutedEventArgs e)
    {
      if (StartStopButton.Content.ToString() == "Start")
      {
        NewWorkEntryDialog dialog = new()
        {
          Owner = this,
          WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        if (dialog.ShowDialog() == true)
        {
          WorkEntry workEntry = timeTracker.CreateWorkEntry(
            dialog.ClientName,
            dialog.ProjectName,
            dialog.StartTime,
            dialog.Description,
            dialog.HourlyRate,
            dialog.Currency,
            dialog.Duration);
          timeTracker.StartWork(workEntry);
        }
      }
      else
      {
        timeTracker.StopWork();
      }
    }

    private void TimeTracker_RunningWorkChanged(object? sender, EventArgs e)
    {
      UpdateStartStopButton();
    }

    private void UpdateStartStopButton()
    {
      StartStopButton.Content = timeTracker.HasRunningWork ? "Stop" : "Start";
    }

    private void JobsButton_Click(object sender, RoutedEventArgs e)
    {
      ShowHome();
    }

    private void ClientsButton_Click(object sender, RoutedEventArgs e)
    {
      ShowClients();
    }

    private void ProjectsButton_Click(object sender, RoutedEventArgs e)
    {
      ShowProjects();
    }

    private void TimesheetsButton_Click(object sender, RoutedEventArgs e)
    {
      ShowTimesheets();
    }

    private void ReportsButton_Click(object sender, RoutedEventArgs e)
    {
      ShowReports();
    }

    private void InvoicesButton_Click(object sender, RoutedEventArgs e)
    {
      ShowInvoices();
    }

    private void ArchiveButton_Click(object sender, RoutedEventArgs e)
    {
      ShowArchive();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
      ShowSettings();
    }

    private void ToggleNavigation()
    {
      isNavCollapsed = !isNavCollapsed;
      TTAppSettings.Instance.IsNavCollapsed = isNavCollapsed;
      TTAppSettings.Instance.Save();

      ApplyNavigationState();
    }

    private void RestoreNavigationState()
    {
      isNavCollapsed = TTAppSettings.Instance.IsNavCollapsed;
      ApplyNavigationState();
    }

    private void ApplyNavigationState()
    {

      NavColumn.Width = new GridLength(isNavCollapsed ? 50 : 200);

      Visibility labelVisibility = isNavCollapsed ? Visibility.Collapsed : Visibility.Visible;
      JobsLabelButton.Visibility = labelVisibility;
      ClientsLabelButton.Visibility = labelVisibility;
      ProjectsLabelButton.Visibility = labelVisibility;
      TimesheetsLabelButton.Visibility = labelVisibility;
      ReportsLabelButton.Visibility = labelVisibility;
      InvoicesLabelButton.Visibility = labelVisibility;
      ArchiveLabelButton.Visibility = labelVisibility;
      SettingsLabelButton.Visibility = labelVisibility;
    }

    private void RestoreWindowState()
    {
      TTAppSettings settings = TTAppSettings.Instance;

      Width = settings.WindowWidth > 0 ? settings.WindowWidth : Width;
      Height = settings.WindowHeight > 0 ? settings.WindowHeight : Height;

      if (!double.IsNaN(settings.WindowLeft) && !double.IsNaN(settings.WindowTop))
      {
        Left = settings.WindowLeft;
        Top = settings.WindowTop;
        WindowStartupLocation = WindowStartupLocation.Manual;
      }
    }

    private void SaveWindowState()
    {
      TTAppSettings settings = TTAppSettings.Instance;

      if (WindowState == WindowState.Normal)
      {
        settings.WindowLeft = Left;
        settings.WindowTop = Top;
        settings.WindowWidth = Width;
        settings.WindowHeight = Height;
      }

      settings.IsNavCollapsed = isNavCollapsed;
      settings.Save();
    }

    private void SaveCurrentPage(string page)
    {
      TTAppSettings.Instance.CurrentPage = page;
      TTAppSettings.Instance.Save();
    }

    private void ShowSavedPage()
    {
      switch (TTAppSettings.Instance.CurrentPage)
      {
        case ClientsPage:
          ShowClients();
          break;

        case ProjectsPage:
          ShowProjects();
          break;

        case TimesheetsPage:
          ShowTimesheets();
          break;

        case ReportsPage:
          ShowReports();
          break;

        case InvoicesPage:
          ShowInvoices();
          break;

        case ArchivePage:
          ShowArchive();
          break;

        case SettingsPage:
          ShowSettings();
          break;

        case JobsPage:
        default:
          ShowHome();
          break;
      }
    }

    private void ShowHome()
    {
      HeaderLabel.Content = "Time Tracker";
      SaveCurrentPage(JobsPage);

      Controls.WorkEntriesListControl homeList = new()
      {
        DataContext = timeTracker.Clients
      };

      ScrollViewer scrollViewer = new()
      {
        Content = homeList
      };

      ShowMainContent(scrollViewer);
    }

    private void ShowClients()
    {
      HeaderLabel.Content = "Clients";
      SaveCurrentPage(ClientsPage);

      DataGrid grid = CreateReadOnlyGrid();
      grid.ItemsSource = timeTracker.ActiveClients;
      grid.MouseDoubleClick += (_, _) =>
      {
        if (grid.SelectedItem is Client client)
        {
          EditClient(client);
        }
      };
      grid.Columns.Add(new DataGridTextColumn { Header = "Client", Binding = new Binding("Name") });
      grid.Columns.Add(new DataGridTextColumn { Header = "Company", Binding = new Binding("CompanyName") });
      grid.Columns.Add(new DataGridTextColumn { Header = "Email", Binding = new Binding("Email") });
      grid.Columns.Add(new DataGridTextColumn { Header = "Phone", Binding = new Binding("Phone") });
      grid.Columns.Add(new DataGridTextColumn { Header = "Address", Binding = new Binding("Address") });
      grid.Columns.Add(new DataGridTextColumn { Header = "Default rate", Binding = new Binding("DefaultHourlyRate") { StringFormat = "0.##" } });
      grid.Columns.Add(new DataGridTextColumn { Header = "Default currency", Binding = new Binding("DefaultCurrency") });
      grid.Columns.Add(new DataGridTextColumn { Header = "Invoice prefix", Binding = new Binding("InvoiceNumberPrefix") });
      grid.Columns.Add(new DataGridTextColumn { Header = "Current invoice number", Binding = new Binding("CurrentInvoiceNumber") });
      grid.Columns.Add(new DataGridTextColumn { Header = "Projects", Binding = new Binding("Projects.Count") });

      ShowMainContent(CreateListPage(
        CreateToolbarButton("New Client", (_, _) => NewClient()),
        CreateToolbarButton("Edit", (_, _) =>
        {
          if (grid.SelectedItem is Client client)
          {
            EditClient(client);
          }
        }),
        CreateToolbarButton("Delete", (_, _) =>
        {
          List<Client> clients = GetSelectedItems<Client>(grid);
          if (clients.Count > 0)
          {
            foreach (Client client in clients)
            {
              timeTracker.ArchiveClient(client);
            }

            ShowClients();
          }
        }),
        grid));
    }

    private void ShowProjects()
    {
      HeaderLabel.Content = "Projects";
      SaveCurrentPage(ProjectsPage);

      DataGrid grid = CreateReadOnlyGrid();
      grid.ItemsSource = timeTracker.Projects;
      grid.MouseDoubleClick += (_, _) =>
      {
        if (grid.SelectedItem is Project project)
        {
          EditProject(project);
        }
      };
      grid.Columns.Add(new DataGridTextColumn { Header = "Project", Binding = new Binding("Name") });
      grid.Columns.Add(new DataGridTextColumn { Header = "Client", Binding = new Binding("Client.Name") });
      grid.Columns.Add(new DataGridTextColumn { Header = "Description", Binding = new Binding("Description") });
      grid.Columns.Add(new DataGridTextColumn { Header = "Rate", Binding = new Binding("Rate") });

      ShowMainContent(CreateListPage(
        CreateToolbarButton("New Project", (_, _) => NewProject()),
        CreateToolbarButton("Edit", (_, _) =>
        {
          if (grid.SelectedItem is Project project)
          {
            EditProject(project);
          }
        }),
        CreateToolbarButton("Delete", (_, _) =>
        {
          List<Project> projects = GetSelectedItems<Project>(grid);
          if (projects.Count > 0)
          {
            foreach (Project project in projects)
            {
              timeTracker.ArchiveProject(project);
            }

            ShowProjects();
          }
        }),
        grid));
    }

    private void ShowTimesheets()
    {
      HeaderLabel.Content = "Time sheets";
      SaveCurrentPage(TimesheetsPage);

      CollectionViewSource timesheets = new()
      {
        Source = timeTracker.WorkEntries
      };
      timesheets.SortDescriptions.Add(new SortDescription(nameof(WorkEntry.StartTime), ListSortDirection.Descending));

      DataGrid grid = CreateReadOnlyGrid();
      grid.IsReadOnly = false;
      grid.ItemsSource = timesheets.View;
      grid.Sorting += TimesheetsGrid_Sorting;
      grid.RowEditEnding += TimesheetsGrid_RowEditEnding;
      grid.MouseDoubleClick += (_, _) =>
      {
        if (grid.SelectedItem is WorkEntry workEntry)
        {
          EditWorkEntry(workEntry);
          ShowTimesheets();
        }
      };
      grid.RowStyle = CreateTimesheetRowStyle();

      grid.Columns.Add(new DataGridTextColumn
      {
        Header = "Time",
        Binding = CreateDateTimeBinding("StartTime"),
        SortMemberPath = nameof(WorkEntry.StartTime),
        SortDirection = ListSortDirection.Descending,
        IsReadOnly = true
      });
      grid.Columns.Add(new DataGridTextColumn
      {
        Header = "Client",
        Binding = new Binding(nameof(WorkEntry.ClientName)),
        SortMemberPath = nameof(WorkEntry.ClientName),
        IsReadOnly = true
      });
      grid.Columns.Add(new DataGridTextColumn
      {
        Header = "Project",
        Binding = new Binding(nameof(WorkEntry.ProjectName)),
        SortMemberPath = nameof(WorkEntry.ProjectName),
        IsReadOnly = true
      });
      grid.Columns.Add(new DataGridTextColumn
      {
        Header = "Description",
        Binding = new Binding("Description"),
        IsReadOnly = true
      });
      grid.Columns.Add(new DataGridTextColumn
      {
        Header = "Rate",
        Binding = new Binding("HourlyRate") { StringFormat = "0.##" }
      });
      grid.Columns.Add(new DataGridTextColumn
      {
        Header = "Currency",
        Binding = new Binding("Currency")
      });
      grid.Columns.Add(new DataGridTextColumn
      {
        Header = "Duration",
        Binding = new Binding("Duration") { StringFormat = @"hh\:mm" },
        IsReadOnly = true
      });

      ShowMainContent(CreateListPage(
        CreateToolbarButton("New Entry", (_, _) => NewManualWorkEntry()),
        CreateToolbarButton("Edit", (_, _) =>
        {
          if (grid.SelectedItem is WorkEntry workEntry)
          {
            EditWorkEntry(workEntry);
            ShowTimesheets();
          }
        }),
        CreateToolbarButton("Delete", (_, _) =>
        {
          List<WorkEntry> workEntries = GetSelectedItems<WorkEntry>(grid);
          if (workEntries.Count > 0)
          {
            foreach (WorkEntry workEntry in workEntries)
            {
              timeTracker.DeleteWorkEntry(workEntry);
            }

            ShowTimesheets();
          }
        }),
        grid));
    }

    private void ShowReports()
    {
      HeaderLabel.Content = "Reports";
      SaveCurrentPage(ReportsPage);

      DataGrid grid = CreateReadOnlyGrid();
      grid.ItemsSource = timeTracker.Reports.Select(CreateReportRow).ToList();
      grid.MouseDoubleClick += (_, _) =>
      {
        if (grid.SelectedItem is ReportRow row)
        {
          EditReport(row.Report);
        }
      };

      grid.Columns.Add(new DataGridTextColumn { Header = "Report", Binding = new Binding(nameof(ReportRow.Name)) });
      grid.Columns.Add(new DataGridTextColumn { Header = "Start", Binding = CreateDateBinding(nameof(ReportRow.StartDate)) });
      grid.Columns.Add(new DataGridTextColumn { Header = "End", Binding = CreateDateBinding(nameof(ReportRow.EndDate)) });
      grid.Columns.Add(new DataGridTextColumn { Header = "Client", Binding = new Binding(nameof(ReportRow.ClientName)) });
      grid.Columns.Add(new DataGridTextColumn { Header = "Project", Binding = new Binding(nameof(ReportRow.ProjectName)) });
      grid.Columns.Add(new DataGridTextColumn { Header = "Jobs", Binding = new Binding(nameof(ReportRow.JobCount)) });
      grid.Columns.Add(new DataGridTextColumn { Header = "Hours", Binding = new Binding(nameof(ReportRow.Hours)) { StringFormat = "0.##" } });
      grid.Columns.Add(new DataGridTextColumn { Header = "Total", Binding = new Binding(nameof(ReportRow.Total)) { StringFormat = "0.00" } });
      grid.Columns.Add(new DataGridTextColumn { Header = "Currency", Binding = new Binding(nameof(ReportRow.Currency)) });

      ShowMainContent(CreateListPage(new[]
      {
        CreateToolbarButton("New Report", (_, _) => NewReport()),
        CreateToolbarButton("Edit", (_, _) =>
        {
          if (grid.SelectedItem is ReportRow row)
          {
            EditReport(row.Report);
          }
        }),
        CreateToolbarButton("Delete", (_, _) =>
        {
          List<ReportRow> rows = GetSelectedItems<ReportRow>(grid);
          if (rows.Count > 0)
          {
            foreach (ReportRow row in rows)
            {
              timeTracker.DeleteReport(row.Report);
            }

            ShowReports();
          }
        }),
        CreateToolbarButton("Print", (_, _) =>
        {
          if (grid.SelectedItem is ReportRow row)
          {
            PrintReport(row.Report);
          }
        }),
        CreateToolbarButton("Export Word", (_, _) =>
        {
          if (grid.SelectedItem is ReportRow row)
          {
            SaveReportFile(row.Report);
          }
        })
      },
        grid));
    }

    private void ShowInvoices()
    {
      HeaderLabel.Content = "Invoices";
      SaveCurrentPage(InvoicesPage);

      DataGrid grid = CreateReadOnlyGrid();
      grid.ItemsSource = timeTracker.Invoices.Select(CreateInvoiceRow).ToList();
      grid.MouseDoubleClick += (_, _) =>
      {
        if (grid.SelectedItem is InvoiceRow row)
        {
          PrintInvoice(row.Invoice);
        }
      };

      grid.Columns.Add(new DataGridTextColumn { Header = "Invoice", Binding = new Binding(nameof(InvoiceRow.InvoiceNumber)) });
      grid.Columns.Add(new DataGridTextColumn { Header = "Client", Binding = new Binding(nameof(InvoiceRow.ClientName)) });
      grid.Columns.Add(new DataGridTextColumn { Header = "Issued", Binding = CreateDateBinding(nameof(InvoiceRow.IssueDate)) });
      grid.Columns.Add(new DataGridTextColumn { Header = "Start", Binding = CreateDateBinding(nameof(InvoiceRow.StartDate)) });
      grid.Columns.Add(new DataGridTextColumn { Header = "End", Binding = CreateDateBinding(nameof(InvoiceRow.EndDate)) });
      grid.Columns.Add(new DataGridTextColumn { Header = "Jobs", Binding = new Binding(nameof(InvoiceRow.JobCount)) });
      grid.Columns.Add(new DataGridTextColumn { Header = "Hours", Binding = new Binding(nameof(InvoiceRow.HoursTotal)) { StringFormat = "0.##" } });
      grid.Columns.Add(new DataGridTextColumn { Header = "Total", Binding = new Binding(nameof(InvoiceRow.Total)) { StringFormat = "0.00" } });
      grid.Columns.Add(new DataGridTextColumn { Header = "Currency", Binding = new Binding(nameof(InvoiceRow.Currency)) });

      DockPanel page = CreateListPage(new[]
      {
        CreateToolbarButton("New Invoice", (_, _) => NewInvoice()),
        CreateToolbarButton("Print", (_, _) =>
        {
          if (grid.SelectedItem is InvoiceRow row)
          {
            PrintInvoice(row.Invoice);
          }
        }),
        CreateToolbarButton("Export Word", (_, _) =>
        {
          if (grid.SelectedItem is InvoiceRow row)
          {
            SaveInvoiceFile(row.Invoice);
          }
        }),
        CreateToolbarButton("Delete", (_, _) =>
        {
          List<InvoiceRow> rows = GetSelectedItems<InvoiceRow>(grid);
          if (rows.Count == 0)
          {
            return;
          }

          string message = rows.Count == 1
            ? "Delete this invoice record?\n\nJobs will not be deleted and the client's current invoice number will not be rolled back."
            : $"Delete these {rows.Count} invoice records?\n\nJobs will not be deleted and the clients' current invoice numbers will not be rolled back.";

          MessageBoxResult result = MessageBox.Show(
            this,
            message,
            rows.Count == 1 ? "Delete invoice" : "Delete invoices",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

          if (result == MessageBoxResult.Yes)
          {
            foreach (InvoiceRow row in rows)
            {
              timeTracker.DeleteInvoice(row.Invoice);
            }

            ShowInvoices();
          }
        }),
        CreateToolbarButton("View Jobs", (_, _) =>
        {
          if (grid.SelectedItem is InvoiceRow row)
          {
            ShowInvoiceJobs(row.Invoice);
          }
        })
      },
        grid);

      ShowMainContent(page);
    }

    private void NewReport()
    {
      if (ShowReportDialog(null, out string name, out DateTime startDate, out DateTime endDate, out Client? client, out Project? project, out bool includeArchivedJobs))
      {
        Report report = timeTracker.CreateReport(name, startDate, endDate, client, project, includeArchivedJobs);
        SaveReportFile(report);
        ShowReports();
      }
    }

    private void EditReport(Report report)
    {
      if (ShowReportDialog(report, out string name, out DateTime startDate, out DateTime endDate, out Client? client, out Project? project, out bool includeArchivedJobs))
      {
        timeTracker.UpdateReport(report, name, startDate, endDate, client, project, includeArchivedJobs);
        ShowReports();
      }
    }

    private void NewInvoice()
    {
      if (!ShowInvoiceRangeDialog(out Client? client, out DateTime startDate, out DateTime endDate) || client == null)
      {
        return;
      }

      List<InvoiceJobSelection> jobs = timeTracker.GetInvoiceCandidateJobs(client, startDate, endDate)
        .Select(workEntry => new InvoiceJobSelection(workEntry))
        .ToList();

      if (jobs.Count == 0)
      {
        MessageBox.Show(this, "No jobs found for that client and date range.", "Invoice", MessageBoxButton.OK, MessageBoxImage.Information);
        return;
      }

      if (!ShowInvoiceJobDialog(jobs))
      {
        return;
      }

      List<WorkEntry> selectedJobs = jobs.Where(job => job.IsIncluded).Select(job => job.WorkEntry).ToList();
      if (selectedJobs.Count == 0)
      {
        MessageBox.Show(this, "Select at least one job for the invoice.", "Invoice", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }

      Invoice invoice = timeTracker.CreateInvoice(client, startDate, endDate, selectedJobs);
      SaveInvoiceFile(invoice);
      MessageBox.Show(this, $"Created invoice {invoice.InvoiceNumber}.", "Invoice", MessageBoxButton.OK, MessageBoxImage.Information);
      ShowInvoices();
    }

    private void SaveInvoiceFile(Invoice invoice)
    {
      Client? client = timeTracker.FindClient(invoice.ClientID);
      string suggestedFileName = $"{SanitizeFileName(invoice.InvoiceNumber)}.docx";

      if (!TryGetSavePath("Save Invoice", suggestedFileName, TTAppSettings.Instance.LastInvoiceFolder, out string filePath))
      {
        return;
      }

      SaveJobsWordDocument(
        filePath,
        $"Invoice {invoice.InvoiceNumber}",
        timeTracker.GetInvoiceJobs(invoice),
        invoice.StartDate,
        invoice.EndDate,
        client?.Name ?? string.Empty,
        client?.CompanyName ?? string.Empty,
        client?.Address ?? string.Empty,
        invoice.IssueDate,
        TTAppSettings.Instance.WordTemplatePath);

      TTAppSettings.Instance.LastInvoiceFolder = Path.GetDirectoryName(filePath) ?? string.Empty;
      TTAppSettings.Instance.Save();
    }

    private void SaveReportFile(Report report)
    {
      string suggestedFileName = $"{SanitizeFileName(report.Name)}.docx";

      if (!TryGetSavePath("Save Report", suggestedFileName, TTAppSettings.Instance.LastReportFolder, out string filePath))
      {
        return;
      }

      SaveJobsWordDocument(
        filePath,
        report.Name,
        timeTracker.GetReportJobs(report),
        report.StartDate,
        report.EndDate,
        string.Empty,
        string.Empty,
        string.Empty,
        null,
        TTAppSettings.Instance.ReportTemplatePath,
        "ReportTemplateExample.docx");

      TTAppSettings.Instance.LastReportFolder = Path.GetDirectoryName(filePath) ?? string.Empty;
      TTAppSettings.Instance.Save();
    }

    private bool TryGetSavePath(string title, string fileName, string initialFolder, out string filePath)
    {
      Microsoft.Win32.SaveFileDialog dialog = new()
      {
        Title = title,
        FileName = fileName,
        DefaultExt = ".docx",
        Filter = "Word documents (*.docx)|*.docx|All files (*.*)|*.*",
        InitialDirectory = Directory.Exists(initialFolder)
          ? initialFolder
          : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
      };

      bool accepted = dialog.ShowDialog(this) == true;
      filePath = accepted ? dialog.FileName : string.Empty;
      return accepted;
    }

    private static void SaveJobsWordDocument(string filePath, string title, IEnumerable<WorkEntry> jobs, DateTime startDate, DateTime endDate, string clientName, string clientCompany, string clientAddress, DateTime? issueDate, string configuredTemplatePath, string fallbackTemplateName = "InvoiceTemplateExample.docx")
    {
      string templatePath = ResolveWordTemplatePath(configuredTemplatePath, fallbackTemplateName);

      if (File.Exists(templatePath))
      {
        File.Copy(templatePath, filePath, true);
        using ZipArchive archive = ZipFile.Open(filePath, ZipArchiveMode.Update);
        string templateDocumentXml = ReadZipEntry(archive, "word/document.xml") ?? CreateJobsWordDocumentXml(title, jobs, startDate, endDate, clientName, issueDate);
        ReplaceZipEntry(archive, "docProps/core.xml", CreateCorePropertiesXml(title));
        ReplaceZipEntry(archive, "word/document.xml", FillTemplateWordDocumentXml(templateDocumentXml, title, jobs, startDate, endDate, clientName, clientCompany, clientAddress, issueDate));
        return;
      }

      using ZipArchive newArchive = ZipFile.Open(filePath, ZipArchiveMode.Create);
      ReplaceZipEntry(newArchive, "[Content_Types].xml", CreateContentTypesXml());
      ReplaceZipEntry(newArchive, "_rels/.rels", CreatePackageRelationshipsXml());
      ReplaceZipEntry(newArchive, "docProps/core.xml", CreateCorePropertiesXml(title));
      ReplaceZipEntry(newArchive, "word/_rels/document.xml.rels", CreateDocumentRelationshipsXml());
      ReplaceZipEntry(newArchive, "word/document.xml", CreateJobsWordDocumentXml(title, jobs, startDate, endDate, clientName, issueDate));
      ReplaceZipEntry(newArchive, "word/styles.xml", CreateStylesXml());
    }

    private static string? ReadZipEntry(ZipArchive archive, string entryName)
    {
      ZipArchiveEntry? entry = archive.GetEntry(entryName);
      if (entry == null)
      {
        return null;
      }

      using StreamReader reader = new(entry.Open(), Encoding.UTF8);
      return reader.ReadToEnd();
    }

    private static string ResolveWordTemplatePath(string configuredTemplatePath, string fallbackTemplateName = "InvoiceTemplateExample.docx")
    {
      if (!string.IsNullOrWhiteSpace(configuredTemplatePath) && File.Exists(configuredTemplatePath))
      {
        return configuredTemplatePath;
      }

      string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", fallbackTemplateName);
      if (File.Exists(templatePath))
      {
        return templatePath;
      }

      return Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Templates", fallbackTemplateName));
    }

    private static string GetTemplatePickerInitialDirectory(string configuredTemplatePath)
    {
      if (!string.IsNullOrWhiteSpace(configuredTemplatePath))
      {
        string? configuredFolder = Path.GetDirectoryName(configuredTemplatePath);
        if (!string.IsNullOrWhiteSpace(configuredFolder) && Directory.Exists(configuredFolder))
        {
          return configuredFolder;
        }
      }

      string bundledTemplatePath = ResolveWordTemplatePath(string.Empty);
      string? bundledTemplateFolder = Path.GetDirectoryName(bundledTemplatePath);
      return !string.IsNullOrWhiteSpace(bundledTemplateFolder) && Directory.Exists(bundledTemplateFolder)
        ? bundledTemplateFolder
        : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }

    private static void ReplaceZipEntry(ZipArchive archive, string entryName, string content)
    {
      ZipArchiveEntry? existingEntry = archive.GetEntry(entryName);
      existingEntry?.Delete();

      ZipArchiveEntry entry = archive.CreateEntry(entryName);
      using StreamWriter writer = new(entry.Open(), new UTF8Encoding(false));
      writer.Write(content);
    }

    private static string CreateJobsWordDocumentXml(string title, IEnumerable<WorkEntry> jobs, DateTime startDate, DateTime endDate, string clientName, DateTime? issueDate)
    {
      List<WorkEntry> jobList = jobs.ToList();
      StringBuilder body = new();

      body.Append(Paragraph(title, "Title"));
      if (!string.IsNullOrWhiteSpace(clientName))
      {
        body.Append(Paragraph($"Client: {clientName}"));
      }
      if (issueDate != null)
      {
        body.Append(Paragraph($"Issue date: {FormatDate(issueDate.Value)}"));
      }
      body.Append(Paragraph($"Period: {FormatDate(startDate)} to {FormatDate(endDate)}"));
      body.Append(Paragraph(""));
      body.Append(CreateLineItemsTableXml(jobList));
      body.Append(Paragraph($"Hours total: {TimeTrackerModel.CalculateHoursTotal(jobList):0.##}", "Total"));
      body.Append(Paragraph($"Total: {TimeTrackerModel.GetCurrencySummary(jobList)} {TimeTrackerModel.CalculateTotal(jobList):0.00}", "Total"));

      return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">" +
        $"<w:body>{body}<w:sectPr><w:pgSz w:w=\"12240\" w:h=\"15840\"/><w:pgMar w:top=\"1440\" w:right=\"1440\" w:bottom=\"1440\" w:left=\"1440\" w:header=\"708\" w:footer=\"708\" w:gutter=\"0\"/></w:sectPr></w:body>" +
        "</w:document>";
    }

    private static string FillTemplateWordDocumentXml(string documentXml, string title, IEnumerable<WorkEntry> jobs, DateTime startDate, DateTime endDate, string clientName, string clientCompany, string clientAddress, DateTime? issueDate)
    {
      List<WorkEntry> jobList = jobs.ToList();
      string invoiceNumber = title.StartsWith("Invoice ", StringComparison.OrdinalIgnoreCase) ? title["Invoice ".Length..] : title;
      string currency = TimeTrackerModel.GetCurrencySummary(jobList);
      string invoiceTotal = TimeTrackerModel.CalculateTotal(jobList).ToString("0.00");
      string hoursTotal = TimeTrackerModel.CalculateHoursTotal(jobList).ToString("0.##");
      string lineItemsXml = CreateLineItemsTableXml(jobList);
      List<string> projectNames = jobList.Select(job => job.ProjectName).Where(name => !string.IsNullOrWhiteSpace(name)).Distinct().ToList();
      string projectName = projectNames.Count == 1 ? projectNames[0] : "Multiple projects";

      Dictionary<string, string> replacements = new()
      {
        ["InvoiceNumber"] = invoiceNumber,
        ["ReportName"] = title,
        ["IssueDate"] = issueDate == null ? string.Empty : FormatDate(issueDate.Value),
        ["StartDate"] = FormatDate(startDate),
        ["EndDate"] = FormatDate(endDate),
        ["ClientName"] = clientName,
        ["ClientCompany"] = clientCompany,
        ["ClientAddress"] = clientAddress,
        ["ProjectName"] = projectName,
        ["Currency"] = currency,
        ["InvoiceTotal"] = invoiceTotal,
        ["HoursTotal"] = hoursTotal
      };

      string filledXml = ReplaceLineItemTemplateRows(documentXml, jobList);
      filledXml = ReplaceParagraphPlaceholders(filledXml, replacements);

      foreach ((string key, string value) in replacements)
      {
        filledXml = filledXml.Replace($"{{{{{key}}}}}", Xml(value));
      }

      filledXml = Regex.Replace(
        filledXml,
        "<w:p\\b[^>]*>.*?\\{\\{LineItems\\}\\}.*?</w:p>",
        lineItemsXml,
        RegexOptions.Singleline);
      filledXml = ReplaceLineItemsParagraphPlaceholder(filledXml, lineItemsXml);

      if (!filledXml.Contains("{{LineItems}}") && filledXml == documentXml)
      {
        return CreateJobsWordDocumentXml(title, jobList, startDate, endDate, clientName, issueDate);
      }

      return filledXml;
    }

    private static string ReplaceLineItemTemplateRows(string documentXml, IReadOnlyList<WorkEntry> jobs)
    {
      return Regex.Replace(
        documentXml,
        "<w:tr\\b[^>]*>.*?</w:tr>",
        match =>
        {
          string rowXml = match.Value;
          string rowText = GetParagraphText(rowXml);
          if (!ContainsLineItemPlaceholder(rowText))
          {
            return rowXml;
          }

          StringBuilder rows = new();
          foreach (WorkEntry job in jobs)
          {
            decimal lineTotal = Convert.ToDecimal(job.Duration.TotalHours) * job.HourlyRate;
            Dictionary<string, string> replacements = new()
            {
              ["ItemDate"] = FormatDate(job.StartTime),
              ["ProjectName"] = job.ProjectName,
              ["Description"] = job.Description ?? string.Empty,
              ["Hours"] = job.Duration.TotalHours.ToString("0.##"),
              ["Rate"] = $"{job.Currency} {job.HourlyRate:0.##}",
              ["LineTotal"] = $"{job.Currency} {lineTotal:0.00}"
            };

            rows.Append(ReplaceParagraphPlaceholders(rowXml, replacements));
          }

          return rows.ToString();
        },
        RegexOptions.Singleline);
    }

    private static bool ContainsLineItemPlaceholder(string text)
    {
      return text.Contains("{{ItemDate}}")
        || text.Contains("{{Description}}")
        || text.Contains("{{Hours}}")
        || text.Contains("{{Rate}}")
        || text.Contains("{{LineTotal}}");
    }

    private static string ReplaceParagraphPlaceholders(string documentXml, IReadOnlyDictionary<string, string> replacements)
    {
      return Regex.Replace(
        documentXml,
        "<w:p\\b[^>]*>.*?</w:p>",
        match =>
        {
          string paragraphXml = match.Value;
          string paragraphText = GetParagraphText(paragraphXml).Trim();

          if (!paragraphText.Contains("{{"))
          {
            return paragraphXml;
          }

          string replacedText = ReplaceVisiblePlaceholders(paragraphText, replacements);
          if (replacedText == paragraphText)
          {
            return paragraphXml;
          }

          return ReplaceParagraphText(paragraphXml, replacedText);
        },
        RegexOptions.Singleline);
    }

    private static string ReplaceVisiblePlaceholders(string text, IReadOnlyDictionary<string, string> replacements)
    {
      return Regex.Replace(
        text,
        "\\{\\{([A-Za-z0-9]+)\\}\\}",
        match => replacements.TryGetValue(match.Groups[1].Value, out string? replacement) ? replacement : match.Value);
    }

    private static string ReplaceLineItemsParagraphPlaceholder(string documentXml, string lineItemsXml)
    {
      return Regex.Replace(
        documentXml,
        "<w:p\\b[^>]*>.*?</w:p>",
        match =>
        {
          string paragraphXml = match.Value;
          return GetParagraphText(paragraphXml).Trim() == "{{LineItems}}"
            ? lineItemsXml
            : paragraphXml;
        },
        RegexOptions.Singleline);
    }

    private static string GetParagraphText(string paragraphXml)
    {
      StringBuilder text = new();
      foreach (Match match in Regex.Matches(paragraphXml, "<w:t\\b[^>]*>(.*?)</w:t>", RegexOptions.Singleline))
      {
        text.Append(System.Net.WebUtility.HtmlDecode(match.Groups[1].Value));
      }

      return text.ToString();
    }

    private static string ReplaceParagraphText(string paragraphXml, string replacement)
    {
      bool replacedFirstTextRun = false;
      return Regex.Replace(
        paragraphXml,
        "<w:t\\b[^>]*>.*?</w:t>",
        match =>
        {
          if (!replacedFirstTextRun)
          {
            replacedFirstTextRun = true;
            return $"<w:t xml:space=\"preserve\">{Xml(replacement)}</w:t>";
          }

          return string.Empty;
        },
        RegexOptions.Singleline);
    }

    private static string CreateLineItemsTableXml(IEnumerable<WorkEntry> jobs)
    {
      StringBuilder table = new();
      table.Append("<w:tbl>");
      table.Append("<w:tblPr><w:tblW w:w=\"9360\" w:type=\"dxa\"/><w:tblBorders><w:top w:val=\"single\" w:sz=\"4\" w:color=\"D9D9D9\"/><w:left w:val=\"single\" w:sz=\"4\" w:color=\"D9D9D9\"/><w:bottom w:val=\"single\" w:sz=\"4\" w:color=\"D9D9D9\"/><w:right w:val=\"single\" w:sz=\"4\" w:color=\"D9D9D9\"/><w:insideH w:val=\"single\" w:sz=\"4\" w:color=\"D9D9D9\"/><w:insideV w:val=\"single\" w:sz=\"4\" w:color=\"D9D9D9\"/></w:tblBorders></w:tblPr>");
      table.Append("<w:tblGrid><w:gridCol w:w=\"1250\"/><w:gridCol w:w=\"1250\"/><w:gridCol w:w=\"1450\"/><w:gridCol w:w=\"2760\"/><w:gridCol w:w=\"850\"/><w:gridCol w:w=\"900\"/><w:gridCol w:w=\"900\"/></w:tblGrid>");
      table.Append(TableRow(new[] { "Date", "Client", "Project", "Description", "Hours", "Rate", "Total" }, true));

      foreach (WorkEntry job in jobs)
      {
        decimal lineTotal = Convert.ToDecimal(job.Duration.TotalHours) * job.HourlyRate;
        table.Append(TableRow(new[]
        {
          FormatDateTime(job.StartTime),
          job.ClientName,
          job.ProjectName,
          job.Description ?? string.Empty,
          job.Duration.TotalHours.ToString("0.##"),
          $"{job.Currency} {job.HourlyRate:0.##}",
          $"{job.Currency} {lineTotal:0.00}"
        }, false));
      }

      table.Append("</w:tbl>");
      return table.ToString();
    }

    private static string FormatDate(DateTime date)
    {
      return date.ToString("d");
    }

    private static string FormatDateTime(DateTime dateTime)
    {
      return dateTime.ToString("g");
    }

    private static string SanitizeFileName(string fileName)
    {
      string sanitized = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim();
      return string.IsNullOrWhiteSpace(sanitized) ? "Time Tracker Export" : sanitized;
    }

    private static string Paragraph(string text, string? style = null)
    {
      string paragraphStyle = string.IsNullOrWhiteSpace(style) ? string.Empty : $"<w:pPr><w:pStyle w:val=\"{style}\"/></w:pPr>";
      return $"<w:p>{paragraphStyle}<w:r><w:t xml:space=\"preserve\">{Xml(text)}</w:t></w:r></w:p>";
    }

    private static string TableRow(IEnumerable<string> values, bool isHeader)
    {
      StringBuilder row = new("<w:tr>");
      foreach (string value in values)
      {
        string shading = isHeader ? "<w:shd w:fill=\"F2F4F7\"/>" : string.Empty;
        string boldStart = isHeader ? "<w:b/>" : string.Empty;
        row.Append($"<w:tc><w:tcPr>{shading}<w:tcMar><w:top w:w=\"80\" w:type=\"dxa\"/><w:left w:w=\"120\" w:type=\"dxa\"/><w:bottom w:w=\"80\" w:type=\"dxa\"/><w:right w:w=\"120\" w:type=\"dxa\"/></w:tcMar></w:tcPr><w:p><w:r><w:rPr>{boldStart}</w:rPr><w:t xml:space=\"preserve\">{Xml(value)}</w:t></w:r></w:p></w:tc>");
      }
      row.Append("</w:tr>");
      return row.ToString();
    }

    private static string CreateContentTypesXml()
    {
      return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
        "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
        "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
        "<Override PartName=\"/docProps/core.xml\" ContentType=\"application/vnd.openxmlformats-package.core-properties+xml\"/>" +
        "<Override PartName=\"/word/document.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml\"/>" +
        "<Override PartName=\"/word/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml\"/>" +
        "</Types>";
    }

    private static string CreatePackageRelationshipsXml()
    {
      return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
        "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"word/document.xml\"/>" +
        "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties\" Target=\"docProps/core.xml\"/>" +
        "</Relationships>";
    }

    private static string CreateCorePropertiesXml(string title)
    {
      string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
      return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        "<cp:coreProperties xmlns:cp=\"http://schemas.openxmlformats.org/package/2006/metadata/core-properties\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:dcterms=\"http://purl.org/dc/terms/\" xmlns:dcmitype=\"http://purl.org/dc/dcmitype/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">" +
        $"<dc:title>{Xml(title)}</dc:title>" +
        "<dc:creator>Time Tracker</dc:creator>" +
        "<cp:lastModifiedBy>Time Tracker</cp:lastModifiedBy>" +
        $"<dcterms:created xsi:type=\"dcterms:W3CDTF\">{timestamp}</dcterms:created>" +
        $"<dcterms:modified xsi:type=\"dcterms:W3CDTF\">{timestamp}</dcterms:modified>" +
        "</cp:coreProperties>";
    }

    private static string CreateDocumentRelationshipsXml()
    {
      return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"/>";
    }

    private static string CreateStylesXml()
    {
      return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        "<w:styles xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">" +
        "<w:style w:type=\"paragraph\" w:default=\"1\" w:styleId=\"Normal\"><w:name w:val=\"Normal\"/><w:rPr><w:rFonts w:ascii=\"Calibri\" w:hAnsi=\"Calibri\"/><w:sz w:val=\"22\"/></w:rPr></w:style>" +
        "<w:style w:type=\"paragraph\" w:styleId=\"Title\"><w:name w:val=\"Title\"/><w:pPr><w:spacing w:after=\"160\"/></w:pPr><w:rPr><w:b/><w:sz w:val=\"48\"/></w:rPr></w:style>" +
        "<w:style w:type=\"paragraph\" w:styleId=\"Total\"><w:name w:val=\"Total\"/><w:pPr><w:jc w:val=\"right\"/><w:spacing w:before=\"240\"/></w:pPr><w:rPr><w:b/><w:sz w:val=\"32\"/></w:rPr></w:style>" +
        "</w:styles>";
    }

    private static string Xml(string value)
    {
      return System.Security.SecurityElement.Escape(value) ?? string.Empty;
    }

    private void ShowInvoiceJobs(Invoice invoice)
    {
      Window dialog = new()
      {
        Title = $"Jobs for {invoice.InvoiceNumber}",
        Owner = this,
        WindowStartupLocation = WindowStartupLocation.CenterOwner,
        Width = 760,
        Height = 420
      };

      DataGrid grid = CreateJobGrid();
      grid.ItemsSource = timeTracker.GetInvoiceJobs(invoice).Select(CreateJobRow).ToList();

      DockPanel panel = new();
      panel.Children.Add(grid);
      dialog.Content = panel;
      dialog.ShowDialog();
    }

    private bool ShowReportDialog(Report? report, out string name, out DateTime startDate, out DateTime endDate, out Client? client, out Project? project, out bool includeArchivedJobs)
    {
      TextBox nameTextBox = CreateDialogTextBox(report?.Name ?? $"Report {DateTime.Today:d}");
      DatePicker startDatePicker = new() { SelectedDate = report?.StartDate ?? DateTime.Today.AddDays(-7), Width = 220 };
      DatePicker endDatePicker = new() { SelectedDate = report?.EndDate ?? DateTime.Today, Width = 220 };
      ComboBox clientComboBox = CreateClientOptionComboBox(report?.ClientID);
      ComboBox projectComboBox = CreateProjectOptionComboBox(report?.ProjectID, null);
      CheckBox includeArchivedCheckBox = new() { IsChecked = report?.IncludeArchivedJobs ?? false };

      clientComboBox.SelectionChanged += (_, _) =>
      {
        Client? selectedClient = (clientComboBox.SelectedItem as ClientOption)?.Client;
        projectComboBox.ItemsSource = ProjectOption.CreateOptions(timeTracker.Projects.Where(p => selectedClient == null || p.Client == selectedClient));
        projectComboBox.DisplayMemberPath = nameof(ProjectOption.Name);
        projectComboBox.SelectedIndex = 0;
      };

      bool accepted = ShowSimpleForm(
        report == null ? "New Report" : "Edit Report",
        new[]
        {
          ("Name", (Control)nameTextBox),
          ("Start", (Control)startDatePicker),
          ("End", (Control)endDatePicker),
          ("Client", (Control)clientComboBox),
          ("Project", (Control)projectComboBox),
          ("Include archived jobs", (Control)includeArchivedCheckBox)
        });

      name = nameTextBox.Text.Trim();
      startDate = startDatePicker.SelectedDate ?? DateTime.Today;
      endDate = endDatePicker.SelectedDate ?? DateTime.Today;
      client = (clientComboBox.SelectedItem as ClientOption)?.Client;
      project = (projectComboBox.SelectedItem as ProjectOption)?.Project;
      includeArchivedJobs = includeArchivedCheckBox.IsChecked == true;

      if (!accepted)
      {
        return false;
      }

      if (string.IsNullOrWhiteSpace(name))
      {
        MessageBox.Show(this, "Enter a report name.", "Report", MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
      }

      if (endDate.Date < startDate.Date)
      {
        MessageBox.Show(this, "Report end date must be on or after the start date.", "Report", MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
      }

      return true;
    }

    private bool ShowInvoiceRangeDialog(out Client? client, out DateTime startDate, out DateTime endDate)
    {
      ComboBox clientComboBox = new()
      {
        ItemsSource = timeTracker.ActiveClients,
        DisplayMemberPath = nameof(Client.Name),
        Width = 220
      };
      DatePicker startDatePicker = new() { SelectedDate = DateTime.Today, Width = 220 };
      DatePicker endDatePicker = new() { SelectedDate = DateTime.Today, Width = 220 };

      clientComboBox.SelectionChanged += (_, _) =>
      {
        if (clientComboBox.SelectedItem is Client selectedClient)
        {
          startDatePicker.SelectedDate = timeTracker.GetDefaultInvoiceStartDate(selectedClient);
        }
      };

      if (clientComboBox.Items.Count > 0)
      {
        clientComboBox.SelectedIndex = 0;
      }

      bool accepted = ShowSimpleForm(
        "New Invoice",
        new[]
        {
          ("Client", (Control)clientComboBox),
          ("Start", (Control)startDatePicker),
          ("End", (Control)endDatePicker)
        });

      client = clientComboBox.SelectedItem as Client;
      startDate = startDatePicker.SelectedDate ?? DateTime.Today;
      endDate = endDatePicker.SelectedDate ?? DateTime.Today;

      if (!accepted)
      {
        return false;
      }

      if (client == null)
      {
        MessageBox.Show(this, "Choose a client.", "Invoice", MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
      }

      if (endDate.Date < startDate.Date)
      {
        MessageBox.Show(this, "Invoice end date must be on or after the start date.", "Invoice", MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
      }

      return true;
    }

    private bool ShowInvoiceJobDialog(List<InvoiceJobSelection> jobs)
    {
      Window dialog = new()
      {
        Title = "Invoice Jobs",
        Owner = this,
        WindowStartupLocation = WindowStartupLocation.CenterOwner,
        Width = 820,
        Height = 500
      };

      DockPanel panel = new() { Margin = new Thickness(12) };
      TextBlock totalTextBlock = new()
      {
        FontSize = 18,
        FontWeight = FontWeights.SemiBold,
        Margin = new Thickness(0, 0, 0, 8)
      };

      DataGrid grid = new()
      {
        AutoGenerateColumns = false,
        CanUserAddRows = false,
        ItemsSource = jobs,
        Margin = new Thickness(0, 0, 0, 8)
      };
      grid.Columns.Add(new DataGridCheckBoxColumn { Header = "Include", Binding = new Binding(nameof(InvoiceJobSelection.IsIncluded)) { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged } });
      grid.Columns.Add(new DataGridTextColumn { Header = "Date", Binding = CreateDateTimeBinding(nameof(InvoiceJobSelection.StartTime)), IsReadOnly = true });
      grid.Columns.Add(new DataGridTextColumn { Header = "Project", Binding = new Binding(nameof(InvoiceJobSelection.ProjectName)), IsReadOnly = true });
      grid.Columns.Add(new DataGridTextColumn { Header = "Description", Binding = new Binding(nameof(InvoiceJobSelection.Description)), IsReadOnly = true });
      grid.Columns.Add(new DataGridTextColumn { Header = "Hours", Binding = new Binding(nameof(InvoiceJobSelection.Hours)) { StringFormat = "0.##" }, IsReadOnly = true });
      grid.Columns.Add(new DataGridTextColumn { Header = "Total", Binding = new Binding(nameof(InvoiceJobSelection.Total)) { StringFormat = "0.00" }, IsReadOnly = true });
      grid.CurrentCellChanged += (_, _) => UpdateInvoiceSelectionTotal(jobs, totalTextBlock);
      grid.CellEditEnding += (_, _) => Dispatcher.BeginInvoke(() => UpdateInvoiceSelectionTotal(jobs, totalTextBlock));

      StackPanel buttons = new()
      {
        Orientation = Orientation.Horizontal,
        HorizontalAlignment = HorizontalAlignment.Right
      };
      Button cancelButton = new() { Content = "Cancel", Width = 80, Height = 28, Margin = new Thickness(0, 0, 8, 0), IsCancel = true };
      Button createButton = new() { Content = "Create", Width = 80, Height = 28, IsDefault = true };
      createButton.Click += (_, _) => dialog.DialogResult = true;
      buttons.Children.Add(cancelButton);
      buttons.Children.Add(createButton);

      DockPanel.SetDock(totalTextBlock, Dock.Top);
      DockPanel.SetDock(buttons, Dock.Bottom);
      panel.Children.Add(totalTextBlock);
      panel.Children.Add(buttons);
      panel.Children.Add(grid);

      dialog.Content = panel;
      UpdateInvoiceSelectionTotal(jobs, totalTextBlock);
      return dialog.ShowDialog() == true;
    }

    private static void UpdateInvoiceSelectionTotal(List<InvoiceJobSelection> jobs, TextBlock totalTextBlock)
    {
      List<WorkEntry> selectedJobs = jobs.Where(job => job.IsIncluded).Select(job => job.WorkEntry).ToList();
      totalTextBlock.Text = $"Total: {TimeTrackerModel.GetCurrencySummary(selectedJobs)} {TimeTrackerModel.CalculateTotal(selectedJobs):0.00}";
    }

    private void PrintReport(Report report)
    {
      PrintFlowDocument(CreateJobsDocument(report.Name, timeTracker.GetReportJobs(report), report.StartDate, report.EndDate));
    }

    private void PrintInvoice(Invoice invoice)
    {
      Client? client = timeTracker.FindClient(invoice.ClientID);
      string title = $"{invoice.InvoiceNumber} - {client?.Name ?? "Client"}";
      PrintFlowDocument(CreateJobsDocument(title, timeTracker.GetInvoiceJobs(invoice), invoice.StartDate, invoice.EndDate));
    }

    private static FlowDocument CreateJobsDocument(string title, IEnumerable<WorkEntry> jobs, DateTime startDate, DateTime endDate)
    {
      List<WorkEntry> jobList = jobs.ToList();
      FlowDocument document = new()
      {
        FontFamily = new FontFamily("Segoe UI"),
        FontSize = 11,
        PagePadding = new Thickness(48)
      };

      document.Blocks.Add(new Paragraph(new Run(title)) { FontSize = 22, FontWeight = FontWeights.SemiBold });
      document.Blocks.Add(new Paragraph(new Run($"{FormatDate(startDate)} to {FormatDate(endDate)}")));

      Table table = new();
      document.Blocks.Add(table);
      for (int i = 0; i < 6; i++)
      {
        table.Columns.Add(new TableColumn());
      }

      TableRowGroup rowGroup = new();
      table.RowGroups.Add(rowGroup);
      TableRow header = new();
      rowGroup.Rows.Add(header);
      AddCell(header, "Date", true);
      AddCell(header, "Client", true);
      AddCell(header, "Project", true);
      AddCell(header, "Description", true);
      AddCell(header, "Hours", true);
      AddCell(header, "Total", true);

      foreach (WorkEntry job in jobList)
      {
        TableRow row = new();
        rowGroup.Rows.Add(row);
        AddCell(row, FormatDateTime(job.StartTime), false);
        AddCell(row, job.ClientName, false);
        AddCell(row, job.ProjectName, false);
        AddCell(row, job.Description ?? string.Empty, false);
        AddCell(row, job.Duration.TotalHours.ToString("0.##"), false);
        AddCell(row, $"{job.Currency} {(Convert.ToDecimal(job.Duration.TotalHours) * job.HourlyRate):0.00}", false);
      }

      document.Blocks.Add(new Paragraph(new Run($"Total: {TimeTrackerModel.GetCurrencySummary(jobList)} {TimeTrackerModel.CalculateTotal(jobList):0.00}"))
      {
        FontSize = 16,
        FontWeight = FontWeights.SemiBold,
        TextAlignment = TextAlignment.Right
      });
      document.Blocks.Add(new Paragraph(new Run($"Hours total: {TimeTrackerModel.CalculateHoursTotal(jobList):0.##}"))
      {
        FontSize = 14,
        FontWeight = FontWeights.SemiBold,
        TextAlignment = TextAlignment.Right
      });

      return document;
    }

    private static void AddCell(TableRow row, string text, bool isHeader)
    {
      row.Cells.Add(new TableCell(new Paragraph(new Run(text)))
      {
        FontWeight = isHeader ? FontWeights.SemiBold : FontWeights.Normal,
        Padding = new Thickness(2, 4, 8, 4)
      });
    }

    private void PrintFlowDocument(FlowDocument document)
    {
      PrintDialog printDialog = new();
      if (printDialog.ShowDialog() != true)
      {
        return;
      }

      document.PageHeight = printDialog.PrintableAreaHeight;
      document.PageWidth = printDialog.PrintableAreaWidth;
      document.ColumnWidth = printDialog.PrintableAreaWidth;
      printDialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, "Time Tracker");
    }

    private void ShowArchive()
    {
      HeaderLabel.Content = "Archive";
      SaveCurrentPage(ArchivePage);

      DataGrid grid = CreateReadOnlyGrid();
      grid.ItemsSource = timeTracker.ArchivedItems.Select(CreateArchiveRow).ToList();
      grid.Columns.Add(new DataGridTextColumn { Header = "Type", Binding = new Binding(nameof(ArchiveRow.Type)) });
      grid.Columns.Add(new DataGridTextColumn { Header = "Name", Binding = new Binding(nameof(ArchiveRow.Name)) });
      grid.Columns.Add(new DataGridTextColumn { Header = "Client", Binding = new Binding(nameof(ArchiveRow.ClientName)) });
      grid.Columns.Add(new DataGridTextColumn { Header = "Project", Binding = new Binding(nameof(ArchiveRow.ProjectName)) });
      grid.Columns.Add(new DataGridTextColumn { Header = "Item Date", Binding = CreateDateTimeBinding(nameof(ArchiveRow.ItemDate)) });
      grid.Columns.Add(new DataGridTextColumn { Header = "Date Deleted", Binding = CreateDateTimeBinding(nameof(ArchiveRow.DateArchived)) });

      Button restoreButton = CreateToolbarButton("Restore", (_, _) =>
      {
        List<ArchiveRow> rows = GetSelectedItems<ArchiveRow>(grid);
        if (rows.Count == 0)
        {
          return;
        }

        foreach (ArchiveRow row in rows)
        {
          timeTracker.Restore(row.Item);
        }

        ShowArchive();
      });

      Button deleteButton = CreateToolbarButton("Delete", (_, _) =>
      {
        List<ArchiveRow> rows = GetSelectedItems<ArchiveRow>(grid);
        if (rows.Count == 0)
        {
          return;
        }

        string message = rows.Count == 1
          ? "This will permanently delete the archived item and cannot be undone. Continue?"
          : $"This will permanently delete {rows.Count} archived items and cannot be undone. Continue?";

        MessageBoxResult result = MessageBox.Show(
          this,
          message,
          "Permanent delete",
          MessageBoxButton.YesNo,
          MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
          foreach (ArchiveRow row in rows)
          {
            timeTracker.PermanentDelete(row.Item);
          }

          ShowArchive();
        }
      });

      DockPanel page = new();
      StackPanel toolbar = new()
      {
        Orientation = Orientation.Horizontal,
        Margin = new Thickness(10, 10, 10, 0)
      };
      toolbar.Children.Add(restoreButton);
      toolbar.Children.Add(deleteButton);
      DockPanel.SetDock(toolbar, Dock.Top);
      page.Children.Add(toolbar);
      page.Children.Add(grid);

      ShowMainContent(page);
    }

    private void NewClient()
    {
      if (ShowClientDialog(null, out string name, out string address, out string companyName, out string email, out string phone, out string invoiceNumberPrefix, out int currentInvoiceNumber, out decimal defaultHourlyRate, out string defaultCurrency))
      {
        Client client = timeTracker.CreateClient(name);
        timeTracker.UpdateClient(client, name, address, companyName, email, phone, invoiceNumberPrefix, currentInvoiceNumber, defaultHourlyRate, defaultCurrency);
        ShowClients();
      }
    }

    private void EditClient(Client client)
    {
      if (ShowClientDialog(client, out string name, out string address, out string companyName, out string email, out string phone, out string invoiceNumberPrefix, out int currentInvoiceNumber, out decimal defaultHourlyRate, out string defaultCurrency))
      {
        timeTracker.UpdateClient(client, name, address, companyName, email, phone, invoiceNumberPrefix, currentInvoiceNumber, defaultHourlyRate, defaultCurrency);
        ShowClients();
      }
    }

    private void NewProject()
    {
      if (ShowProjectDialog(null, out Client? client, out string name, out string description, out double rate) && client != null)
      {
        Project project = timeTracker.CreateProject(client, name, description);
        project.Rate = rate;
        timeTracker.SaveChanges();
        ShowProjects();
      }
    }

    private void EditProject(Project project)
    {
      if (ShowProjectDialog(project, out Client? client, out string name, out string description, out double rate) && client != null)
      {
        double oldRate = project.Rate;
        timeTracker.UpdateProject(project, client, name, description, rate);

        if (oldRate != rate)
        {
          MessageBoxResult result = MessageBox.Show(
            this,
            "Apply the new project rate to all active jobs for this project?",
            "Project rate changed",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

          if (result == MessageBoxResult.Yes)
          {
            timeTracker.ApplyProjectRateToJobs(project);
          }
        }

        ShowProjects();
      }
    }

    private void NewManualWorkEntry()
    {
      NewWorkEntryDialog dialog = new()
      {
        Owner = this,
        WindowStartupLocation = WindowStartupLocation.CenterOwner
      };

      if (dialog.ShowDialog() == true)
      {
        timeTracker.CreateWorkEntry(
          dialog.ClientName,
          dialog.ProjectName,
          dialog.StartTime,
          dialog.Description,
          dialog.HourlyRate,
          dialog.Currency,
          dialog.Duration);
        ShowTimesheets();
      }
    }

    private void EditWorkEntry(WorkEntry workEntry)
    {
      NewWorkEntryDialog dialog = new(workEntry)
      {
        Owner = this,
        WindowStartupLocation = WindowStartupLocation.CenterOwner
      };

      if (dialog.ShowDialog() == true)
      {
        timeTracker.UpdateWorkEntry(
          workEntry,
          dialog.ClientName,
          dialog.ProjectName,
          dialog.StartTime,
          dialog.Description,
          dialog.HourlyRate,
          dialog.Currency,
          dialog.Duration);
      }
    }

    private void ShowSettings()
    {
      HeaderLabel.Content = "Settings";
      SaveCurrentPage(SettingsPage);

      TTAppSettings settings = TTAppSettings.Instance;

      StackPanel panel = new()
      {
        Margin = new Thickness(20),
        MaxWidth = 520,
        HorizontalAlignment = HorizontalAlignment.Left
      };

      panel.Children.Add(new TextBlock
      {
        Text = "Money",
        FontSize = 22,
        FontWeight = FontWeights.SemiBold,
        Margin = new Thickness(0, 0, 0, 15)
      });

      TextBox defaultHourlyRateTextBox = CreateSettingsTextBox(settings.DefaultHourlyRate.ToString("0.##"));
      panel.Children.Add(CreateSettingsLabel("Default hourly rate"));
      panel.Children.Add(defaultHourlyRateTextBox);

      ComboBox defaultCurrencyComboBox = CreateCurrencyComboBox(settings.DefaultCurrency);
      panel.Children.Add(CreateSettingsLabel("Default currency"));
      panel.Children.Add(defaultCurrencyComboBox);

      panel.Children.Add(new TextBlock
      {
        Text = "Templates",
        FontSize = 22,
        FontWeight = FontWeights.SemiBold,
        Margin = new Thickness(0, 10, 0, 15)
      });

      TextBox wordTemplatePathTextBox = new()
      {
        Text = settings.WordTemplatePath,
        Width = 400,
        Margin = new Thickness(0, 0, 8, 12),
        HorizontalAlignment = HorizontalAlignment.Left
      };
      TextBox reportTemplatePathTextBox = new()
      {
        Text = settings.ReportTemplatePath,
        Width = 400,
        Margin = new Thickness(0, 0, 8, 12),
        HorizontalAlignment = HorizontalAlignment.Left
      };
      Button browseTemplateButton = new()
      {
        Content = "Browse",
        Width = 80,
        Height = 24,
        Margin = new Thickness(0, 0, 0, 12)
      };
      browseTemplateButton.Click += (_, _) =>
      {
        Microsoft.Win32.OpenFileDialog dialog = new()
        {
          Title = "Select Word Template",
          DefaultExt = ".docx",
          Filter = "Word documents (*.docx)|*.docx|All files (*.*)|*.*",
          InitialDirectory = GetTemplatePickerInitialDirectory(wordTemplatePathTextBox.Text)
        };

        if (dialog.ShowDialog(this) == true)
        {
          wordTemplatePathTextBox.Text = dialog.FileName;
        }
      };
      Button browseReportTemplateButton = new()
      {
        Content = "Browse",
        Width = 80,
        Height = 24,
        Margin = new Thickness(0, 0, 0, 12)
      };
      browseReportTemplateButton.Click += (_, _) =>
      {
        Microsoft.Win32.OpenFileDialog dialog = new()
        {
          Title = "Select Report Word Template",
          DefaultExt = ".docx",
          Filter = "Word documents (*.docx)|*.docx|All files (*.*)|*.*",
          InitialDirectory = GetTemplatePickerInitialDirectory(reportTemplatePathTextBox.Text)
        };

        if (dialog.ShowDialog(this) == true)
        {
          reportTemplatePathTextBox.Text = dialog.FileName;
        }
      };

      StackPanel templatePickerPanel = new()
      {
        Orientation = Orientation.Horizontal
      };
      templatePickerPanel.Children.Add(wordTemplatePathTextBox);
      templatePickerPanel.Children.Add(browseTemplateButton);
      panel.Children.Add(CreateSettingsLabel("Invoice Word template"));
      panel.Children.Add(templatePickerPanel);

      StackPanel reportTemplatePickerPanel = new()
      {
        Orientation = Orientation.Horizontal
      };
      reportTemplatePickerPanel.Children.Add(reportTemplatePathTextBox);
      reportTemplatePickerPanel.Children.Add(browseReportTemplateButton);
      panel.Children.Add(CreateSettingsLabel("Report Word template"));
      panel.Children.Add(reportTemplatePickerPanel);

      panel.Children.Add(new TextBlock
      {
        Text = "Long Running Jobs",
        FontSize = 22,
        FontWeight = FontWeights.SemiBold,
        Margin = new Thickness(0, 10, 0, 15)
      });

      TextBox thresholdTextBox = CreateSettingsTextBox(settings.LongRunningJobThresholdMinutes.ToString());
      panel.Children.Add(CreateSettingsLabel("Warn after minutes"));
      panel.Children.Add(thresholdTextBox);

      ComboBox behaviorComboBox = new()
      {
        ItemsSource = LongRunningJobBehaviorOption.All,
        SelectedItem = LongRunningJobBehaviorOption.All.First(option => option.Value == settings.LongRunningJobBehavior),
        DisplayMemberPath = nameof(LongRunningJobBehaviorOption.DisplayName),
        Margin = new Thickness(0, 0, 0, 12),
        Width = 260,
        HorizontalAlignment = HorizontalAlignment.Left
      };
      panel.Children.Add(CreateSettingsLabel("Behaviour"));
      panel.Children.Add(behaviorComboBox);

      TextBox reminderTextBox = CreateSettingsTextBox(settings.LongRunningJobReminderMinutes.ToString());
      panel.Children.Add(CreateSettingsLabel("Repeat reminder minutes"));
      panel.Children.Add(reminderTextBox);

      RoutedEventHandler saveOnLostFocus = (_, _) => SaveSettingsIfValid(defaultHourlyRateTextBox, defaultCurrencyComboBox, wordTemplatePathTextBox, reportTemplatePathTextBox, thresholdTextBox, behaviorComboBox, reminderTextBox, true);
      defaultHourlyRateTextBox.LostFocus += saveOnLostFocus;
      defaultCurrencyComboBox.LostFocus += saveOnLostFocus;
      wordTemplatePathTextBox.LostFocus += saveOnLostFocus;
      reportTemplatePathTextBox.LostFocus += saveOnLostFocus;
      thresholdTextBox.LostFocus += saveOnLostFocus;
      reminderTextBox.LostFocus += saveOnLostFocus;
      behaviorComboBox.SelectionChanged += (_, _) => SaveSettingsIfValid(defaultHourlyRateTextBox, defaultCurrencyComboBox, wordTemplatePathTextBox, reportTemplatePathTextBox, thresholdTextBox, behaviorComboBox, reminderTextBox, false);
      wordTemplatePathTextBox.TextChanged += (_, _) =>
      {
        if (File.Exists(wordTemplatePathTextBox.Text) || string.IsNullOrWhiteSpace(wordTemplatePathTextBox.Text))
        {
          SaveSettingsIfValid(defaultHourlyRateTextBox, defaultCurrencyComboBox, wordTemplatePathTextBox, reportTemplatePathTextBox, thresholdTextBox, behaviorComboBox, reminderTextBox, false);
        }
      };
      reportTemplatePathTextBox.TextChanged += (_, _) =>
      {
        if (File.Exists(reportTemplatePathTextBox.Text) || string.IsNullOrWhiteSpace(reportTemplatePathTextBox.Text))
        {
          SaveSettingsIfValid(defaultHourlyRateTextBox, defaultCurrencyComboBox, wordTemplatePathTextBox, reportTemplatePathTextBox, thresholdTextBox, behaviorComboBox, reminderTextBox, false);
        }
      };

      ShowMainContent(new ScrollViewer
      {
        Content = panel,
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
      });
    }

    private void SaveSettingsIfValid(TextBox defaultHourlyRateTextBox, ComboBox defaultCurrencyComboBox, TextBox wordTemplatePathTextBox, TextBox reportTemplatePathTextBox, TextBox thresholdTextBox, ComboBox behaviorComboBox, TextBox reminderTextBox, bool showWarnings)
    {
      if (!int.TryParse(thresholdTextBox.Text, out int thresholdMinutes) || thresholdMinutes <= 0)
      {
        ShowSettingsWarning(showWarnings, "Enter a warning threshold greater than zero.");
        return;
      }

      if (!int.TryParse(reminderTextBox.Text, out int reminderMinutes) || reminderMinutes <= 0)
      {
        ShowSettingsWarning(showWarnings, "Enter a repeat reminder interval greater than zero.");
        return;
      }

      if (!decimal.TryParse(defaultHourlyRateTextBox.Text, out decimal defaultHourlyRate) || defaultHourlyRate < 0)
      {
        ShowSettingsWarning(showWarnings, "Enter a default hourly rate of zero or greater.");
        return;
      }

      if (string.IsNullOrWhiteSpace(defaultCurrencyComboBox.Text))
      {
        ShowSettingsWarning(showWarnings, "Enter a default currency.");
        return;
      }

      if (!string.IsNullOrWhiteSpace(wordTemplatePathTextBox.Text) && !File.Exists(wordTemplatePathTextBox.Text))
      {
        ShowSettingsWarning(showWarnings, "Choose an existing invoice Word template file.");
        return;
      }

      if (!string.IsNullOrWhiteSpace(reportTemplatePathTextBox.Text) && !File.Exists(reportTemplatePathTextBox.Text))
      {
        ShowSettingsWarning(showWarnings, "Choose an existing report Word template file.");
        return;
      }

      TTAppSettings settings = TTAppSettings.Instance;
      settings.DefaultHourlyRate = defaultHourlyRate;
      settings.DefaultCurrency = TTAppSettings.NormalizeCurrency(defaultCurrencyComboBox.Text);
      settings.WordTemplatePath = wordTemplatePathTextBox.Text.Trim();
      settings.ReportTemplatePath = reportTemplatePathTextBox.Text.Trim();
      settings.LongRunningJobThresholdMinutes = thresholdMinutes;
      settings.LongRunningJobBehavior = ((LongRunningJobBehaviorOption)behaviorComboBox.SelectedItem).Value;
      settings.LongRunningJobReminderMinutes = reminderMinutes;
      settings.Save();
    }

    private void ShowSettingsWarning(bool showWarnings, string message)
    {
      if (showWarnings)
      {
        MessageBox.Show(this, message, "Settings", MessageBoxButton.OK, MessageBoxImage.Warning);
      }
    }

    private static DataGrid CreateReadOnlyGrid()
    {
      return new DataGrid
      {
        AutoGenerateColumns = false,
        CanUserAddRows = false,
        IsReadOnly = true,
        GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
        HeadersVisibility = DataGridHeadersVisibility.Column,
        Margin = new Thickness(10)
      };
    }

    private static List<T> GetSelectedItems<T>(DataGrid grid)
    {
      return grid.SelectedItems.Cast<T>().ToList();
    }

    private static Binding CreateDateBinding(string path)
    {
      return new Binding(path)
      {
        Converter = new LocaleDateConverter(includeTime: false)
      };
    }

    private static Binding CreateDateTimeBinding(string path)
    {
      return new Binding(path)
      {
        Converter = new LocaleDateConverter(includeTime: true)
      };
    }

    private static DockPanel CreateListPage(Button primaryButton, Button editButton, Button deleteButton, UIElement content)
    {
      return CreateListPage(new[] { primaryButton, editButton, deleteButton }, content);
    }

    private static DockPanel CreateListPage(Button primaryButton, Button editButton, Button deleteButton, Button printButton, UIElement content)
    {
      return CreateListPage(new[] { primaryButton, editButton, deleteButton, printButton }, content);
    }

    private static DockPanel CreateListPage(IEnumerable<Button> buttons, UIElement content)
    {
      DockPanel page = new();

      StackPanel toolbar = new()
      {
        Orientation = Orientation.Horizontal,
        Margin = new Thickness(10, 10, 10, 0)
      };
      foreach (Button button in buttons)
      {
        toolbar.Children.Add(button);
      }

      DockPanel.SetDock(toolbar, Dock.Top);
      page.Children.Add(toolbar);
      page.Children.Add(content);

      return page;
    }

    private static Button CreateToolbarButton(string text, RoutedEventHandler handler)
    {
      Button button = new()
      {
        Content = text,
        MinWidth = 90,
        Height = 28,
        Margin = new Thickness(0, 0, 8, 0)
      };
      button.Click += handler;

      return button;
    }

    private static ArchiveRow CreateArchiveRow(TTDataObject item)
    {
      return item switch
      {
        Client client => new ArchiveRow("Client", client.Name, client.Name, string.Empty, null, client.DateArchived, client),
        Project project => new ArchiveRow("Project", project.Name ?? string.Empty, project.Client?.Name ?? string.Empty, project.Name ?? string.Empty, null, project.DateArchived, project),
        WorkEntry workEntry => new ArchiveRow("Job", workEntry.Description ?? string.Empty, workEntry.ClientName, workEntry.ProjectName, workEntry.StartTime, workEntry.DateArchived, workEntry),
        _ => new ArchiveRow("Item", string.Empty, string.Empty, string.Empty, null, item.DateArchived, item)
      };
    }

    private bool ShowClientDialog(Client? client, out string name, out string address, out string companyName, out string email, out string phone, out string invoiceNumberPrefix, out int currentInvoiceNumber, out decimal defaultHourlyRate, out string defaultCurrency)
    {
      TextBox nameTextBox = CreateDialogTextBox(client?.Name ?? string.Empty);
      TextBox companyNameTextBox = CreateDialogTextBox(client?.CompanyName ?? string.Empty);
      TextBox emailTextBox = CreateDialogTextBox(client?.Email ?? string.Empty);
      TextBox phoneTextBox = CreateDialogTextBox(client?.Phone ?? string.Empty);
      TextBox addressTextBox = CreateDialogTextBox(client?.Address ?? string.Empty);
      TextBox defaultHourlyRateTextBox = CreateDialogTextBox(client?.DefaultHourlyRate.ToString("0.##") ?? TTAppSettings.Instance.DefaultHourlyRate.ToString("0.##"));
      ComboBox defaultCurrencyComboBox = CreateCurrencyComboBox(string.IsNullOrWhiteSpace(client?.DefaultCurrency) ? TTAppSettings.Instance.DefaultCurrency : client.DefaultCurrency);
      defaultCurrencyComboBox.Width = 220;
      defaultCurrencyComboBox.HorizontalAlignment = HorizontalAlignment.Center;
      TextBox invoiceNumberPrefixTextBox = CreateDialogTextBox(client?.InvoiceNumberPrefix ?? string.Empty);
      TextBox currentInvoiceNumberTextBox = CreateDialogTextBox((client?.CurrentInvoiceNumber > 0 ? client.CurrentInvoiceNumber : 1).ToString());

      bool accepted = ShowSimpleForm(
        client == null ? "New Client" : "Edit Client",
        new[]
        {
          ("Name", (Control)nameTextBox),
          ("Company name", (Control)companyNameTextBox),
          ("Email", (Control)emailTextBox),
          ("Phone", (Control)phoneTextBox),
          ("Address", (Control)addressTextBox),
          ("Default hourly rate", (Control)defaultHourlyRateTextBox),
          ("Default currency", (Control)defaultCurrencyComboBox),
          ("Invoice number prefix", (Control)invoiceNumberPrefixTextBox),
          ("Current invoice number", (Control)currentInvoiceNumberTextBox)
        });

      name = nameTextBox.Text.Trim();
      companyName = companyNameTextBox.Text.Trim();
      email = emailTextBox.Text.Trim();
      phone = phoneTextBox.Text.Trim();
      address = addressTextBox.Text.Trim();
      defaultCurrency = defaultCurrencyComboBox.Text.Trim();
      invoiceNumberPrefix = invoiceNumberPrefixTextBox.Text.Trim();
      currentInvoiceNumber = 1;
      defaultHourlyRate = 0;

      if (accepted && string.IsNullOrWhiteSpace(name))
      {
        MessageBox.Show(this, "Enter a client name.", "Client", MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
      }

      if (accepted && (!int.TryParse(currentInvoiceNumberTextBox.Text, out currentInvoiceNumber) || currentInvoiceNumber <= 0))
      {
        MessageBox.Show(this, "Enter a current invoice number greater than zero.", "Client", MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
      }

      if (accepted && (!decimal.TryParse(defaultHourlyRateTextBox.Text, out defaultHourlyRate) || defaultHourlyRate < 0))
      {
        MessageBox.Show(this, "Enter a default hourly rate of zero or greater.", "Client", MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
      }

      if (accepted && string.IsNullOrWhiteSpace(defaultCurrency))
      {
        MessageBox.Show(this, "Enter a default currency.", "Client", MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
      }

      return accepted;
    }

    private bool ShowProjectDialog(Project? project, out Client? client, out string name, out string description, out double rate)
    {
      ComboBox clientComboBox = new()
      {
        ItemsSource = timeTracker.ActiveClients,
        DisplayMemberPath = "Name",
        SelectedItem = project?.Client,
        Width = 220
      };
      TextBox nameTextBox = CreateDialogTextBox(project?.Name ?? string.Empty);
      TextBox descriptionTextBox = CreateDialogTextBox(project?.Description ?? string.Empty);
      TextBox rateTextBox = CreateDialogTextBox(project?.Rate.ToString("0.##") ?? "0");

      bool accepted = ShowSimpleForm(
        project == null ? "New Project" : "Edit Project",
        new[]
        {
          ("Client", (Control)clientComboBox),
          ("Name", (Control)nameTextBox),
          ("Description", (Control)descriptionTextBox),
          ("Rate", (Control)rateTextBox)
        });

      client = clientComboBox.SelectedItem as Client;
      name = nameTextBox.Text.Trim();
      description = descriptionTextBox.Text.Trim();
      rate = 0;

      if (!accepted)
      {
        return false;
      }

      if (client == null)
      {
        MessageBox.Show(this, "Choose a client.", "Project", MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
      }

      if (string.IsNullOrWhiteSpace(name))
      {
        MessageBox.Show(this, "Enter a project name.", "Project", MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
      }

      if (!double.TryParse(rateTextBox.Text, out rate) || rate < 0)
      {
        MessageBox.Show(this, "Enter a project rate of zero or greater.", "Project", MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
      }

      return true;
    }

    private bool ShowSimpleForm(string title, IEnumerable<(string Label, Control Input)> fields)
    {
      Window dialog = new()
      {
        Title = title,
        Owner = this,
        WindowStartupLocation = WindowStartupLocation.CenterOwner,
        ResizeMode = ResizeMode.NoResize,
        Width = 340,
        SizeToContent = SizeToContent.Height
      };

      StackPanel panel = new()
      {
        Margin = new Thickness(16)
      };

      foreach ((string label, Control input) in fields)
      {
        panel.Children.Add(new TextBlock
        {
          Text = label,
          FontWeight = FontWeights.SemiBold,
          Margin = new Thickness(0, 0, 0, 4)
        });
        input.Margin = new Thickness(0, 0, 0, 12);
        panel.Children.Add(input);
      }

      StackPanel buttons = new()
      {
        Orientation = Orientation.Horizontal,
        HorizontalAlignment = HorizontalAlignment.Right
      };
      Button cancelButton = new() { Content = "Cancel", Width = 80, Height = 28, Margin = new Thickness(0, 0, 8, 0), IsCancel = true };
      Button saveButton = new() { Content = "Save", Width = 80, Height = 28, IsDefault = true };
      saveButton.Click += (_, _) => dialog.DialogResult = true;
      buttons.Children.Add(cancelButton);
      buttons.Children.Add(saveButton);
      panel.Children.Add(buttons);

      dialog.Content = panel;

      return dialog.ShowDialog() == true;
    }

    private static TextBox CreateDialogTextBox(string text)
    {
      return new TextBox
      {
        Text = text,
        Width = 220
      };
    }

    private static TextBlock CreateSettingsLabel(string text)
    {
      return new TextBlock
      {
        Text = text,
        FontWeight = FontWeights.SemiBold,
        Margin = new Thickness(0, 0, 0, 4)
      };
    }

    private static TextBox CreateSettingsTextBox(string text)
    {
      return new TextBox
      {
        Text = text,
        Width = 120,
        Margin = new Thickness(0, 0, 0, 12),
        HorizontalAlignment = HorizontalAlignment.Left
      };
    }

    private static ComboBox CreateCurrencyComboBox(string selectedCurrency)
    {
      return new ComboBox
      {
        ItemsSource = MajorCurrencySymbols,
        Text = selectedCurrency,
        IsEditable = true,
        IsTextSearchEnabled = true,
        Width = 120,
        Margin = new Thickness(0, 0, 0, 12),
        HorizontalAlignment = HorizontalAlignment.Left
      };
    }

    private static IReadOnlyList<string> MajorCurrencySymbols { get; } = new[]
    {
      "£",
      "$",
      "€",
      "¥",
      "₹",
      "₩",
      "₽",
      "₺",
      "₿",
      "A$",
      "C$",
      "NZ$",
      "R$",
      "CHF",
      "kr",
      "zł",
      "د.إ"
    };

    private ReportRow CreateReportRow(Report report)
    {
      List<WorkEntry> jobs = timeTracker.GetReportJobs(report).ToList();
      Client? client = report.ClientID == null ? null : timeTracker.FindClient(report.ClientID.Value);
      Project? project = report.ProjectID == null ? null : timeTracker.FindProject(report.ProjectID.Value);

      return new ReportRow(
        report,
        client?.Name ?? "All clients",
        project?.Name ?? "All projects",
        jobs.Count,
        jobs.Sum(job => job.Duration.TotalHours),
        TimeTrackerModel.CalculateTotal(jobs),
        TimeTrackerModel.GetCurrencySummary(jobs));
    }

    private InvoiceRow CreateInvoiceRow(Invoice invoice)
    {
      Client? client = timeTracker.FindClient(invoice.ClientID);
      double hoursTotal = invoice.HoursTotal > 0
        ? invoice.HoursTotal
        : TimeTrackerModel.CalculateHoursTotal(timeTracker.GetInvoiceJobs(invoice));
      return new InvoiceRow(invoice, client?.Name ?? string.Empty, hoursTotal);
    }

    private static JobRow CreateJobRow(WorkEntry workEntry)
    {
      return new JobRow(workEntry);
    }

    private static DataGrid CreateJobGrid()
    {
      DataGrid grid = CreateReadOnlyGrid();
      grid.Columns.Add(new DataGridTextColumn { Header = "Date", Binding = CreateDateTimeBinding(nameof(JobRow.StartTime)) });
      grid.Columns.Add(new DataGridTextColumn { Header = "Client", Binding = new Binding(nameof(JobRow.ClientName)) });
      grid.Columns.Add(new DataGridTextColumn { Header = "Project", Binding = new Binding(nameof(JobRow.ProjectName)) });
      grid.Columns.Add(new DataGridTextColumn { Header = "Description", Binding = new Binding(nameof(JobRow.Description)) });
      grid.Columns.Add(new DataGridTextColumn { Header = "Hours", Binding = new Binding(nameof(JobRow.Hours)) { StringFormat = "0.##" } });
      grid.Columns.Add(new DataGridTextColumn { Header = "Rate", Binding = new Binding(nameof(JobRow.HourlyRate)) { StringFormat = "0.##" } });
      grid.Columns.Add(new DataGridTextColumn { Header = "Total", Binding = new Binding(nameof(JobRow.Total)) { StringFormat = "0.00" } });
      grid.Columns.Add(new DataGridTextColumn { Header = "Currency", Binding = new Binding(nameof(JobRow.Currency)) });
      return grid;
    }

    private ComboBox CreateClientOptionComboBox(Guid? selectedClientID)
    {
      ComboBox comboBox = new()
      {
        ItemsSource = ClientOption.CreateOptions(timeTracker.ActiveClients),
        DisplayMemberPath = nameof(ClientOption.Name),
        Width = 220
      };
      comboBox.SelectedItem = comboBox.Items.Cast<ClientOption>().FirstOrDefault(option => option.Client?.ID == selectedClientID) ?? comboBox.Items[0];
      return comboBox;
    }

    private ComboBox CreateProjectOptionComboBox(Guid? selectedProjectID, Client? client)
    {
      ComboBox comboBox = new()
      {
        ItemsSource = ProjectOption.CreateOptions(timeTracker.Projects.Where(project => client == null || project.Client == client)),
        DisplayMemberPath = nameof(ProjectOption.Name),
        Width = 220
      };
      comboBox.SelectedItem = comboBox.Items.Cast<ProjectOption>().FirstOrDefault(option => option.Project?.ID == selectedProjectID) ?? comboBox.Items[0];
      return comboBox;
    }

    private sealed class ClientOption
    {
      public ClientOption(string name, Client? client)
      {
        Name = name;
        Client = client;
      }

      public string Name { get; }
      public Client? Client { get; }

      public static IReadOnlyList<ClientOption> CreateOptions(IEnumerable<Client> clients)
      {
        List<ClientOption> options = new() { new ClientOption("All clients", null) };
        options.AddRange(clients.Select(client => new ClientOption(client.Name, client)));
        return options;
      }
    }

    private sealed class ProjectOption
    {
      public ProjectOption(string name, Project? project)
      {
        Name = name;
        Project = project;
      }

      public string Name { get; }
      public Project? Project { get; }

      public static IReadOnlyList<ProjectOption> CreateOptions(IEnumerable<Project> projects)
      {
        List<ProjectOption> options = new() { new ProjectOption("All projects", null) };
        options.AddRange(projects.Select(project => new ProjectOption(project.Name ?? string.Empty, project)));
        return options;
      }
    }

    private sealed class ReportRow
    {
      public ReportRow(Report report, string clientName, string projectName, int jobCount, double hours, decimal total, string currency)
      {
        Report = report;
        Name = report.Name;
        StartDate = report.StartDate;
        EndDate = report.EndDate;
        ClientName = clientName;
        ProjectName = projectName;
        JobCount = jobCount;
        Hours = hours;
        Total = total;
        Currency = currency;
      }

      public Report Report { get; }
      public string Name { get; }
      public DateTime StartDate { get; }
      public DateTime EndDate { get; }
      public string ClientName { get; }
      public string ProjectName { get; }
      public int JobCount { get; }
      public double Hours { get; }
      public decimal Total { get; }
      public string Currency { get; }
    }

    private sealed class InvoiceRow
    {
      public InvoiceRow(Invoice invoice, string clientName, double hoursTotal)
      {
        Invoice = invoice;
        InvoiceNumber = invoice.InvoiceNumber;
        ClientName = clientName;
        IssueDate = invoice.IssueDate;
        StartDate = invoice.StartDate;
        EndDate = invoice.EndDate;
        JobCount = invoice.WorkEntryIDs.Count;
        HoursTotal = hoursTotal;
        Total = invoice.Total;
        Currency = invoice.Currency;
      }

      public Invoice Invoice { get; }
      public string InvoiceNumber { get; }
      public string ClientName { get; }
      public DateTime IssueDate { get; }
      public DateTime StartDate { get; }
      public DateTime EndDate { get; }
      public int JobCount { get; }
      public double HoursTotal { get; }
      public decimal Total { get; }
      public string Currency { get; }
    }

    private class JobRow
    {
      public JobRow(WorkEntry workEntry)
      {
        StartTime = workEntry.StartTime;
        ClientName = workEntry.ClientName;
        ProjectName = workEntry.ProjectName;
        Description = workEntry.Description ?? string.Empty;
        Hours = workEntry.Duration.TotalHours;
        HourlyRate = workEntry.HourlyRate;
        Total = Convert.ToDecimal(workEntry.Duration.TotalHours) * workEntry.HourlyRate;
        Currency = workEntry.Currency;
      }

      public DateTime StartTime { get; }
      public string ClientName { get; }
      public string ProjectName { get; }
      public string Description { get; }
      public double Hours { get; }
      public decimal HourlyRate { get; }
      public decimal Total { get; }
      public string Currency { get; }
    }

    private sealed class InvoiceJobSelection : JobRow
    {
      public InvoiceJobSelection(WorkEntry workEntry) : base(workEntry)
      {
        WorkEntry = workEntry;
      }

      public bool IsIncluded { get; set; } = true;
      public WorkEntry WorkEntry { get; }
    }

    private sealed class LongRunningJobBehaviorOption
    {
      public LongRunningJobBehaviorOption(LongRunningJobBehavior value, string displayName)
      {
        Value = value;
        DisplayName = displayName;
      }

      public LongRunningJobBehavior Value { get; }
      public string DisplayName { get; }

      public static IReadOnlyList<LongRunningJobBehaviorOption> All { get; } = new[]
      {
        new LongRunningJobBehaviorOption(LongRunningJobBehavior.PromptAndContinue, "Prompt and continue"),
        new LongRunningJobBehaviorOption(LongRunningJobBehavior.PromptAndStop, "Prompt and stop"),
        new LongRunningJobBehaviorOption(LongRunningJobBehavior.RepeatReminder, "Repeat reminder")
      };
    }

    private sealed class LocaleDateConverter : IValueConverter
    {
      private readonly bool _includeTime;

      public LocaleDateConverter(bool includeTime)
      {
        _includeTime = includeTime;
      }

      public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
      {
        if (value is DateTime dateTime)
        {
          return dateTime.ToString(_includeTime ? "g" : "d", CultureInfo.CurrentCulture);
        }

        return string.Empty;
      }

      public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
      {
        return Binding.DoNothing;
      }
    }

    private sealed class ArchiveRow
    {
      public ArchiveRow(string type, string name, string clientName, string projectName, DateTime? itemDate, DateTime? dateArchived, TTDataObject item)
      {
        Type = type;
        Name = name;
        ClientName = clientName;
        ProjectName = projectName;
        ItemDate = itemDate;
        DateArchived = dateArchived;
        Item = item;
      }

      public string Type { get; }
      public string Name { get; }
      public string ClientName { get; }
      public string ProjectName { get; }
      public DateTime? ItemDate { get; }
      public DateTime? DateArchived { get; }
      public TTDataObject Item { get; }
    }

    private static Style CreateTimesheetRowStyle()
    {
      Style rowStyle = new(typeof(DataGridRow));
      rowStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));

      DataTrigger runningTrigger = new()
      {
        Binding = new Binding(nameof(WorkEntry.IsRunning)),
        Value = true
      };
      runningTrigger.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(223, 245, 223))));
      rowStyle.Triggers.Add(runningTrigger);

      return rowStyle;
    }

    private void TimesheetsGrid_Sorting(object sender, DataGridSortingEventArgs e)
    {
      e.Handled = true;

      if (sender is not DataGrid grid || e.Column.SortMemberPath == null)
      {
        return;
      }

      ListSortDirection direction = e.Column.SortDirection == ListSortDirection.Ascending
        ? ListSortDirection.Descending
        : ListSortDirection.Ascending;

      foreach (DataGridColumn column in grid.Columns)
      {
        column.SortDirection = null;
      }

      e.Column.SortDirection = direction;

      ICollectionView view = CollectionViewSource.GetDefaultView(grid.ItemsSource);
      view.SortDescriptions.Clear();
      view.SortDescriptions.Add(new SortDescription(e.Column.SortMemberPath, direction));

      if (e.Column.SortMemberPath == nameof(WorkEntry.ClientName) || e.Column.SortMemberPath == nameof(WorkEntry.ProjectName))
      {
        view.SortDescriptions.Add(new SortDescription(nameof(WorkEntry.StartTime), ListSortDirection.Descending));
      }

      view.Refresh();
    }

    private void TimesheetsGrid_RowEditEnding(object? sender, DataGridRowEditEndingEventArgs e)
    {
      Dispatcher.BeginInvoke(() =>
      {
        if (e.Row.Item is WorkEntry workEntry)
        {
          workEntry.Currency = TTAppSettings.NormalizeCurrency(workEntry.Currency);
          timeTracker.SaveChanges();
        }
      });
    }

    private void ShowMainContent(UIElement content)
    {
      mainContentGrid.Children.Clear();
      mainContentGrid.Children.Add(content);
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
      if (!timeTracker.HasRunningWork)
      {
        SaveWindowState();
        return;
      }

      MessageBoxResult result = MessageBox.Show(
        this,
        "A job is currently running. Are you sure you want to close Time Tracker?",
        "Running job",
        MessageBoxButton.YesNo,
        MessageBoxImage.Warning);

      if (result != MessageBoxResult.Yes)
      {
        e.Cancel = true;
        return;
      }

      SaveWindowState();
    }
  }
}
