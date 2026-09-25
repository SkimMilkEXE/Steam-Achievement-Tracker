using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AchievementTracker.Models;
using AchievementTracker.Services;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AchievementTracker.ViewModels;

public partial class LibraryViewModel : ViewModelBase
{
    private static readonly HttpClient IconClient = new();
    private readonly SteamApiService _steamApiService;
    private readonly AppSettings _settings;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    public ObservableCollection<GameListItem> Games { get; } = new();

    public event EventHandler? OpenSettingsRequested;

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

            await LoadIconsAsync();
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

    private async Task LoadIconsAsync()
    {
        using var throttle = new SemaphoreSlim(4);

        var tasks = Games.Select(async item =>
        {
            if (string.IsNullOrEmpty(item.Game.IconUrl))
                return;

            await throttle.WaitAsync();
            try
            {
                var bytes = await IconClient.GetByteArrayAsync(item.Game.IconUrl);
                item.Icon = new Bitmap(new MemoryStream(bytes));
            }
            catch
            {
                // ponytail: a missing icon just shows blank, not worth surfacing per-game errors
            }
            finally
            {
                throttle.Release();
            }
        });

        await Task.WhenAll(tasks);
    }

    [RelayCommand]
    private void OpenSettings() => OpenSettingsRequested?.Invoke(this, EventArgs.Empty);
}
