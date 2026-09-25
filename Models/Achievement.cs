using System;

namespace AchievementTracker.Models;

public class Achievement
{
    public string ApiName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconUrl { get; set; } = string.Empty;
    public string IconGrayUrl { get; set; } = string.Empty;
    public bool Unlocked { get; set; }
    public DateTimeOffset? UnlockedAt { get; set; }
    public double? GlobalPercent { get; set; }
}
