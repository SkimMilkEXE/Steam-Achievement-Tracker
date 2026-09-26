namespace AchievementTracker.Models;

public class AchievementNote
{
    public int AppId { get; set; }
    public string ApiName { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public bool Pinned { get; set; }
}
