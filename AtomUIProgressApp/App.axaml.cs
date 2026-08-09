using System.Globalization;
using AtomUI;
using AtomUI.Desktop.Controls;
using AtomUI.Localization;
using AtomUI.Theme;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace AtomUIProgressApp;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        this.UseAtomUI(builder =>
        {
            builder.UseLanguages(
                SampleLanguageDefaults.Resolve(CultureInfo.CurrentUICulture),
                [LanguageTags.EnUS, LanguageTags.ZhCN, LanguageTags.ZhTW, LanguageTags.PtBR]);
            builder.WithInitialTheme(IThemeManager.DEFAULT_THEME_ID);
            builder.UseSampleThemes();
            builder.UseAlibabaSansFont();
            builder.UseDesktopControls();
        });
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
