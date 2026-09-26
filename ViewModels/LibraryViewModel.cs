using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AchievementTracker.Models;
using AchievementTracker.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AchievementTracker.ViewModels;

public partial class LibraryViewModel : ViewModelBase
{
    // Pace for the background completion warmup - well under the Worker's 60 req/min cap.
    private static readonly TimeSpan WarmupDelay = TimeSpan.FromSeconds(1.5);

    // If the Worker's rate limit is hit anyway (e.g. a manual refresh competing with the
    // warmup), back off for a while instead of continuing to hammer it.
    private static readonly TimeSpan RateLimitBackoff = TimeSpan.FromSeconds(30);

    private readonly ISteamDataProvider _steamDataProvider;
    private readonly DatabaseService _databaseService;
    private readonly AppSettings _settings;
    private List<GameListItem> _allGames = [];
    private CancellationTokenSource? _warmupCts;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial LibrarySortMode SortMode { get; set; } = LibrarySortMode.NameAZ;

    public LibrarySortMode[] SortOptions { get; } = Enum.GetValues<LibrarySortMode>();

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
            StartCompletionWarmup();
        }
        else
        {
            _ = RefreshAsync();
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnSortModeChanged(LibrarySortMode value) => ApplyFilter();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var steamId = await _steamDataProvider.ResolveSteamIdAsync(_settings.SteamIdOrVanity);
            var games = await _steamDataProvider.GetOwnedGamesAsync(steamId);

            _databaseService.SaveGames(games);
            SetGames(games);

            await IconLoader.LoadAllAsync(_allGames, g => g.Game.IconUrl, (g, bmp) => g.Icon = bmp);
            StartCompletionWarmup();
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
        {
            ErrorMessage = "Steam requests are rate-limited right now - try again in a minute.";
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

    // Slowly backfills completion bars for games that have never had their achievements fetched,
    // one request every WarmupDelay, so the whole library fills in over time without bursting
    // past the Worker's rate limit or blocking the UI.
    private void StartCompletionWarmup()
    {
        _warmupCts?.Cancel();
        _warmupCts = new CancellationTokenSource();
        _ = WarmCompletionsAsync(_warmupCts.Token);
    }

    private async Task WarmCompletionsAsync(CancellationToken token)
    {
        string? steamId = null;

        foreach (var item in _allGames.Where(g => !g.HasCompletion).ToList())
        {
            try
            {
                steamId ??= await _steamDataProvider.ResolveSteamIdAsync(_settings.SteamIdOrVanity);
                var achievements = await _steamDataProvider.GetAchievementsAsync(item.Game.AppId, steamId);

                if (achievements.Count > 0)
                {
                    _databaseService.SaveAchievements(item.Game.AppId, achievements);
                    item.SetCompletion(achievements.Count(a => a.Unlocked), achievements.Count);

                    if (SortMode is LibrarySortMode.MostComplete or LibrarySortMode.LeastComplete)
                        ApplyFilter();
                }

                await Task.Delay(WarmupDelay, token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
            {
                // Someone else (a manual refresh, another instance) is using the same rate-limit
                // budget - back off for a while rather than immediately trying the next game too.
                await Task.Delay(RateLimitBackoff, token);
            }
            catch
            {
                // Best-effort: games with no achievements, a transient error, etc. just get skipped this pass.
            }
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
        IEnumerable<GameListItem> query = string.IsNullOrWhiteSpace(SearchText)
            ? _allGames
            : _allGames.Where(g => g.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        query = SortMode switch
        {
            LibrarySortMode.NameAZ => query.OrderBy(g => g.Name, StringComparer.OrdinalIgnoreCase),
            LibrarySortMode.NameZA => query.OrderByDescending(g => g.Name, StringComparer.OrdinalIgnoreCase),
            LibrarySortMode.MostComplete => query.OrderByDescending(g => g.CompletionFraction),
            LibrarySortMode.LeastComplete => query.OrderBy(g => g.CompletionFraction),
            _ => query
        };

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
