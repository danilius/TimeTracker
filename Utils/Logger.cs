using System;
using System.IO;

namespace TimeTracker.Utils
{
  public sealed class Logger
  {
    private static Logger? _instance = null;
    private static readonly object _locker = new object();
    private static readonly object _objlocker = new object();

    private TTAppSettings? _settings;

    private Logger() { }

    public static Logger Instance
    {
      get
      {
        if (_instance == null)
        {
          lock (_objlocker)
          {
            if (_instance == null)
            {
              _instance = new Logger();
            }
          }
        }

        return _instance;
      }
    }

    // Method to initialize or set the parameter for the Logger
    public void Initialize(TTAppSettings settings)
    {
      _settings = settings;
    }

    public void LogEvent(string message)
    {
      lock (_locker)
      {

        if (_settings != null && !string.IsNullOrEmpty(_settings.LogFilePath))
        {
          // append the log message to the log file
          using StreamWriter sw = File.AppendText(_settings.LogFilePath);
          sw.WriteLine($"{DateTime.Now:dd-MM-yyyy HH:mm:ss} - {message}");
          sw.Close();
        }
      }
    }
  }
  
}
