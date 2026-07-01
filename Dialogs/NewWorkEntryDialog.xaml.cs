using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TimeTracker.Models;
using TimeTracker.Utils;

namespace TimeTracker.Dialogs
{
  /// <summary>
  /// Interaction logic for NewWorkEntryDialog.xaml
  /// </summary>
  public partial class NewWorkEntryDialog : Window
  {
    //private WorkEntry _workEntry;
    private bool _isEditingExistingEntry;

    public NewWorkEntryDialog()
    {
      InitializeComponent();

      LoadCombos();
      LoadBillingDefaults();
      AttachNumericPasteFilters();

      //_workEntry = workEntry;
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
      DurationHoursTextBox.Text = ((int)workEntry.Duration.TotalHours).ToString();
      DurationMinutesTextBox.Text = workEntry.Duration.Minutes.ToString("D2");
      HourlyRateTextBox.Text = workEntry.HourlyRate.ToString("0.##");
      CurrencyComboBox.Text = workEntry.Currency;
    }

    //public WorkEntry WorkEntry
    //{
    //  get => _workEntry;
    //}

    public string ClientName { get; private set; } = string.Empty;
    public string ProjectName { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal HourlyRate { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public DateTime StartTime { get; private set; }
    public TimeSpan? Duration { get; private set; }
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
      DurationHoursTextBox.Text = "0";
      DurationMinutesTextBox.Text = "00";
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

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
      StartTimerNow = sender == StartButton;

      // is the client name or project name empty?
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

      if (!string.IsNullOrWhiteSpace(DurationHoursTextBox.Text) || !string.IsNullOrWhiteSpace(DurationMinutesTextBox.Text))
      {
        string durationHoursText = string.IsNullOrWhiteSpace(DurationHoursTextBox.Text) ? "0" : DurationHoursTextBox.Text;
        string durationMinutesText = string.IsNullOrWhiteSpace(DurationMinutesTextBox.Text) ? "0" : DurationMinutesTextBox.Text;

        if (!int.TryParse(durationHoursText, out int durationHours) || durationHours < 0)
        {
          DurationHoursTextBox.BorderBrush = Brushes.Red;
          MessageBox.Show("Please enter duration hours of zero or greater.");
          return;
        }

        if (!int.TryParse(durationMinutesText, out int durationMinutes) || durationMinutes < 0 || durationMinutes > 59)
        {
          DurationMinutesTextBox.BorderBrush = Brushes.Red;
          MessageBox.Show("Please enter duration minutes between 0 and 59.");
          return;
        }

        Duration = new TimeSpan(durationHours, durationMinutes, 0);
      }
      else
      {
        Duration = null;
      }

      if (!StartTimerNow && (Duration == null || Duration.Value.TotalMinutes < 1))
      {
        DurationHoursTextBox.BorderBrush = Brushes.Red;
        DurationMinutesTextBox.BorderBrush = Brushes.Red;
        MessageBox.Show("Please enter at least one minute of work before saving an entry.");
        return;
      }

      if (string.IsNullOrWhiteSpace(CurrencyComboBox.Text))
      {
        CurrencyComboBox.BorderBrush = Brushes.Red;
        MessageBox.Show("Please enter a currency.");
        return;
      }

      HourlyRate = hourlyRate;
      Currency = TTAppSettings.NormalizeCurrency(CurrencyComboBox.Text);
      StartTime = StartTimerNow ? DateTime.Now : StartTimePicker.SelectedDate;
      if (StartTimerNow)
      {
        Duration = null;
      }

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

    private void WholeNumberTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      e.Handled = !e.Text.All(char.IsDigit);
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
      DataObject.AddPastingHandler(DurationHoursTextBox, WholeNumberTextBox_Pasting);
      DataObject.AddPastingHandler(DurationMinutesTextBox, WholeNumberTextBox_Pasting);
      DataObject.AddPastingHandler(HourlyRateTextBox, DecimalTextBox_Pasting);
    }

    private void WholeNumberTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
    {
      if (!e.DataObject.GetDataPresent(DataFormats.Text))
      {
        e.CancelCommand();
        return;
      }

      string text = (string)e.DataObject.GetData(DataFormats.Text);
      if (!text.All(char.IsDigit))
      {
        e.CancelCommand();
      }
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

    //private void PrepWorkEntry(WorkEntry workEntry)
    //{
    //  // does this client exist?
    //  var client = TimeTrackerModel.Instance.Clients.FirstOrDefault(c => c.Name == newWorkEntryModel.ClientName);

    //  if (client == null)
    //  {
    //    // create a new client
    //    client = new Client()
    //    {
    //      Name = newWorkEntryModel.ClientName,
    //      ID = Guid.NewGuid()
    //    };

    //    // add it to the list of clients
    //    TimeTrackerModel.Instance.Clients.Add(client);
    //  }

    //  // does this project exist?
    //  Project? project = TimeTrackerModel.Instance.Projects.FirstOrDefault(p => p.Name == newWorkEntryModel.ProjectName);

    //  if (project == null)
    //  {
    //    project = new Project()
    //    {
    //      Name = newWorkEntryModel.ProjectName,
    //      ClientID = client.ID,
    //      ProjectColour = GenerateRandomPastelColor(),
    //      ID = Guid.NewGuid()
    //    };

    //    client.Projects.Add(project);
    //  }

    //  project.WorkEntries.Add(workEntry);

    //  workEntry.ProjectID = project.ID;
    //  workEntry.ClientID = client.ID;


    //  workEntry.StartTime = newWorkEntryModel.StartTime;
    //  workEntry.EndTime = newWorkEntryModel.EndTime;
    //  workEntry.Description = newWorkEntryModel.Description;
    //}
  }
}
