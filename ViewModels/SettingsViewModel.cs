using System;
using System.Text.RegularExpressions;
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
        SteamIdOrVanity = NormalizeSteamIdOrVanity(SteamIdOrVanity);
        _settingsService.Save(new AppSettings { ApiKey = ApiKey, SteamIdOrVanity = SteamIdOrVanity });
        StatusMessage = "Saved.";
        Saved?.Invoke(this, EventArgs.Empty);
    }

    // Lets people paste a full profile URL (steamcommunity.com/id/NAME or /profiles/ID)
    // instead of having to extract the name/ID themselves.
    private static string NormalizeSteamIdOrVanity(string input)
    {
        var trimmed = input.Trim();
        var match = Regex.Match(trimmed, @"steamcommunity\.com/(?:id|profiles)/([^/\s]+)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : trimmed;
    }
}
