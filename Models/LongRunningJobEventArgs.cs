using System;

namespace TimeTracker.Models
{
  /// <summary>
  /// Raised when a running job passes the long running threshold. The handler decides whether the
  /// job keeps running by setting <see cref="ContinueRunning" />; the model owns what happens next.
  /// </summary>
  public class LongRunningJobEventArgs : EventArgs
  {
    public LongRunningJobEventArgs(WorkEntry workEntry, DateTime cutoff, bool allowContinue)
    {
      WorkEntry = workEntry;
      Cutoff = cutoff;
      AllowContinue = allowContinue;
      ContinueRunning = allowContinue;
    }

    public WorkEntry WorkEntry { get; }

    /// <summary>
    /// When the job will be recorded as having ended if it is stopped — the moment the threshold
    /// was reached, not the moment the user answers.
    /// </summary>
    public DateTime Cutoff { get; }

    /// <summary>
    /// False when the configured policy stops the job regardless, so the handler should not offer
    /// a way to keep it running.
    /// </summary>
    public bool AllowContinue { get; }

    /// <summary>
    /// Set by the handler. Defaults to <see cref="AllowContinue" />, which is what happens when
    /// nothing is listening.
    /// </summary>
    public bool ContinueRunning { get; set; }
  }
}
