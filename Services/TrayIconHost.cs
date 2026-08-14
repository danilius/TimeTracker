using System;
using System.Drawing;
using System.IO;
using WinForms = System.Windows.Forms;

namespace TimeTracker.Services
{
  /// <summary>
  /// Owns the notification area icon and its context menu. The host raises events only; it never
  /// decides what "open" or "exit" mean, so the window and the application lifetime stay the
  /// caller's business.
  /// </summary>
  public sealed class TrayIconHost : IDisposable
  {
    // NotifyIcon.Text is backed by a fixed Win32 buffer and throws above this length.
    private const int MaxTooltipLength = 63;
    private const int BalloonTimeoutMilliseconds = 5000;
    private const string IconResourceUri = "pack://application:,,,/resources/time tracker icon.ico";

    private readonly WinForms.NotifyIcon notifyIcon;
    private readonly WinForms.ContextMenuStrip contextMenu;
    private readonly WinForms.ToolStripMenuItem stopTimerItem;

    private bool disposed;

    public event EventHandler? OpenRequested;
    public event EventHandler? StopTimerRequested;
    public event EventHandler? ExitRequested;

    public TrayIconHost(string tooltip)
    {
      WinForms.ToolStripMenuItem openItem = new("Open Time Tracker");
      openItem.Click += OnOpenClicked;

      stopTimerItem = new WinForms.ToolStripMenuItem("Stop timer");
      stopTimerItem.Click += OnStopTimerClicked;

      WinForms.ToolStripMenuItem exitItem = new("Exit");
      exitItem.Click += OnExitClicked;

      contextMenu = new WinForms.ContextMenuStrip();
      contextMenu.Items.Add(openItem);
      contextMenu.Items.Add(stopTimerItem);
      contextMenu.Items.Add(new WinForms.ToolStripSeparator());
      contextMenu.Items.Add(exitItem);

      notifyIcon = new WinForms.NotifyIcon
      {
        Icon = LoadApplicationIcon(),
        Text = ClampTooltip(tooltip),
        ContextMenuStrip = contextMenu,
        Visible = false
      };

      notifyIcon.DoubleClick += OnIconDoubleClicked;
    }

    public void Show()
    {
      if (!disposed)
      {
        notifyIcon.Visible = true;
      }
    }

    public void Hide()
    {
      if (!disposed)
      {
        notifyIcon.Visible = false;
      }
    }

    public void SetTooltip(string text)
    {
      if (!disposed)
      {
        notifyIcon.Text = ClampTooltip(text);
      }
    }

    public void SetStopTimerEnabled(bool enabled)
    {
      if (!disposed)
      {
        stopTimerItem.Enabled = enabled;
      }
    }

    /// <summary>Shows a notification balloon. Does nothing while the icon is hidden.</summary>
    public void ShowBalloon(string title, string message)
    {
      if (disposed || !notifyIcon.Visible || string.IsNullOrWhiteSpace(message))
      {
        return;
      }

      notifyIcon.BalloonTipIcon = WinForms.ToolTipIcon.Info;
      notifyIcon.BalloonTipTitle = title ?? string.Empty;
      notifyIcon.BalloonTipText = message;
      notifyIcon.ShowBalloonTip(BalloonTimeoutMilliseconds);
    }

    public void Dispose()
    {
      if (disposed)
      {
        return;
      }

      disposed = true;

      notifyIcon.DoubleClick -= OnIconDoubleClicked;

      // Hiding before disposing is what actually removes the icon. Skipping it leaves a ghost in
      // the notification area until the user happens to hover over it.
      notifyIcon.Visible = false;
      notifyIcon.Dispose();
      contextMenu.Dispose();
    }

    private void OnOpenClicked(object? sender, EventArgs e)
    {
      OpenRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnStopTimerClicked(object? sender, EventArgs e)
    {
      StopTimerRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnExitClicked(object? sender, EventArgs e)
    {
      ExitRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnIconDoubleClicked(object? sender, EventArgs e)
    {
      OpenRequested?.Invoke(this, EventArgs.Empty);
    }

    private static string ClampTooltip(string text)
    {
      if (string.IsNullOrEmpty(text))
      {
        return string.Empty;
      }

      return text.Length <= MaxTooltipLength ? text : text.Substring(0, MaxTooltipLength);
    }

    /// <summary>
    /// Reads the icon out of the application resources. A missing or unreadable resource must not
    /// stop the tray icon appearing, so it degrades to the stock application icon.
    /// </summary>
    private static Icon LoadApplicationIcon()
    {
      try
      {
        System.Windows.Resources.StreamResourceInfo? resource =
          System.Windows.Application.GetResourceStream(new Uri(IconResourceUri, UriKind.Absolute));

        if (resource?.Stream != null)
        {
          using Stream stream = resource.Stream;
          return new Icon(stream, WinForms.SystemInformation.SmallIconSize);
        }
      }
      catch (Exception)
      {
      }

      return SystemIcons.Application;
    }
  }
}
