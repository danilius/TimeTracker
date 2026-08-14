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

    /// <summary>
    /// Roll the log over once it passes this size, so a repeating fault cannot fill the disk.
    /// </summary>
    private const long MaximumLogBytes = 1024 * 1024;

    public void LogEvent(string message)
    {
      lock (_locker)
      {

        if (_settings != null && !string.IsNullOrEmpty(_settings.LogFilePath))
        {
          RollLogIfOversized(_settings.LogFilePath);

          // append the log message to the log file
          using StreamWriter sw = File.AppendText(_settings.LogFilePath);
          sw.WriteLine($"{DateTime.Now:dd-MM-yyyy HH:mm:ss} - {message}");
          sw.Close();
        }
      }
    }

    /// <summary>
    /// Keeps the current log and one previous generation. Logging must never throw, so any
    /// failure here is swallowed and the caller simply appends to the existing file.
    /// </summary>
    private static void RollLogIfOversized(string logFilePath)
    {
      try
      {
        FileInfo logFile = new(logFilePath);

        if (!logFile.Exists || logFile.Length < MaximumLogBytes)
        {
          return;
        }

        string previousPath = logFilePath + ".1";

        if (File.Exists(previousPath))
        {
          File.Delete(previousPath);
        }

        File.Move(logFilePath, previousPath);
      }
      catch (IOException)
      {
      }
      catch (UnauthorizedAccessException)
      {
      }
    }
  }
  
}
