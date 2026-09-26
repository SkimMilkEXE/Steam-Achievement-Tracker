using AchievementTracker.Models;
using AchievementTracker.Services;
using AchievementTracker.Views;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AchievementTracker.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService = new();
    private LibraryViewModel? _libraryPage;

    [ObservableProperty]
    public partial ViewModelBase CurrentPage { get; set; }

    public MainViewModel()
    {
        var settings = _settingsService.Load();

        // v2 (the Worker) needs no API key - only a Steam ID/vanity name to get started.
        CurrentPage = string.IsNullOrWhiteSpace(settings.SteamIdOrVanity) ? CreateSettingsPage() : GetLibraryPage();
    }

    private SettingsViewModel CreateSettingsPage()
    {
        var vm = new SettingsViewModel(_settingsService);
        vm.Saved += (_, _) =>
        {
            _libraryPage = null;
            CurrentPage = GetLibraryPage();
        };
        return vm;
    }

    private LibraryViewModel GetLibraryPage()
    {
        if (_libraryPage is not null)
            return _libraryPage;

        var vm = new LibraryViewModel(_settingsService.Load());
        vm.OpenSettingsRequested += (_, _) => CurrentPage = CreateSettingsPage();
        vm.OpenHuntingRequested += (_, _) => CurrentPage = CreateHuntingPage();
        vm.OpenGameRequested += (_, game) => CurrentPage = CreateGameDetailPage(game);
        _libraryPage = vm;
        return vm;
    }

    private GameDetailViewModel CreateGameDetailPage(Game game)
    {
        var vm = new GameDetailViewModel(game, _settingsService.Load());
        vm.BackRequested += (_, _) => CurrentPage = GetLibraryPage();
        return vm;
    }

    private HuntingViewModel CreateHuntingPage()
    {
        var vm = new HuntingViewModel();
        vm.BackRequested += (_, _) => CurrentPage = GetLibraryPage();
        vm.OpenGameRequested += (_, game) => CurrentPage = CreateGameDetailPage(game);
        return vm;
    }

    [RelayCommand]
    private void NavigateLibrary() => CurrentPage = GetLibraryPage();

    [RelayCommand]
    private void NavigateSettings() => CurrentPage = CreateSettingsPage();

    [RelayCommand]
    private void NavigateHunting() => CurrentPage = CreateHuntingPage();

    [RelayCommand]
    private void ShowAbout()
    {
        var about = new AboutWindow();
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } main })
            about.ShowDialog(main);
        else
            about.Show();
    }

    [RelayCommand]
    private void Exit()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            lifetime.Shutdown();
    }
}
