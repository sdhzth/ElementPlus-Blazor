using System.Globalization;

namespace ElementPlus
{
    internal static class ElementCssUtility
    {
        public static string NormalizeCssSize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            var trimmed = value.Trim();
            return decimal.TryParse(trimmed, NumberStyles.Number, CultureInfo.InvariantCulture, out _)
                ? $"{trimmed}px"
                : trimmed;
        }
    }
}
