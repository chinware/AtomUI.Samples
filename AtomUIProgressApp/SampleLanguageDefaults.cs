using System;
using System.Globalization;
using AtomUI.Localization;

namespace AtomUIProgressApp;

internal static class SampleLanguageDefaults
{
    public static LanguageTag Resolve(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);

        var name = culture.Name;
        if (name.Equals("zh-TW", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("zh-HK", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("zh-MO", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("zh-Hant", StringComparison.OrdinalIgnoreCase))
        {
            return LanguageTags.ZhTW;
        }

        if (name.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
        {
            return LanguageTags.ZhCN;
        }

        if (name.Equals("pt-BR", StringComparison.OrdinalIgnoreCase))
        {
            return LanguageTags.PtBR;
        }

        return LanguageTags.EnUS;
    }
}
