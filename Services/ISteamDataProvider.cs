using System.Collections.Generic;
using System.Threading.Tasks;
using AchievementTracker.Models;

namespace AchievementTracker.Services;

public interface ISteamDataProvider
{
    Task<string> ResolveSteamIdAsync(string steamIdOrVanity, string apiKey);
    Task<List<Game>> GetOwnedGamesAsync(string steamId, string apiKey);
    Task<List<Achievement>> GetAchievementsAsync(int appId, string steamId, string apiKey);
}
