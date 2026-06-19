// ---------------------------------------------------------------------
// MIT License
//
// Copyright (c) 2020 Brian Lehnen
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using TvHeadEndM3uProxy;
using TvHeadEndM3uProxyService;
using TvHeadEndM3uProxyService.Config;

if (args.Length > 0 && args[0] == "healthcheck")
{
    var config = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: true)
        .AddEnvironmentVariables()
        .Build();

    var port = Program.ResolveHealthCheckPort(
        config,
        aspnetcoreUrls: Environment.GetEnvironmentVariable("ASPNETCORE_URLS"),
        dotnetUrls: Environment.GetEnvironmentVariable("DOTNET_URLS"));

    try
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var response = await client.GetAsync($"http://localhost:{port}/health");
        Environment.Exit(response.IsSuccessStatusCode ? 0 : 1);
    }
    catch
    {
        Environment.Exit(1);
    }
}

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));

// Map flat documented env names to Proxy:* config keys so both forms bind:
//   PROXY_API_KEY        -> Proxy:ApiKey          (documented flat name)
//   Proxy__ApiKey        -> Proxy:ApiKey          (ASP.NET Core double-underscore form)
//   CACHE_TTL_SECONDS    -> Proxy:CacheTtlSeconds  (documented flat name)
//   Proxy__CacheTtlSeconds -> Proxy:CacheTtlSeconds (double-underscore form)
var flat = new Dictionary<string, string?>();
if (Environment.GetEnvironmentVariable("PROXY_API_KEY") is { } apiKey)
{
    flat["Proxy:ApiKey"] = apiKey;
}
if (Environment.GetEnvironmentVariable("CACHE_TTL_SECONDS") is { } cacheTtl)
{
    flat["Proxy:CacheTtlSeconds"] = cacheTtl;
}
if (flat.Count > 0)
{
    builder.Configuration.AddInMemoryCollection(flat);
}

builder.Services.AddOptions<TvHeadendOptions>()
    .BindConfiguration(TvHeadendOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<ProxyOptions>()
    .BindConfiguration(ProxyOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHttpClient<TvHeadendClient>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<PlaylistRewriter>();

builder.Services.AddHealthChecks();

var app = builder.Build();

// Per-request logging via Serilog. Its RequestPath property is the request PATH only
// (the query string is excluded), so a key supplied as ?apikey=... is never written to
// our logs (CWE-598). The framework's own query-bearing "Request starting/finished"
// lines are already suppressed by the Serilog "Microsoft": "Warning" override in
// appsettings.json. NOTE: an external reverse proxy may still log the full URL — prefer
// the X-Api-Key header where the consumer supports it (documented in the README).
app.UseSerilogRequestLogging();

// Restrict /health to loopback so it is not reachable from the LAN.
// Docker's in-container HEALTHCHECK calls http://localhost:33721/health, which
// satisfies this restriction. NOTE: if an external orchestrator (e.g. Kubernetes
// probing the pod IP) needs to reach /health, remove RequireHost or add the pod
// IP / cluster CIDR here — loopback-only is intentional for the Docker use case.
// (A bracketed IPv6 literal "[::1]" is intentionally NOT listed — .NET 10's host
// matcher fails to parse it; "localhost"/"127.0.0.1" cover the Docker healthcheck.)
app.MapHealthChecks("/health").RequireHost("localhost", "127.0.0.1");

const string CacheKey = "channels-playlist";

app.MapGet("/api/tvheadend/channels", async (
    TvHeadendClient client,
    PlaylistRewriter rewriter,
    IOptions<TvHeadendOptions> tvh,
    IOptions<ProxyOptions> proxy,
    IMemoryCache cache,
    ILogger<Program> log,
    CancellationToken ct) =>
{
    var ttl = proxy.Value.CacheTtlSeconds;

    if (ttl > 0 && cache.TryGetValue(CacheKey, out byte[]? cached))
    {
        return Results.File(cached!, "application/octet-stream", "channels.m3u");
    }

    try
    {
        var raw = await client.FetchAsync(ct);
        var body = rewriter.Rewrite(raw, tvh.Value);
        var bytes = Encoding.UTF8.GetBytes(body);

        if (ttl > 0)
        {
            cache.Set(CacheKey, bytes, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(ttl)
            });
        }

        return Results.File(bytes, "application/octet-stream", "channels.m3u");
    }
    catch (OperationCanceledException) when (ct.IsCancellationRequested)
    {
        // Genuine client cancellation — propagate.
        throw;
    }
    catch (HttpRequestException ex)
    {
        log.LogError(ex, "Upstream TvHeadend fetch failed");
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
    catch (OperationCanceledException ex)
    {
        // Timeout or other server-side cancellation — treat as upstream failure.
        log.LogError(ex, "Upstream TvHeadend fetch failed");
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
}).AddEndpointFilter<ApiKeyEndpointFilter>();

app.Run();

// Required by WebApplicationFactory<Program> in integration tests (PLAN-3.1).
public partial class Program
{
    /// <summary>
    /// Resolves the listen port used by the healthcheck self-probe.
    /// Priority: ASPNETCORE_URLS env → DOTNET_URLS env → config["Urls"] → default 33721.
    /// Accepts optional overrides for the two env-var values so tests can call this
    /// method without mutating process-level environment variables.
    /// </summary>
    public static int ResolveHealthCheckPort(
        IConfiguration config,
        string? aspnetcoreUrls = null,
        string? dotnetUrls = null)
    {
        var urlString =
            aspnetcoreUrls
            ?? dotnetUrls
            ?? config["Urls"]
            ?? "http://*:33721";

        // Normalise wildcard/plus-sign hosts so Uri can parse the port.
        var normalised = urlString
            .Replace("*", "localhost", StringComparison.Ordinal)
            .Replace("+", "localhost", StringComparison.Ordinal);

        // Take the first URL when multiple are listed (semicolon-separated).
        var firstUrl = normalised.Split(';')[0].Trim();

        if (Uri.TryCreate(firstUrl, UriKind.Absolute, out var uri) && uri.Port > 0)
        {
            return uri.Port;
        }

        return 33721;
    }
}
