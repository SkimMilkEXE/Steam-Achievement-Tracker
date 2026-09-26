using AchievementTracker.Models;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AchievementTracker.ViewModels;

public partial class GameListItem : ObservableObject
{
    public Game Game { get; }
    public string Name => Game.Name;

    public bool HasCompletion { get; }
    public double CompletionFraction { get; }
    public string CompletionText { get; }

    [ObservableProperty]
    public partial Bitmap? Icon { get; set; }

    public GameListItem(Game game, int unlocked = 0, int total = 0)
    {
        Game = game;
        HasCompletion = total > 0;
        CompletionFraction = total > 0 ? (double)unlocked / total : 0;
        CompletionText = total > 0 ? $"{unlocked}/{total} ({CompletionFraction:P0})" : string.Empty;
    }
}
