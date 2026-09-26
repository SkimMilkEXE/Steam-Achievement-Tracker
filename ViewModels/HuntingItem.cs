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
    public string Note { get; }
    public bool HasNote => !string.IsNullOrWhiteSpace(Note);

    [ObservableProperty]
    public partial Bitmap? Icon { get; set; }

    public HuntingItem(Game game, Achievement achievement, string note)
    {
        Game = game;
        Achievement = achievement;
        Note = note;
    }
}
