using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using AchievementTracker.Models;
using AchievementTracker.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AchievementTracker.ViewModels;

public partial class LibraryViewModel : ViewModelBase
{
    private readonly SteamApiService _steamApiService;
    private readonly AppSettings _settings;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    public ObservableCollection<GameListItem> Games { get; } = new();

    public event EventHandler? OpenSettingsRequested;
    public event EventHandler<Game>? OpenGameRequested;

    public LibraryViewModel(AppSettings settings) : this(settings, new SteamApiService())
    {
    }

    public LibraryViewModel(AppSettings settings, SteamApiService steamApiService)
    {
        _settings = settings;
        _steamApiService = steamApiService;
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        Games.Clear();

        try
        {
            var steamId = await _steamApiService.ResolveSteamIdAsync(_settings.SteamIdOrVanity, _settings.ApiKey);
            var games = await _steamApiService.GetOwnedGamesAsync(steamId, _settings.ApiKey);

            foreach (var game in games)
                Games.Add(new GameListItem(game));

            await IconLoader.LoadAllAsync(Games, g => g.Game.IconUrl, (g, bmp) => g.Icon = bmp);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Couldn't load your library: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void OpenSettings() => OpenSettingsRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void OpenGame(GameListItem? item)
    {
        if (item is not null)
            OpenGameRequested?.Invoke(this, item.Game);
    }
}
