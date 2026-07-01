using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TimeTracker.Scratch
{
  /// <summary>
  /// Interaction logic for Colours.xaml
  /// </summary>
  public partial class Colours : Window
  {
    List<Brush> strongPastelColors = new();

    public Colours()
    {
      InitializeComponent();

      for (int i = 0; i < 40; i++)
      {
        strongPastelColors.Add(GenerateRandomPastelColor());
      }

      foreach (var brush in strongPastelColors)
      {
        var rectangle = new Rectangle
        {
          Width = 50,
          Height = 50,
          Fill = brush
        };

        colorDock.Children.Add(rectangle);
      }
    }

    private Random _random = new();

    public Brush GenerateRandomPastelColor()
    {
      int red = _random.Next(128, 240); // Generates a random value between 128 and 255 (inclusive)
      int green = _random.Next(128, 240);
      int blue = _random.Next(128, 240);

      Color pastelColor = Color.FromRgb((byte)red, (byte)green, (byte)blue);
      return new SolidColorBrush(pastelColor);
    }
  }
}
