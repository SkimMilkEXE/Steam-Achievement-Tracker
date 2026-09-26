using System;
using AchievementTracker.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AchievementTracker.ViewModels;

// Branded landing screen shown on every launch. Kept separate from SettingsViewModel so this
// personal, branded screen doesn't get muddled with the plain settings screen used later.
public partial class WelcomeViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;

    public event EventHandler? Saved;

    [ObservableProperty]
    public partial string SteamIdOrVanity { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    public WelcomeViewModel() : this(new SettingsService())
    {
    }

    public WelcomeViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;
        SteamIdOrVanity = settingsService.Load().SteamIdOrVanity;
    }

    [RelayCommand]
    private void GetStarted()
    {
        var normalized = SettingsViewModel.NormalizeSteamIdOrVanity(SteamIdOrVanity);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            StatusMessage = "Enter your Steam ID or profile URL to continue.";
            return;
        }

        var settings = _settingsService.Load();
        settings.SteamIdOrVanity = normalized;
        _settingsService.Save(settings);
        Saved?.Invoke(this, EventArgs.Empty);
    }
}
