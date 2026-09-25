using System;
using System.Collections.Generic;
using System.Linq;
using AchievementTracker.Models;

namespace AchievementTracker.Services;

internal static class SteamAchievementMerger
{
    public static List<Game> ToGames(OwnedGamesResponse? response)
    {
        var games = response?.Response.Games ?? [];

        return games.Select(g => new Game
        {
            AppId = g.AppId,
            Name = g.Name,
            IconUrl = string.IsNullOrEmpty(g.ImgIconUrl)
                ? string.Empty
                : $"https://media.steampowered.com/steamcommunity/public/images/apps/{g.AppId}/{g.ImgIconUrl}.jpg"
        }).ToList();
    }

    public static List<Achievement> Merge(
        Dictionary<string, SchemaAchievementDto> schema,
        Dictionary<string, PlayerAchievementDto> unlocked,
        Dictionary<string, double> percentages)
    {
        return schema.Select(kvp =>
        {
            unlocked.TryGetValue(kvp.Key, out var unlock);
            percentages.TryGetValue(kvp.Key, out var percent);

            return new Achievement
            {
                ApiName = kvp.Key,
                DisplayName = kvp.Value.DisplayName,
                Description = kvp.Value.Description ?? string.Empty,
                IconUrl = kvp.Value.Icon ?? string.Empty,
                IconGrayUrl = kvp.Value.IconGray ?? string.Empty,
                Unlocked = unlock?.Achieved == 1,
                UnlockedAt = unlock?.UnlockTime is > 0
                    ? DateTimeOffset.FromUnixTimeSeconds(unlock.UnlockTime)
                    : null,
                GlobalPercent = percent
            };
        })
        .OrderByDescending(a => a.Unlocked)
        .ThenByDescending(a => a.GlobalPercent)
        .ToList();
    }
}
