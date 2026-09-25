using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AchievementTracker.Models;

namespace AchievementTracker.Services;

public class SteamApiService
{
    private static readonly HttpClient Client = new() { BaseAddress = new Uri("https://api.steampowered.com/") };

    public async Task<string> ResolveSteamIdAsync(string steamIdOrVanity, string apiKey)
    {
        if (steamIdOrVanity.Length == 17 && steamIdOrVanity.All(char.IsDigit))
            return steamIdOrVanity;

        var url = $"ISteamUser/ResolveVanityURL/v1/?key={apiKey}&format=json&vanityurl={Uri.EscapeDataString(steamIdOrVanity)}";
        var result = await Client.GetFromJsonAsync<ResolveVanityResponse>(url)
            ?? throw new InvalidOperationException("No response resolving your Steam ID.");

        if (result.Response.Success != 1 || result.Response.SteamId is null)
            throw new InvalidOperationException("Couldn't find that Steam profile. Check the ID or vanity name.");

        return result.Response.SteamId;
    }

    public async Task<List<Game>> GetOwnedGamesAsync(string steamId, string apiKey)
    {
        var url = $"IPlayerService/GetOwnedGames/v1/?key={apiKey}&format=json&steamid={steamId}&include_appinfo=1";
        var result = await Client.GetFromJsonAsync<OwnedGamesResponse>(url)
            ?? throw new InvalidOperationException("No response fetching your library.");

        var games = result.Response.Games ?? [];

        return games.Select(g => new Game
        {
            AppId = g.AppId,
            Name = g.Name,
            IconUrl = string.IsNullOrEmpty(g.ImgIconUrl)
                ? string.Empty
                : $"https://media.steampowered.com/steamcommunity/public/images/apps/{g.AppId}/{g.ImgIconUrl}.jpg"
        }).ToList();
    }

    public async Task<List<Achievement>> GetAchievementsAsync(int appId, string steamId, string apiKey)
    {
        var schema = await GetSchemaAsync(appId, apiKey);
        if (schema.Count == 0)
            return [];

        var unlocked = await GetPlayerAchievementsAsync(appId, steamId, apiKey);
        var percentages = await GetGlobalPercentagesAsync(appId);

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

    private async Task<Dictionary<string, SchemaAchievementDto>> GetSchemaAsync(int appId, string apiKey)
    {
        var url = $"ISteamUserStats/GetSchemaForGame/v2/?key={apiKey}&format=json&appid={appId}";
        var result = await Client.GetFromJsonAsync<SchemaResponse>(url);
        var achievements = result?.Game.AvailableGameStats?.Achievements ?? [];
        return achievements.ToDictionary(a => a.Name);
    }

    private async Task<Dictionary<string, PlayerAchievementDto>> GetPlayerAchievementsAsync(int appId, string steamId, string apiKey)
    {
        try
        {
            var url = $"ISteamUserStats/GetPlayerAchievements/v1/?key={apiKey}&format=json&steamid={steamId}&appid={appId}";
            var result = await Client.GetFromJsonAsync<PlayerAchievementsResponse>(url);
            var achievements = result?.PlayerStats.Achievements ?? [];
            return achievements.ToDictionary(a => a.ApiName);
        }
        catch (HttpRequestException)
        {
            // Games without achievements (or a private profile) return an error here; treat as "nothing unlocked".
            return new Dictionary<string, PlayerAchievementDto>();
        }
    }

    private async Task<Dictionary<string, double>> GetGlobalPercentagesAsync(int appId)
    {
        try
        {
            var url = $"ISteamUserStats/GetGlobalAchievementPercentagesForApp/v2/?format=json&gameid={appId}";
            var result = await Client.GetFromJsonAsync<GlobalPercentagesResponse>(url);
            var achievements = result?.AchievementPercentages.Achievements ?? [];
            return achievements.ToDictionary(a => a.Name, a => a.Percent);
        }
        catch (HttpRequestException)
        {
            return new Dictionary<string, double>();
        }
    }

    private class SchemaResponse
    {
        [JsonPropertyName("game")]
        public SchemaGame Game { get; set; } = new();
    }

    private class SchemaGame
    {
        [JsonPropertyName("availableGameStats")]
        public SchemaGameStats? AvailableGameStats { get; set; }
    }

    private class SchemaGameStats
    {
        [JsonPropertyName("achievements")]
        public List<SchemaAchievementDto>? Achievements { get; set; }
    }

    private class SchemaAchievementDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("icon")]
        public string? Icon { get; set; }

        [JsonPropertyName("icongray")]
        public string? IconGray { get; set; }
    }

    private class PlayerAchievementsResponse
    {
        [JsonPropertyName("playerstats")]
        public PlayerStatsDto PlayerStats { get; set; } = new();
    }

    private class PlayerStatsDto
    {
        [JsonPropertyName("achievements")]
        public List<PlayerAchievementDto>? Achievements { get; set; }
    }

    private class PlayerAchievementDto
    {
        [JsonPropertyName("apiname")]
        public string ApiName { get; set; } = string.Empty;

        [JsonPropertyName("achieved")]
        public int Achieved { get; set; }

        [JsonPropertyName("unlocktime")]
        public long UnlockTime { get; set; }
    }

    private class GlobalPercentagesResponse
    {
        [JsonPropertyName("achievementpercentages")]
        public GlobalPercentagesInner AchievementPercentages { get; set; } = new();
    }

    private class GlobalPercentagesInner
    {
        [JsonPropertyName("achievements")]
        public List<GlobalPercentageDto>? Achievements { get; set; }
    }

    private class GlobalPercentageDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("percent")]
        public double Percent { get; set; }
    }

    private class ResolveVanityResponse
    {
        [JsonPropertyName("response")]
        public ResolveVanityInner Response { get; set; } = new();
    }

    private class ResolveVanityInner
    {
        [JsonPropertyName("success")]
        public int Success { get; set; }

        [JsonPropertyName("steamid")]
        public string? SteamId { get; set; }
    }

    private class OwnedGamesResponse
    {
        [JsonPropertyName("response")]
        public OwnedGamesInner Response { get; set; } = new();
    }

    private class OwnedGamesInner
    {
        [JsonPropertyName("games")]
        public List<OwnedGameDto>? Games { get; set; }
    }

    private class OwnedGameDto
    {
        [JsonPropertyName("appid")]
        public int AppId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("img_icon_url")]
        public string? ImgIconUrl { get; set; }
    }
}
