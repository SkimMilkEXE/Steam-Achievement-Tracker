using AchievementTracker.Models;
using AchievementTracker.Services;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AchievementTracker.ViewModels;

public partial class GameListItem : ObservableObject
{
    public Game Game { get; }
    public string Name => Game.Name;

    [ObservableProperty]
    public partial bool HasCompletion { get; set; }

    [ObservableProperty]
    public partial double CompletionFraction { get; set; }

    [ObservableProperty]
    public partial string CompletionText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial IImage? TrophyIcon { get; set; }

    [ObservableProperty]
    public partial Bitmap? Icon { get; set; }

    public GameListItem(Game game, int unlocked = 0, int total = 0)
    {
        Game = game;
        SetCompletion(unlocked, total);
    }

    public void SetCompletion(int unlocked, int total)
    {
        HasCompletion = total > 0;
        CompletionFraction = total > 0 ? (double)unlocked / total : 0;
        CompletionText = total > 0 ? $"{unlocked}/{total} ({CompletionFraction:P0})" : string.Empty;
        TrophyIcon = total > 0 ? TrophyIcons.ForFraction(CompletionFraction) : null;
    }
}
