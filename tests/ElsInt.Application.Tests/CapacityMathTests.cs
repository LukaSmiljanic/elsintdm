using ElsInt.Application.Common;
using FluentAssertions;

namespace ElsInt.Application.Tests;

public class CapacityMathTests
{
    [Theory]
    [InlineData(2.6, 9000)]
    [InlineData(3.5, 12000)]
    [InlineData(5.0, 18000)]
    [InlineData(6.2, 21000)]
    [InlineData(7.0, 24000)]
    [InlineData(10.5, 36000)]
    public void SnapsCommonSizesToNominalBtu(decimal kw, int expected)
        => CapacityMath.BtuFromKw(kw).Should().Be(expected);

    [Fact]
    public void ReturnsZeroWhenCapacityMissing()
        => CapacityMath.BtuFromKw(0).Should().Be(0);

    [Fact]
    public void RoundsUnusualCapacityInsteadOfSnapping()
        => CapacityMath.BtuFromKw(4.6m).Should().Be(15500);
}
