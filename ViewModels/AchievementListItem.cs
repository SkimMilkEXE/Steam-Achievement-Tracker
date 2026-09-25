using AchievementTracker.Models;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AchievementTracker.ViewModels;

public partial class AchievementListItem : ObservableObject
{
    public Achievement Achievement { get; }
    public string DisplayName => Achievement.DisplayName;
    public string Description => Achievement.Description;
    public bool Unlocked => Achievement.Unlocked;
    public string StatusText => Achievement.Unlocked ? "Unlocked" : "Locked";
    public string IconUrl => Achievement.Unlocked ? Achievement.IconUrl : Achievement.IconGrayUrl;

    public string GlobalPercentText => Achievement.GlobalPercent is { } percent
        ? $"{percent:0.0}% of players"
        : string.Empty;

    [ObservableProperty]
    public partial Bitmap? Icon { get; set; }

    public AchievementListItem(Achievement achievement)
    {
        Achievement = achievement;
    }
}
