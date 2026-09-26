using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AchievementTracker.Services;
using AchievementTracker.ViewModels;
using AchievementTracker.Views;

namespace AchievementTracker;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var settings = new SettingsService().Load();
        if (Enum.TryParse<ThemeOption>(settings.Theme, out var theme))
            SettingsViewModel.ApplyTheme(theme);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}