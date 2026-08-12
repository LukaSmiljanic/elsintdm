using System.Globalization;
using ElsInt.Application.Common;
using FluentAssertions;

namespace ElsInt.Application.Tests;

public class PriceTextTests
{
    [Theory]
    [InlineData(42000, "42.000")]
    [InlineData(129990, "129.990")]
    [InlineData(999, "999")]
    [InlineData(1234567, "1.234.567")]
    [InlineData(0, "0")]
    public void FormatsWithSerbianThousandsSeparator(decimal value, string expected)
        => PriceText.Rsd(value).Should().Be(expected);

    [Fact]
    public void RoundsAwayDecimals()
        => PriceText.Rsd(42000.49m).Should().Be("42.000");

    [Fact]
    public void IgnoresAmbientCulture()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-US");
            PriceText.Rsd(42000).Should().Be("42.000");
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
