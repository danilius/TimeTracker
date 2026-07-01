using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace TimeTracker.Controls
{
  /// <summary>
  /// A date/time picker that stacks a native <see cref="DatePicker" /> above an
  /// editable time <see cref="ComboBox" />. Exposes a single combined
  /// <see cref="SelectedDate" /> value.
  /// </summary>
  public partial class StackedDateTimePicker : UserControl
  {
    private static readonly string[] TimeFormats = { "HH:mm", "H:mm", "h:mm tt", "h tt" };
    private bool _isSyncing;

    public static readonly DependencyProperty SelectedDateProperty =
        DependencyProperty.Register(nameof(SelectedDate), typeof(DateTime), typeof(StackedDateTimePicker),
            new FrameworkPropertyMetadata(DateTime.Now, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedDateChanged));

    public DateTime SelectedDate
    {
      get => (DateTime)GetValue(SelectedDateProperty);
      set => SetValue(SelectedDateProperty, value);
    }

    public StackedDateTimePicker()
    {
      InitializeComponent();

      for (int minutes = 0; minutes < 24 * 60; minutes += 15)
      {
        TimeComboBox.Items.Add(new TimeSpan(0, minutes, 0).ToString(@"hh\:mm"));
      }

      SyncControlsFromValue(SelectedDate);
    }

    private static void OnSelectedDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      ((StackedDateTimePicker)d).SyncControlsFromValue((DateTime)e.NewValue);
    }

    private void SyncControlsFromValue(DateTime value)
    {
      _isSyncing = true;
      DatePickerControl.SelectedDate = value.Date;
      TimeComboBox.Text = value.ToString("HH:mm");
      _isSyncing = false;
    }

    private void DatePickerControl_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
      if (_isSyncing)
      {
        return;
      }

      DateTime date = DatePickerControl.SelectedDate ?? SelectedDate.Date;
      SelectedDate = date.Date + SelectedDate.TimeOfDay;
    }

    private void TimeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      CommitTime();
    }

    private void TimeComboBox_LostFocus(object sender, RoutedEventArgs e)
    {
      CommitTime();
    }

    private void CommitTime()
    {
      if (_isSyncing)
      {
        return;
      }

      if (TryParseTime(TimeComboBox.Text, out TimeSpan timeOfDay))
      {
        SelectedDate = SelectedDate.Date + timeOfDay;
      }
    }

    private static bool TryParseTime(string text, out TimeSpan timeOfDay)
    {
      timeOfDay = TimeSpan.Zero;

      if (string.IsNullOrWhiteSpace(text))
      {
        return false;
      }

      text = text.Trim();

      if (DateTime.TryParseExact(text, TimeFormats, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime parsed))
      {
        timeOfDay = parsed.TimeOfDay;
        return true;
      }

      if (TimeSpan.TryParse(text, CultureInfo.CurrentCulture, out TimeSpan span) && span >= TimeSpan.Zero && span < TimeSpan.FromDays(1))
      {
        timeOfDay = span;
        return true;
      }

      return false;
    }
  }
}
