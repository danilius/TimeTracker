using System;
using TimeTracker.Utils;
using Xunit;

namespace TimeTracker.Tests
{
  public class BillingRoundingTests
  {
    [Fact]
    public void NoneLeavesTheDurationAlone()
    {
      TimeSpan duration = TimeSpan.FromMinutes(37);

      Assert.Equal(duration, BillingRounding.Round(duration, BillingRoundingMode.None, 15));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-15)]
    public void AGranularityOfZeroOrLessLeavesTheDurationAlone(int minutes)
    {
      TimeSpan duration = TimeSpan.FromMinutes(37);

      Assert.Equal(duration, BillingRounding.Round(duration, BillingRoundingMode.Up, minutes));
    }

    [Fact]
    public void ZeroStaysZeroRatherThanBecomingAWholeUnit()
    {
      Assert.Equal(TimeSpan.Zero, BillingRounding.Round(TimeSpan.Zero, BillingRoundingMode.Up, 15));
    }

    [Fact]
    public void ANegativeDurationIsLeftAlone()
    {
      TimeSpan duration = TimeSpan.FromMinutes(-20);

      Assert.Equal(duration, BillingRounding.Round(duration, BillingRoundingMode.Up, 15));
      Assert.Equal(duration, BillingRounding.Round(duration, BillingRoundingMode.Nearest, 15));
    }

    [Theory]
    [InlineData(BillingRoundingMode.Nearest)]
    [InlineData(BillingRoundingMode.Up)]
    public void AnExactMultipleIsNeverChanged(BillingRoundingMode mode)
    {
      Assert.Equal(TimeSpan.FromMinutes(45), BillingRounding.Round(TimeSpan.FromMinutes(45), mode, 15));
    }

    [Fact]
    public void NearestRoundsDownBelowTheHalfway()
    {
      Assert.Equal(TimeSpan.FromMinutes(30), BillingRounding.Round(TimeSpan.FromMinutes(37), BillingRoundingMode.Nearest, 15));
    }

    [Fact]
    public void NearestRoundsUpAboveTheHalfway()
    {
      Assert.Equal(TimeSpan.FromMinutes(45), BillingRounding.Round(TimeSpan.FromMinutes(38), BillingRoundingMode.Nearest, 15));
    }

    [Fact]
    public void NearestRoundsUpExactlyOnTheHalfway()
    {
      Assert.Equal(TimeSpan.FromMinutes(45), BillingRounding.Round(TimeSpan.FromMinutes(37.5), BillingRoundingMode.Nearest, 15));
    }

    [Fact]
    public void UpTakesAOneSecondOverrunToTheNextUnit()
    {
      TimeSpan duration = TimeSpan.FromMinutes(15) + TimeSpan.FromSeconds(1);

      Assert.Equal(TimeSpan.FromMinutes(30), BillingRounding.Round(duration, BillingRoundingMode.Up, 15));
    }

    [Fact]
    public void NearestLeavesAOneSecondOverrunOnTheUnitBelow()
    {
      TimeSpan duration = TimeSpan.FromMinutes(15) + TimeSpan.FromSeconds(1);

      Assert.Equal(TimeSpan.FromMinutes(15), BillingRounding.Round(duration, BillingRoundingMode.Nearest, 15));
    }

    [Fact]
    public void SixMinuteGranularityBillsInTenthsOfAnHour()
    {
      Assert.Equal(TimeSpan.FromMinutes(12), BillingRounding.Round(TimeSpan.FromMinutes(11), BillingRoundingMode.Nearest, 6));
      Assert.Equal(TimeSpan.FromMinutes(12), BillingRounding.Round(TimeSpan.FromMinutes(7), BillingRoundingMode.Up, 6));
    }

    [Fact]
    public void FifteenMinuteGranularityBillsInQuarterHours()
    {
      Assert.Equal(TimeSpan.FromMinutes(60), BillingRounding.Round(TimeSpan.FromMinutes(53), BillingRoundingMode.Nearest, 15));
      Assert.Equal(TimeSpan.FromMinutes(60), BillingRounding.Round(TimeSpan.FromMinutes(46), BillingRoundingMode.Up, 15));
    }

    [Fact]
    public void AClientWithNoRoundingOfItsOwnUsesTheApplicationDefault()
    {
      TimeSpan rounded = BillingRounding.RoundForClient(TimeSpan.FromMinutes(37), null, null, BillingRoundingMode.Up, 15);

      Assert.Equal(TimeSpan.FromMinutes(45), rounded);
    }

    [Fact]
    public void AClientsOwnRoundingOverridesTheApplicationDefault()
    {
      TimeSpan rounded = BillingRounding.RoundForClient(TimeSpan.FromMinutes(37), BillingRoundingMode.Nearest, 30, BillingRoundingMode.Up, 15);

      Assert.Equal(TimeSpan.FromMinutes(30), rounded);
    }

    [Fact]
    public void AClientCanTurnOffRoundingTheDefaultAppliesElsewhere()
    {
      TimeSpan duration = TimeSpan.FromMinutes(37);

      Assert.Equal(duration, BillingRounding.RoundForClient(duration, BillingRoundingMode.None, null, BillingRoundingMode.Up, 15));
    }

    [Fact]
    public void AClientMayOverrideOnlyTheGranularity()
    {
      TimeSpan rounded = BillingRounding.RoundForClient(TimeSpan.FromMinutes(37), null, 30, BillingRoundingMode.Up, 15);

      Assert.Equal(TimeSpan.FromMinutes(60), rounded);
    }
  }
}
