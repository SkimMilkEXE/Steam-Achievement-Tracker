using AchievementTracker.Models;
using AchievementTracker.Services;
using CommunityToolkit.Mvvm.ComponentModel;

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
        var hasCredentials = !string.IsNullOrWhiteSpace(settings.ApiKey) && !string.IsNullOrWhiteSpace(settings.SteamIdOrVanity);

        CurrentPage = hasCredentials ? GetLibraryPage() : CreateSettingsPage();
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
}
