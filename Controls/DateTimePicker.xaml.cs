using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using TimeTracker.Models;

namespace TimeTracker.Controls
{
  public partial class DateTimePicker : UserControl
  {
    //public static readonly DependencyProperty SelectedDateTimeProperty =
    //    DependencyProperty.Register(nameof(SelectedDate), typeof(DateTime), typeof(DateTimePicker),
    //        new FrameworkPropertyMetadata(DateTime.Now, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnDateTimeChanged));

    public static readonly DependencyProperty SelectedDateProperty =
        DependencyProperty.Register(nameof(SelectedDate), typeof(DateTime), typeof(DateTimePicker),
            new FrameworkPropertyMetadata(DateTime.Now, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnDateTimeChanged));

    public DateTime SelectedDate
    {
      get => (DateTime)GetValue(SelectedDateProperty);
      set => SetValue(SelectedDateProperty, value);
    }


    public int SelectedHour
    {
      get => SelectedDate.Hour;
      set => SelectedDate = new DateTime(SelectedDate.Year, SelectedDate.Month, SelectedDate.Day, value, SelectedDate.Minute, SelectedDate.Second);
    }

    public int SelectedMinute
    {
      get => SelectedDate.Minute;
      set => SelectedDate = new DateTime(SelectedDate.Year, SelectedDate.Month, SelectedDate.Day, SelectedDate.Hour, value, SelectedDate.Second);
    }

    public DateTimePicker()
    {
      InitializeComponent();

      // Fill hour and minute combo boxes.
      for (int i = 0; i < 24; i++)
        HourComboBox.Items.Add(i.ToString("D2"));
      for (int i = 0; i < 60; i++)
        MinuteComboBox.Items.Add(i.ToString("D2"));
    }

    private static void OnDateTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      var control = (DateTimePicker)d;

      control.HourComboBox.SelectedIndex = control.SelectedDate.Hour;
      control.MinuteComboBox.SelectedIndex = control.SelectedDate.Minute;
    }

    private void this_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
      ;
    }
  }
}
