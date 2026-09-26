using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
    private List<GameListItem> _allGames = [];

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    public ObservableCollection<GameListItem> Games { get; } = new();

    public event EventHandler? OpenSettingsRequested;
    public event EventHandler? OpenHuntingRequested;
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
            SetGames(cachedGames);
            _ = IconLoader.LoadAllAsync(_allGames, g => g.Game.IconUrl, (g, bmp) => g.Icon = bmp);
        }
        else
        {
            _ = RefreshAsync();
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

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
            SetGames(games);

            await IconLoader.LoadAllAsync(_allGames, g => g.Game.IconUrl, (g, bmp) => g.Icon = bmp);
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

    private void SetGames(IEnumerable<Game> games)
    {
        var completion = _databaseService.GetCompletionSummaries();

        _allGames = games.Select(g =>
        {
            var (unlocked, total) = completion.TryGetValue(g.AppId, out var summary) ? summary : (0, 0);
            return new GameListItem(g, unlocked, total);
        }).ToList();

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var query = string.IsNullOrWhiteSpace(SearchText)
            ? _allGames
            : _allGames.Where(g => g.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        Games.Clear();
        foreach (var game in query)
            Games.Add(game);
    }

    [RelayCommand]
    private void OpenSettings() => OpenSettingsRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void OpenHunting() => OpenHuntingRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void OpenGame(GameListItem? item)
    {
        if (item is not null)
            OpenGameRequested?.Invoke(this, item.Game);
    }
}
