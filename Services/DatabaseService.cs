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
            """);
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
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
