using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TimeTracker.Controls;
using TimeTracker.Models;
using TimeTracker.Utils;

namespace TimeTracker.Dialogs
{
  /// <summary>
  /// Interaction logic for NewWorkEntryDialog.xaml
  /// </summary>
  public partial class NewWorkEntryDialog : Window
  {
    public enum DialogMode
    {
      ManualEntry,
      JobTimer
    }

    private bool _isEditingExistingEntry;
    private readonly DialogMode _mode;
    private bool _isSyncingDuration;

    public NewWorkEntryDialog(DialogMode mode = DialogMode.ManualEntry)
    {
      _mode = mode;

      InitializeComponent();

      LoadCombos();
      LoadBillingDefaults();
      AttachNumericPasteFilters();
      HookDurationUpdates();
      ApplyMode();
      UpdateDurationDisplay();
    }

    public NewWorkEntryDialog(WorkEntry workEntry) : this()
    {
      _isEditingExistingEntry = true;
      Title = "Edit Work Entry";
      DialogTitleTextBlock.Text = "Edit work entry";
      SaveEntryButton.Content = "Save entry";
      StartButton.Content = "Save and start";

      ClientComboBox.Text = workEntry.Project?.Client?.Name ?? string.Empty;
      ProjectComboBox.Text = workEntry.Project?.Name ?? string.Empty;
      DescriptionTextBox.Text = workEntry.Description ?? string.Empty;
      StartTimePicker.SelectedDate = workEntry.StartTime;
      EndTimePicker.SelectedDate = workEntry.EndTime == default
        ? workEntry.StartTime + workEntry.Duration
        : workEntry.EndTime;
      HourlyRateTextBox.Text = workEntry.HourlyRate.ToString("0.##");
      CurrencyComboBox.Text = workEntry.Currency;
      BillableToggle.IsChecked = workEntry.IsBillable;

      UpdateDurationDisplay();
    }

    public NewWorkEntryDialog(Project project, DialogMode mode = DialogMode.JobTimer) : this(mode)
    {
      Title = "Start job";
      DialogTitleTextBlock.Text = "Start job";
      ApplyProject(project);
    }

    public string ClientName { get; private set; } = string.Empty;
    public string ProjectName { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal HourlyRate { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public DateTime StartTime { get; private set; }
    public TimeSpan? Duration { get; private set; }
    public bool IsBillable { get; private set; } = true;
    public bool StartTimerNow { get; private set; }

    private void LoadCombos()
    {
      ClientComboBox.ItemsSource = TimeTrackerModel.Instance.ActiveClients;
      ProjectComboBox.ItemsSource = null;
    }

    private void LoadBillingDefaults()
    {
      TTAppSettings settings = TTAppSettings.Instance;
      HourlyRateTextBox.Text = settings.DefaultHourlyRate.ToString("0.##");
      CurrencyComboBox.ItemsSource = MajorCurrencySymbols;
      CurrencyComboBox.Text = settings.DefaultCurrency;
    }

    private void HookDurationUpdates()
    {
      DependencyPropertyDescriptor descriptor =
        DependencyPropertyDescriptor.FromProperty(StackedDateTimePicker.SelectedDateProperty, typeof(StackedDateTimePicker));
      descriptor?.AddValueChanged(StartTimePicker, (_, _) => UpdateDurationDisplay());
      descriptor?.AddValueChanged(EndTimePicker, (_, _) => UpdateDurationDisplay());
    }

    private void ApplyMode()
    {
      if (_mode != DialogMode.JobTimer)
      {
        return;
      }

      Title = "New job";
      DialogTitleTextBlock.Text = "New job";
      EndTimePanel.Visibility = Visibility.Collapsed;
      DurationPanel.Visibility = Visibility.Collapsed;
      StartTimerNowCheckBox.Visibility = Visibility.Collapsed;
      StartButton.Content = "Start job";
      SaveEntryButton.Content = "Save job";
      StartButton.IsDefault = true;
      SaveEntryButton.IsDefault = false;
    }

    private void ApplyProject(Project project)
    {
      ClientComboBox.Text = project.Client?.Name ?? string.Empty;
      ProjectComboBox.Text = project.Name ?? string.Empty;
      DescriptionTextBox.Text = project.Description ?? string.Empty;

      HourlyRateTextBox.Text = TimeTrackerModel.GetProjectHourlyRate(project).ToString("0.##");
      CurrencyComboBox.Text = string.IsNullOrWhiteSpace(project.Client?.DefaultCurrency)
        ? TTAppSettings.Instance.DefaultCurrency
        : project.Client.DefaultCurrency;
    }

    private void UpdateDurationDisplay()
    {
      if (_isSyncingDuration)
      {
        return;
      }

      if (StartTimerNowCheckBox.IsChecked == true)
      {
        SetDurationText("Running");
        return;
      }

      TimeSpan duration = EndTimePicker.SelectedDate - StartTimePicker.SelectedDate;
      SetDurationText(duration <= TimeSpan.Zero ? "0m" : FormatDuration(duration));
    }

    private void SetDurationText(string text)
    {
      if (DurationTextBox.Text == text)
      {
        return;
      }

      _isSyncingDuration = true;
      DurationTextBox.Text = text;
      _isSyncingDuration = false;
    }

    private void DurationTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (_isSyncingDuration || StartTimerNowCheckBox is null || StartTimerNowCheckBox.IsChecked == true)
      {
        return;
      }

      if (!TryParseDuration(DurationTextBox.Text, out TimeSpan duration) || duration <= TimeSpan.Zero)
      {
        return;
      }

      _isSyncingDuration = true;
      EndTimePicker.SelectedDate = StartTimePicker.SelectedDate + duration;
      _isSyncingDuration = false;
    }

    private static string FormatDuration(TimeSpan duration)
    {
      int hours = (int)duration.TotalHours;
      int minutes = duration.Minutes;

      if (hours > 0 && minutes > 0)
      {
        return $"{hours}h {minutes}m";
      }

      return hours > 0 ? $"{hours}h" : $"{minutes}m";
    }

    private static bool TryParseDuration(string text, out TimeSpan duration)
    {
      duration = TimeSpan.Zero;

      if (string.IsNullOrWhiteSpace(text))
      {
        return false;
      }

      text = text.Trim();

      if (text.Contains(':') && TimeSpan.TryParse(text, CultureInfo.CurrentCulture, out duration))
      {
        return duration >= TimeSpan.Zero;
      }

      MatchCollection matches = Regex.Matches(
        text,
        @"(?<value>\d+(?:[\.,]\d+)?)\s*(?<unit>h|hr|hrs|hour|hours|m|min|mins|minute|minutes)\b",
        RegexOptions.IgnoreCase);

      if (matches.Count > 0)
      {
        string remainder = Regex.Replace(
          text,
          @"(?<value>\d+(?:[\.,]\d+)?)\s*(?<unit>h|hr|hrs|hour|hours|m|min|mins|minute|minutes)\b",
          string.Empty,
          RegexOptions.IgnoreCase);

        if (!string.IsNullOrWhiteSpace(remainder))
        {
          return false;
        }

        double totalMinutes = 0;
        foreach (Match match in matches.Cast<Match>())
        {
          if (!TryParseFlexibleDouble(match.Groups["value"].Value, out double value))
          {
            return false;
          }

          string unit = match.Groups["unit"].Value.ToLowerInvariant();
          totalMinutes += unit.StartsWith("h", StringComparison.Ordinal)
            ? value * 60
            : value;
        }

        duration = TimeSpan.FromMinutes(totalMinutes);
        return duration >= TimeSpan.Zero;
      }

      if (TryParseFlexibleDouble(text, out double hours))
      {
        duration = TimeSpan.FromHours(hours);
        return duration >= TimeSpan.Zero;
      }

      return false;
    }

    private static bool TryParseFlexibleDouble(string text, out double value)
    {
      return double.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value)
        || double.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    private void ClientComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (ClientComboBox.SelectedItem is not Client selectedClient)
      {
        return;
      }

      if (!_isEditingExistingEntry)
      {
        ApplyClientBillingDefaults(selectedClient);
      }
    }

    private void ClientComboBox_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (ProjectComboBox == null)
      {
        return;
      }

      // The project list is driven entirely by the resolved client. Projects are only
      // offered once a client is filled in; a new/unknown client offers none, so typing
      // a project name can never auto-match another client's project (and flip the client).
      Client? client = ResolveTypedClient();
      Project? currentProject = ProjectComboBox.SelectedItem as Project;

      if (client == null)
      {
        ProjectComboBox.ItemsSource = null;
        ProjectComboBox.SelectedItem = null;
        ProjectComboBox.Text = string.Empty;
        return;
      }

      ProjectComboBox.ItemsSource = client.Projects;

      // Drop any project left over from a different client.
      if (currentProject != null && currentProject.Client != client)
      {
        ProjectComboBox.SelectedItem = null;
        ProjectComboBox.Text = string.Empty;
      }
    }

    private Client? ResolveTypedClient()
    {
      if (ClientComboBox.SelectedItem is Client selected)
      {
        return selected;
      }

      string typedName = ClientComboBox.Text.Trim();
      return string.IsNullOrEmpty(typedName)
        ? null
        : TimeTrackerModel.Instance.ActiveClients
            .FirstOrDefault(c => string.Equals(c.Name, typedName, StringComparison.OrdinalIgnoreCase));
    }

    private void ApplyClientBillingDefaults(Client client)
    {
      TTAppSettings settings = TTAppSettings.Instance;

      decimal rate = client.DefaultHourlyRate > 0 ? client.DefaultHourlyRate : settings.DefaultHourlyRate;
      HourlyRateTextBox.Text = rate.ToString("0.##");

      CurrencyComboBox.Text = string.IsNullOrWhiteSpace(client.DefaultCurrency)
        ? settings.DefaultCurrency
        : client.DefaultCurrency;
    }

    private void RateUpButton_Click(object sender, RoutedEventArgs e)
    {
      StepRate(1);
    }

    private void RateDownButton_Click(object sender, RoutedEventArgs e)
    {
      StepRate(-1);
    }

    private void StepRate(decimal delta)
    {
      decimal.TryParse(HourlyRateTextBox.Text, out decimal rate);
      rate = Math.Max(0, rate + delta);
      HourlyRateTextBox.Text = rate.ToString("0.##");
    }

    private void StartTimerNowCheckBox_Changed(object sender, RoutedEventArgs e)
    {
      bool startNow = StartTimerNowCheckBox.IsChecked == true;
      EndTimePicker.IsEnabled = !startNow;
      DurationTextBox.IsEnabled = !startNow;
      UpdateDurationDisplay();
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
      StartTimerNow = sender == StartButton || StartTimerNowCheckBox.IsChecked == true;

      if (string.IsNullOrWhiteSpace(ClientComboBox.Text))
      {
        ClientComboBox.BorderBrush = Brushes.Red;
        MessageBox.Show("Please enter a client.");
        return;
      }
      else
      {
        ClientComboBox.BorderBrush = Brushes.Gray;
      }

      if (string.IsNullOrWhiteSpace(ProjectComboBox.Text))
      {
        ProjectComboBox.BorderBrush = Brushes.Red;
        MessageBox.Show("Please enter a project.");
        return;
      }
      else
      {
        ProjectComboBox.BorderBrush = Brushes.Gray;
      }

      ClientName = ClientComboBox.Text.Trim();
      ProjectName = ProjectComboBox.Text.Trim();
      Description = DescriptionTextBox.Text.Trim();

      if (!decimal.TryParse(HourlyRateTextBox.Text, out decimal hourlyRate) || hourlyRate < 0)
      {
        HourlyRateTextBox.BorderBrush = Brushes.Red;
        MessageBox.Show("Please enter an hourly rate of zero or greater.");
        return;
      }

      if (_mode == DialogMode.JobTimer)
      {
        Duration = StartTimerNow ? null : TimeSpan.Zero;
      }
      else if (StartTimerNow)
      {
        Duration = null;
      }
      else
      {
        if (!TryParseDuration(DurationTextBox.Text, out TimeSpan duration))
        {
          DurationTextBox.BorderBrush = Brushes.Red;
          MessageBox.Show("Please enter a valid duration.");
          return;
        }

        if (duration <= TimeSpan.Zero)
        {
          DurationTextBox.BorderBrush = Brushes.Red;
          MessageBox.Show("Please enter a duration greater than zero.");
          return;
        }

        if (duration.TotalMinutes < 1)
        {
          DurationTextBox.BorderBrush = Brushes.Red;
          MessageBox.Show("Please enter at least one minute of work before saving an entry.");
          return;
        }

        DurationTextBox.BorderBrush = Brushes.Gray;
        EndTimePicker.SelectedDate = StartTimePicker.SelectedDate + duration;
        Duration = duration;
      }

      if (string.IsNullOrWhiteSpace(CurrencyComboBox.Text))
      {
        CurrencyComboBox.BorderBrush = Brushes.Red;
        MessageBox.Show("Please enter a currency.");
        return;
      }

      HourlyRate = hourlyRate;
      Currency = TTAppSettings.NormalizeCurrency(CurrencyComboBox.Text);
      IsBillable = BillableToggle.IsChecked == true;
      StartTime = StartTimePicker.SelectedDate;

      DialogResult = true;
      Close();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      ClientComboBox.Focus();
    }

    private void DecimalTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      if (sender is not TextBox textBox)
      {
        e.Handled = true;
        return;
      }

      e.Handled = !WouldBeValidDecimalInput(textBox, e.Text);
    }

    private void AttachNumericPasteFilters()
    {
      DataObject.AddPastingHandler(HourlyRateTextBox, DecimalTextBox_Pasting);
    }

    private void DecimalTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
    {
      if (sender is not TextBox textBox || !e.DataObject.GetDataPresent(DataFormats.Text))
      {
        e.CancelCommand();
        return;
      }

      string text = (string)e.DataObject.GetData(DataFormats.Text);
      if (!WouldBeValidDecimalInput(textBox, text))
      {
        e.CancelCommand();
      }
    }

    private static bool WouldBeValidDecimalInput(TextBox textBox, string input)
    {
      string decimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
      string candidate = textBox.Text.Remove(textBox.SelectionStart, textBox.SelectionLength)
        .Insert(textBox.SelectionStart, input);

      if (string.IsNullOrEmpty(candidate))
      {
        return true;
      }

      if (candidate.Count(character => character.ToString() == decimalSeparator) > 1)
      {
        return false;
      }

      return candidate.All(character => char.IsDigit(character) || character.ToString() == decimalSeparator);
    }

    private static readonly string[] MajorCurrencySymbols =
    {
      "\u00A3",
      "$",
      "\u20AC",
      "\u00A5",
      "\u20B9",
      "\u20A9",
      "\u20BD",
      "\u20BA",
      "\u20BF",
      "A$",
      "C$",
      "NZ$",
      "R$",
      "CHF",
      "kr",
      "z\u0142",
      "\u062F.\u0625"
    };
  }
}
