using System;
using System.IO;
using System.Text.Json;
using AchievementTracker.Models;

namespace AchievementTracker.Services;

public class SettingsService
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AchievementTracker",
        "settings.json");

    public AppSettings Load()
    {
        if (!File.Exists(FilePath))
            return new AppSettings();

        var json = File.ReadAllText(FilePath);
        return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FilePath, json);
    }
}
