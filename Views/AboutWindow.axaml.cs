using System.Reflection;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AchievementTracker.Views;

public partial class AboutWindow : Window
{
    public string Version { get; } = $"v{Assembly.GetExecutingAssembly().GetName().Version?.ToString(2)}";

    public AboutWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
