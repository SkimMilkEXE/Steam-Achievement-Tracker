using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AchievementTracker.Models;
using AchievementTracker.Services;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AchievementTracker.ViewModels;

public partial class GameDetailViewModel : ViewModelBase
{
    private readonly Game _game;
    private readonly AppSettings _settings;
    private readonly ISteamDataProvider _steamDataProvider;
    private readonly DatabaseService _databaseService;
    private List<AchievementListItem> _allAchievements = [];

    public string GameName => _game.Name;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial AchievementFilter Filter { get; set; } = AchievementFilter.All;

    [ObservableProperty]
    public partial string CompletionText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double CompletionFraction { get; set; }

    [ObservableProperty]
    public partial IImage? TrophyIcon { get; set; }

    public AchievementFilter[] FilterOptions { get; } = Enum.GetValues<AchievementFilter>();

    public ObservableCollection<AchievementListItem> Achievements { get; } = new();

    public event EventHandler? BackRequested;

    public GameDetailViewModel(Game game, AppSettings settings)
        : this(game, settings, new WorkerSteamDataProvider(), new DatabaseService())
    {
    }

    public GameDetailViewModel(Game game, AppSettings settings, ISteamDataProvider steamDataProvider, DatabaseService databaseService)
    {
        _game = game;
        _settings = settings;
        _steamDataProvider = steamDataProvider;
        _databaseService = databaseService;

        var cachedAchievements = _databaseService.GetAchievements(game.AppId);
        if (cachedAchievements.Count > 0)
        {
            SetAchievements(cachedAchievements);
            _ = IconLoader.LoadAllAsync(_allAchievements, a => a.IconUrl, (a, bmp) => a.Icon = bmp);
        }
        else
        {
            _ = RefreshAsync();
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyView();
    partial void OnFilterChanged(AchievementFilter value) => ApplyView();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var steamId = await _steamDataProvider.ResolveSteamIdAsync(_settings.SteamIdOrVanity);
            var achievements = await _steamDataProvider.GetAchievementsAsync(_game.AppId, steamId);

            if (achievements.Count == 0)
            {
                ErrorMessage = "This game has no trackable achievements.";
                return;
            }

            _databaseService.SaveAchievements(_game.AppId, achievements);
            SetAchievements(achievements);

            await IconLoader.LoadAllAsync(_allAchievements, a => a.IconUrl, (a, bmp) => a.Icon = bmp);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
        {
            ErrorMessage = "Steam requests are rate-limited right now - try again in a minute.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Couldn't refresh achievements: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SetAchievements(IEnumerable<Achievement> achievements)
    {
        _allAchievements = achievements
            .Select(a => new AchievementListItem(_game.AppId, a, _databaseService, _settings.RevealHiddenAchievements))
            .ToList();

        RecomputeCompletion();
        ApplyView();
    }

    private void RecomputeCompletion()
    {
        var total = _allAchievements.Count;
        var unlocked = _allAchievements.Count(a => a.Unlocked);
        CompletionFraction = total > 0 ? (double)unlocked / total : 0;
        CompletionText = total > 0 ? $"{unlocked}/{total} ({CompletionFraction:P0})" : string.Empty;
        TrophyIcon = total > 0 ? TrophyIcons.ForFraction(CompletionFraction) : null;
    }

    // Lightweight periodic check (see MainViewModel) so newly-earned achievements show up without
    // the user having to hit Refresh - just the unlock status, not a full schema/rarity re-fetch.
    public async Task RefreshUnlocksAsync()
    {
        try
        {
            var steamId = await _steamDataProvider.ResolveSteamIdAsync(_settings.SteamIdOrVanity);
            var unlocks = await _steamDataProvider.GetUnlockStatusAsync(_game.AppId, steamId);
            if (unlocks.Count == 0)
                return;

            var newlyUnlocked = _allAchievements
                .Where(a => unlocks.TryGetValue(a.Achievement.ApiName, out var at) && a.ApplyUnlock(at))
                .ToList();

            if (newlyUnlocked.Count == 0)
                return;

            _databaseService.SaveAchievements(_game.AppId, _allAchievements.Select(a => a.Achievement));
            RecomputeCompletion();
            ApplyView();
            await IconLoader.LoadAllAsync(newlyUnlocked, a => a.IconUrl, (a, bmp) => a.Icon = bmp);
        }
        catch
        {
            // Best-effort background poll - stay silent (rate limit, offline, etc.); the next
            // manual Refresh or poll tick will catch up.
        }
    }

    private void ApplyView()
    {
        IEnumerable<AchievementListItem> query = _allAchievements;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            query = query.Where(a =>
                a.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                a.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        query = Filter switch
        {
            AchievementFilter.Missing => query.Where(a => !a.Unlocked),
            AchievementFilter.Rarest => query.OrderBy(a => a.Achievement.GlobalPercent ?? 100),
            AchievementFilter.Easiest => query.OrderByDescending(a => a.Achievement.GlobalPercent ?? 0),
            _ => query
        };

        Achievements.Clear();
        foreach (var item in query)
            Achievements.Add(item);
    }

    [RelayCommand]
    private void Back() => BackRequested?.Invoke(this, EventArgs.Empty);
}
