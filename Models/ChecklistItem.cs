namespace AchievementTracker.Models;

public class ChecklistItem
{
    public int Id { get; set; }
    public int AppId { get; set; }
    public string ApiName { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public bool Checked { get; set; }
}
