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
    private readonly ISteamDataProvider _steamDataProvider;
    private readonly DatabaseService _databaseService;
    private readonly AppSettings _settings;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    public ObservableCollection<GameListItem> Games { get; } = new();

    public event EventHandler? OpenSettingsRequested;
    public event EventHandler<Game>? OpenGameRequested;

    public LibraryViewModel(AppSettings settings) : this(settings, new WorkerSteamDataProvider(), new DatabaseService())
    {
    }

    public LibraryViewModel(AppSettings settings, ISteamDataProvider steamDataProvider, DatabaseService databaseService)
    {
        _settings = settings;
        _steamDataProvider = steamDataProvider;
        _databaseService = databaseService;

        var cachedGames = _databaseService.GetGames();
        if (cachedGames.Count > 0)
        {
            foreach (var game in cachedGames)
                Games.Add(new GameListItem(game));

            _ = IconLoader.LoadAllAsync(Games, g => g.Game.IconUrl, (g, bmp) => g.Icon = bmp);
        }
        else
        {
            _ = RefreshAsync();
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var steamId = await _steamDataProvider.ResolveSteamIdAsync(_settings.SteamIdOrVanity, _settings.ApiKey);
            var games = await _steamDataProvider.GetOwnedGamesAsync(steamId, _settings.ApiKey);

            _databaseService.SaveGames(games);

            Games.Clear();
            foreach (var game in games)
                Games.Add(new GameListItem(game));

            await IconLoader.LoadAllAsync(Games, g => g.Game.IconUrl, (g, bmp) => g.Icon = bmp);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Couldn't refresh your library: {ex.Message}";
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
