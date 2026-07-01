using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
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
    private const string DashboardPage = "Dashboard";
    private const string JobsPage = "Jobs";
    private const string ClientsPage = "Clients";
    private const string ProjectsPage = "Projects";
    private const string TimesheetsPage = "Time sheets";
    private const string ReportsPage = "Reports";
    private const string InvoicesPage = "Invoices";
    private const string ArchivePage = "Archive";
    private const string SettingsPage = "Settings";
    private const double GridColumnBreathingSpace = 100;
    private const double GridColumnMinimumWidth = 80;
    private DashboardPeriodMode dashboardPeriodMode = DashboardPeriodMode.Week;
    private DateTime dashboardAnchorDate = DateTime.Today;
    private Popup? dashboardDatePopup;
    private readonly HashSet<string> autoSizedPages = new();
    private bool shouldRestoreMaximizedWindow;
    private bool isRestoringWindowState;
    private readonly System.Windows.Threading.DispatcherTimer jobsPageTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private TextBlock? jobsActiveTimerTextBlock;
    private static readonly Brush AccentBrush = BrushFromHex("#0090EE");
    private static readonly Brush PrimaryTextBrush = BrushFromHex("#17212B");
    private static readonly Brush SecondaryTextBrush = BrushFromHex("#64707D");
    private static readonly Brush PanelBorderBrush = BrushFromHex("#D9E0E7");
    private static readonly Brush SuccessBrush = BrushFromHex("#1F9D61");
    private static readonly Brush WarningBrush = BrushFromHex("#D98B16");

    public MainWindow()
    {
      InitializeComponent();

      timeTracker.RunningWorkChanged += TimeTracker_RunningWorkChanged;
      jobsPageTimer.Tick += JobsPageTimer_Tick;
      SourceInitialized += MainWindow_SourceInitialized;

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
            dialog.Duration,
            dialog.IsBillable);
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
      UpdateJobsTimerText();
    }

    private void JobsPageTimer_Tick(object? sender, EventArgs e)
    {
      UpdateJobsTimerText();
    }

    private void UpdateJobsTimerText()
    {
      if (jobsActiveTimerTextBlock != null && timeTracker.CurrentWorkEntry != null)
      {
        jobsActiveTimerTextBlock.Text = FormatDurationClock(timeTracker.CurrentWorkEntry.Duration);
      }
    }

    private void UpdateStartStopButton()
    {
      StartStopButton.Content = timeTracker.HasRunningWork ? "Stop" : "Start";
      StartStopButton.Background = timeTracker.HasRunningWork ? BrushFromHex("#D14343") : AccentBrush;
      StartStopButton.BorderBrush = StartStopButton.Background;
    }

    private void DashboardButton_Click(object sender, RoutedEventArgs e)
    {
      ShowDashboard();
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

      NavColumn.Width = new GridLength(isNavCollapsed ? 50 : 220);

      Visibility labelVisibility = isNavCollapsed ? Visibility.Collapsed : Visibility.Visible;
      AppTitleLabel.Visibility = labelVisibility;
      DashboardLabelButton.Visibility = labelVisibility;
      JobsLabelButton.Visibility = labelVisibility;
      ClientsLabelButton.Visibility = labelVisibility;
      ProjectsLabelButton.Visibility = labelVisibility;
      TimesheetsLabelButton.Visibility = labelVisibility;
      ReportsLabelButton.Visibility = labelVisibility;
      InvoicesLabelButton.Visibility = labelVisibility;
      ArchiveLabelButton.Visibility = labelVisibility;
      SettingsLabelButton.Visibility = labelVisibility;
    }

    private void UpdateSelectedNav(string page)
    {
      ResetNavButton(DashboardIconButton, DashboardLabelButton);
      ResetNavButton(JobsIconButton, JobsLabelButton);
      ResetNavButton(ClientsIconButton, ClientsLabelButton);
      ResetNavButton(ProjectsIconButton, ProjectsLabelButton);
      ResetNavButton(TimesheetsIconButton, TimesheetsLabelButton);
      ResetNavButton(ReportsIconButton, ReportsLabelButton);
      ResetNavButton(InvoicesIconButton, InvoicesLabelButton);
      ResetNavButton(ArchiveIconButton, ArchiveLabelButton);
      ResetNavButton(SettingsIconButton, SettingsLabelButton);

      switch (page)
      {
        case DashboardPage:
          SelectNavButton(DashboardIconButton, DashboardLabelButton);
          break;
        case JobsPage:
          SelectNavButton(JobsIconButton, JobsLabelButton);
          break;
        case ClientsPage:
          SelectNavButton(ClientsIconButton, ClientsLabelButton);
          break;
        case ProjectsPage:
          SelectNavButton(ProjectsIconButton, ProjectsLabelButton);
          break;
        case TimesheetsPage:
          SelectNavButton(TimesheetsIconButton, TimesheetsLabelButton);
          break;
        case ReportsPage:
          SelectNavButton(ReportsIconButton, ReportsLabelButton);
          break;
        case InvoicesPage:
          SelectNavButton(InvoicesIconButton, InvoicesLabelButton);
          break;
        case ArchivePage:
          SelectNavButton(ArchiveIconButton, ArchiveLabelButton);
          break;
        case SettingsPage:
          SelectNavButton(SettingsIconButton, SettingsLabelButton);
          break;
      }
    }

    private static void SelectNavButton(Button iconButton, Button labelButton)
    {
      Brush selectedBackground = BrushFromHex("#122232");
      iconButton.Background = selectedBackground;
      labelButton.Background = selectedBackground;
      iconButton.Foreground = Brushes.White;
      labelButton.Foreground = Brushes.White;
    }

    private static void ResetNavButton(Button iconButton, Button labelButton)
    {
      iconButton.Background = Brushes.Transparent;
      labelButton.Background = Brushes.Transparent;
      iconButton.Foreground = BrushFromHex("#C8D1DC");
      labelButton.Foreground = BrushFromHex("#C8D1DC");
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

      shouldRestoreMaximizedWindow = settings.IsWindowMaximized;
    }

    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
      RestoreMaximizedWindowState();
    }

    private void Window_ContentRendered(object? sender, EventArgs e)
    {
      RestoreMaximizedWindowState();
    }

    private void RestoreMaximizedWindowState()
    {
      if (!shouldRestoreMaximizedWindow || WindowState == WindowState.Maximized)
      {
        return;
      }

      isRestoringWindowState = true;
      WindowState = WindowState.Maximized;
      isRestoringWindowState = false;
    }

    private void SaveWindowState()
    {
      TTAppSettings settings = TTAppSettings.Instance;

      settings.IsWindowMaximized = WindowState == WindowState.Maximized;

      if (WindowState == WindowState.Normal)
      {
        settings.WindowLeft = Left;
        settings.WindowTop = Top;
        settings.WindowWidth = Width;
        settings.WindowHeight = Height;
      }
      else if (WindowState == WindowState.Maximized)
      {
        settings.WindowLeft = RestoreBounds.Left;
        settings.WindowTop = RestoreBounds.Top;
        settings.WindowWidth = RestoreBounds.Width;
        settings.WindowHeight = RestoreBounds.Height;
      }

      settings.IsNavCollapsed = isNavCollapsed;
      settings.Save();
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
      if (!isRestoringWindowState)
      {
        SaveWindowState();
      }
    }

    private void SaveCurrentPage(string page)
    {
      TTAppSettings.Instance.CurrentPage = page;
      TTAppSettings.Instance.Save();
      if (page != JobsPage)
      {
        jobsPageTimer.Stop();
        jobsActiveTimerTextBlock = null;
      }
      UpdateSelectedNav(page);
    }

    private void ShowSavedPage()
    {
      switch (TTAppSettings.Instance.CurrentPage)
      {
        case DashboardPage:
          ShowDashboard();
          break;

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
          ShowHome();
          break;

        default:
          ShowDashboard();
          break;
      }
    }

    private void ShowDashboard()
    {
      SaveCurrentPage(DashboardPage);

      DateTime periodStart = GetDashboardPeriodStart();
      DateTime periodEnd = GetDashboardPeriodEnd(periodStart);
      List<WorkEntry> periodEntries = timeTracker.WorkEntries
        .Where(workEntry => workEntry.StartTime.Date >= periodStart && workEntry.StartTime.Date <= periodEnd)
        .OrderByDescending(workEntry => workEntry.StartTime)
        .ToList();

      double totalHours = TimeTrackerModel.CalculateHoursTotal(periodEntries);
      decimal totalEarned = TimeTrackerModel.CalculateTotal(periodEntries);
      string currency = TimeTrackerModel.GetCurrencySummary(periodEntries);
      double averagePerWorkedDay = periodEntries
        .GroupBy(workEntry => workEntry.StartTime.Date)
        .Where(group => group.Sum(workEntry => workEntry.Duration.TotalHours) > 0)
        .DefaultIfEmpty()
        .Average(group => group?.Sum(workEntry => workEntry.Duration.TotalHours) ?? 0);

      HeaderLabel.Content = "Dashboard";
      HeaderSubLabel.Text = string.Empty;
      HeaderSubLabel.Visibility = Visibility.Collapsed;
      StartStopButton.Visibility = Visibility.Collapsed;
      HeaderCenterContent.Content = CreateDashboardPeriodToolbar(periodStart, periodEnd);

      Grid page = new();
      page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
      page.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
      page.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

      Grid metricsRow = CreateDashboardTwoColumnGrid(new Thickness(0, 0, 0, 16));
      UniformGrid leftMetrics = new()
      {
        Columns = 2,
        Margin = new Thickness(0)
      };
      leftMetrics.Children.Add(CreateMetricTile("Hours", $"{totalHours:0.#}h", "Logged this week", DashboardMetricIcon.Hours, AccentBrush, new Thickness(0, 0, 8, 0)));
      leftMetrics.Children.Add(CreateMetricTile("Earnings", $"{currency} {totalEarned:0.00}".Trim(), "Estimated from rates", DashboardMetricIcon.Earnings, SuccessBrush, new Thickness(8, 0, 0, 0)));
      metricsRow.Children.Add(leftMetrics);

      UniformGrid rightMetrics = new()
      {
        Columns = 2,
        Margin = new Thickness(16, 0, 0, 0)
      };
      rightMetrics.Children.Add(CreateMetricTile("Billable entries", periodEntries.Count.ToString(), "Work entries", DashboardMetricIcon.BillableEntries, WarningBrush, new Thickness(0, 0, 8, 0)));
      rightMetrics.Children.Add(CreateMetricTile("Avg/day", $"{averagePerWorkedDay:0.#}h", "Worked days", DashboardMetricIcon.AvgDay, BrushFromHex("#5E6AD2"), new Thickness(8, 0, 0, 0)));
      Grid.SetColumn(rightMetrics, 1);
      metricsRow.Children.Add(rightMetrics);
      page.Children.Add(metricsRow);

      Grid topPanels = CreateDashboardTwoColumnGrid(new Thickness(0, 0, 0, 16));
      Border dailyHoursPanel = CreateDailyHoursPanel(periodStart, periodEntries);
      topPanels.Children.Add(dailyHoursPanel);
      Border byClientPanel = CreateBreakdownPanel(periodEntries);
      byClientPanel.Margin = new Thickness(16, 0, 0, 0);
      Grid.SetColumn(byClientPanel, 1);
      topPanels.Children.Add(byClientPanel);
      Grid.SetRow(topPanels, 1);
      page.Children.Add(topPanels);

      Grid bottom = CreateDashboardTwoColumnGrid(new Thickness(0));
      bottom.Children.Add(CreateRecentWorkPanel(periodEntries));
      Border invoicePanel = CreateReadyToInvoicePanel(periodEntries);
      invoicePanel.Margin = new Thickness(16, 0, 0, 0);
      Grid.SetColumn(invoicePanel, 1);
      bottom.Children.Add(invoicePanel);
      Grid.SetRow(bottom, 2);
      page.Children.Add(bottom);

      ShowMainContent(page);
    }

    private static DateTime GetStartOfWeek(DateTime date, DayOfWeek weekStartsOn)
    {
      int diff = (7 + (date.DayOfWeek - weekStartsOn)) % 7;
      return date.Date.AddDays(-diff);
    }

    private DateTime GetDashboardPeriodStart()
    {
      return dashboardPeriodMode == DashboardPeriodMode.Week
        ? GetStartOfWeek(dashboardAnchorDate, TTAppSettings.Instance.WeekStartsOn)
        : new DateTime(dashboardAnchorDate.Year, dashboardAnchorDate.Month, 1);
    }

    private DateTime GetDashboardPeriodEnd(DateTime periodStart)
    {
      return dashboardPeriodMode == DashboardPeriodMode.Week
        ? periodStart.AddDays(6)
        : periodStart.AddMonths(1).AddDays(-1);
    }

    private void MoveDashboardPeriod(int direction)
    {
      dashboardAnchorDate = dashboardPeriodMode == DashboardPeriodMode.Week
        ? dashboardAnchorDate.AddDays(7 * direction)
        : dashboardAnchorDate.AddMonths(direction);
      ShowDashboard();
    }

    private void SetDashboardPeriodMode(DashboardPeriodMode periodMode)
    {
      dashboardPeriodMode = periodMode;
      ShowDashboard();
    }

    private static Grid CreateDashboardTwoColumnGrid(Thickness margin)
    {
      Grid grid = new()
      {
        Margin = margin
      };
      grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
      grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
      return grid;
    }

    private static Grid CreateMetricAndPanelCell(UniformGrid metrics, Border panel)
    {
      Grid cell = new();
      cell.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
      cell.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
      cell.Children.Add(metrics);
      Grid.SetRow(panel, 1);
      panel.VerticalAlignment = VerticalAlignment.Stretch;
      cell.Children.Add(panel);
      return cell;
    }

    private DockPanel CreateDashboardPeriodToolbar(DateTime periodStart, DateTime periodEnd)
    {
      DockPanel toolbar = new()
      {
        LastChildFill = false,
        HorizontalAlignment = HorizontalAlignment.Center
      };

      StackPanel rangeControls = new()
      {
        Orientation = Orientation.Horizontal,
        VerticalAlignment = VerticalAlignment.Center
      };
      rangeControls.Children.Add(CreateIconControlButton("\uE76B", (_, _) => MoveDashboardPeriod(-1)));
      Button dateButton = CreateDashboardDateButton(periodStart, periodEnd);
      rangeControls.Children.Add(dateButton);
      rangeControls.Children.Add(CreateIconControlButton("\uE76C", (_, _) => MoveDashboardPeriod(1)));

      StackPanel periodButtons = new()
      {
        Orientation = Orientation.Horizontal,
        HorizontalAlignment = HorizontalAlignment.Right,
        Margin = new Thickness(14, 0, 0, 0)
      };
      periodButtons.Children.Add(CreateSegmentButton("Week", dashboardPeriodMode == DashboardPeriodMode.Week, (_, _) => SetDashboardPeriodMode(DashboardPeriodMode.Week)));
      periodButtons.Children.Add(CreateSegmentButton("Month", dashboardPeriodMode == DashboardPeriodMode.Month, (_, _) => SetDashboardPeriodMode(DashboardPeriodMode.Month)));
      DockPanel.SetDock(periodButtons, Dock.Right);

      toolbar.Children.Add(periodButtons);
      toolbar.Children.Add(rangeControls);
      return toolbar;
    }

    private Button CreateDashboardDateButton(DateTime periodStart, DateTime periodEnd)
    {
      Button button = new()
      {
        Content = $"{FormatDate(periodStart)} - {FormatDate(periodEnd)}",
        Background = Brushes.White,
        BorderBrush = PanelBorderBrush,
        BorderThickness = new Thickness(1),
        Foreground = PrimaryTextBrush,
        FontSize = 14,
        FontWeight = FontWeights.SemiBold,
        Height = 38,
        MinWidth = 210,
        Margin = new Thickness(6, 0, 6, 0),
        Padding = new Thickness(18, 0, 18, 0)
      };
      button.Click += (_, _) => ToggleDashboardDatePopup(button);
      return button;
    }

    private void ToggleDashboardDatePopup(Button placementTarget)
    {
      if (dashboardDatePopup?.IsOpen == true)
      {
        dashboardDatePopup.IsOpen = false;
        return;
      }

      System.Windows.Controls.Calendar calendar = new()
      {
        SelectedDate = dashboardAnchorDate,
        DisplayDate = dashboardAnchorDate,
        Margin = new Thickness(8)
      };
      calendar.SelectedDatesChanged += (_, _) =>
      {
        if (calendar.SelectedDate == null)
        {
          return;
        }

        dashboardAnchorDate = calendar.SelectedDate.Value.Date;
        if (dashboardDatePopup != null)
        {
          dashboardDatePopup.IsOpen = false;
        }
        ShowDashboard();
      };

      Border popupChrome = new()
      {
        Background = Brushes.White,
        BorderBrush = PanelBorderBrush,
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(8),
        Child = calendar
      };

      dashboardDatePopup = new Popup
      {
        PlacementTarget = placementTarget,
        Placement = PlacementMode.Bottom,
        StaysOpen = false,
        AllowsTransparency = true,
        Child = popupChrome
      };
      dashboardDatePopup.IsOpen = true;
    }

    private static Button CreateIconControlButton(string icon, RoutedEventHandler handler)
    {
      Button button = new()
      {
        Content = icon,
        Margin = new Thickness(0)
      };
      ApplyStyle(button, "IconButtonStyle");
      button.Click += handler;
      return button;
    }

    private static Border CreateMetricTile(string label, string value, string note, DashboardMetricIcon icon, Brush accent, Thickness margin)
    {
      Border tile = CreatePanelBorder(margin);
      Grid content = new();
      content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
      content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

      FrameworkElement iconElement = CreateDashboardMetricIcon(icon);
      iconElement.Margin = new Thickness(0, 4, 14, 0);
      content.Children.Add(iconElement);

      StackPanel textContent = new();
      textContent.Children.Add(new TextBlock
      {
        Text = label,
        Foreground = SecondaryTextBrush,
        FontSize = 12,
        FontWeight = FontWeights.SemiBold
      });
      textContent.Children.Add(new TextBlock
      {
        Text = value,
        Foreground = PrimaryTextBrush,
        FontSize = 26,
        FontWeight = FontWeights.SemiBold,
        Margin = new Thickness(0, 6, 0, 2)
      });
      textContent.Children.Add(new TextBlock
      {
        Text = note,
        Foreground = SecondaryTextBrush,
        FontSize = 12
      });
      textContent.Children.Add(new Border
      {
        Height = 3,
        Width = 42,
        Background = accent,
        CornerRadius = new CornerRadius(2),
        HorizontalAlignment = HorizontalAlignment.Left,
        Margin = new Thickness(0, 12, 0, 0)
      });
      Grid.SetColumn(textContent, 1);
      content.Children.Add(textContent);
      tile.Child = content;
      return tile;
    }

    private static FrameworkElement CreateDashboardMetricIcon(DashboardMetricIcon icon)
    {
      Brush stroke = BrushFromHex("#0079E7");
      Canvas canvas = icon == DashboardMetricIcon.BillableEntries
        ? CreateIconCanvas(10.258305, 13.202387)
        : icon == DashboardMetricIcon.AvgDay
          ? CreateIconCanvas(13.4324, 13.202396)
          : CreateIconCanvas(13.294396, 13.202396);

      switch (icon)
      {
        case DashboardMetricIcon.Hours:
          canvas.Children.Add(CreateIconEllipse(6.647198, 6.601193, 6.2396092, 6.1936069, 0.815179, stroke));
          canvas.Children.Add(CreateIconPath("m 35.097412,61.607805 v 3.553137 l 2.17954,1.651326", -28.612854, -58.559749, 0.868627, stroke));
          break;

        case DashboardMetricIcon.Earnings:
          canvas.Children.Add(CreateIconEllipse(6.647192, 6.601193, 6.2396092, 6.1936069, 0.815179, stroke));
          TextBlock poundText = new()
          {
            Text = "£",
            Foreground = stroke,
            FontFamily = new FontFamily("Georgia"),
            FontWeight = FontWeights.Bold,
            FontSize = 8.55,
            LineHeight = 8.55
          };
          canvas.Children.Add(poundText);
          Canvas.SetLeft(poundText, 4.16);
          Canvas.SetTop(poundText, 1.55);
          break;

        case DashboardMetricIcon.BillableEntries:
          canvas.Children.Add(CreateIconRectangle(0.40391, 0.403908, 9.4504986, 12.394573, 0.807816, stroke));
          canvas.Children.Add(CreateIconPath("m 171.45446,62.527306 h 4.81412", -168.68702, -58.559753, 0.868627, stroke));
          canvas.Children.Add(CreateIconPath("m 171.45446,65.173138 h 4.81412", -168.68702, -58.559753, 0.868627, stroke));
          canvas.Children.Add(CreateIconPath("m 171.45446,67.818969 h 3.16077", -168.68702, -58.559753, 0.868625, stroke));
          break;

        case DashboardMetricIcon.AvgDay:
          canvas.Children.Add(CreateIconEllipse(6.71619, 6.601193, 6.3086104, 6.1936069, 0.815179, stroke));
          canvas.Children.Add(CreateIconPath("m 240.12082,65.868959 h 1.91914 l 1.20353,-3.25279 1.66781,5.920076 1.2597,-3.67565 0.61803,0.878253 h 2.69982", -238.19511, -58.559749, 0.868627, stroke));
          break;
      }

      return new Viewbox
      {
        Width = 42,
        Height = 42,
        Stretch = Stretch.Uniform,
        Child = canvas
      };
    }

    private static Canvas CreateIconCanvas(double width, double height)
    {
      return new Canvas
      {
        Width = width,
        Height = height
      };
    }

    private static System.Windows.Shapes.Ellipse CreateIconEllipse(double centerX, double centerY, double radiusX, double radiusY, double strokeThickness, Brush stroke)
    {
      System.Windows.Shapes.Ellipse ellipse = new()
      {
        Width = radiusX * 2,
        Height = radiusY * 2,
        Fill = Brushes.Transparent,
        Stroke = stroke,
        StrokeThickness = strokeThickness
      };
      Canvas.SetLeft(ellipse, centerX - radiusX);
      Canvas.SetTop(ellipse, centerY - radiusY);
      return ellipse;
    }

    private static System.Windows.Shapes.Rectangle CreateIconRectangle(double x, double y, double width, double height, double strokeThickness, Brush stroke)
    {
      System.Windows.Shapes.Rectangle rectangle = new()
      {
        Width = width,
        Height = height,
        Fill = Brushes.Transparent,
        Stroke = stroke,
        StrokeThickness = strokeThickness,
        StrokeLineJoin = PenLineJoin.Round
      };
      Canvas.SetLeft(rectangle, x);
      Canvas.SetTop(rectangle, y);
      return rectangle;
    }

    private static System.Windows.Shapes.Path CreateIconPath(string data, double translateX, double translateY, double strokeThickness, Brush stroke)
    {
      return new System.Windows.Shapes.Path
      {
        Data = Geometry.Parse(data),
        Fill = Brushes.Transparent,
        Stroke = stroke,
        StrokeThickness = strokeThickness,
        StrokeStartLineCap = PenLineCap.Round,
        StrokeEndLineCap = PenLineCap.Round,
        StrokeLineJoin = PenLineJoin.Round,
        RenderTransform = new TranslateTransform(translateX, translateY)
      };
    }

    private static Border CreateDailyHoursPanel(DateTime periodStart, List<WorkEntry> entries)
    {
      Border panel = CreatePanelBorder();
      StackPanel content = new();
      content.Children.Add(CreateSectionTitle("Daily hours", "Hours logged by day"));

      UniformGrid chart = new()
      {
        Columns = 7,
        Margin = new Thickness(0, 18, 0, 0),
        MinHeight = 190
      };

      List<double> dayTotals = Enumerable.Range(0, 7)
        .Select(offset => entries.Where(entry => entry.StartTime.Date == periodStart.AddDays(offset)).Sum(entry => entry.Duration.TotalHours))
        .ToList();
      double maxHours = Math.Max(1, dayTotals.Max());

      for (int i = 0; i < 7; i++)
      {
        DateTime day = periodStart.AddDays(i);
        double hours = dayTotals[i];
        StackPanel dayPanel = new()
        {
          Margin = new Thickness(4, 0, 4, 0),
          VerticalAlignment = VerticalAlignment.Bottom
        };
        dayPanel.Children.Add(new TextBlock
        {
          Text = $"{hours:0.#}h",
          HorizontalAlignment = HorizontalAlignment.Center,
          Foreground = SecondaryTextBrush,
          FontSize = 11,
          Margin = new Thickness(0, 0, 0, 6)
        });
        dayPanel.Children.Add(new Border
        {
          Height = Math.Max(8, 120 * hours / maxHours),
          Background = hours > 0 ? AccentBrush : BrushFromHex("#E7ECF1"),
          CornerRadius = new CornerRadius(5, 5, 2, 2),
          VerticalAlignment = VerticalAlignment.Bottom
        });
        dayPanel.Children.Add(new TextBlock
        {
          Text = day.ToString("ddd", CultureInfo.CurrentCulture),
          HorizontalAlignment = HorizontalAlignment.Center,
          Foreground = SecondaryTextBrush,
          FontSize = 12,
          FontWeight = FontWeights.SemiBold,
          Margin = new Thickness(0, 8, 0, 0)
        });
        chart.Children.Add(dayPanel);
      }

      content.Children.Add(chart);
      panel.Child = content;
      return panel;
    }

    private static Border CreateBreakdownPanel(List<WorkEntry> entries)
    {
      Border panel = CreatePanelBorder();
      StackPanel content = new();
      content.Children.Add(CreateSectionTitle("By client", "Top work this period"));

      List<IGrouping<string, WorkEntry>> groups = entries
        .GroupBy(entry => string.IsNullOrWhiteSpace(entry.ClientName) ? "No client" : entry.ClientName)
        .OrderByDescending(group => group.Sum(entry => entry.Duration.TotalHours))
        .Take(5)
        .ToList();
      double maxHours = Math.Max(1, groups.Select(group => group.Sum(entry => entry.Duration.TotalHours)).DefaultIfEmpty(0).Max());

      if (groups.Count == 0)
      {
        content.Children.Add(CreateEmptyText("No work logged in this period yet."));
      }
      else
      {
        foreach (IGrouping<string, WorkEntry> group in groups)
        {
          double hours = group.Sum(entry => entry.Duration.TotalHours);
          decimal total = TimeTrackerModel.CalculateTotal(group);
          string currency = TimeTrackerModel.GetCurrencySummary(group);
          content.Children.Add(CreateBreakdownRow(group.Key, $"{hours:0.#}h", $"{currency} {total:0.00}".Trim(), hours / maxHours, GetClientBrush(group.Key)));
        }
      }

      panel.Child = content;
      return panel;
    }

    private static Border CreateRecentWorkPanel(List<WorkEntry> entries)
    {
      Border panel = CreatePanelBorder();
      StackPanel content = new();
      content.Children.Add(CreateSectionTitle("Recent work", "Latest entries in this period"));

      List<WorkEntry> recentEntries = entries.Take(6).ToList();
      if (recentEntries.Count == 0)
      {
        content.Children.Add(CreateEmptyText("Start a job to see recent work here."));
      }
      else
      {
        content.Children.Add(CreateRecentWorkHeader());
        foreach (WorkEntry entry in recentEntries)
        {
          content.Children.Add(CreateRecentWorkRow(entry));
        }
      }

      panel.Child = content;
      return panel;
    }

    private static Border CreateReadyToInvoicePanel(List<WorkEntry> entries)
    {
      Border panel = CreatePanelBorder();
      StackPanel content = new();
      content.Children.Add(CreateSectionTitle("Ready to invoice", "Estimated unbilled value"));

      List<IGrouping<string, WorkEntry>> groups = entries
        .GroupBy(entry => $"{(string.IsNullOrWhiteSpace(entry.ClientName) ? "No client" : entry.ClientName)}|{(string.IsNullOrWhiteSpace(entry.ProjectName) ? "No project" : entry.ProjectName)}")
        .OrderByDescending(group => TimeTrackerModel.CalculateTotal(group))
        .Take(6)
        .ToList();

      if (groups.Count == 0)
      {
        content.Children.Add(CreateEmptyText("No invoice candidates in this period."));
      }
      else
      {
        content.Children.Add(CreateInvoiceHeader());
        foreach (IGrouping<string, WorkEntry> group in groups)
        {
          string[] parts = group.Key.Split('|');
          string clientName = parts.Length > 0 ? parts[0] : "No client";
          string projectName = parts.Length > 1 ? parts[1] : "No project";
          content.Children.Add(CreateInvoiceCandidateRow(
            clientName,
            projectName,
            $"{group.Sum(entry => entry.Duration.TotalHours):0.#}h",
            $"{TimeTrackerModel.GetCurrencySummary(group)} {TimeTrackerModel.CalculateTotal(group):0.00}".Trim(),
            GetClientBrush(clientName)));
        }
      }

      panel.Child = content;
      return panel;
    }

    private static TextBlock CreateSectionTitle(string title, string subtitle)
    {
      TextBlock text = new()
      {
        Foreground = PrimaryTextBrush,
        FontSize = 17,
        FontWeight = FontWeights.SemiBold,
        Text = title
      };
      text.Inlines.Add(new LineBreak());
      text.Inlines.Add(new Run(subtitle)
      {
        Foreground = SecondaryTextBrush,
        FontSize = 12,
        FontWeight = FontWeights.Normal
      });
      return text;
    }

    private static UIElement CreateBreakdownRow(string label, string hours, string total, double ratio, Brush clientBrush)
    {
      StackPanel row = new()
      {
        Margin = new Thickness(0, 14, 0, 0)
      };
      DockPanel line = new();
      TextBlock labelText = new()
      {
        Text = label,
        FontWeight = FontWeights.SemiBold,
        Foreground = PrimaryTextBrush
      };
      TextBlock valueText = new()
      {
        Text = $"{hours}  {total}",
        Foreground = SecondaryTextBrush,
        HorizontalAlignment = HorizontalAlignment.Right
      };
      DockPanel.SetDock(valueText, Dock.Right);
      line.Children.Add(valueText);
      line.Children.Add(labelText);
      row.Children.Add(line);
      Grid barTrack = new()
      {
        Height = 8,
        Margin = new Thickness(0, 7, 0, 0)
      };
      barTrack.Children.Add(new Border
      {
        Background = BrushFromHex("#EEF2F6"),
        CornerRadius = new CornerRadius(4)
      });
      barTrack.Children.Add(new Border
      {
        Width = Math.Max(24, 260 * ratio),
        Background = clientBrush,
        CornerRadius = new CornerRadius(4),
        HorizontalAlignment = HorizontalAlignment.Left
      });
      row.Children.Add(barTrack);
      return row;
    }

    private static UIElement CreateRecentWorkRow(WorkEntry entry)
    {
      Grid row = CreateRecentWorkGridRow();
      row.Margin = new Thickness(0, 10, 0, 0);
      row.Children.Add(CreateMiniCell(entry.StartTime.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture), false, HorizontalAlignment.Left));
      TextBlock client = CreateMiniCell(entry.ClientName, true, HorizontalAlignment.Left);
      Grid.SetColumn(client, 1);
      row.Children.Add(client);
      TextBlock project = CreateMiniCell(entry.ProjectName, false, HorizontalAlignment.Left);
      Grid.SetColumn(project, 2);
      row.Children.Add(project);
      TextBlock task = CreateMiniCell(entry.Description ?? string.Empty, false, HorizontalAlignment.Left);
      Grid.SetColumn(task, 3);
      row.Children.Add(task);
      TextBlock duration = CreateMiniCell($"{entry.Duration.TotalHours:0.#}h", true, HorizontalAlignment.Right);
      Grid.SetColumn(duration, 4);
      row.Children.Add(duration);
      return row;
    }

    private static UIElement CreateInvoiceCandidateRow(string client, string project, string hours, string total, Brush clientBrush)
    {
      Grid row = CreateInvoiceGridRow();
      row.Margin = new Thickness(0, 12, 0, 0);

      DockPanel clientCell = new();
      clientCell.Children.Add(new Border
      {
        Width = 9,
        Height = 9,
        Background = clientBrush,
        CornerRadius = new CornerRadius(5),
        Margin = new Thickness(0, 5, 8, 0),
        VerticalAlignment = VerticalAlignment.Top
      });
      clientCell.Children.Add(CreateMiniCell(client, true, HorizontalAlignment.Left));
      row.Children.Add(clientCell);

      TextBlock projectCell = CreateMiniCell(project, false, HorizontalAlignment.Left);
      Grid.SetColumn(projectCell, 1);
      row.Children.Add(projectCell);
      TextBlock hoursText = CreateMiniCell(hours, false, HorizontalAlignment.Left);
      Grid.SetColumn(hoursText, 2);
      row.Children.Add(hoursText);
      TextBlock totalText = CreateMiniCell(total, true, HorizontalAlignment.Right);
      totalText.Foreground = SuccessBrush;
      Grid.SetColumn(totalText, 3);
      row.Children.Add(totalText);
      return row;
    }

    private static UIElement CreateRecentWorkHeader()
    {
      Grid header = CreateRecentWorkGridRow();
      header.Margin = new Thickness(0, 16, 0, 2);
      AddHeaderCell(header, "Date", 0, HorizontalAlignment.Left);
      AddHeaderCell(header, "Client", 1, HorizontalAlignment.Left);
      AddHeaderCell(header, "Project", 2, HorizontalAlignment.Left);
      AddHeaderCell(header, "Task", 3, HorizontalAlignment.Left);
      AddHeaderCell(header, "Hours", 4, HorizontalAlignment.Right);
      return header;
    }

    private static UIElement CreateInvoiceHeader()
    {
      Grid header = CreateInvoiceGridRow();
      header.Margin = new Thickness(0, 16, 0, 2);
      AddHeaderCell(header, "Client", 0, HorizontalAlignment.Left);
      AddHeaderCell(header, "Project", 1, HorizontalAlignment.Left);
      AddHeaderCell(header, "Uninvoiced hours", 2, HorizontalAlignment.Left);
      AddHeaderCell(header, "Uninvoiced amount", 3, HorizontalAlignment.Right);
      return header;
    }

    private static Grid CreateRecentWorkGridRow()
    {
      Grid grid = new();
      grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.9, GridUnitType.Star) });
      grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) });
      grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });
      grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
      grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.7, GridUnitType.Star) });
      return grid;
    }

    private static Grid CreateInvoiceGridRow()
    {
      Grid grid = new();
      grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });
      grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) });
      grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) });
      grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });
      return grid;
    }

    private static void AddHeaderCell(Grid grid, string text, int column, HorizontalAlignment alignment)
    {
      TextBlock cell = CreateMiniGridHeaderText(text, alignment);
      Grid.SetColumn(cell, column);
      grid.Children.Add(cell);
    }

    private static TextBlock CreateMiniCell(string text, bool isStrong, HorizontalAlignment alignment)
    {
      return new TextBlock
      {
        Text = text,
        FontWeight = isStrong ? FontWeights.SemiBold : FontWeights.Normal,
        Foreground = isStrong ? PrimaryTextBrush : SecondaryTextBrush,
        HorizontalAlignment = alignment,
        VerticalAlignment = VerticalAlignment.Center,
        TextTrimming = TextTrimming.CharacterEllipsis
      };
    }

    private static TextBlock CreateMiniGridHeaderText(string text, HorizontalAlignment alignment)
    {
      return new TextBlock
      {
        Text = text,
        Foreground = SecondaryTextBrush,
        FontSize = 11,
        FontWeight = FontWeights.SemiBold,
        HorizontalAlignment = alignment
      };
    }

    private static Button CreateSegmentButton(string text, bool isSelected, RoutedEventHandler handler)
    {
      Button button = new()
      {
        Content = text,
        Height = 30,
        MinWidth = 72,
        Margin = new Thickness(0, 0, 6, 0),
        Background = isSelected ? AccentBrush : Brushes.White,
        Foreground = isSelected ? Brushes.White : PrimaryTextBrush,
        BorderBrush = isSelected ? AccentBrush : PanelBorderBrush,
        BorderThickness = new Thickness(1),
        FontWeight = FontWeights.SemiBold
      };
      button.Click += handler;
      return button;
    }

    private static TextBlock CreateEmptyText(string text)
    {
      return new TextBlock
      {
        Text = text,
        Foreground = SecondaryTextBrush,
        Margin = new Thickness(0, 18, 0, 0),
        TextWrapping = TextWrapping.Wrap
      };
    }

    private static Border CreatePanelBorder()
    {
      return CreatePanelBorder(new Thickness(0));
    }

    private static Border CreatePanelBorder(Thickness margin)
    {
      return new Border
      {
        Background = Brushes.White,
        BorderBrush = PanelBorderBrush,
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(8),
        Padding = new Thickness(16),
        Margin = margin
      };
    }

    private void ShowHome()
    {
      HeaderLabel.Content = "Jobs";
      HeaderSubLabel.Visibility = Visibility.Visible;
      HeaderSubLabel.Text = string.Empty;
      HeaderSubLabel.Visibility = Visibility.Collapsed;
      StartStopButton.Visibility = Visibility.Collapsed;
      HeaderCenterContent.Content = CreateJobsHeaderToolbar();
      SaveCurrentPage(JobsPage);
      if (timeTracker.HasRunningWork)
      {
        jobsPageTimer.Start();
      }
      else
      {
        jobsPageTimer.Stop();
        jobsActiveTimerTextBlock = null;
      }

      Grid page = new();
      page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
      page.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

      Grid topRow = new()
      {
        Margin = new Thickness(0, 0, 0, 16)
      };
      topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });
      topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
      topRow.Children.Add(CreateActiveTimerPanel());
      Border quickStartPanel = CreateQuickStartPanel();
      quickStartPanel.Margin = new Thickness(16, 0, 0, 0);
      Grid.SetColumn(quickStartPanel, 1);
      topRow.Children.Add(quickStartPanel);
      page.Children.Add(topRow);

      Border recentJobsPanel = CreateRecentJobsPanel();
      Grid.SetRow(recentJobsPanel, 1);
      page.Children.Add(recentJobsPanel);

      ShowMainContent(page);
    }

    private StackPanel CreateJobsHeaderToolbar()
    {
      StackPanel toolbar = new()
      {
        Orientation = Orientation.Horizontal,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center
      };

      TextBox searchBox = new()
      {
        Text = "Search jobs",
        Width = 260,
        Height = 38,
        Padding = new Thickness(12, 8, 12, 0),
        Foreground = SecondaryTextBrush,
        BorderBrush = PanelBorderBrush,
        BorderThickness = new Thickness(1),
        Margin = new Thickness(0, 0, 12, 0)
      };
      toolbar.Children.Add(searchBox);
      toolbar.Children.Add(CreateJobsIconButton("\uE71C", (_, _) => { }));

      Button newJobButton = new()
      {
        Content = "+  New job",
        Height = 38,
        MinWidth = 128,
        Margin = new Thickness(12, 0, 0, 0)
      };
      ApplyStyle(newJobButton, "PrimaryButtonStyle");
      newJobButton.Click += (_, _) => NewJobFromJobs(startNow: false);
      toolbar.Children.Add(newJobButton);

      return toolbar;
    }

    private Border CreateActiveTimerPanel()
    {
      Border panel = CreatePanelBorder();
      WorkEntry? currentWorkEntry = timeTracker.CurrentWorkEntry;
      StackPanel content = new();
      content.Children.Add(CreateStatusTitle("Active timer", timeTracker.HasRunningWork ? SuccessBrush : SecondaryTextBrush));

      if (currentWorkEntry == null)
      {
        jobsActiveTimerTextBlock = null;
        content.Children.Add(new TextBlock
        {
          Text = "No active timer",
          FontSize = 32,
          FontWeight = FontWeights.SemiBold,
          Foreground = PrimaryTextBrush,
          Margin = new Thickness(0, 18, 0, 8)
        });
        content.Children.Add(new TextBlock
        {
          Text = "Start a quick project or create a new job to begin tracking.",
          Foreground = SecondaryTextBrush,
          TextWrapping = TextWrapping.Wrap
        });
        StackPanel emptyActions = new()
        {
          Orientation = Orientation.Horizontal,
          Margin = new Thickness(0, 22, 0, 0)
        };
        Button newJobButton = CreatePanelActionButton("New job", true, (_, _) => NewJobFromJobs(startNow: false));
        emptyActions.Children.Add(newJobButton);
        content.Children.Add(emptyActions);
        panel.Child = content;
        return panel;
      }

      jobsActiveTimerTextBlock = new TextBlock
      {
        Text = FormatDurationClock(currentWorkEntry.Duration),
        FontSize = 42,
        FontWeight = FontWeights.SemiBold,
        Foreground = PrimaryTextBrush,
        Margin = new Thickness(0, 18, 0, 14)
      };
      content.Children.Add(jobsActiveTimerTextBlock);
      content.Children.Add(new TextBlock
      {
        Text = currentWorkEntry.ClientName,
        FontSize = 16,
        FontWeight = FontWeights.SemiBold,
        Foreground = PrimaryTextBrush
      });
      content.Children.Add(new TextBlock
      {
        Text = currentWorkEntry.ProjectName,
        Foreground = PrimaryTextBrush,
        Margin = new Thickness(0, 4, 0, 0)
      });
      content.Children.Add(new TextBlock
      {
        Text = currentWorkEntry.Description ?? string.Empty,
        Foreground = SecondaryTextBrush,
        Margin = new Thickness(0, 4, 0, 14),
        TextTrimming = TextTrimming.CharacterEllipsis
      });
      content.Children.Add(CreateBillableLine(currentWorkEntry));

      StackPanel actions = new()
      {
        Orientation = Orientation.Horizontal,
        Margin = new Thickness(0, 20, 0, 0)
      };
      actions.Children.Add(CreatePanelActionButton("Stop", true, (_, _) =>
      {
        timeTracker.StopWork();
        ShowHome();
      }, "DangerButtonStyle"));
      actions.Children.Add(CreatePanelActionButton("\uE769  Pause", false, (_, _) => { }, isEnabled: false));
      content.Children.Add(actions);

      panel.Child = content;
      return panel;
    }

    private Border CreateQuickStartPanel()
    {
      Border panel = CreatePanelBorder();
      StackPanel content = new();
      content.Children.Add(CreateSectionTitle("Quick start", "Recent projects"));

      List<Project> projects = timeTracker.Projects
        .OrderByDescending(project => project.WorkEntries.Select(entry => entry.StartTime).DefaultIfEmpty(DateTime.MinValue).Max())
        .Take(4)
        .ToList();

      if (projects.Count == 0)
      {
        content.Children.Add(CreateEmptyText("Create a project to enable quick start."));
      }
      else
      {
        foreach (Project project in projects)
        {
          content.Children.Add(CreateQuickStartRow(project));
        }
      }

      panel.Child = content;
      return panel;
    }

    private Border CreateRecentJobsPanel()
    {
      Border panel = CreatePanelBorder();
      panel.Padding = new Thickness(0);
      DockPanel content = new();
      Border titleArea = new()
      {
        Padding = new Thickness(16, 14, 16, 10),
        BorderBrush = PanelBorderBrush,
        BorderThickness = new Thickness(0, 0, 0, 1),
        Child = new TextBlock
        {
          Text = "Recent jobs",
          FontSize = 17,
          FontWeight = FontWeights.SemiBold,
          Foreground = PrimaryTextBrush
        }
      };
      DockPanel.SetDock(titleArea, Dock.Top);
      content.Children.Add(titleArea);

      StackPanel table = new();
      table.Children.Add(CreateJobsTableHeader());

      List<WorkEntry> jobs = timeTracker.WorkEntries
        .OrderByDescending(workEntry => workEntry.IsRunning)
        .ThenByDescending(workEntry => workEntry.StartTime)
        .Take(8)
        .ToList();

      if (jobs.Count == 0)
      {
        table.Children.Add(CreateEmptyText("No jobs yet. Create a new job to start tracking."));
      }
      else
      {
        foreach (WorkEntry job in jobs)
        {
          table.Children.Add(CreateJobTableRow(job));
        }
      }

      ScrollViewer scrollViewer = new()
      {
        Content = table,
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto
      };
      content.Children.Add(scrollViewer);
      panel.Child = content;
      return panel;
    }

    private UIElement CreateQuickStartRow(Project project)
    {
      DockPanel row = new()
      {
        Margin = new Thickness(0, 16, 0, 0)
      };

      Button startButton = CreatePanelActionButton("Start", false, (_, _) => StartProjectFromJobs(project));
      DockPanel.SetDock(startButton, Dock.Right);
      row.Children.Add(startButton);

      Border colorChip = new()
      {
        Width = 14,
        Height = 14,
        Background = GetClientBrush(project.Client?.Name ?? project.Name ?? string.Empty),
        CornerRadius = new CornerRadius(3),
        Margin = new Thickness(0, 4, 14, 0),
        VerticalAlignment = VerticalAlignment.Top
      };
      DockPanel.SetDock(colorChip, Dock.Left);
      row.Children.Add(colorChip);

      StackPanel text = new();
      text.Children.Add(new TextBlock
      {
        Text = project.Name ?? string.Empty,
        FontWeight = FontWeights.SemiBold,
        Foreground = PrimaryTextBrush,
        TextTrimming = TextTrimming.CharacterEllipsis
      });
      text.Children.Add(new TextBlock
      {
        Text = project.Client?.Name ?? string.Empty,
        Foreground = SecondaryTextBrush,
        Margin = new Thickness(0, 3, 0, 0),
        TextTrimming = TextTrimming.CharacterEllipsis
      });
      row.Children.Add(text);
      return row;
    }

    private static Grid CreateJobsTableHeader()
    {
      Grid header = CreateJobsTableGrid();
      header.Background = BrushFromHex("#F8FAFC");
      header.Children.Add(CreateJobsHeaderCell("Client", 0));
      header.Children.Add(CreateJobsHeaderCell("Project", 1));
      header.Children.Add(CreateJobsHeaderCell("Last entry", 2));
      header.Children.Add(CreateJobsHeaderCell("Duration", 3));
      header.Children.Add(CreateJobsHeaderCell("Rate", 4));
      header.Children.Add(CreateJobsHeaderCell("Status", 5));
      header.Children.Add(CreateJobsHeaderCell("Actions", 6));
      return header;
    }

    private UIElement CreateJobTableRow(WorkEntry job)
    {
      Grid row = CreateJobsTableGrid();
      row.MinHeight = 56;
      row.Background = Brushes.White;

      row.Children.Add(CreateClientCell(job));
      row.Children.Add(CreateJobsTextCell(job.ProjectName, 1, true));
      row.Children.Add(CreateJobsTextCell(job.StartTime.ToString("dd/MM/yyyy " + TTAppSettings.Instance.ShortTimePattern, CultureInfo.CurrentCulture), 2, false));
      row.Children.Add(CreateJobsTextCell(FormatDurationShort(job.Duration), 3, true));
      row.Children.Add(CreateJobsTextCell($"{job.Currency}{job.HourlyRate:0.##}/hr", 4, false));
      row.Children.Add(CreateStatusBadge(job.IsRunning ? "Running" : job.Duration > TimeSpan.Zero ? "Ready" : "Draft", 5, job.IsRunning));
      row.Children.Add(CreateJobActionsCell(job));
      row.Children.Add(new Border
      {
        Height = 1,
        Background = BrushFromHex("#E7ECF1"),
        VerticalAlignment = VerticalAlignment.Bottom
      });
      Grid.SetColumnSpan(row.Children[^1], 7);
      return row;
    }

    private static Grid CreateJobsTableGrid()
    {
      Grid grid = new();
      grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) });
      grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.45, GridUnitType.Star) });
      grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.25, GridUnitType.Star) });
      grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.85, GridUnitType.Star) });
      grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.8, GridUnitType.Star) });
      grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.9, GridUnitType.Star) });
      grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.05, GridUnitType.Star) });
      return grid;
    }

    private static TextBlock CreateJobsHeaderCell(string text, int column)
    {
      TextBlock cell = new()
      {
        Text = text,
        Foreground = PrimaryTextBrush,
        FontWeight = FontWeights.SemiBold,
        Padding = new Thickness(16, 12, 8, 12)
      };
      Grid.SetColumn(cell, column);
      return cell;
    }

    private UIElement CreateClientCell(WorkEntry job)
    {
      DockPanel cell = new()
      {
        Margin = new Thickness(16, 10, 8, 10)
      };
      Border avatar = new()
      {
        Width = 26,
        Height = 26,
        Background = GetClientBrush(job.ClientName),
        CornerRadius = new CornerRadius(4),
        Margin = new Thickness(0, 0, 10, 0),
        Child = new TextBlock
        {
          Text = GetInitial(job.ClientName),
          Foreground = Brushes.White,
          FontWeight = FontWeights.SemiBold,
          HorizontalAlignment = HorizontalAlignment.Center,
          VerticalAlignment = VerticalAlignment.Center
        }
      };
      DockPanel.SetDock(avatar, Dock.Left);
      cell.Children.Add(avatar);
      cell.Children.Add(new TextBlock
      {
        Text = job.ClientName,
        Foreground = PrimaryTextBrush,
        FontWeight = FontWeights.SemiBold,
        VerticalAlignment = VerticalAlignment.Center,
        TextTrimming = TextTrimming.CharacterEllipsis
      });
      return cell;
    }

    private static TextBlock CreateJobsTextCell(string text, int column, bool isStrong)
    {
      TextBlock cell = new()
      {
        Text = text,
        Foreground = isStrong ? PrimaryTextBrush : SecondaryTextBrush,
        FontWeight = isStrong ? FontWeights.SemiBold : FontWeights.Normal,
        Padding = new Thickness(16, 0, 8, 0),
        VerticalAlignment = VerticalAlignment.Center,
        TextTrimming = TextTrimming.CharacterEllipsis
      };
      Grid.SetColumn(cell, column);
      return cell;
    }

    private static Border CreateStatusBadge(string text, int column, bool isRunning)
    {
      Brush statusBrush = isRunning ? SuccessBrush : text == "Ready" ? AccentBrush : SecondaryTextBrush;
      Border badge = new()
      {
        Background = WithOpacity(statusBrush, 0.12),
        BorderBrush = WithOpacity(statusBrush, 0.45),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(5),
        Padding = new Thickness(10, 3, 10, 4),
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(16, 0, 8, 0),
        Child = new TextBlock
        {
          Text = text,
          Foreground = statusBrush,
          FontWeight = FontWeights.SemiBold,
          FontSize = 12
        }
      };
      Grid.SetColumn(badge, column);
      return badge;
    }

    private UIElement CreateJobActionsCell(WorkEntry job)
    {
      StackPanel actions = new()
      {
        Orientation = Orientation.Horizontal,
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(12, 0, 0, 0)
      };
      actions.Children.Add(CreateSmallActionButton("\uE768", (_, _) =>
      {
        timeTracker.StartNewWorkEntryBasedOn(job);
        ShowHome();
      }));
      actions.Children.Add(CreateSmallActionButton("\uE70F", (_, _) =>
      {
        EditWorkEntry(job);
        ShowHome();
      }));
      Grid.SetColumn(actions, 6);
      return actions;
    }

    private Button CreateJobsIconButton(string icon, RoutedEventHandler handler)
    {
      Button button = new()
      {
        Content = icon,
        Margin = new Thickness(0, 0, 8, 0)
      };
      ApplyStyle(button, "IconButtonStyle");
      button.Click += handler;
      return button;
    }

    private static Button CreateSmallActionButton(string icon, RoutedEventHandler handler)
    {
      Button button = new()
      {
        Content = icon,
        Margin = new Thickness(0, 0, 4, 0),
      };
      ApplyStyle(button, "FlatIconButtonStyle");
      button.Click += handler;
      return button;
    }

    private Button CreatePanelActionButton(string text, bool isPrimary, RoutedEventHandler handler, string? styleKey = null, bool isEnabled = true)
    {
      Button button = new()
      {
        Content = text,
        Height = 34,
        MinWidth = 86,
        Margin = new Thickness(0, 0, 10, 0),
        IsEnabled = isEnabled
      };
      ApplyStyle(button, styleKey ?? (isPrimary ? "PrimaryButtonStyle" : "SecondaryButtonStyle"));
      button.Click += handler;
      return button;
    }

    private static TextBlock CreateStatusTitle(string title, Brush dotBrush)
    {
      TextBlock text = new()
      {
        FontSize = 17,
        FontWeight = FontWeights.SemiBold,
        Foreground = PrimaryTextBrush
      };
      text.Inlines.Add(new Run("\u25CF")
      {
        Foreground = dotBrush,
        FontSize = 14
      });
      text.Inlines.Add(new Run($"  {title}"));
      return text;
    }

    private static UIElement CreateBillableLine(WorkEntry workEntry)
    {
      StackPanel line = new()
      {
        Orientation = Orientation.Horizontal,
        Margin = new Thickness(0, 2, 0, 0)
      };
      line.Children.Add(new Border
      {
        Width = 16,
        Height = 16,
        Background = workEntry.IsBillable ? SuccessBrush : SecondaryTextBrush,
        CornerRadius = new CornerRadius(3),
        Margin = new Thickness(0, 0, 8, 0),
        Child = new TextBlock
        {
          Text = workEntry.IsBillable ? "\uE73E" : "\uE711",
          FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
          FontSize = 10,
          Foreground = Brushes.White,
          HorizontalAlignment = HorizontalAlignment.Center,
          VerticalAlignment = VerticalAlignment.Center
        }
      });
      line.Children.Add(new TextBlock
      {
        Text = workEntry.IsBillable
          ? $"Billable  {workEntry.Currency}{workEntry.HourlyRate:0.##}/hr"
          : "Non-billable",
        Foreground = PrimaryTextBrush,
        VerticalAlignment = VerticalAlignment.Center
      });
      return line;
    }

    private void NewJobFromJobs(bool startNow)
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
          dialog.Duration,
          dialog.IsBillable);

        if (startNow || dialog.StartTimerNow)
        {
          timeTracker.StartWork(workEntry);
        }

        ShowHome();
      }
    }

    private void StartProjectFromJobs(Project project)
    {
      decimal clientRate = project.Client?.DefaultHourlyRate ?? 0m;
      decimal rate = project.Rate > 0
        ? Convert.ToDecimal(project.Rate)
        : (clientRate > 0 ? clientRate : TTAppSettings.Instance.DefaultHourlyRate);

      WorkEntry workEntry = timeTracker.CreateWorkEntry(
        project,
        DateTime.Now,
        project.Description,
        rate,
        string.IsNullOrWhiteSpace(project.Client?.DefaultCurrency) ? TTAppSettings.Instance.DefaultCurrency : project.Client.DefaultCurrency);
      timeTracker.StartWork(workEntry);
      ShowHome();
    }

    private static string FormatDurationClock(TimeSpan duration)
    {
      return duration.TotalHours >= 100
        ? $"{(int)duration.TotalHours:000}:{duration.Minutes:00}:{duration.Seconds:00}"
        : $"{(int)duration.TotalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}";
    }

    private static string FormatDurationShort(TimeSpan duration)
    {
      int hours = (int)duration.TotalHours;
      return $"{hours}:{duration.Minutes:00}";
    }

    private static string GetInitial(string value)
    {
      return string.IsNullOrWhiteSpace(value) ? "?" : value.Trim()[0].ToString().ToUpperInvariant();
    }

    private void ShowClients()
    {
      HeaderLabel.Content = "Clients";
      HeaderSubLabel.Visibility = Visibility.Visible;
      HeaderSubLabel.Text = $"{timeTracker.ActiveClients.Count} active clients";
      StartStopButton.Visibility = Visibility.Visible;
      HeaderCenterContent.Content = null;
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
      AutoSizeGridColumnsOnFirstPageOpen(ClientsPage, grid);

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
      HeaderSubLabel.Visibility = Visibility.Visible;
      HeaderSubLabel.Text = $"{timeTracker.Projects.Count} active projects";
      StartStopButton.Visibility = Visibility.Visible;
      HeaderCenterContent.Content = null;
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
      AutoSizeGridColumnsOnFirstPageOpen(ProjectsPage, grid);

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
      HeaderSubLabel.Visibility = Visibility.Visible;
      HeaderSubLabel.Text = $"{timeTracker.WorkEntries.Count} active entries";
      StartStopButton.Visibility = Visibility.Visible;
      HeaderCenterContent.Content = null;
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
      AutoSizeGridColumnsOnFirstPageOpen(TimesheetsPage, grid);

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
      HeaderSubLabel.Visibility = Visibility.Visible;
      HeaderSubLabel.Text = $"{timeTracker.Reports.Count} saved reports";
      StartStopButton.Visibility = Visibility.Visible;
      HeaderCenterContent.Content = null;
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
      AutoSizeGridColumnsOnFirstPageOpen(ReportsPage, grid);

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
      HeaderSubLabel.Visibility = Visibility.Visible;
      HeaderSubLabel.Text = $"{timeTracker.Invoices.Count} saved invoices";
      StartStopButton.Visibility = Visibility.Visible;
      HeaderCenterContent.Content = null;
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
      AutoSizeGridColumnsOnFirstPageOpen(InvoicesPage, grid);

      DockPanel page = CreateListPage(new[]
      {
        CreateToolbarButton("New", (_, _) => NewInvoice()),
        CreateToolbarButton("Print", (_, _) =>
        {
          if (grid.SelectedItem is InvoiceRow row)
          {
            PrintInvoice(row.Invoice);
          }
        }),
        CreateToolbarButton("Word", (_, _) =>
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
      return dateTime.ToString("dd/MM/yyyy " + TTAppSettings.Instance.ShortTimePattern, CultureInfo.CurrentCulture);
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
        report == null ? "New report" : "Edit report",
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
      ApplyStyle(cancelButton, "SecondaryButtonStyle");
      ApplyStyle(createButton, "PrimaryButtonStyle");
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
      HeaderSubLabel.Visibility = Visibility.Visible;
      HeaderSubLabel.Text = $"{timeTracker.ArchivedItems.Count} archived items";
      StartStopButton.Visibility = Visibility.Visible;
      HeaderCenterContent.Content = null;
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
        WorkEntry workEntry = timeTracker.CreateWorkEntry(
          dialog.ClientName,
          dialog.ProjectName,
          dialog.StartTime,
          dialog.Description,
          dialog.HourlyRate,
          dialog.Currency,
          dialog.Duration,
          dialog.IsBillable);
        if (dialog.StartTimerNow)
        {
          timeTracker.StartWork(workEntry);
        }
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
          dialog.Duration,
          dialog.IsBillable);
      }
    }

    private void ShowSettings()
    {
      HeaderLabel.Content = "Settings";
      HeaderSubLabel.Visibility = Visibility.Visible;
      HeaderSubLabel.Text = "Defaults, templates, and timer behavior";
      StartStopButton.Visibility = Visibility.Visible;
      HeaderCenterContent.Content = null;
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

      panel.Children.Add(new TextBlock
      {
        Text = "Display",
        FontSize = 22,
        FontWeight = FontWeights.SemiBold,
        Margin = new Thickness(0, 10, 0, 15)
      });

      ComboBox timeFormatComboBox = new()
      {
        ItemsSource = TimeDisplayFormatOption.All,
        SelectedItem = TimeDisplayFormatOption.All.First(option => option.Value == settings.TimeDisplayFormat),
        DisplayMemberPath = nameof(TimeDisplayFormatOption.DisplayName),
        Margin = new Thickness(0, 0, 0, 12),
        Width = 260,
        HorizontalAlignment = HorizontalAlignment.Left
      };
      panel.Children.Add(CreateSettingsLabel("Time format"));
      panel.Children.Add(timeFormatComboBox);
      timeFormatComboBox.SelectionChanged += (_, _) =>
      {
        settings.TimeDisplayFormat = ((TimeDisplayFormatOption)timeFormatComboBox.SelectedItem).Value;
        settings.Save();
      };

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
      DataGrid grid = new()
      {
        AutoGenerateColumns = false,
        CanUserAddRows = false,
        IsReadOnly = true,
        AlternationCount = 2,
        Margin = new Thickness(0)
      };
      ApplyStyle(grid, "DenseDataGridStyle");
      grid.CellStyle = CreateCenteredDataGridCellStyle();
      return grid;
    }

    private static Style CreateCenteredDataGridCellStyle()
    {
      Style style = new(typeof(DataGridCell));
      style.Setters.Add(new Setter(Control.VerticalContentAlignmentProperty, VerticalAlignment.Center));
      style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(8, 0, 8, 0)));
      style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
      return style;
    }

    private void AutoSizeGridColumnsOnFirstPageOpen(string page, DataGrid grid)
    {
      if (!autoSizedPages.Add(page))
      {
        return;
      }

      grid.Loaded += (_, _) =>
      {
        grid.Dispatcher.BeginInvoke(
          () => AutoSizeGridColumnsWithBreathingSpace(grid),
          DispatcherPriority.Loaded);
      };
    }

    private void AutoSizeGridColumnsWithBreathingSpace(DataGrid grid)
    {
      if (grid.Columns.Count == 0 || grid.ActualWidth <= 0)
      {
        return;
      }

      double availableWidth = Math.Max(0, grid.ActualWidth - SystemParameters.VerticalScrollBarWidth - 2);
      if (availableWidth <= 0)
      {
        return;
      }

      List<double> widths = grid.Columns
        .Select(column => Math.Max(GridColumnMinimumWidth, MeasureGridColumnContent(grid, column) + GridColumnBreathingSpace))
        .ToList();

      double totalWidth = widths.Sum();
      while (totalWidth > availableWidth)
      {
        int widestIndex = 0;
        double widestWidth = widths[0];

        for (int index = 1; index < widths.Count; index++)
        {
          if (widths[index] > widestWidth)
          {
            widestIndex = index;
            widestWidth = widths[index];
          }
        }

        double reducibleWidth = widestWidth - GridColumnMinimumWidth;
        if (reducibleWidth <= 0)
        {
          break;
        }

        double reduction = Math.Min(reducibleWidth, totalWidth - availableWidth);
        widths[widestIndex] -= reduction;
        totalWidth -= reduction;
      }

      for (int index = 0; index < grid.Columns.Count; index++)
      {
        grid.Columns[index].Width = new DataGridLength(widths[index]);
      }
    }

    private static double MeasureGridColumnContent(DataGrid grid, DataGridColumn column)
    {
      double maxWidth = MeasureGridText(grid, column.Header?.ToString() ?? string.Empty);

      if (column is not DataGridTextColumn textColumn || textColumn.Binding is not Binding binding)
      {
        return maxWidth;
      }

      foreach (object item in grid.Items)
      {
        if (item == CollectionView.NewItemPlaceholder)
        {
          continue;
        }

        string text = FormatGridCellValue(item, binding);
        maxWidth = Math.Max(maxWidth, MeasureGridText(grid, text));
      }

      return maxWidth;
    }

    private static double MeasureGridText(DataGrid grid, string text)
    {
      FormattedText formattedText = new(
        text,
        CultureInfo.CurrentCulture,
        FlowDirection.LeftToRight,
        new Typeface(grid.FontFamily, grid.FontStyle, grid.FontWeight, grid.FontStretch),
        grid.FontSize,
        Brushes.Black,
        VisualTreeHelper.GetDpi(grid).PixelsPerDip);

      return Math.Ceiling(formattedText.WidthIncludingTrailingWhitespace);
    }

    private static string FormatGridCellValue(object item, Binding binding)
    {
      object? value = ResolveBindingPathValue(item, binding.Path?.Path);

      if (binding.Converter != null)
      {
        value = binding.Converter.Convert(
          value,
          typeof(string),
          binding.ConverterParameter,
          binding.ConverterCulture ?? CultureInfo.CurrentCulture);
      }

      if (value == null)
      {
        return string.Empty;
      }

      if (!string.IsNullOrEmpty(binding.StringFormat))
      {
        return ApplyBindingStringFormat(value, binding.StringFormat);
      }

      return Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;
    }

    private static string ApplyBindingStringFormat(object value, string stringFormat)
    {
      if (stringFormat.Contains("{0", StringComparison.Ordinal))
      {
        return string.Format(CultureInfo.CurrentCulture, stringFormat, value);
      }

      return value is IFormattable formattable
        ? formattable.ToString(stringFormat, CultureInfo.CurrentCulture)
        : Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;
    }

    private static object? ResolveBindingPathValue(object source, string? path)
    {
      if (string.IsNullOrWhiteSpace(path))
      {
        return source;
      }

      object? value = source;
      foreach (string memberName in path.Split('.'))
      {
        if (value == null)
        {
          return null;
        }

        PropertyInfo? property = value.GetType().GetProperty(memberName);
        value = property?.GetValue(value);
      }

      return value;
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
      DockPanel page = new()
      {
        LastChildFill = true
      };

      StackPanel toolbar = new()
      {
        Orientation = Orientation.Horizontal,
        Margin = new Thickness(0, 0, 0, 14),
        HorizontalAlignment = HorizontalAlignment.Right
      };
      foreach (Button button in buttons)
      {
        toolbar.Children.Add(button);
      }

      Border contentPanel = CreatePanelBorder();
      contentPanel.Padding = new Thickness(0);
      contentPanel.Child = content;

      DockPanel.SetDock(toolbar, Dock.Top);
      page.Children.Add(toolbar);
      page.Children.Add(contentPanel);

      return page;
    }

    private static Button CreateToolbarButton(string text, RoutedEventHandler handler)
    {
      Button button = new()
      {
        Content = text,
        MinWidth = 90,
        Height = 32,
        Margin = new Thickness(0, 0, 8, 0)
      };
      ApplyStyle(button, text.StartsWith("New", StringComparison.OrdinalIgnoreCase) ? "PrimaryButtonStyle" : "ToolbarButtonStyle");
      button.Click += handler;

      return button;
    }

    private static void ApplyStyle(FrameworkElement element, string resourceKey)
    {
      object? resource = Application.Current.MainWindow?.TryFindResource(resourceKey)
        ?? Application.Current.TryFindResource(resourceKey);

      if (resource is Style style)
      {
        element.Style = style;
      }
    }

    private static SolidColorBrush BrushFromHex(string hex)
    {
      return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
    }

    private static SolidColorBrush WithOpacity(Brush brush, double opacity)
    {
      Color color = brush is SolidColorBrush solidColorBrush ? solidColorBrush.Color : Colors.Transparent;
      return new SolidColorBrush(Color.FromArgb((byte)(255 * opacity), color.R, color.G, color.B));
    }

    private static Brush GetClientBrush(string clientName)
    {
      string[] palette =
      {
        "#0090EE",
        "#1F9D61",
        "#D98B16",
        "#5E6AD2",
        "#D14343",
        "#0EA5A3",
        "#8E5CF7",
        "#C47F00",
        "#2F80ED",
        "#7A9A01"
      };

      int hash = 17;
      foreach (char character in clientName)
      {
        hash = (hash * 31) + character;
      }

      return BrushFromHex(palette[Math.Abs(hash) % palette.Length]);
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
        client == null ? "New client" : "Edit client",
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
        project == null ? "New project" : "Edit project",
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
        Width = 520,
        SizeToContent = SizeToContent.Height
      };

      Grid shell = new();
      shell.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
      shell.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
      shell.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

      Border header = new()
      {
        BorderBrush = PanelBorderBrush,
        BorderThickness = new Thickness(0, 0, 0, 1),
        Padding = new Thickness(24, 24, 24, 22),
        Child = new TextBlock
        {
          Text = title,
          FontSize = 23,
          FontWeight = FontWeights.SemiBold,
          Foreground = PrimaryTextBrush
        }
      };
      Grid.SetRow(header, 0);
      shell.Children.Add(header);

      StackPanel form = new()
      {
        Margin = new Thickness(24, 22, 24, 14)
      };

      foreach ((string label, Control input) in fields)
      {
        form.Children.Add(new TextBlock
        {
          Text = label,
          Foreground = PrimaryTextBrush,
          FontSize = 14,
          FontWeight = FontWeights.SemiBold,
          Margin = new Thickness(0, 0, 0, 4)
        });

        PrepareDialogInput(input);
        form.Children.Add(input);
      }
      Grid.SetRow(form, 1);
      shell.Children.Add(form);

      Border footer = new()
      {
        BorderBrush = PanelBorderBrush,
        BorderThickness = new Thickness(0, 1, 0, 0),
        Padding = new Thickness(24, 16, 24, 18)
      };
      Grid buttons = new();
      buttons.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
      buttons.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
      buttons.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
      Button cancelButton = new() { Content = "Cancel", MinWidth = 104, Height = 36, IsCancel = true };
      Button saveButton = new() { Content = "Save", MinWidth = 104, Height = 36, IsDefault = true };
      ApplyStyle(cancelButton, "SecondaryButtonStyle");
      ApplyStyle(saveButton, "PrimaryButtonStyle");
      saveButton.Click += (_, _) => dialog.DialogResult = true;
      buttons.Children.Add(cancelButton);
      Grid.SetColumn(saveButton, 2);
      buttons.Children.Add(saveButton);
      footer.Child = buttons;
      Grid.SetRow(footer, 2);
      shell.Children.Add(footer);

      dialog.Content = shell;

      return dialog.ShowDialog() == true;
    }

    private static void PrepareDialogInput(Control input)
    {
      input.Margin = new Thickness(0, 0, 0, 14);
      input.HorizontalAlignment = HorizontalAlignment.Stretch;

      if (input is CheckBox checkBox)
      {
        checkBox.Margin = new Thickness(0, 4, 0, 14);
        checkBox.VerticalAlignment = VerticalAlignment.Center;
        return;
      }

      input.Width = double.NaN;

      if (input is TextBox textBox)
      {
        textBox.Height = 42;
        textBox.Padding = new Thickness(12, 0, 12, 0);
        textBox.VerticalContentAlignment = VerticalAlignment.Center;
      }
      else if (input is ComboBox comboBox)
      {
        comboBox.Height = 42;
        comboBox.Padding = new Thickness(10, 0, 10, 0);
        comboBox.VerticalContentAlignment = VerticalAlignment.Center;
      }
      else if (input is DatePicker datePicker)
      {
        datePicker.Height = 42;
      }
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

    private sealed class TimeDisplayFormatOption
    {
      public TimeDisplayFormatOption(TimeDisplayFormat value, string displayName)
      {
        Value = value;
        DisplayName = displayName;
      }

      public TimeDisplayFormat Value { get; }
      public string DisplayName { get; }

      public static IReadOnlyList<TimeDisplayFormatOption> All { get; } = new[]
      {
        new TimeDisplayFormatOption(TimeDisplayFormat.TwentyFourHour, "24-hour (14:30)"),
        new TimeDisplayFormatOption(TimeDisplayFormat.TwelveHour, "12-hour (2:30 PM)")
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

  public enum DashboardPeriodMode
  {
    Week,
    Month
  }

  public enum DashboardMetricIcon
  {
    Hours,
    Earnings,
    BillableEntries,
    AvgDay
  }
}
