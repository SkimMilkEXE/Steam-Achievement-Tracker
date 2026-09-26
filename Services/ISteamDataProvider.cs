using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AchievementTracker.Models;

namespace AchievementTracker.Services;

public interface ISteamDataProvider
{
    Task<string> ResolveSteamIdAsync(string steamIdOrVanity);
    Task<List<Game>> GetOwnedGamesAsync(string steamId);
    Task<List<Achievement>> GetAchievementsAsync(int appId, string steamId);

    // Lightweight poll for just the unlock status (apiName -> unlock time, or null if still
    // locked) - skips the schema/rarity calls since those essentially never change.
    Task<Dictionary<string, DateTimeOffset?>> GetUnlockStatusAsync(int appId, string steamId);
}
