using TimeTracker.Utils;
using Xunit;

namespace TimeTracker.Tests
{
  public class MoneyTextTests
  {
    [Theory]
    [InlineData(143.64, "£", "£143.64")]
    [InlineData(143.64, "$", "$143.64")]
    [InlineData(143.64, "A$", "A$143.64")]
    [InlineData(143.64, "CHF", "CHF143.64")]
    [InlineData(900, "£", "£900.00")]
    [InlineData(0, "€", "€0.00")]
    public void WritesTheCurrencyAgainstTheAmount(decimal amount, string currency, string expected)
    {
      Assert.Equal(expected, MoneyText.Format(amount, currency));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void OmitsAMissingCurrencyRatherThanLeadingWithASpace(string? currency)
    {
      Assert.Equal("143.64", MoneyText.Format(143.64m, currency));
    }

    [Fact]
    public void TrimsAStoredCurrencySoItCannotIntroduceASpace()
    {
      Assert.Equal("£143.64", MoneyText.Format(143.64m, " £ "));
    }

    [Theory]
    [InlineData(50, "£", "£50")]
    [InlineData(47.5, "£", "£47.5")]
    [InlineData(47.25, "£", "£47.25")]
    public void DropsTrailingZeroesFromARate(decimal rate, string currency, string expected)
    {
      Assert.Equal(expected, MoneyText.FormatRate(rate, currency));
    }

    [Fact]
    public void LabelsARateWithItsUnit()
    {
      Assert.Equal("£50/hr", MoneyText.FormatRatePerHour(50m, "£"));
    }

    [Fact]
    public void JoinsAnAmountThatArrivesAsText()
    {
      Assert.Equal("£543.07", MoneyText.Join("£", "543.07"));
    }
  }
}
