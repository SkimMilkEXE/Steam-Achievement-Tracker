using System.Collections.ObjectModel;
using AchievementTracker.Models;
using AchievementTracker.Services;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AchievementTracker.ViewModels;

public partial class AchievementListItem : ObservableObject
{
    private readonly DatabaseService _databaseService;

    public int AppId { get; }
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

    [ObservableProperty]
    public partial string Note { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool Pinned { get; set; }

    [ObservableProperty]
    public partial string NewChecklistItemText { get; set; } = string.Empty;

    public ObservableCollection<ChecklistItemViewModel> Checklist { get; } = new();

    public AchievementListItem(int appId, Achievement achievement, DatabaseService databaseService)
    {
        AppId = appId;
        Achievement = achievement;
        _databaseService = databaseService;

        var note = _databaseService.GetNote(appId, achievement.ApiName);
        Note = note.Note;
        Pinned = note.Pinned;

        foreach (var item in _databaseService.GetChecklistItems(appId, achievement.ApiName))
            Checklist.Add(new ChecklistItemViewModel(item, _databaseService, RemoveChecklistItem));
    }

    partial void OnPinnedChanged(bool value) => SaveNote();

    [RelayCommand]
    private void SaveNote()
    {
        _databaseService.SaveNote(new AchievementNote
        {
            AppId = AppId,
            ApiName = Achievement.ApiName,
            Note = Note,
            Pinned = Pinned
        });
    }

    [RelayCommand]
    private void AddChecklistItem()
    {
        if (string.IsNullOrWhiteSpace(NewChecklistItemText))
            return;

        var item = _databaseService.AddChecklistItem(AppId, Achievement.ApiName, NewChecklistItemText.Trim());
        Checklist.Add(new ChecklistItemViewModel(item, _databaseService, RemoveChecklistItem));
        NewChecklistItemText = string.Empty;
    }

    private void RemoveChecklistItem(ChecklistItemViewModel item) => Checklist.Remove(item);
}
