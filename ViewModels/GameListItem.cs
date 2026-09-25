using AchievementTracker.Models;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AchievementTracker.ViewModels;

public partial class GameListItem : ObservableObject
{
    public Game Game { get; }
    public string Name => Game.Name;

    [ObservableProperty]
    public partial Bitmap? Icon { get; set; }

    public GameListItem(Game game)
    {
        Game = game;
    }
}
