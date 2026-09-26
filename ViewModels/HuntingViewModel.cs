using System;
using System.Collections.ObjectModel;
using AchievementTracker.Models;
using AchievementTracker.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AchievementTracker.ViewModels;

public partial class HuntingViewModel : ViewModelBase
{
    private readonly DatabaseService _databaseService;

    public ObservableCollection<HuntingItem> Items { get; } = new();

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    public event EventHandler? BackRequested;
    public event EventHandler<Game>? OpenGameRequested;

    public HuntingViewModel() : this(new DatabaseService())
    {
    }

    public HuntingViewModel(DatabaseService databaseService)
    {
        _databaseService = databaseService;

        foreach (var (game, achievement, note) in _databaseService.GetPinnedAchievements())
            Items.Add(new HuntingItem(game, achievement, note));

        IsEmpty = Items.Count == 0;

        _ = IconLoader.LoadAllAsync(Items, i => i.IconUrl, (i, bmp) => i.Icon = bmp);
    }

    [RelayCommand]
    private void Back() => BackRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void OpenGame(HuntingItem? item)
    {
        if (item is not null)
            OpenGameRequested?.Invoke(this, item.Game);
    }
}
