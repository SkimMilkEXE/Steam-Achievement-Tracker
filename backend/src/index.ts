interface Env {
	STEAM_API_KEY: string;
	RATE_LIMIT: KVNamespace;
}

const STEAM_BASE = 'https://api.steampowered.com';
const RATE_LIMIT_PER_MINUTE = 60;

function isNumeric(value: string): boolean {
	return /^\d+$/.test(value);
}

function isValidVanity(value: string): boolean {
	return /^[\w-]{1,64}$/.test(value);
}

function jsonError(message: string, status: number): Response {
	return new Response(JSON.stringify({ error: message }), {
		status,
		headers: { 'content-type': 'application/json' },
	});
}

async function checkRateLimit(request: Request, env: Env): Promise<boolean> {
	const ip = request.headers.get('CF-Connecting-IP') ?? 'unknown';
	const key = `rl:${ip}`;
	const current = await env.RATE_LIMIT.get(key);
	const count = current ? parseInt(current, 10) : 0;

	if (count >= RATE_LIMIT_PER_MINUTE) return false;

	await env.RATE_LIMIT.put(key, String(count + 1), { expirationTtl: 60 });
	return true;
}

// Fetches from Steam (adding the secret key), caching the response at the edge.
// Passes Steam's response body and status straight through - errors like "private
// profile" or "no achievements" already come back as JSON, so there's nothing to reshape.
async function proxySteam(upstreamUrl: URL, cacheTtlSeconds: number, request: Request): Promise<Response> {
	const cache = caches.default;
	const cacheKey = new Request(upstreamUrl.toString(), request);

	const cached = await cache.match(cacheKey);
	if (cached) return cached;

	const upstream = await fetch(upstreamUrl.toString());
	const body = await upstream.text();

	const response = new Response(body, {
		status: upstream.status,
		headers: {
			'content-type': 'application/json',
			'cache-control': `public, max-age=${cacheTtlSeconds}`,
		},
	});

	if (upstream.ok) {
		await cache.put(cacheKey, response.clone());
	}

	return response;
}

export default {
	async fetch(request, env): Promise<Response> {
		if (request.method !== 'GET') {
			return jsonError('Only GET is supported.', 405);
		}

		if (!(await checkRateLimit(request, env))) {
			return jsonError('Rate limit exceeded. Try again in a minute.', 429);
		}

		const url = new URL(request.url);
		const params = url.searchParams;

		switch (url.pathname) {
			case '/resolve': {
				const vanityUrl = params.get('vanityurl') ?? '';
				if (!isValidVanity(vanityUrl)) return jsonError('Invalid vanityurl.', 400);

				const upstream = new URL(`${STEAM_BASE}/ISteamUser/ResolveVanityURL/v1/`);
				upstream.searchParams.set('key', env.STEAM_API_KEY);
				upstream.searchParams.set('format', 'json');
				upstream.searchParams.set('vanityurl', vanityUrl);
				return proxySteam(upstream, 86400, request);
			}

			case '/owned-games': {
				const steamId = params.get('steamid') ?? '';
				if (!isNumeric(steamId)) return jsonError('Invalid steamid.', 400);

				const upstream = new URL(`${STEAM_BASE}/IPlayerService/GetOwnedGames/v1/`);
				upstream.searchParams.set('key', env.STEAM_API_KEY);
				upstream.searchParams.set('format', 'json');
				upstream.searchParams.set('steamid', steamId);
				upstream.searchParams.set('include_appinfo', '1');
				return proxySteam(upstream, 300, request);
			}

			case '/achievements': {
				const steamId = params.get('steamid') ?? '';
				const appId = params.get('appid') ?? '';
				if (!isNumeric(steamId)) return jsonError('Invalid steamid.', 400);
				if (!isNumeric(appId)) return jsonError('Invalid appid.', 400);

				const upstream = new URL(`${STEAM_BASE}/ISteamUserStats/GetPlayerAchievements/v1/`);
				upstream.searchParams.set('key', env.STEAM_API_KEY);
				upstream.searchParams.set('format', 'json');
				upstream.searchParams.set('steamid', steamId);
				upstream.searchParams.set('appid', appId);
				return proxySteam(upstream, 120, request);
			}

			case '/schema': {
				const appId = params.get('appid') ?? '';
				if (!isNumeric(appId)) return jsonError('Invalid appid.', 400);

				const upstream = new URL(`${STEAM_BASE}/ISteamUserStats/GetSchemaForGame/v2/`);
				upstream.searchParams.set('key', env.STEAM_API_KEY);
				upstream.searchParams.set('format', 'json');
				upstream.searchParams.set('appid', appId);
				return proxySteam(upstream, 86400, request);
			}

			case '/rarity': {
				const appId = params.get('appid') ?? '';
				if (!isNumeric(appId)) return jsonError('Invalid appid.', 400);

				const upstream = new URL(`${STEAM_BASE}/ISteamUserStats/GetGlobalAchievementPercentagesForApp/v2/`);
				upstream.searchParams.set('format', 'json');
				upstream.searchParams.set('gameid', appId);
				return proxySteam(upstream, 43200, request);
			}

			default:
				return jsonError('Not found.', 404);
		}
	},
} satisfies ExportedHandler<Env>;
