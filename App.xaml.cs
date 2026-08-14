using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using TimeTracker.Services;
using TimeTracker.Utils;

namespace Time_Tracker
{
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App : Application
  {
    private const string SingleInstanceAppId = "TimeTracker";

    private TimeTracker.MainWindow? mainWindow;

    public App()
    {
      DispatcherUnhandledException += App_DispatcherUnhandledException;
      AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
      TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
      try
      {
        base.OnStartup(e);

        // The guard is claimed before the window is built and before the model opens the database,
        // so a second copy signals the first and dies without touching anything the owner holds.
        // This is also why App.xaml no longer carries a StartupUri: the window has to be created
        // here, after the check, rather than by the framework before OnStartup returns.
        if (!SingleInstanceGuard.TryAcquire(SingleInstanceAppId))
        {
          Shutdown();
          return;
        }

        SingleInstanceGuard.ActivationRequested += SingleInstanceGuard_ActivationRequested;

        mainWindow = new TimeTracker.MainWindow();
        MainWindow = mainWindow;
        mainWindow.Show();
      }
      catch (Exception ex)
      {
        ReportFatalException("Startup exception", ex);
        throw;
      }
    }

    /// <summary>
    /// Another copy of the application was started. The guard raises this on its own wait thread,
    /// so the work of showing a window has to be handed to the dispatcher.
    /// </summary>
    private void SingleInstanceGuard_ActivationRequested(object? sender, EventArgs e)
    {
      Dispatcher.BeginInvoke(new Action(() => mainWindow?.RestoreFromTray()));
    }

    protected override void OnExit(ExitEventArgs e)
    {
      SingleInstanceGuard.ActivationRequested -= SingleInstanceGuard_ActivationRequested;
      SingleInstanceGuard.Release();

      // Belt and braces: a normal exit disposes the icon on the way through Window_Closing, but an
      // exit that never reaches it would otherwise leave a ghost icon in the notification area.
      mainWindow?.DisposeTrayIcon();

      base.OnExit(e);
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
      ReportFatalException("Dispatcher exception", e.Exception);

      // Mark handled so WPF does not tear the process down before the crash
      // report is shown, then exit deliberately rather than continuing on with
      // whatever state the exception left behind.
      e.Handled = true;
      Shutdown(1);
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
      if (e.ExceptionObject is Exception exception)
      {
        ReportFatalException("Unhandled domain exception", exception);
      }
    }

    private static void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
      ReportFatalException("Unobserved task exception", e.Exception);
      e.SetObserved();
    }

    private static void ReportFatalException(string title, Exception exception)
    {
      string errorMessage = $"{title}: {exception}";
      string? crashLogPath = null;

      try
      {
        crashLogPath = WriteCrashLog(title, exception);
        Logger.Instance.LogEvent(errorMessage);
      }
      catch
      {
        // Avoid throwing from the exception handler itself.
      }

      try
      {
        MessageBox.Show(
          crashLogPath is null
            ? errorMessage
            : $"{errorMessage}{Environment.NewLine}{Environment.NewLine}Crash log: {crashLogPath}",
          "Time Tracker error",
          MessageBoxButton.OK,
          MessageBoxImage.Error);
      }
      catch
      {
        // The UI layer may not be available during early startup failures.
      }
    }

    private static string WriteCrashLog(string title, Exception exception)
    {
      string crashLogFolder = GetCrashLogFolder();
      Directory.CreateDirectory(crashLogFolder);

      string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
      string safeTitle = string.Join("-", title.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries))
        .Replace(' ', '-')
        .ToLowerInvariant();
      string crashLogPath = Path.Combine(crashLogFolder, $"{timestamp}-{safeTitle}.log");

      File.WriteAllText(
        crashLogPath,
        $"Time Tracker crash log{Environment.NewLine}" +
        $"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff zzz}{Environment.NewLine}" +
        $"Event: {title}{Environment.NewLine}" +
        $"Exception type: {exception.GetType().FullName}{Environment.NewLine}" +
        $"{Environment.NewLine}{exception}{Environment.NewLine}");

      return crashLogPath;
    }

    private static string GetCrashLogFolder()
    {
      return Path.Combine(GetAppDataFolder(), "crash logs");
    }

    private static string GetAppDataFolder()
    {
      return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TimeTrackerApp");
    }
  }
}
