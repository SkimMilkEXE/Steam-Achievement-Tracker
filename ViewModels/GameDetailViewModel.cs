using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using AchievementTracker.Models;
using AchievementTracker.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AchievementTracker.ViewModels;

public partial class GameDetailViewModel : ViewModelBase
{
    private readonly Game _game;
    private readonly AppSettings _settings;
    private readonly ISteamDataProvider _steamDataProvider;
    private readonly DatabaseService _databaseService;

    public string GameName => _game.Name;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

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
            foreach (var achievement in cachedAchievements)
                Achievements.Add(new AchievementListItem(achievement));

            _ = IconLoader.LoadAllAsync(Achievements, a => a.IconUrl, (a, bmp) => a.Icon = bmp);
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
            var achievements = await _steamDataProvider.GetAchievementsAsync(_game.AppId, steamId, _settings.ApiKey);

            if (achievements.Count == 0)
            {
                ErrorMessage = "This game has no trackable achievements.";
                return;
            }

            _databaseService.SaveAchievements(_game.AppId, achievements);

            Achievements.Clear();
            foreach (var achievement in achievements)
                Achievements.Add(new AchievementListItem(achievement));

            await IconLoader.LoadAllAsync(Achievements, a => a.IconUrl, (a, bmp) => a.Icon = bmp);
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

    [RelayCommand]
    private void Back() => BackRequested?.Invoke(this, EventArgs.Empty);
}
