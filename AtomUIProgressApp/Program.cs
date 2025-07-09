using Avalonia;
using System;
using System.Globalization;
using AtomUI.Controls;
using AtomUI.Theme;
using AtomUI.Utils;

namespace AtomUIProgressApp;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        var builder = AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .With(new Win32PlatformOptions())
            .LogToTrace();
        
        var themeBuilder = builder.CreateThemeManagerBuilder();
        themeBuilder.UseCultureInfo(new CultureInfo(LanguageCode.en_US));
        themeBuilder.UseTheme(ThemeManager.DEFAULT_THEME_ID);
        themeBuilder.UseOSSControls();
        
        return builder.UseAtomUI(themeBuilder);
    }
}