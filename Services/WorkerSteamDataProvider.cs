using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AchievementTracker.Models;

namespace AchievementTracker.Services;

// v2: calls the developer's Cloudflare Worker, which holds the Steam key as a secret.
// Users need no key of their own - the apiKey parameters on ISteamDataProvider are simply unused here.
public class WorkerSteamDataProvider : ISteamDataProvider
{
    private static readonly HttpClient Client = new() { BaseAddress = new Uri("https://steam-tracker-api.richardhurley374.workers.dev/") };

    public async Task<string> ResolveSteamIdAsync(string steamIdOrVanity, string apiKey)
    {
        if (steamIdOrVanity.Length == 17 && steamIdOrVanity.All(char.IsDigit))
            return steamIdOrVanity;

        var url = $"resolve?vanityurl={Uri.EscapeDataString(steamIdOrVanity)}";
        var result = await Client.GetFromJsonAsync<ResolveVanityResponse>(url)
            ?? throw new InvalidOperationException("No response resolving your Steam ID.");

        if (result.Response.Success != 1 || result.Response.SteamId is null)
            throw new InvalidOperationException("Couldn't find that Steam profile. Check the ID or vanity name.");

        return result.Response.SteamId;
    }

    public async Task<List<Game>> GetOwnedGamesAsync(string steamId, string apiKey)
    {
        var url = $"owned-games?steamid={steamId}";
        var result = await Client.GetFromJsonAsync<OwnedGamesResponse>(url);
        return SteamAchievementMerger.ToGames(result);
    }

    public async Task<List<Achievement>> GetAchievementsAsync(int appId, string steamId, string apiKey)
    {
        var schema = await GetSchemaAsync(appId);
        if (schema.Count == 0)
            return [];

        var unlocked = await GetPlayerAchievementsAsync(appId, steamId);
        var percentages = await GetGlobalPercentagesAsync(appId);

        return SteamAchievementMerger.Merge(schema, unlocked, percentages);
    }

    private async Task<Dictionary<string, SchemaAchievementDto>> GetSchemaAsync(int appId)
    {
        var url = $"schema?appid={appId}";
        var result = await Client.GetFromJsonAsync<SchemaResponse>(url);
        var achievements = result?.Game.AvailableGameStats?.Achievements ?? [];
        return achievements.ToDictionary(a => a.Name);
    }

    private async Task<Dictionary<string, PlayerAchievementDto>> GetPlayerAchievementsAsync(int appId, string steamId)
    {
        try
        {
            var url = $"achievements?steamid={steamId}&appid={appId}";
            var result = await Client.GetFromJsonAsync<PlayerAchievementsResponse>(url);
            var achievements = result?.PlayerStats.Achievements ?? [];
            return achievements.ToDictionary(a => a.ApiName);
        }
        catch (HttpRequestException)
        {
            return new Dictionary<string, PlayerAchievementDto>();
        }
    }

    private async Task<Dictionary<string, double>> GetGlobalPercentagesAsync(int appId)
    {
        try
        {
            var url = $"rarity?appid={appId}";
            var result = await Client.GetFromJsonAsync<GlobalPercentagesResponse>(url);
            var achievements = result?.AchievementPercentages.Achievements ?? [];
            return achievements.ToDictionary(a => a.Name, a => a.Percent);
        }
        catch (HttpRequestException)
        {
            return new Dictionary<string, double>();
        }
    }
}
