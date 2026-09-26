using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AchievementTracker.Models;
using Dapper;
using Microsoft.Data.Sqlite;

namespace AchievementTracker.Services;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService()
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AchievementTracker",
            "library.db");

        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        _connectionString = $"Data Source={dbPath}";

        using var connection = OpenConnection();
        connection.Execute(
            """
            CREATE TABLE IF NOT EXISTS Games (
                AppId INTEGER PRIMARY KEY,
                Name TEXT NOT NULL,
                IconUrl TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Achievements (
                AppId INTEGER NOT NULL,
                ApiName TEXT NOT NULL,
                DisplayName TEXT NOT NULL,
                Description TEXT NOT NULL,
                IconUrl TEXT NOT NULL,
                IconGrayUrl TEXT NOT NULL,
                Unlocked INTEGER NOT NULL,
                UnlockedAtUnix INTEGER NULL,
                GlobalPercent REAL NULL,
                PRIMARY KEY (AppId, ApiName)
            );

            CREATE TABLE IF NOT EXISTS AchievementNotes (
                AppId INTEGER NOT NULL,
                ApiName TEXT NOT NULL,
                Note TEXT NOT NULL DEFAULT '',
                Pinned INTEGER NOT NULL DEFAULT 0,
                PRIMARY KEY (AppId, ApiName)
            );

            CREATE TABLE IF NOT EXISTS ChecklistItems (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                AppId INTEGER NOT NULL,
                ApiName TEXT NOT NULL,
                Text TEXT NOT NULL,
                Checked INTEGER NOT NULL DEFAULT 0
            );
            """);
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    // AppId -> (unlocked, total), for games whose achievements have been fetched at least once.
    public Dictionary<int, (int Unlocked, int Total)> GetCompletionSummaries()
    {
        using var connection = OpenConnection();
        var rows = connection.Query<CompletionRow>(
            "SELECT AppId, SUM(Unlocked) AS Unlocked, COUNT(*) AS Total FROM Achievements GROUP BY AppId");

        return rows.ToDictionary(r => r.AppId, r => (r.Unlocked, r.Total));
    }

    private class CompletionRow
    {
        public int AppId { get; set; }
        public int Unlocked { get; set; }
        public int Total { get; set; }
    }

    public List<Game> GetGames()
    {
        using var connection = OpenConnection();
        return connection.Query<Game>("SELECT AppId, Name, IconUrl FROM Games").ToList();
    }

    public void SaveGames(IEnumerable<Game> games)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();

        connection.Execute("DELETE FROM Games", transaction: transaction);
        connection.Execute(
            "INSERT INTO Games (AppId, Name, IconUrl) VALUES (@AppId, @Name, @IconUrl)",
            games, transaction);

        transaction.Commit();
    }

    public List<Achievement> GetAchievements(int appId)
    {
        using var connection = OpenConnection();
        var rows = connection.Query<AchievementRow>(
            "SELECT * FROM Achievements WHERE AppId = @appId", new { appId });

        return rows.Select(ToAchievement)
            .OrderByDescending(a => a.Unlocked)
            .ThenByDescending(a => a.GlobalPercent)
            .ToList();
    }

    public void SaveAchievements(int appId, IEnumerable<Achievement> achievements)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();

        connection.Execute("DELETE FROM Achievements WHERE AppId = @appId", new { appId }, transaction);
        connection.Execute(
            """
            INSERT INTO Achievements
                (AppId, ApiName, DisplayName, Description, IconUrl, IconGrayUrl, Unlocked, UnlockedAtUnix, GlobalPercent)
            VALUES
                (@AppId, @ApiName, @DisplayName, @Description, @IconUrl, @IconGrayUrl, @Unlocked, @UnlockedAtUnix, @GlobalPercent)
            """,
            achievements.Select(a => new AchievementRow
            {
                AppId = appId,
                ApiName = a.ApiName,
                DisplayName = a.DisplayName,
                Description = a.Description,
                IconUrl = a.IconUrl,
                IconGrayUrl = a.IconGrayUrl,
                Unlocked = a.Unlocked,
                UnlockedAtUnix = a.UnlockedAt?.ToUnixTimeSeconds(),
                GlobalPercent = a.GlobalPercent
            }),
            transaction);

        transaction.Commit();
    }

    public AchievementNote GetNote(int appId, string apiName)
    {
        using var connection = OpenConnection();
        return connection.QuerySingleOrDefault<AchievementNote>(
            "SELECT AppId, ApiName, Note, Pinned FROM AchievementNotes WHERE AppId = @appId AND ApiName = @apiName",
            new { appId, apiName })
            ?? new AchievementNote { AppId = appId, ApiName = apiName };
    }

    public void SaveNote(AchievementNote note)
    {
        using var connection = OpenConnection();
        connection.Execute(
            "INSERT OR REPLACE INTO AchievementNotes (AppId, ApiName, Note, Pinned) VALUES (@AppId, @ApiName, @Note, @Pinned)",
            note);
    }

    public List<(Game Game, Achievement Achievement)> GetPinnedAchievements()
    {
        using var connection = OpenConnection();
        var pins = connection.Query<PinKey>(
            "SELECT AppId, ApiName FROM AchievementNotes WHERE Pinned = 1").ToList();

        var result = new List<(Game, Achievement)>();
        foreach (var pin in pins)
        {
            var game = connection.QuerySingleOrDefault<Game>(
                "SELECT AppId, Name, IconUrl FROM Games WHERE AppId = @AppId", pin);
            var row = connection.QuerySingleOrDefault<AchievementRow>(
                "SELECT * FROM Achievements WHERE AppId = @AppId AND ApiName = @ApiName", pin);

            if (game is not null && row is not null)
                result.Add((game, ToAchievement(row)));
        }

        return result;
    }

    public List<ChecklistItem> GetChecklistItems(int appId, string apiName)
    {
        using var connection = OpenConnection();
        return connection.Query<ChecklistItem>(
            "SELECT Id, AppId, ApiName, Text, Checked FROM ChecklistItems WHERE AppId = @appId AND ApiName = @apiName ORDER BY Id",
            new { appId, apiName }).ToList();
    }

    public ChecklistItem AddChecklistItem(int appId, string apiName, string text)
    {
        using var connection = OpenConnection();
        var id = connection.ExecuteScalar<long>(
            "INSERT INTO ChecklistItems (AppId, ApiName, Text, Checked) VALUES (@appId, @apiName, @text, 0) RETURNING Id",
            new { appId, apiName, text });

        return new ChecklistItem { Id = (int)id, AppId = appId, ApiName = apiName, Text = text, Checked = false };
    }

    public void SetChecklistItemChecked(int id, bool isChecked)
    {
        using var connection = OpenConnection();
        connection.Execute("UPDATE ChecklistItems SET Checked = @isChecked WHERE Id = @id", new { id, isChecked });
    }

    public void DeleteChecklistItem(int id)
    {
        using var connection = OpenConnection();
        connection.Execute("DELETE FROM ChecklistItems WHERE Id = @id", new { id });
    }

    private class PinKey
    {
        public int AppId { get; set; }
        public string ApiName { get; set; } = string.Empty;
    }

    private static Achievement ToAchievement(AchievementRow row) => new()
    {
        ApiName = row.ApiName,
        DisplayName = row.DisplayName,
        Description = row.Description,
        IconUrl = row.IconUrl,
        IconGrayUrl = row.IconGrayUrl,
        Unlocked = row.Unlocked,
        UnlockedAt = row.UnlockedAtUnix is long unix ? DateTimeOffset.FromUnixTimeSeconds(unix) : null,
        GlobalPercent = row.GlobalPercent
    };

    private class AchievementRow
    {
        public int AppId { get; set; }
        public string ApiName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public string IconGrayUrl { get; set; } = string.Empty;
        public bool Unlocked { get; set; }
        public long? UnlockedAtUnix { get; set; }
        public double? GlobalPercent { get; set; }
    }
}
