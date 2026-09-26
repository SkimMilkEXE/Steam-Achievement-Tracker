namespace AchievementTracker.Models;

public class AppSettings
{
    public string SteamIdOrVanity { get; set; } = string.Empty;
    public string Theme { get; set; } = "System";
    public bool RevealHiddenAchievements { get; set; }
}
