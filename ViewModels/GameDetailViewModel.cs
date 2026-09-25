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

    public string GameName => _game.Name;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    public ObservableCollection<AchievementListItem> Achievements { get; } = new();

    public event EventHandler? BackRequested;

    public GameDetailViewModel(Game game, AppSettings settings) : this(game, settings, new DirectSteamDataProvider())
    {
    }

    public GameDetailViewModel(Game game, AppSettings settings, ISteamDataProvider steamDataProvider)
    {
        _game = game;
        _settings = settings;
        _steamDataProvider = steamDataProvider;
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        Achievements.Clear();

        try
        {
            var steamId = await _steamDataProvider.ResolveSteamIdAsync(_settings.SteamIdOrVanity, _settings.ApiKey);
            var achievements = await _steamDataProvider.GetAchievementsAsync(_game.AppId, steamId, _settings.ApiKey);

            if (achievements.Count == 0)
            {
                ErrorMessage = "This game has no trackable achievements.";
                return;
            }

            foreach (var achievement in achievements)
                Achievements.Add(new AchievementListItem(achievement));

            await IconLoader.LoadAllAsync(Achievements, a => a.IconUrl, (a, bmp) => a.Icon = bmp);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Couldn't load achievements: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Back() => BackRequested?.Invoke(this, EventArgs.Empty);
}
