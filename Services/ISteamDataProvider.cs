using System.Collections.Generic;
using System.Threading.Tasks;
using AchievementTracker.Models;

namespace AchievementTracker.Services;

public interface ISteamDataProvider
{
    Task<string> ResolveSteamIdAsync(string steamIdOrVanity);
    Task<List<Game>> GetOwnedGamesAsync(string steamId);
    Task<List<Achievement>> GetAchievementsAsync(int appId, string steamId);
}
