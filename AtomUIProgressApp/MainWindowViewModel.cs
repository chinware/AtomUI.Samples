using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using AtomUI;
using AtomUI.Controls;
using AtomUI.Desktop.Controls;
using AtomUI.Localization;
using AtomUI.Theme;
using AtomUI.Theme.Algorithms;
using AtomUI.Theme.Configuration;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Styling;

namespace AtomUIProgressApp;

public sealed class MainWindowViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IThemeManager? _themeManager;
    private readonly ILanguageManager? _languageManager;
    private readonly EventHandler<ThemeChangedEventArgs>? _themeChangedHandler;
    private readonly EventHandler<ThemeCatalogChangedEventArgs>? _themeCatalogChangedHandler;
    private readonly EventHandler<LanguageChangedEventArgs>? _languageChangedHandler;
    private IDisposable? _systemAppearanceSubscription;
    private bool _isDisposed;
    private bool _isSyncingSelection;
    private bool _isApplyingTheme;
    private bool _isCompact;
    private double _progressValue = 30;
    private SampleCopy _copy;
    private IReadOnlyList<ISelectOption> _themeOptions = [];
    private IReadOnlyList<ISelectOption> _appearanceOptions = [];
    private SampleSelectOption<LanguageTag>? _selectedLanguageOption;
    private SampleSelectOption<string>? _selectedThemeOption;
    private SampleSelectOption<SampleAppearanceMode>? _selectedAppearanceOption;
    private string _statusText = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainWindowViewModel()
    {
        _copy = ResolveCopy(LanguageTags.EnUS);
        LanguageOptions =
        [
            new SampleSelectOption<LanguageTag>("zh-CN", "简体中文", LanguageTags.ZhCN),
            new SampleSelectOption<LanguageTag>("zh-TW", "繁體中文", LanguageTags.ZhTW),
            new SampleSelectOption<LanguageTag>("en-US", "English", LanguageTags.EnUS),
            new SampleSelectOption<LanguageTag>("pt-BR", "Português (Brasil)", LanguageTags.PtBR)
        ];

        _themeManager = Application.Current?.GetThemeManager();
        _languageManager = Application.Current?.GetLanguageManager();

        RebuildThemeOptions(_themeManager?.AvailableThemes);
        RebuildAppearanceOptions(SampleAppearanceMode.Light);
        SyncThemeState(_themeManager?.CurrentTheme);
        SyncLanguageState(_languageManager?.Current);

        if (_themeManager is not null)
        {
            _themeChangedHandler = HandleThemeChanged;
            _themeCatalogChangedHandler = HandleThemeCatalogChanged;
            _themeManager.ThemeChanged += _themeChangedHandler;
            _themeManager.ThemeCatalogChanged += _themeCatalogChangedHandler;
        }

        if (_languageManager is not null)
        {
            _languageChangedHandler = HandleLanguageChanged;
            _languageManager.LanguageChanged += _languageChangedHandler;
        }

        UpdateStatus();
    }

    public IReadOnlyList<ISelectOption> LanguageOptions { get; }

    public IReadOnlyList<ISelectOption> ThemeOptions
    {
        get => _themeOptions;
        private set => SetField(ref _themeOptions, value);
    }

    public IReadOnlyList<ISelectOption> AppearanceOptions
    {
        get => _appearanceOptions;
        private set => SetField(ref _appearanceOptions, value);
    }

    public ISelectOption? SelectedLanguageOption
    {
        get => _selectedLanguageOption;
        set
        {
            if (ReferenceEquals(_selectedLanguageOption, value))
            {
                return;
            }

            _selectedLanguageOption = value as SampleSelectOption<LanguageTag>;
            OnPropertyChanged();
            RaiseLanguageSelectionChanged();

            if (!_isSyncingSelection && _selectedLanguageOption is { } option)
            {
                ChangeLanguage(option.Value);
            }
        }
    }

    public ISelectOption? SelectedThemeOption
    {
        get => _selectedThemeOption;
        set
        {
            if (ReferenceEquals(_selectedThemeOption, value))
            {
                return;
            }

            _selectedThemeOption = value as SampleSelectOption<string>;
            OnPropertyChanged();
            RaiseThemeSelectionChanged();
            UpdateStatus();

            if (!_isSyncingSelection)
            {
                ApplyCurrentThemeSettings(ThemeTransitionReason.UserRequest);
            }
        }
    }

    public ISelectOption? SelectedAppearanceOption
    {
        get => _selectedAppearanceOption;
        set
        {
            if (ReferenceEquals(_selectedAppearanceOption, value))
            {
                return;
            }

            _selectedAppearanceOption = value as SampleSelectOption<SampleAppearanceMode>;
            OnPropertyChanged();
            RaiseAppearanceSelectionChanged();
            UpdateSystemAppearanceSubscription();
            UpdateStatus();

            if (!_isSyncingSelection)
            {
                ApplyCurrentThemeSettings(
                    CurrentAppearanceMode == SampleAppearanceMode.System
                        ? ThemeTransitionReason.FollowSystem
                        : ThemeTransitionReason.UserRequest);
            }
        }
    }

    public bool IsCompact
    {
        get => _isCompact;
        set
        {
            if (SetField(ref _isCompact, value) && !_isSyncingSelection && !_isApplyingTheme)
            {
                ApplyCurrentThemeSettings(ThemeTransitionReason.UserRequest);
            }
        }
    }

    public double ProgressValue
    {
        get => _progressValue;
        set => SetField(ref _progressValue, Math.Clamp(value, 0, 100));
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetField(ref _statusText, value);
    }

    public string WindowTitle => _copy.WindowTitle;
    public string ToolbarTitle => _copy.ToolbarTitle;
    public string LanguageLabel => _copy.LanguageLabel;
    public string ThemeLabel => _copy.ThemeLabel;
    public string AppearanceLabel => _copy.AppearanceLabel;
    public string CompactLabel => _copy.CompactLabel;
    public string LightModeLabel => _copy.LightMode;
    public string DarkModeLabel => _copy.DarkMode;
    public string SystemModeLabel => _copy.SystemMode;
    public string ProgressSectionTitle => _copy.ProgressSectionTitle;
    public string IconsSectionTitle => _copy.IconsSectionTitle;
    public string AddButtonText => _copy.AddButtonText;
    public string SubButtonText => _copy.SubButtonText;
    public string IconButtonText => _copy.IconButtonText;
    public bool IsDaybreakBlueTheme => IsSelectedTheme(IThemeManager.DEFAULT_THEME_ID);
    public bool IsPolarGreenTheme => IsSelectedTheme("PolarGreen");
    public bool IsSunsetOrangeTheme => IsSelectedTheme("SunsetOrange");
    public bool IsGoldenPurpleTheme => IsSelectedTheme("GoldenPurple");
    public bool IsMagentaTheme => IsSelectedTheme("Magenta");
    public bool IsLightAppearanceMode => CurrentAppearanceMode == SampleAppearanceMode.Light;
    public bool IsDarkAppearanceMode => CurrentAppearanceMode == SampleAppearanceMode.Dark;
    public bool IsSystemAppearanceMode => CurrentAppearanceMode == SampleAppearanceMode.System;
    public bool IsZhCN => _selectedLanguageOption?.Value == LanguageTags.ZhCN;
    public bool IsZhTW => _selectedLanguageOption?.Value == LanguageTags.ZhTW;
    public bool IsEnUS => _selectedLanguageOption?.Value == LanguageTags.EnUS;
    public bool IsPtBR => _selectedLanguageOption?.Value == LanguageTags.PtBR;

    private SampleAppearanceMode CurrentAppearanceMode =>
        _selectedAppearanceOption?.Value ?? SampleAppearanceMode.Light;

    public void AddProgress()
    {
        ProgressValue += 10;
    }

    public void SubtractProgress()
    {
        ProgressValue -= 10;
    }

    public void SelectLanguage(LanguageTag language)
    {
        ChangeLanguage(language);
    }

    public void SelectTheme(string themeId)
    {
        var option = ThemeOptions
                     .OfType<SampleSelectOption<string>>()
                     .FirstOrDefault(item => string.Equals(item.Value, themeId, StringComparison.Ordinal));
        if (option is not null)
        {
            SelectedThemeOption = option;
        }
    }

    public void SelectAppearance(SampleAppearanceMode mode)
    {
        var option = AppearanceOptions
                     .OfType<SampleSelectOption<SampleAppearanceMode>>()
                     .FirstOrDefault(item => item.Value == mode);
        if (option is not null)
        {
            SelectedAppearanceOption = option;
        }
    }

    public void SetCompact(bool isCompact)
    {
        IsCompact = isCompact;
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _systemAppearanceSubscription?.Dispose();
        _systemAppearanceSubscription = null;

        if (_themeManager is not null && _themeChangedHandler is not null)
        {
            _themeManager.ThemeChanged -= _themeChangedHandler;
        }

        if (_themeManager is not null && _themeCatalogChangedHandler is not null)
        {
            _themeManager.ThemeCatalogChanged -= _themeCatalogChangedHandler;
        }

        if (_languageManager is not null && _languageChangedHandler is not null)
        {
            _languageManager.LanguageChanged -= _languageChangedHandler;
        }
    }

    private void ChangeLanguage(LanguageTag language)
    {
        try
        {
            _languageManager?.ChangeLanguage(language);
            SyncLanguageState(_languageManager?.Current);
        }
        catch (Exception exception)
        {
            StatusText = string.Format(CultureInfo.CurrentCulture, _copy.ErrorStatus, exception.Message);
        }
    }

    private async void ApplyCurrentThemeSettings(ThemeTransitionReason reason)
    {
        if (_themeManager is null || _isApplyingTheme || _selectedThemeOption is null)
        {
            return;
        }

        _isApplyingTheme = true;
        try
        {
            var result = await _themeManager.ApplyThemeAsync(
                new ThemeRequest(_selectedThemeOption.Value, CreateThemeConfig(), reason));

            if (result.Status == ThemeTransitionStatus.Failed)
            {
                var message = result.Exception?.Message ??
                              string.Join(" ", result.Diagnostics.Select(static diagnostic => diagnostic.Message));
                StatusText = string.Format(CultureInfo.CurrentCulture, _copy.ErrorStatus, message);
                return;
            }

            SyncThemeState(result.State ?? _themeManager.CurrentTheme);
        }
        catch (Exception exception)
        {
            StatusText = string.Format(CultureInfo.CurrentCulture, _copy.ErrorStatus, exception.Message);
        }
        finally
        {
            _isApplyingTheme = false;
        }
    }

    private ThemeConfig CreateThemeConfig()
    {
        var algorithms = new List<ThemeAlgorithm> { ThemeAlgorithm.Default };
        if (IsCompact)
        {
            algorithms.Add(ThemeAlgorithm.Compact);
        }

        if (ResolveRequestedAppearance() == ThemeAppearance.Dark)
        {
            algorithms.Add(ThemeAlgorithm.Dark);
        }

        return new ThemeConfigBuilder()
               .WithAlgorithms(algorithms.ToArray())
               .Build();
    }

    private ThemeAppearance ResolveRequestedAppearance()
    {
        return CurrentAppearanceMode switch
        {
            SampleAppearanceMode.Dark => ThemeAppearance.Dark,
            SampleAppearanceMode.System => GetCurrentSystemAppearance(),
            _ => ThemeAppearance.Light
        };
    }

    private void RebuildThemeOptions(IReadOnlyList<ThemeInfo>? themes)
    {
        var previousThemeId = _selectedThemeOption?.Value ??
                              _themeManager?.CurrentTheme?.ThemeId ??
                              IThemeManager.DEFAULT_THEME_ID;
        var orderedThemes = (themes is { Count: > 0 } ? themes : Array.Empty<ThemeInfo>())
                            .OrderByDescending(static theme => theme.IsDefault)
                            .ThenBy(static theme => theme.Name, StringComparer.Ordinal)
                            .Select(static theme => new SampleSelectOption<string>(
                                theme.Id,
                                theme.Name,
                                theme.Id))
                            .Cast<ISelectOption>()
                            .ToArray();

        ThemeOptions = orderedThemes;
        SyncSelectedTheme(previousThemeId);
    }

    private void RebuildAppearanceOptions(SampleAppearanceMode currentMode)
    {
        AppearanceOptions =
        [
            new SampleSelectOption<SampleAppearanceMode>("light", _copy.LightMode, SampleAppearanceMode.Light),
            new SampleSelectOption<SampleAppearanceMode>("dark", _copy.DarkMode, SampleAppearanceMode.Dark),
            new SampleSelectOption<SampleAppearanceMode>("system", _copy.SystemMode, SampleAppearanceMode.System)
        ];
        SyncSelectedAppearance(currentMode);
    }

    private void SyncThemeState(ThemeState? state)
    {
        if (state is null)
        {
            SyncSelectedTheme(IThemeManager.DEFAULT_THEME_ID);
            return;
        }

        SyncSelectedTheme(state.ThemeId);
        var appearanceMode = state.Appearance == ThemeAppearance.Dark
            ? SampleAppearanceMode.Dark
            : SampleAppearanceMode.Light;
        if (CurrentAppearanceMode != SampleAppearanceMode.System)
        {
            SyncSelectedAppearance(appearanceMode);
        }
        SetSelectionWithoutApplying(() => IsCompact = state.Algorithms.Contains(ThemeAlgorithm.Compact));
        UpdateStatus();
    }

    private void SyncLanguageState(LanguageState? state)
    {
        var language = state?.CurrentLanguage ?? SampleLanguageDefaults.Resolve(CultureInfo.CurrentUICulture);
        _copy = ResolveCopy(language);
        SyncSelectedLanguage(language);
        RebuildAppearanceOptions(CurrentAppearanceMode);
        RaiseCopyChanged();
        UpdateStatus();
    }

    private void SyncSelectedLanguage(LanguageTag language)
    {
        var option = LanguageOptions
                     .OfType<SampleSelectOption<LanguageTag>>()
                     .FirstOrDefault(item => item.Value == language);
        if (option is null)
        {
            return;
        }

        SetSelectionWithoutApplying(() => SelectedLanguageOption = option);
    }

    private void SyncSelectedTheme(string themeId)
    {
        var option = ThemeOptions
                     .OfType<SampleSelectOption<string>>()
                     .FirstOrDefault(item => string.Equals(item.Value, themeId, StringComparison.Ordinal)) ??
                     ThemeOptions.OfType<SampleSelectOption<string>>().FirstOrDefault();
        if (option is null)
        {
            return;
        }

        SetSelectionWithoutApplying(() => SelectedThemeOption = option);
    }

    private void SyncSelectedAppearance(SampleAppearanceMode mode)
    {
        var option = AppearanceOptions
                     .OfType<SampleSelectOption<SampleAppearanceMode>>()
                     .FirstOrDefault(item => item.Value == mode);
        if (option is null)
        {
            return;
        }

        SetSelectionWithoutApplying(() => SelectedAppearanceOption = option);
    }

    private void UpdateSystemAppearanceSubscription()
    {
        _systemAppearanceSubscription?.Dispose();
        _systemAppearanceSubscription = null;

        if (CurrentAppearanceMode != SampleAppearanceMode.System)
        {
            return;
        }

        var settings = Application.Current?.PlatformSettings;
        if (settings is null)
        {
            return;
        }

        void HandleColorValuesChanged(object? sender, PlatformColorValues values)
        {
            ApplyCurrentThemeSettings(ThemeTransitionReason.FollowSystem);
        }

        settings.ColorValuesChanged += HandleColorValuesChanged;
        _systemAppearanceSubscription = new ActionDisposable(
            () => settings.ColorValuesChanged -= HandleColorValuesChanged);
    }

    private static ThemeAppearance GetCurrentSystemAppearance()
    {
        if (Application.Current?.PlatformSettings is { } settings)
        {
            return settings.GetColorValues().ThemeVariant == PlatformThemeVariant.Dark
                ? ThemeAppearance.Dark
                : ThemeAppearance.Light;
        }

        return Application.Current?.ActualThemeVariant == ThemeVariant.Dark
            ? ThemeAppearance.Dark
            : ThemeAppearance.Light;
    }

    private void HandleThemeChanged(object? sender, ThemeChangedEventArgs args)
    {
        if (!_isDisposed)
        {
            SyncThemeState(args.State);
        }
    }

    private void HandleThemeCatalogChanged(object? sender, ThemeCatalogChangedEventArgs args)
    {
        if (!_isDisposed)
        {
            RebuildThemeOptions(args.AvailableThemes);
            if (args.CurrentTheme is not null)
            {
                SyncThemeState(args.CurrentTheme);
            }
        }
    }

    private void HandleLanguageChanged(object? sender, LanguageChangedEventArgs args)
    {
        if (!_isDisposed)
        {
            SyncLanguageState(args.Result.NewState);
        }
    }

    private void SetSelectionWithoutApplying(Action update)
    {
        _isSyncingSelection = true;
        try
        {
            update();
        }
        finally
        {
            _isSyncingSelection = false;
        }
    }

    private void UpdateStatus()
    {
        var language = _selectedLanguageOption?.Header?.ToString() ?? "-";
        var theme = _selectedThemeOption?.Header?.ToString() ?? "-";
        var appearance = _selectedAppearanceOption?.Header?.ToString() ?? "-";
        StatusText = string.Format(CultureInfo.CurrentCulture, _copy.Status, language, theme, appearance);
    }

    private bool IsSelectedTheme(string themeId)
    {
        return string.Equals(_selectedThemeOption?.Value, themeId, StringComparison.Ordinal);
    }

    private void RaiseCopyChanged()
    {
        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(ToolbarTitle));
        OnPropertyChanged(nameof(LanguageLabel));
        OnPropertyChanged(nameof(ThemeLabel));
        OnPropertyChanged(nameof(AppearanceLabel));
        OnPropertyChanged(nameof(CompactLabel));
        OnPropertyChanged(nameof(LightModeLabel));
        OnPropertyChanged(nameof(DarkModeLabel));
        OnPropertyChanged(nameof(SystemModeLabel));
        OnPropertyChanged(nameof(ProgressSectionTitle));
        OnPropertyChanged(nameof(IconsSectionTitle));
        OnPropertyChanged(nameof(AddButtonText));
        OnPropertyChanged(nameof(SubButtonText));
        OnPropertyChanged(nameof(IconButtonText));
    }

    private void RaiseThemeSelectionChanged()
    {
        OnPropertyChanged(nameof(IsDaybreakBlueTheme));
        OnPropertyChanged(nameof(IsPolarGreenTheme));
        OnPropertyChanged(nameof(IsSunsetOrangeTheme));
        OnPropertyChanged(nameof(IsGoldenPurpleTheme));
        OnPropertyChanged(nameof(IsMagentaTheme));
    }

    private void RaiseAppearanceSelectionChanged()
    {
        OnPropertyChanged(nameof(IsLightAppearanceMode));
        OnPropertyChanged(nameof(IsDarkAppearanceMode));
        OnPropertyChanged(nameof(IsSystemAppearanceMode));
    }

    private void RaiseLanguageSelectionChanged()
    {
        OnPropertyChanged(nameof(IsZhCN));
        OnPropertyChanged(nameof(IsZhTW));
        OnPropertyChanged(nameof(IsEnUS));
        OnPropertyChanged(nameof(IsPtBR));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private static SampleCopy ResolveCopy(LanguageTag language)
    {
        if (language == LanguageTags.ZhCN)
        {
            return new SampleCopy(
                "AtomUI 示例",
                "偏好设置",
                "语言",
                "主题",
                "外观",
                "紧凑",
                "亮色",
                "暗色",
                "跟随系统",
                "进度",
                "图标",
                "增加",
                "减少",
                "点击我",
                "当前语言：{0} · 主题：{1} · 外观：{2}",
                "操作失败：{0}");
        }

        if (language == LanguageTags.ZhTW)
        {
            return new SampleCopy(
                "AtomUI 範例",
                "偏好設定",
                "語言",
                "主題",
                "外觀",
                "緊湊",
                "亮色",
                "暗色",
                "跟隨系統",
                "進度",
                "圖示",
                "增加",
                "減少",
                "點擊我",
                "目前語言：{0} · 主題：{1} · 外觀：{2}",
                "操作失敗：{0}");
        }

        if (language == LanguageTags.PtBR)
        {
            return new SampleCopy(
                "Amostra AtomUI",
                "Preferencias",
                "Idioma",
                "Tema",
                "Aparencia",
                "Compacto",
                "Claro",
                "Escuro",
                "Sistema",
                "Progresso",
                "Icones",
                "Adicionar",
                "Reduzir",
                "Clique",
                "Idioma: {0} · Tema: {1} · Aparencia: {2}",
                "Falha na operacao: {0}");
        }

        return new SampleCopy(
            "AtomUI Sample",
            "Preferences",
            "Language",
            "Theme",
            "Appearance",
            "Compact",
            "Light",
            "Dark",
            "System",
            "Progress",
            "Icons",
            "Add",
            "Sub",
            "Click me",
            "Language: {0} · Theme: {1} · Appearance: {2}",
            "Operation failed: {0}");
    }

    private sealed record SampleCopy(
        string WindowTitle,
        string ToolbarTitle,
        string LanguageLabel,
        string ThemeLabel,
        string AppearanceLabel,
        string CompactLabel,
        string LightMode,
        string DarkMode,
        string SystemMode,
        string ProgressSectionTitle,
        string IconsSectionTitle,
        string AddButtonText,
        string SubButtonText,
        string IconButtonText,
        string Status,
        string ErrorStatus);

    private sealed class SampleSelectOption<T> : ISelectOption
    {
        public SampleSelectOption(string key, string header, T value)
        {
            ItemKey = key;
            Header = header;
            Value = value;
        }

        public T Value { get; }
        public EntityKey? ItemKey { get; }
        public string? Group { get; init; }
        public bool IsEnabled { get; set; } = true;
        public object? Content { get; set; }
        public object? Header { get; }
        public bool IsDynamicAdded => false;
        public override string ToString() => Header?.ToString() ?? string.Empty;
    }

    private sealed class ActionDisposable(Action dispose) : IDisposable
    {
        private Action? _dispose = dispose;

        public void Dispose()
        {
            Interlocked.Exchange(ref _dispose, null)?.Invoke();
        }
    }
}

public enum SampleAppearanceMode
{
    Light,
    Dark,
    System
}
