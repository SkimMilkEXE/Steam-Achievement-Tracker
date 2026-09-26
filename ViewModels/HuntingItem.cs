using AchievementTracker.Models;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AchievementTracker.ViewModels;

public partial class HuntingItem : ObservableObject
{
    public Game Game { get; }
    public Achievement Achievement { get; }
    public string GameName => Game.Name;
    public string DisplayName => Achievement.DisplayName;
    public string IconUrl => Achievement.Unlocked ? Achievement.IconUrl : Achievement.IconGrayUrl;

    [ObservableProperty]
    public partial Bitmap? Icon { get; set; }

    public HuntingItem(Game game, Achievement achievement)
    {
        Game = game;
        Achievement = achievement;
    }
}
