using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TimeTracker.Utils
{
  public enum LongRunningJobBehavior
  {
    PromptAndContinue,
    PromptAndStop,
    RepeatReminder
  }

  public enum TimeDisplayFormat
  {
    TwentyFourHour,
    TwelveHour
  }

  public class TTAppSettings
  {
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
      NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
    };

    private static TTAppSettings? _instance = null;
    private static readonly object _locker = new object();
    private static readonly object _objlocker = new object();

    public static TTAppSettings Instance
    {
      get
      {
        if (_instance == null)
        {
          lock (_objlocker)
          {
            if (_instance == null)
            {
              _instance = Load();
            }
          }
        }

        return _instance;
      }
    }

    private int _timerInterval = 1;
    public int TimerInterval
    {
      get { return _timerInterval; }
      set { _timerInterval = value; }
    }

    private int _longRunningJobThresholdMinutes = 60;
    public int LongRunningJobThresholdMinutes
    {
      get { return _longRunningJobThresholdMinutes; }
      set { _longRunningJobThresholdMinutes = value; }
    }

    private LongRunningJobBehavior _longRunningJobBehavior = LongRunningJobBehavior.PromptAndContinue;
    public LongRunningJobBehavior LongRunningJobBehavior
    {
      get { return _longRunningJobBehavior; }
      set { _longRunningJobBehavior = value; }
    }

    private int _longRunningJobReminderMinutes = 15;
    public int LongRunningJobReminderMinutes
    {
      get { return _longRunningJobReminderMinutes; }
      set { _longRunningJobReminderMinutes = value; }
    }

    private decimal _defaultHourlyRate = 0;
    public decimal DefaultHourlyRate
    {
      get { return _defaultHourlyRate; }
      set { _defaultHourlyRate = value; }
    }

    private string _defaultCurrency = RegionInfo.CurrentRegion.CurrencySymbol;
    public string DefaultCurrency
    {
      get { return _defaultCurrency; }
      set { _defaultCurrency = value; }
    }

    private string _currentPage = "Dashboard";
    public string CurrentPage
    {
      get { return _currentPage; }
      set { _currentPage = value; }
    }

    private DayOfWeek _weekStartsOn = DayOfWeek.Monday;
    public DayOfWeek WeekStartsOn
    {
      get { return _weekStartsOn; }
      set { _weekStartsOn = value; }
    }

    private TimeDisplayFormat _timeDisplayFormat = TimeDisplayFormat.TwentyFourHour;
    public TimeDisplayFormat TimeDisplayFormat
    {
      get { return _timeDisplayFormat; }
      set { _timeDisplayFormat = value; }
    }

    /// <summary>
    /// The .NET custom format string for rendering a time of day, based on
    /// <see cref="TimeDisplayFormat" /> (24-hour "HH:mm" or 12-hour "h:mm tt").
    /// </summary>
    [JsonIgnore]
    public string ShortTimePattern
    {
      get { return _timeDisplayFormat == TimeDisplayFormat.TwelveHour ? "h:mm tt" : "HH:mm"; }
    }

    private bool _isNavCollapsed;
    public bool IsNavCollapsed
    {
      get { return _isNavCollapsed; }
      set { _isNavCollapsed = value; }
    }

    private double _windowLeft = double.NaN;
    public double WindowLeft
    {
      get { return _windowLeft; }
      set { _windowLeft = value; }
    }

    private double _windowTop = double.NaN;
    public double WindowTop
    {
      get { return _windowTop; }
      set { _windowTop = value; }
    }

    private double _windowWidth = 800;
    public double WindowWidth
    {
      get { return _windowWidth; }
      set { _windowWidth = value; }
    }

    private double _windowHeight = 450;
    public double WindowHeight
    {
      get { return _windowHeight; }
      set { _windowHeight = value; }
    }

    private bool _isWindowMaximized = false;
    public bool IsWindowMaximized
    {
      get { return _isWindowMaximized; }
      set { _isWindowMaximized = value; }
    }

    public static string NormalizeCurrency(string currency)
    {
      string trimmedCurrency = currency.Trim();
      bool isAsciiCurrencyCode = true;

      foreach (char character in trimmedCurrency)
      {
        if (!((character >= 'A' && character <= 'Z') || (character >= 'a' && character <= 'z')))
        {
          isAsciiCurrencyCode = false;
          break;
        }
      }

      return isAsciiCurrencyCode
        ? trimmedCurrency.ToUpperInvariant()
        : trimmedCurrency;
    }

    private string _username = "";
    public string Username
    {
      get { return _username; }
      set { _username = value; }
    }

    private string _password = "";
    public string Password
    {
      get { return _password; }
      set { _password = value; }
    }

    private string _logFilePath = "";
    public string LogFilePath
    {
      get { return _logFilePath; }
      set { _logFilePath = value; }
    }

    private string _lastInvoiceFolder = "";
    public string LastInvoiceFolder
    {
      get { return _lastInvoiceFolder; }
      set { _lastInvoiceFolder = value; }
    }

    private string _lastReportFolder = "";
    public string LastReportFolder
    {
      get { return _lastReportFolder; }
      set { _lastReportFolder = value; }
    }

    private string _wordTemplatePath = "";
    public string WordTemplatePath
    {
      get { return _wordTemplatePath; }
      set { _wordTemplatePath = value; }
    }

    private string _reportTemplatePath = "";
    public string ReportTemplatePath
    {
      get { return _reportTemplatePath; }
      set { _reportTemplatePath = value; }
    }

    public static TTAppSettings Load()
    {
      // get the settings file path
      string roamingFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

      // get the settings folder from the settings file path
      string settingsFolder = Path.Combine(roamingFolder, "TimeTrackerApp");

      // create the settings folder if it doesn't exist
      if (!Directory.Exists(settingsFolder))
      {
        Directory.CreateDirectory(settingsFolder);
      }

      // if the settings file exists, load it
      if (File.Exists(Path.Combine(settingsFolder, "TimeTrackerAppSettings.json")))
      {
        string json = File.ReadAllText(Path.Combine(settingsFolder, "TimeTrackerAppSettings.json"));

        // if json is not null, deserialize it into the _settings object
        if (!string.IsNullOrEmpty(json))
        {
          var settings = JsonSerializer.Deserialize<TTAppSettings>(json, JsonOptions);

          if (settings != null)
          {
            return settings;
          }
        }
      }
      // otherwise just return a new settings object
      return new TTAppSettings();
    }

    public void Save()
    {
      // get the settings file path
      string roamingFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

      // get the settings folder from the settings file path
      string settingsFolder = Path.Combine(roamingFolder, "TimeTrackerApp");

      // create the settings folder if it doesn't exist
      if (!Directory.Exists(settingsFolder))
      {
        Directory.CreateDirectory(settingsFolder);
      }

      // serialize the _settings object into json
      string json = JsonSerializer.Serialize(this, JsonOptions);

      // write the json to the settings file
      File.WriteAllText(Path.Combine(settingsFolder, "TimeTrackerAppSettings.json"), json);
    }
  }
}
