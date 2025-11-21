using System;
using AtomUI.Desktop.Controls;
using Avalonia;
using Avalonia.Interactivity;

namespace AtomUIProgressApp;

public partial class MainWindow : Window
{
    public static readonly StyledProperty<double> ProgressValueProperty =
        AvaloniaProperty.Register<MainWindow, double>(nameof(ProgressValue), 30);

    public double ProgressValue
    {
        get => GetValue(ProgressValueProperty);
        set => SetValue(ProgressValueProperty, value);
    }
    
    protected override Type StyleKeyOverride { get; } = typeof(Window);
    
    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void HandleAddBtnClicked(object? sender, RoutedEventArgs e)
    {
        var value = ProgressValue;
        value         += 10;
        ProgressValue =  Math.Min(value, 100);
    }
    
    private void HandleSubBtnClicked(object? sender, RoutedEventArgs e)
    {
        var value = ProgressValue;
        value         -= 10;
        ProgressValue =  Math.Max(value, 0);
    }
}