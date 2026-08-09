using System;
using AtomUI.Desktop.Controls;
using AtomUI.Localization;
using AtomUI.Theme;
using Avalonia.Interactivity;
using Avalonia.Media;
using AvaloniaMenuItemToggleType = Avalonia.Controls.MenuItemToggleType;

namespace AtomUIProgressApp;

internal enum SampleWindowMenuItemKind
{
    ThemeDaybreakBlue,
    ThemePolarGreen,
    ThemeSunsetOrange,
    ThemeGoldenPurple,
    ThemeMagenta,
    AppearanceLight,
    AppearanceDark,
    AppearanceSystem,
    Compact,
    LanguageZhCN,
    LanguageZhTW,
    LanguageEnUS,
    LanguagePtBR
}

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    
    protected override Type StyleKeyOverride { get; } = typeof(Window);

    protected override WindowTitleBar? NotifyCreateTitleBar(WindowTitleBar? oldTitleBar)
    {
        return new WindowTitleBar
        {
            Name = "PART_TitleBar",
            FontWeight = FontWeight.Normal
        };
    }
    
    public MainWindow()
    {
        _viewModel = new MainWindowViewModel();
        InitializeComponent();
        DataContext = _viewModel;
    }

    private void HandleAddBtnClicked(object? sender, RoutedEventArgs e)
    {
        _viewModel.AddProgress();
    }
    
    private void HandleSubBtnClicked(object? sender, RoutedEventArgs e)
    {
        _viewModel.SubtractProgress();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        AddHandler(MenuItem.ClickEvent, HandleMenuItemClick);
    }

    private void HandleMenuItemClick(object? sender, RoutedEventArgs e)
    {
        if (e.Source is not MenuItem menuItem ||
            menuItem.Tag is not SampleWindowMenuItemKind kind)
        {
            return;
        }

        if (menuItem.ToggleType == AvaloniaMenuItemToggleType.Radio && !menuItem.IsChecked)
        {
            return;
        }

        switch (kind)
        {
            case SampleWindowMenuItemKind.ThemeDaybreakBlue:
                _viewModel.SelectTheme(IThemeManager.DEFAULT_THEME_ID);
                break;
            case SampleWindowMenuItemKind.ThemePolarGreen:
                _viewModel.SelectTheme("PolarGreen");
                break;
            case SampleWindowMenuItemKind.ThemeSunsetOrange:
                _viewModel.SelectTheme("SunsetOrange");
                break;
            case SampleWindowMenuItemKind.ThemeGoldenPurple:
                _viewModel.SelectTheme("GoldenPurple");
                break;
            case SampleWindowMenuItemKind.ThemeMagenta:
                _viewModel.SelectTheme("Magenta");
                break;
            case SampleWindowMenuItemKind.AppearanceLight:
                _viewModel.SelectAppearance(SampleAppearanceMode.Light);
                break;
            case SampleWindowMenuItemKind.AppearanceDark:
                _viewModel.SelectAppearance(SampleAppearanceMode.Dark);
                break;
            case SampleWindowMenuItemKind.AppearanceSystem:
                _viewModel.SelectAppearance(SampleAppearanceMode.System);
                break;
            case SampleWindowMenuItemKind.Compact:
                _viewModel.SetCompact(menuItem.IsChecked);
                break;
            case SampleWindowMenuItemKind.LanguageZhCN:
                _viewModel.SelectLanguage(LanguageTags.ZhCN);
                break;
            case SampleWindowMenuItemKind.LanguageZhTW:
                _viewModel.SelectLanguage(LanguageTags.ZhTW);
                break;
            case SampleWindowMenuItemKind.LanguageEnUS:
                _viewModel.SelectLanguage(LanguageTags.EnUS);
                break;
            case SampleWindowMenuItemKind.LanguagePtBR:
                _viewModel.SelectLanguage(LanguageTags.PtBR);
                break;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        RemoveHandler(MenuItem.ClickEvent, HandleMenuItemClick);
        _viewModel.Dispose();
        base.OnClosed(e);
    }
}
