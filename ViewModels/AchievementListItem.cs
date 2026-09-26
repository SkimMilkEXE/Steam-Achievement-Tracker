using System;
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
    private readonly bool _revealHidden;

    public int AppId { get; }
    public Achievement Achievement { get; }

    // Steam marks some achievements "hidden" so their name/description stay spoilers until
    // unlocked. Masking here (display-time) rather than replacing the Achievement's own data
    // means an unlock detected later - by a full Refresh or the lightweight background poll -
    // automatically reveals the real text, since these are just live computed properties.
    private bool IsMasked => Achievement.Hidden && !Achievement.Unlocked && !_revealHidden;

    public string DisplayName => IsMasked ? "Hidden Achievement" : Achievement.DisplayName;
    public string Description => IsMasked ? "Unlock this achievement to reveal its details." : Achievement.Description;
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

    public AchievementListItem(int appId, Achievement achievement, DatabaseService databaseService, bool revealHidden = false)
    {
        AppId = appId;
        Achievement = achievement;
        _databaseService = databaseService;
        _revealHidden = revealHidden;

        var note = _databaseService.GetNote(appId, achievement.ApiName);
        Note = note.Note;
        Pinned = note.Pinned;

        foreach (var item in _databaseService.GetChecklistItems(appId, achievement.ApiName))
            Checklist.Add(new ChecklistItemViewModel(item, _databaseService, RemoveChecklistItem));
    }

    partial void OnPinnedChanged(bool value) => SaveNote();
    partial void OnNoteChanged(string value) => SaveNote();

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

    public bool ApplyUnlock(DateTimeOffset? unlockedAt)
    {
        if (Achievement.Unlocked || unlockedAt is null)
            return false;

        Achievement.Unlocked = true;
        Achievement.UnlockedAt = unlockedAt;
        OnPropertyChanged(nameof(Unlocked));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(IconUrl));
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(Description));
        return true;
    }
}
