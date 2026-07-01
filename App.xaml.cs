using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using TimeTracker.Utils;

namespace Time_Tracker
{
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App : Application
  {
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
      }
      catch (Exception ex)
      {
        ReportFatalException("Startup exception", ex);
        throw;
      }
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
      ReportFatalException("Dispatcher exception", e.Exception);
      e.Handled = false;
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

      try
      {
        Directory.CreateDirectory(GetAppDataFolder());
        File.AppendAllText(GetStartupErrorLogPath(), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {errorMessage}{Environment.NewLine}{Environment.NewLine}");
        Logger.Instance.LogEvent(errorMessage);
      }
      catch
      {
        // Avoid throwing from the exception handler itself.
      }

      try
      {
        MessageBox.Show(
          errorMessage,
          "Time Tracker startup error",
          MessageBoxButton.OK,
          MessageBoxImage.Error);
      }
      catch
      {
        // The UI layer may not be available during early startup failures.
      }
    }

    private static string GetStartupErrorLogPath()
    {
      return Path.Combine(GetAppDataFolder(), "startup-errors.log");
    }

    private static string GetAppDataFolder()
    {
      return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TimeTrackerApp");
    }
  }
}
