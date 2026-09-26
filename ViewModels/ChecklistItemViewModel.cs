using System;
using AchievementTracker.Models;
using AchievementTracker.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AchievementTracker.ViewModels;

public partial class ChecklistItemViewModel : ObservableObject
{
    private readonly DatabaseService _databaseService;
    private readonly Action<ChecklistItemViewModel> _onDelete;

    public int Id { get; }
    public string Text { get; }

    [ObservableProperty]
    public partial bool Checked { get; set; }

    public ChecklistItemViewModel(ChecklistItem item, DatabaseService databaseService, Action<ChecklistItemViewModel> onDelete)
    {
        Id = item.Id;
        Text = item.Text;
        Checked = item.Checked;
        _databaseService = databaseService;
        _onDelete = onDelete;
    }

    partial void OnCheckedChanged(bool value) => _databaseService.SetChecklistItemChecked(Id, value);

    [RelayCommand]
    private void Delete()
    {
        _databaseService.DeleteChecklistItem(Id);
        _onDelete(this);
    }
}
