using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AchievementTracker.Services;

// Shapes shared by DirectSteamDataProvider (calls Steam directly) and WorkerSteamDataProvider
// (calls the Cloudflare Worker, which proxies Steam's JSON through unchanged).

internal class ResolveVanityResponse
{
    [JsonPropertyName("response")]
    public ResolveVanityInner Response { get; set; } = new();
}

internal class ResolveVanityInner
{
    [JsonPropertyName("success")]
    public int Success { get; set; }

    [JsonPropertyName("steamid")]
    public string? SteamId { get; set; }
}

internal class OwnedGamesResponse
{
    [JsonPropertyName("response")]
    public OwnedGamesInner Response { get; set; } = new();
}

internal class OwnedGamesInner
{
    [JsonPropertyName("games")]
    public List<OwnedGameDto>? Games { get; set; }
}

internal class OwnedGameDto
{
    [JsonPropertyName("appid")]
    public int AppId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("img_icon_url")]
    public string? ImgIconUrl { get; set; }
}

internal class SchemaResponse
{
    [JsonPropertyName("game")]
    public SchemaGame Game { get; set; } = new();
}

internal class SchemaGame
{
    [JsonPropertyName("availableGameStats")]
    public SchemaGameStats? AvailableGameStats { get; set; }
}

internal class SchemaGameStats
{
    [JsonPropertyName("achievements")]
    public List<SchemaAchievementDto>? Achievements { get; set; }
}

internal class SchemaAchievementDto
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

internal class PlayerAchievementsResponse
{
    [JsonPropertyName("playerstats")]
    public PlayerStatsDto PlayerStats { get; set; } = new();
}

internal class PlayerStatsDto
{
    [JsonPropertyName("achievements")]
    public List<PlayerAchievementDto>? Achievements { get; set; }
}

internal class PlayerAchievementDto
{
    [JsonPropertyName("apiname")]
    public string ApiName { get; set; } = string.Empty;

    [JsonPropertyName("achieved")]
    public int Achieved { get; set; }

    [JsonPropertyName("unlocktime")]
    public long UnlockTime { get; set; }
}

internal class GlobalPercentagesResponse
{
    [JsonPropertyName("achievementpercentages")]
    public GlobalPercentagesInner AchievementPercentages { get; set; } = new();
}

internal class GlobalPercentagesInner
{
    [JsonPropertyName("achievements")]
    public List<GlobalPercentageDto>? Achievements { get; set; }
}

internal class GlobalPercentageDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("percent")]
    public double Percent { get; set; }
}
