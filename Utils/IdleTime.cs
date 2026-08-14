using System;
using System.Runtime.InteropServices;

namespace TimeTracker.Utils
{
  /// <summary>
  /// Machine-wide input idle time. Windows only reports this for the whole desktop, so there is no
  /// managed equivalent to fall back on; the P/Invoke is isolated here to keep the idle rules in
  /// <see cref="IdlePolicy"/> testable without a real machine.
  /// </summary>
  public static class IdleTime
  {
    /// <summary>
    /// How long the keyboard and mouse have been quiet, or <see cref="TimeSpan.Zero"/> if Windows
    /// declines to say. This is polled from a timer, so it reports nothing rather than throwing.
    /// </summary>
    public static TimeSpan GetIdleTime()
    {
      LASTINPUTINFO info = new()
      {
        cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>()
      };

      if (!GetLastInputInfo(ref info))
      {
        return TimeSpan.Zero;
      }

      // Both values are the low 32 bits of the tick counter, which wraps roughly every 49 days.
      long ticksNow = unchecked((uint)Environment.TickCount64);
      long idleMilliseconds = ticksNow - info.dwTime;

      if (idleMilliseconds < 0)
      {
        idleMilliseconds += 1L << 32;
      }

      return TimeSpan.FromMilliseconds(idleMilliseconds);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO
    {
      public uint cbSize;
      public uint dwTime;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);
  }
}
