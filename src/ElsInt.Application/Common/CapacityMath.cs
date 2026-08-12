namespace ElsInt.Application.Common;

public static class CapacityMath
{
    private const double BtuPerKw = 3412.14;

    private static readonly int[] StandardBtu =
        [5000, 7000, 9000, 12000, 18000, 21000, 24000, 30000, 36000, 42000, 48000, 60000];

    /// <summary>
    /// Derives BTU/h from kW and snaps it to the nearest nominal AC size when close enough,
    /// so catalog filters (9000, 12000, 18000, 24000...) keep matching.
    /// </summary>
    public static int BtuFromKw(decimal kilowatts)
    {
        if (kilowatts <= 0) return 0;

        var raw = (double)kilowatts * BtuPerKw;
        var nearest = StandardBtu.MinBy(b => Math.Abs(b - raw));

        return Math.Abs(nearest - raw) / raw <= 0.10
            ? nearest
            : (int)(Math.Round(raw / 500.0) * 500);
    }
}
