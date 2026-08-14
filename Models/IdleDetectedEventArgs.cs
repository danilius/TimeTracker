using System;

namespace TimeTracker.Models
{
  /// <summary>
  /// Raised when a running job has sat through a stretch of machine idle time. The handler decides
  /// what happens to that time by setting <see cref="DiscardIdleTime" />; the model applies it.
  /// </summary>
  public class IdleDetectedEventArgs : EventArgs
  {
    public IdleDetectedEventArgs(WorkEntry workEntry, TimeSpan idleDuration, DateTime cutoff)
    {
      WorkEntry = workEntry;
      IdleDuration = idleDuration;
      Cutoff = cutoff;
    }

    public WorkEntry WorkEntry { get; }

    /// <summary>
    /// How long the machine has been idle at the moment the prompt is raised. The user may take a
    /// while to answer, so the span actually discarded is measured again once they do.
    /// </summary>
    public TimeSpan IdleDuration { get; }

    /// <summary>
    /// When the machine went quiet, clamped to the job's start time. Idle time is discarded from
    /// here onwards.
    /// </summary>
    public DateTime Cutoff { get; }

    /// <summary>
    /// Set by the handler. Defaults to false, which keeps the time — what happens when nothing is
    /// listening, and the answer that cannot lose anything.
    /// </summary>
    public bool DiscardIdleTime { get; set; }
  }
}
