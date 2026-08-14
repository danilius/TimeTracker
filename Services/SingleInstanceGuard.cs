using System;
using System.Text;
using System.Threading;

namespace TimeTracker.Services
{
  /// <summary>
  /// Keeps a single copy of the application alive. The first process to call
  /// <see cref="TryAcquire" /> owns the named handles; every later process signals the owner and is
  /// told to exit.
  /// </summary>
  public static class SingleInstanceGuard
  {
    private static readonly object SyncRoot = new();

    private static Mutex? instanceMutex;
    private static EventWaitHandle? activationEvent;
    private static Thread? activationThread;
    private static volatile bool listening;

    /// <summary>
    /// Raised in the owning process when another instance tried to start. This fires on a
    /// background wait thread, so the subscriber is responsible for marshalling to the UI thread
    /// (for example via <c>Dispatcher.BeginInvoke</c>) before touching any window.
    /// </summary>
    public static event EventHandler? ActivationRequested;

    /// <summary>
    /// Claims ownership for this process.
    /// </summary>
    /// <returns>
    /// True when this process is the only instance and should carry on starting. False when
    /// another instance already owns the handles; the existing instance has been asked to show
    /// itself and the caller should shut down without displaying anything.
    /// </returns>
    public static bool TryAcquire(string appId)
    {
      lock (SyncRoot)
      {
        if (instanceMutex != null)
        {
          return true;
        }

        string key = BuildKey(appId);

        try
        {
          activationEvent = new EventWaitHandle(false, EventResetMode.AutoReset, key + ".activate");
          instanceMutex = new Mutex(false, key + ".instance", out bool createdNew);

          if (!createdNew)
          {
            activationEvent.Set();
            ReleaseCore();
            return false;
          }

          StartActivationThread();
          return true;
        }
        catch (Exception)
        {
          // Named handles can be refused by policy, an unusual session or a sandbox. Losing
          // single-instance behaviour is far better than refusing to start, so fail open.
          ReleaseCore();
          return true;
        }
      }
    }

    /// <summary>Disposes the handles. Safe to call more than once.</summary>
    public static void Release()
    {
      lock (SyncRoot)
      {
        listening = false;

        try
        {
          // Wake the wait thread so it observes the flag instead of sitting on a disposed handle.
          activationEvent?.Set();
        }
        catch (Exception)
        {
        }

        ReleaseCore();
        activationThread = null;
      }
    }

    private static void ReleaseCore()
    {
      listening = false;

      try
      {
        instanceMutex?.Dispose();
      }
      catch (Exception)
      {
      }

      try
      {
        activationEvent?.Dispose();
      }
      catch (Exception)
      {
      }

      instanceMutex = null;
      activationEvent = null;
    }

    private static void StartActivationThread()
    {
      listening = true;

      activationThread = new Thread(WaitForActivationRequests)
      {
        IsBackground = true,
        Name = "SingleInstanceGuard"
      };

      activationThread.Start();
    }

    private static void WaitForActivationRequests()
    {
      while (listening)
      {
        EventWaitHandle? handle = activationEvent;

        if (handle == null)
        {
          return;
        }

        try
        {
          handle.WaitOne();
        }
        catch (ObjectDisposedException)
        {
          return;
        }
        catch (AbandonedMutexException)
        {
          return;
        }

        if (!listening)
        {
          return;
        }

        try
        {
          ActivationRequested?.Invoke(null, EventArgs.Empty);
        }
        catch (Exception)
        {
          // A faulty subscriber must not kill the wait thread; the next request still gets through.
        }
      }
    }

    /// <summary>
    /// Builds a handle name. The "Local\" prefix scopes the handles to the logon session, which is
    /// what a per-user desktop app wants: two people signed in to the same machine, or the same
    /// person on separate remote sessions, each get their own instance.
    /// </summary>
    private static string BuildKey(string appId)
    {
      if (string.IsNullOrWhiteSpace(appId))
      {
        return "Local\\TimeTracker";
      }

      StringBuilder builder = new("Local\\");

      foreach (char character in appId)
      {
        // Backslash separates the namespace from the name, so it cannot appear in the name itself.
        builder.Append(character == '\\' ? '_' : character);
      }

      return builder.ToString();
    }
  }
}
