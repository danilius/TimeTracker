using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
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
    private bool _isEditingExistingEntry;

    public NewWorkEntryDialog()
    {
      InitializeComponent();

      LoadCombos();
      LoadBillingDefaults();
      AttachNumericPasteFilters();
      HookDurationUpdates();
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
      ProjectComboBox.ItemsSource = TimeTrackerModel.Instance.Projects;
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
        DependencyPropertyDescriptor.FromProperty(DateTimePicker.SelectedDateProperty, typeof(DateTimePicker));
      descriptor?.AddValueChanged(StartTimePicker, (_, _) => UpdateDurationDisplay());
      descriptor?.AddValueChanged(EndTimePicker, (_, _) => UpdateDurationDisplay());
    }

    private void UpdateDurationDisplay()
    {
      if (StartTimerNowCheckBox.IsChecked == true)
      {
        DurationDisplayText.Text = "Running";
        return;
      }

      TimeSpan duration = EndTimePicker.SelectedDate - StartTimePicker.SelectedDate;
      DurationDisplayText.Text = duration <= TimeSpan.Zero ? "0m" : FormatDuration(duration);
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

    private void ClientComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (ClientComboBox.SelectedItem is not Client selectedClient)
      {
        return;
      }

      ProjectComboBox.ItemsSource = TimeTrackerModel.Instance.Projects.Where(p => p.Client == selectedClient);
      ProjectComboBox.SelectedIndex = 0;

      if (!_isEditingExistingEntry)
      {
        ApplyClientBillingDefaults(selectedClient);
      }
    }

    private void ProjectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (ProjectComboBox.SelectedItem is Project selectedProject && ClientComboBox.SelectedItem == null)
      {
        ClientComboBox.ItemsSource = TimeTrackerModel.Instance.ActiveClients.Where(c => c == selectedProject.Client);
        ClientComboBox.SelectedIndex = 0;
      }
    }

    private void ApplyClientBillingDefaults(Client client)
    {
      HourlyRateTextBox.Text = client.DefaultHourlyRate.ToString("0.##");

      if (!string.IsNullOrWhiteSpace(client.DefaultCurrency))
      {
        CurrencyComboBox.Text = client.DefaultCurrency;
      }
    }

    private void CreateClientButton_Click(object sender, RoutedEventArgs e)
    {
      string name = ClientComboBox.Text.Trim();
      if (string.IsNullOrWhiteSpace(name))
      {
        ClientComboBox.BorderBrush = Brushes.Red;
        MessageBox.Show("Please type a client name first, then create it.");
        ClientComboBox.Focus();
        return;
      }

      ClientComboBox.BorderBrush = null;

      Client client = TimeTrackerModel.Instance.ActiveClients
        .FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase))
        ?? TimeTrackerModel.Instance.CreateClient(name);

      ClientComboBox.ItemsSource = TimeTrackerModel.Instance.ActiveClients;
      ClientComboBox.SelectedItem = client;
      ProjectComboBox.Focus();
    }

    private void CreateProjectButton_Click(object sender, RoutedEventArgs e)
    {
      string clientName = ClientComboBox.Text.Trim();
      if (string.IsNullOrWhiteSpace(clientName))
      {
        ClientComboBox.BorderBrush = Brushes.Red;
        MessageBox.Show("Please choose or create a client before adding a project.");
        ClientComboBox.Focus();
        return;
      }

      string projectName = ProjectComboBox.Text.Trim();
      if (string.IsNullOrWhiteSpace(projectName))
      {
        ProjectComboBox.BorderBrush = Brushes.Red;
        MessageBox.Show("Please type a project name first, then create it.");
        ProjectComboBox.Focus();
        return;
      }

      ProjectComboBox.BorderBrush = null;

      Client client = TimeTrackerModel.Instance.ActiveClients
        .FirstOrDefault(c => string.Equals(c.Name, clientName, StringComparison.OrdinalIgnoreCase))
        ?? TimeTrackerModel.Instance.CreateClient(clientName);

      Project project = client.Projects
        .FirstOrDefault(p => string.Equals(p.Name, projectName, StringComparison.OrdinalIgnoreCase))
        ?? TimeTrackerModel.Instance.CreateProject(client, projectName, DescriptionTextBox.Text.Trim());

      ClientComboBox.ItemsSource = TimeTrackerModel.Instance.ActiveClients;
      ClientComboBox.SelectedItem = client;
      ProjectComboBox.ItemsSource = TimeTrackerModel.Instance.Projects.Where(p => p.Client == client).ToList();
      ProjectComboBox.SelectedItem = project;
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

      if (StartTimerNow)
      {
        Duration = null;
      }
      else
      {
        TimeSpan duration = EndTimePicker.SelectedDate - StartTimePicker.SelectedDate;

        if (duration <= TimeSpan.Zero)
        {
          MessageBox.Show("The end time must be after the start time.");
          return;
        }

        if (duration.TotalMinutes < 1)
        {
          MessageBox.Show("Please enter at least one minute of work before saving an entry.");
          return;
        }

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
      StartTime = StartTimerNow ? DateTime.Now : StartTimePicker.SelectedDate;

      DialogResult = true;
      Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = false;
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
