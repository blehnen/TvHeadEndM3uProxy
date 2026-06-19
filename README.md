# TvHeadEndM3uProxy

A lightweight HTTP proxy that downloads the M3U channel playlist from a [TvHeadend](https://tvheadend.org/) server and rewrites each stream URL to embed HTTP basic-auth credentials inline.

TvHeadend's default playlist uses per-channel `?ticket=...` tokens that expire after 300 seconds, which breaks downstream consumers like IPTV managers and DVR front-ends. This proxy replaces the expiring-ticket form with a stable, credentialed URL so consumers always have a usable channel list:

```
http://127.0.0.1:9981/stream/channelid/1234?ticket=dfdkdjflsdjfdsl&profile=pass
  ->  http://username:password@127.0.0.1:9981/stream/channelid/1234?profile=pass
```

The rewritten playlist is served at `GET /api/tvheadend/channels`.

### Compatible consumers

The proxy emits a standard M3U playlist and should work with any M3U-consuming client. The following are **illustrative examples only** — this is not a tested-compatibility matrix and none of these have been individually verified against this proxy:

- **xTeVe** — the original target for this proxy; note that xTeVe is **no longer maintained**.
- **[Threadfin](https://github.com/Threadfin/Threadfin)** — the actively-maintained fork of xTeVe; recommended as the successor.
- **StreamMaster**, **IPTVBoss** — other M3U playlist managers.
- **Plex DVR / Jellyfin / Emby** — media servers with direct M3U-tuner support.

For example, point any of these at `http://<proxy-host>:33721/api/tvheadend/channels` and they should be able to ingest the channel list.

---

## Quick start

### docker run

```bash
docker run -d \
  -e TVHEADEND__ADDRESS=http://192.168.1.10:9981 \
  -e TVHEADEND__USERNAME=tvuser \
  -e TVHEADEND__PASSWORD=yourpassword \
  -p 33721:33721 \
  ghcr.io/blehnen/tvheadendm3uproxy:latest
```

### docker compose

A `docker-compose.yml` is included in the repository root:

```bash
# Copy and edit the compose file to set your environment variables, then:
docker compose up -d
```

### Pull from GHCR

```bash
docker pull ghcr.io/blehnen/tvheadendm3uproxy:latest
```

Tagged releases are also available as `ghcr.io/blehnen/tvheadendm3uproxy:<version>`. Both `amd64` and `arm64` architectures are published.

---

## Configuration

All configuration is provided via environment variables.

| Variable | Required | Default | Description |
|---|---|---|---|
| `TVHEADEND__ADDRESS` | yes | — | TvHeadend base URL including scheme and port (e.g. `http://192.168.1.10:9981`) |
| `TVHEADEND__USERNAME` | yes | — | TvHeadend HTTP basic-auth username |
| `TVHEADEND__PASSWORD` | yes | — | TvHeadend HTTP basic-auth password |
| `PROXY_API_KEY` | no | (unset = open) | If set, require this key as an `X-Api-Key` header or `?apikey=` query parameter on the channels endpoint |
| `CACHE_TTL_SECONDS` | no | `0` (disabled) | Cache the rewritten playlist for this many seconds (0 = always fetch fresh) |
| `ASPNETCORE_URLS` | no | `http://+:33721` | Kestrel listen URL |

**Notes:**
- The double-underscore (`__`) is the .NET configuration hierarchy separator for nested keys.
- Missing required variables (`TVHEADEND__ADDRESS`, `TVHEADEND__USERNAME`, `TVHEADEND__PASSWORD`) cause an immediate startup failure with a clear error message.

---

## Endpoints

| Method | Path | Description |
|---|---|---|
| `GET` | `/api/tvheadend/channels` | Returns the rewritten M3U playlist as a `channels.m3u` file attachment (`Content-Type: application/octet-stream`). Returns HTTP 503 if the upstream TvHeadend server is unreachable. Requires the `PROXY_API_KEY` credential if that variable is configured. |
| `GET` | `/health` | Health check endpoint restricted to localhost. Returns HTTP 200 when the application is running. Used by the container `HEALTHCHECK` directive. |

---

## Security

> **WARNING: Run this proxy only on a trusted network.**
>
> - The rewritten playlist embeds TvHeadend credentials (`username:password`) inline in every stream URL. Any client that receives the playlist can read those credentials in plain text.
> - There is no in-application TLS. All traffic between the proxy and its clients is unencrypted.
> - Run this proxy **only on a trusted LAN** or behind an operator-controlled reverse proxy (e.g. nginx with TLS termination). **Never expose port 33721 directly to the internet.**
> - The optional `PROXY_API_KEY` provides a lightweight gate on the `/api/tvheadend/channels` endpoint but does not encrypt the stream URLs or the channel list in transit.

---

## Releasing

1. Bump `<Version>` in `Source/TvHeadEndM3uProxy/TvHeadEndM3uProxy.csproj`.
2. Commit: `git commit -am "Release vX.Y.Z.W"`
3. Tag and push: `git tag vX.Y.Z.W && git push --tags`
4. The `publish.yml` GitHub Actions workflow triggers automatically:
   - Verifies the tag matches the csproj `<Version>` (version-gate check).
   - Builds a multi-arch Docker image (amd64 + arm64).
   - Pushes to GHCR as `ghcr.io/blehnen/tvheadendm3uproxy:X.Y.Z.W` and `:latest`.

To validate a release candidate without pushing, trigger `workflow_dispatch` with `dry_run: true` — this builds the image but does not push.

---

## Building from source

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
# Build the solution
dotnet build Source/TvHeadEndM3uProxy.sln -c Release

# Run tests (25 tests)
dotnet test Source/TvHeadEndM3uProxy.sln

# Build Docker image locally
docker build -t tvheadend-m3u-proxy .
```
