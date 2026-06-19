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
# Build the whole solution (multi-targets net472, netcoreapp3.1, net6.0)
dotnet build Source/TvHeadEndM3uProxy.sln

# Run on Linux / .NET (console mode, "press any key to stop")
dotnet Source/TvHeadEndM3uProxy/bin/Debug/net6.0/TvHeadEndM3uProxy.dll

# Windows: install/uninstall as a Windows service (Topshelf verbs)
TvHeadEndM3uProxy.exe install
TvHeadEndM3uProxy.exe uninstall
```

There is no test project — `dotnet test` has nothing to run.

The build requires the bundled `Lib/PlayLists.Net/` assemblies (a vendored PlaylistsNET referenced by HintPath, not a NuGet package).

## Configuration

Runtime settings live in `TvHeadEndM3uProxy.json` (copied to the output dir on build). `TvHeadendAddress`, `TvHeadEndUserName`, and `TvHeadEndPassword` are required and validated at startup (`Configuration.Validate()` throws if any is empty). `HostAddress` defaults to `http://+:33721/`. The checked-in JSON holds placeholder credentials — never commit real ones.

Serilog output config (console + rolling file `Logs/Log.txt`) lives in `App.config` `appSettings`, read via `ReadFrom.AppSettings()`.

## Architecture

Two projects, wired with SimpleInjector:

- **`TvHeadEndM3uProxy`** — the executable / entry point (`Program.cs`). Detects OS at runtime: on Windows it hosts `MainService` as a Topshelf Windows service (auto-start, crash recovery); on non-Windows it resolves `RunForDotNetCore` and runs as a blocking console app. Both paths call the same `MainService.Start()/Stop()`.
- **`TvHeadEndM3uProxyService`** — the actual proxy logic, framework-agnostic (multi-targets including netstandard2.0).

Request flow:

1. `WebServer` (`WebServer.cs`) hosts an [EmbedIO](https://github.com/unosquare/embedio) web server bound to `HostAddress`, mounting the Web API under `/api`. It bridges EmbedIO's Swan logger to Serilog via `Logging/WebServerLogger.cs`. Start/Stop are guarded by a lock and a `CancellationTokenSource` so Stop can't race ahead of Start.
2. `TvHeadendController` (`Controllers/TvHeadendController.cs`) handles `GET /tvheadend/channels`: downloads `{TvHeadendAddress}/playlist/channels.m3u` with a Basic auth header into a temp file, rewrites it, and streams it back as a `channels.m3u` attachment.
3. `ModifyPlayList` (`ModifyPlayList.cs`) parses the M3U with PlaylistsNET (`IPTVContent`) and rewrites each entry's `Path` to the inline-credential form, preserving any `&profile=...` suffix.

DI composition root is `RegisterServices.Register(Container)` — registers everything as singletons, loads + validates `Configuration`, and calls `container.Verify()`.

`SharedAssemblyInfo.cs` (repo root) is linked into both projects for shared assembly metadata; `Version` is set per-csproj.
