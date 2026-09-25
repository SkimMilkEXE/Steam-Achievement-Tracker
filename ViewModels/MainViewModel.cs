using AchievementTracker.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AchievementTracker.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService = new();

    [ObservableProperty]
    public partial ViewModelBase CurrentPage { get; set; }

    public MainViewModel()
    {
        var settings = _settingsService.Load();
        var hasCredentials = !string.IsNullOrWhiteSpace(settings.ApiKey) && !string.IsNullOrWhiteSpace(settings.SteamIdOrVanity);

        CurrentPage = hasCredentials ? CreateLibraryPage() : CreateSettingsPage();
    }

    private SettingsViewModel CreateSettingsPage()
    {
        var vm = new SettingsViewModel(_settingsService);
        vm.Saved += (_, _) => CurrentPage = CreateLibraryPage();
        return vm;
    }

    private LibraryViewModel CreateLibraryPage()
    {
        var vm = new LibraryViewModel(_settingsService.Load());
        vm.OpenSettingsRequested += (_, _) => CurrentPage = CreateSettingsPage();
        return vm;
    }
}
