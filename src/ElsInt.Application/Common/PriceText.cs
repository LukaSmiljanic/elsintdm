using System.Globalization;

namespace ElsInt.Application.Common;

/// <summary>
/// Serbian thousands formatting for text that ends up in front of customers (meta descriptions,
/// emails). Hardcoded separators instead of a culture lookup so the output cannot change with the
/// server's locale or ICU availability.
/// </summary>
public static class PriceText
{
    private static readonly NumberFormatInfo Format = new()
    {
        NumberGroupSeparator = ".",
        NumberDecimalSeparator = ",",
        NumberGroupSizes = [3]
    };

    /// <summary>42000 -> "42.000".</summary>
    public static string Rsd(decimal value) => value.ToString("N0", Format);
}
