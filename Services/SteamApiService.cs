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
