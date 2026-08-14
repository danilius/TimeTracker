using System;
using TimeTracker.Utils;
using Xunit;

namespace TimeTracker.Tests
{
  public class DurationTextTests
  {
    [Theory]
    [InlineData("1h 30m", 90)]
    [InlineData("1h30m", 90)]
    [InlineData("90m", 90)]
    [InlineData("1:30", 90)]
    [InlineData("2h", 120)]
    [InlineData("45 minutes", 45)]
    [InlineData("1.5", 90)]
    [InlineData("  2h 5m  ", 125)]
    public void ParsesTheFormsPeopleType(string text, double expectedMinutes)
    {
      Assert.True(DurationText.TryParse(text, out TimeSpan duration));
      Assert.Equal(expectedMinutes, duration.TotalMinutes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("about an hour")]
    [InlineData("1h of admin")]
    public void RejectsTextThatIsNotJustADuration(string text)
    {
      Assert.False(DurationText.TryParse(text, out _));
    }

    [Theory]
    [InlineData(90, "1h 30m")]
    [InlineData(120, "2h")]
    [InlineData(45, "45m")]
    [InlineData(0, "0m")]
    public void FormatsBackToTheSameShape(double minutes, string expected)
    {
      Assert.Equal(expected, DurationText.Format(TimeSpan.FromMinutes(minutes)));
    }

    [Fact]
    public void FormattingRoundTripsThroughParsing()
    {
      TimeSpan original = TimeSpan.FromMinutes(215);

      Assert.True(DurationText.TryParse(DurationText.Format(original), out TimeSpan parsed));
      Assert.Equal(original, parsed);
    }
  }
}
