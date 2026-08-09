using System;
using AtomUI;
using AtomUI.Theme.Definitions;

namespace AtomUIProgressApp;

internal static class SampleThemeCatalog
{
    private const string ResolverId = "AtomUIProgressApp.BuiltInThemes";

    public static IAtomUIBuilder UseSampleThemes(this IAtomUIBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Theme.AddThemeDefinitionResolver(
            new AvaloniaAssetThemeDefinitionResolver(
                ResolverId,
                [
                    new Uri("avares://AtomUIProgressApp/Assets/Themes/PolarGreen.theme.xml"),
                    new Uri("avares://AtomUIProgressApp/Assets/Themes/SunsetOrange.theme.xml"),
                    new Uri("avares://AtomUIProgressApp/Assets/Themes/GoldenPurple.theme.xml"),
                    new Uri("avares://AtomUIProgressApp/Assets/Themes/Magenta.theme.xml")
                ],
                typeof(SampleThemeCatalog).Assembly.GetName().Version?.ToString() ?? "0"));

        return builder;
    }
}
