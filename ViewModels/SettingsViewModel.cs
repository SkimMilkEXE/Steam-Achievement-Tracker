using System;
using System.Text.RegularExpressions;
using AchievementTracker.Models;
using AchievementTracker.Services;
using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AchievementTracker.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;

    public event EventHandler? Saved;

    [ObservableProperty]
    public partial string SteamIdOrVanity { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ThemeOption Theme { get; set; } = ThemeOption.System;

    [ObservableProperty]
    public partial bool RevealHiddenAchievements { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    public ThemeOption[] ThemeOptions { get; } = Enum.GetValues<ThemeOption>();

    public SettingsViewModel() : this(new SettingsService())
    {
    }

    public SettingsViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;
        var settings = _settingsService.Load();
        SteamIdOrVanity = settings.SteamIdOrVanity;
        Theme = Enum.TryParse<ThemeOption>(settings.Theme, out var theme) ? theme : ThemeOption.System;
        RevealHiddenAchievements = settings.RevealHiddenAchievements;
    }

    // Theme and the hidden-achievement toggle are simple preferences - apply and persist
    // immediately, rather than waiting on the explicit Save button used for credentials below.
    partial void OnThemeChanged(ThemeOption value)
    {
        ApplyTheme(value);
        var current = _settingsService.Load();
        current.Theme = value.ToString();
        _settingsService.Save(current);
    }

    partial void OnRevealHiddenAchievementsChanged(bool value)
    {
        var current = _settingsService.Load();
        current.RevealHiddenAchievements = value;
        _settingsService.Save(current);
    }

    public static void ApplyTheme(ThemeOption theme)
    {
        if (Application.Current is { } app)
        {
            app.RequestedThemeVariant = theme switch
            {
                ThemeOption.Light => ThemeVariant.Light,
                ThemeOption.Dark => ThemeVariant.Dark,
                _ => ThemeVariant.Default
            };
        }
    }

    [RelayCommand]
    private void Save()
    {
        SteamIdOrVanity = NormalizeSteamIdOrVanity(SteamIdOrVanity);
        _settingsService.Save(new AppSettings
        {
            SteamIdOrVanity = SteamIdOrVanity,
            Theme = Theme.ToString(),
            RevealHiddenAchievements = RevealHiddenAchievements
        });
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
