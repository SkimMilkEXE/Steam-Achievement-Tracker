using System;
using AchievementTracker.Models;
using AchievementTracker.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AchievementTracker.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;

    public event EventHandler? Saved;

    [ObservableProperty]
    public partial string ApiKey { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SteamIdOrVanity { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    public SettingsViewModel() : this(new SettingsService())
    {
    }

    public SettingsViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;
        var settings = _settingsService.Load();
        ApiKey = settings.ApiKey;
        SteamIdOrVanity = settings.SteamIdOrVanity;
    }

    [RelayCommand]
    private void Save()
    {
        _settingsService.Save(new AppSettings { ApiKey = ApiKey, SteamIdOrVanity = SteamIdOrVanity });
        StatusMessage = "Saved.";
        Saved?.Invoke(this, EventArgs.Empty);
    }
}
