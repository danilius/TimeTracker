using System;
using TimeTracker.Utils;
using Xunit;

namespace TimeTracker.Tests
{
  public class IdlePolicyTests
  {
    private static readonly DateTime Start = new(2026, 8, 14, 9, 0, 0);

    [Fact]
    public void DoesNotPromptBeforeTheThreshold()
    {
      Assert.False(IdlePolicy.ShouldPrompt(TimeSpan.FromMinutes(9), Start, Start.AddMinutes(30), 10));
    }

    [Fact]
    public void PromptsOnceTheThresholdIsReached()
    {
      Assert.True(IdlePolicy.ShouldPrompt(TimeSpan.FromMinutes(10), Start, Start.AddMinutes(30), 10));
    }

    [Fact]
    public void DoesNotPromptWhenTheIdleStretchBeganBeforeTheJob()
    {
      // Quiet for half an hour but the job only started five minutes ago: the silence is not this
      // job's idle time.
      Assert.False(IdlePolicy.ShouldPrompt(TimeSpan.FromMinutes(30), Start, Start.AddMinutes(5), 10));
    }

    [Fact]
    public void PromptsWhenTheIdleStretchBeganExactlyAtTheJobStart()
    {
      Assert.True(IdlePolicy.ShouldPrompt(TimeSpan.FromMinutes(30), Start, Start.AddMinutes(30), 10));
    }

    [Fact]
    public void IdleStartedAtIsTheMomentInputStopped()
    {
      Assert.Equal(Start.AddMinutes(20), IdlePolicy.IdleStartedAt(TimeSpan.FromMinutes(10), Start.AddMinutes(30)));
    }

    [Fact]
    public void CutoffIsTheMomentInputStoppedNotTheMomentTheUserAnswers()
    {
      // The prompt sat on screen while the idle time kept growing; the job still ends where the
      // keyboard went quiet.
      DateTime cutoff = IdlePolicy.Cutoff(TimeSpan.FromMinutes(25), Start, Start.AddMinutes(40));

      Assert.Equal(Start.AddMinutes(15), cutoff);
    }

    [Fact]
    public void CutoffNeverPrecedesTheJobStart()
    {
      DateTime cutoff = IdlePolicy.Cutoff(TimeSpan.FromMinutes(90), Start, Start.AddMinutes(20));

      Assert.Equal(Start, cutoff);
    }

    [Fact]
    public void CutoffNeverRunsPastNow()
    {
      DateTime now = Start.AddMinutes(20);
      DateTime cutoff = IdlePolicy.Cutoff(TimeSpan.FromMinutes(-5), Start, now);

      Assert.Equal(now, cutoff);
    }
  }
}
