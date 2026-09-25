using Avalonia.Controls;
using AchievementTracker.ViewModels;

namespace AchievementTracker.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        DataContext = new SettingsViewModel();
    }
}
