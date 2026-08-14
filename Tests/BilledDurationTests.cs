using System;
using TimeTracker.Models;
using TimeTracker.Utils;
using Xunit;

namespace TimeTracker.Tests
{
  /// <summary>
  /// The application defaults are passed in rather than read from
  /// <see cref="TTAppSettings.Instance" />, which would make these depend on the settings file of
  /// whoever runs them.
  /// </summary>
  public class BilledDurationTests
  {
    private static readonly DateTime Start = new(2026, 8, 14, 9, 0, 0);

    private static WorkEntry EntryOf(TimeSpan duration, BillingRoundingMode? clientMode = null, int? clientMinutes = null)
    {
      Client client = new()
      {
        ID = Guid.NewGuid(),
        Name = "Test client",
        RoundingMode = clientMode,
        RoundingMinutes = clientMinutes
      };
      Project project = new()
      {
        ID = Guid.NewGuid(),
        Name = "Test project",
        Client = client
      };
      WorkEntry workEntry = new()
      {
        ID = Guid.NewGuid(),
        Project = project,
        StartTime = Start,
        EndTime = Start + duration
      };

      client.Projects.Add(project);
      project.WorkEntries.Add(workEntry);

      return workEntry;
    }

    [Fact]
    public void WithNoRoundingConfiguredTheTrackedTimeIsBilled()
    {
      WorkEntry workEntry = EntryOf(TimeSpan.FromMinutes(37));

      Assert.Equal(TimeSpan.FromMinutes(37), TimeTrackerModel.GetBilledDuration(workEntry, BillingRoundingMode.None, 15));
    }

    [Fact]
    public void AClientWithoutItsOwnRoundingInheritsTheApplicationDefault()
    {
      WorkEntry workEntry = EntryOf(TimeSpan.FromMinutes(37));

      Assert.Equal(TimeSpan.FromMinutes(45), TimeTrackerModel.GetBilledDuration(workEntry, BillingRoundingMode.Up, 15));
    }

    [Fact]
    public void AClientsOwnRoundingOverridesTheApplicationDefault()
    {
      WorkEntry workEntry = EntryOf(TimeSpan.FromMinutes(37), BillingRoundingMode.Nearest, 30);

      Assert.Equal(TimeSpan.FromMinutes(30), TimeTrackerModel.GetBilledDuration(workEntry, BillingRoundingMode.Up, 15));
    }

    [Fact]
    public void AClientCanTurnRoundingOffWhileTheDefaultRoundsUp()
    {
      WorkEntry workEntry = EntryOf(TimeSpan.FromMinutes(37), BillingRoundingMode.None);

      Assert.Equal(TimeSpan.FromMinutes(37), TimeTrackerModel.GetBilledDuration(workEntry, BillingRoundingMode.Up, 15));
    }

    [Fact]
    public void AClientMayOverrideOnlyTheGranularity()
    {
      WorkEntry workEntry = EntryOf(TimeSpan.FromMinutes(37), null, 30);

      Assert.Equal(TimeSpan.FromMinutes(60), TimeTrackerModel.GetBilledDuration(workEntry, BillingRoundingMode.Up, 15));
    }

    [Fact]
    public void PausedTimeIsRoundedRatherThanBilled()
    {
      WorkEntry workEntry = EntryOf(TimeSpan.FromMinutes(60));
      workEntry.PausedDuration = TimeSpan.FromMinutes(23);

      Assert.Equal(TimeSpan.FromMinutes(45), TimeTrackerModel.GetBilledDuration(workEntry, BillingRoundingMode.Up, 15));
    }

    [Fact]
    public void AJobWithNoProjectStillBillsAgainstTheApplicationDefault()
    {
      WorkEntry workEntry = new()
      {
        ID = Guid.NewGuid(),
        StartTime = Start,
        EndTime = Start.AddMinutes(37)
      };

      Assert.Equal(TimeSpan.FromMinutes(45), TimeTrackerModel.GetBilledDuration(workEntry, BillingRoundingMode.Up, 15));
    }
  }
}
