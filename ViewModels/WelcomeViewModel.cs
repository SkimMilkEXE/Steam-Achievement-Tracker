using System;
using System.Linq;
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
        var value = SettingsViewModel.ExtractSteamId(SteamIdOrVanity);
        if (value.Length != 17 || !value.All(char.IsDigit))
        {
            StatusMessage = "Enter your 17-digit SteamID64 or a full profile URL.";
            return;
        }

        var settings = _settingsService.Load();
        settings.SteamIdOrVanity = value;
        _settingsService.Save(settings);
        Saved?.Invoke(this, EventArgs.Empty);
    }
}
