# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A small HTTP proxy that downloads the M3U channel playlist from a [TvHeadend](https://tvheadend.org/) server and rewrites each stream URL to embed HTTP basic-auth credentials inline. TvHeadend's default playlist uses per-channel `?ticket=...` tokens that expire after 300 seconds, which breaks downstream consumers like xTeVe/Plex. The proxy replaces the ticket form with a credentialed URL:

```
http://127.0.0.1:9981/stream/channelid/1234?ticket=...&profile=pass
  ->  http://username:password@127.0.0.1:9981/stream/channelid/1234&profile=pass
```

Consumers fetch the rewritten list from `GET http://<host>:33721/api/tvheadend/channels`.

## Build & run

```bash
# Build the whole solution (net10.0)
dotnet build Source/TvHeadEndM3uProxy.sln

# Run the test suite (MSTest, 25 tests under tests/TvHeadEndM3uProxyService.Tests/)
dotnet test Source/TvHeadEndM3uProxy.sln

# Run as a Docker container
docker build -t tvheadendm3uproxy .
docker compose up

# Or pull the pre-built image
docker pull ghcr.io/blehnen/tvheadendm3uproxy:latest
```

## Configuration

Settings are bound via strongly-typed `IOptions` from environment variables first, with `appsettings.json` for local development. Options classes live in `Source/TvHeadEndM3uProxyService/Config/TvHeadendOptions.cs` and `Config/ProxyOptions.cs`.

The double-underscore (`__`) is the hierarchy separator for environment variables.

**Required** (validated at startup via `ValidateOnStart` — missing values fail fast with a clear message):

| Variable | Description |
|---|---|
| `TVHEADEND__ADDRESS` | Base URL of the TvHeadend server (e.g. `http://192.168.1.10:9981`) |
| `TVHEADEND__USERNAME` | TvHeadend username |
| `TVHEADEND__PASSWORD` | TvHeadend password |

**Optional**:

| Variable | Default | Description |
|---|---|---|
| `PROXY_API_KEY` | _(none)_ | When set, all requests to `/api/tvheadend/channels` require an `X-Api-Key` header matching this value |
| `CACHE_TTL_SECONDS` | `0` | Cache the rewritten playlist in memory for this many seconds; `0` disables caching |
| `ASPNETCORE_URLS` | `http://+:33721` | Bind address for the web server |

Serilog console logging is configured in `appsettings.json`. There is no `App.config`.

## Architecture

Two projects, both targeting `net10.0`:

- **`TvHeadEndM3uProxy`** — ASP.NET Core Minimal API host. `Program.cs` builds a `WebApplication` with built-in DI and maps all endpoints. Hosts `ApiKeyEndpointFilter.cs` which implements the optional API-key gate.
- **`TvHeadEndM3uProxyService`** — framework-agnostic class library containing all proxy logic.

Request flow:

1. `GET /api/tvheadend/channels` (mapped in `Program.cs`) invokes `TvHeadendChannelService.cs`. The endpoint is optionally gated by `ApiKeyEndpointFilter` when `PROXY_API_KEY` is set (responds 401 otherwise), and optionally served from an `IMemoryCache` when `CACHE_TTL_SECONDS > 0`. Returns the rewritten playlist as a `channels.m3u` `application/octet-stream` attachment; returns 503 on upstream failure.
2. `TvHeadendClient.cs` downloads `{TvHeadendAddress}/playlist/channels.m3u` fully in memory via `IHttpClientFactory` with an HTTP Basic auth header. No temp files, no `WebClient`.
3. `PlaylistRewriter.cs` performs the in-house verbatim M3U rewrite: only stream-URL lines are transformed (injects `user:pass@`, drops the `?ticket=...` query string, re-appends any `&profile=...` suffix); all other lines and line endings pass through unchanged.

`GET /health` is localhost-restricted (`RequireHost`) and backs the container HEALTHCHECK (`dotnet TvHeadEndM3uProxy.dll healthcheck`).

`SharedAssemblyInfo.cs` (repo root) is linked into both projects for shared assembly metadata; `Version` is set per-csproj.
