# ── Stage 1: build + test ─────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project and solution files first for layer-cached restore.
# The .sln references tests/ at ..\tests\... (relative to Source/),
# so the test csproj must be present before dotnet restore.
COPY Source/TvHeadEndM3uProxy.sln                                                           Source/
COPY Source/TvHeadEndM3uProxy/TvHeadEndM3uProxy.csproj                                      Source/TvHeadEndM3uProxy/
COPY Source/TvHeadEndM3uProxyService/TvHeadEndM3uProxyService.csproj                         Source/TvHeadEndM3uProxyService/
COPY tests/TvHeadEndM3uProxyService.Tests/TvHeadEndM3uProxyService.Tests.csproj              tests/TvHeadEndM3uProxyService.Tests/
COPY SharedAssemblyInfo.cs ./

# Restore all packages (cached until a .csproj/.sln changes).
RUN dotnet restore "Source/TvHeadEndM3uProxy.sln"

# Copy full source (cache miss on any source edit is acceptable here).
COPY Source/  Source/
COPY tests/   tests/

# Run all tests. A failing test aborts the build — a broken image cannot be produced.
RUN dotnet test "Source/TvHeadEndM3uProxy.sln" --no-restore -c Release \
    --logger "console;verbosity=minimal"

# Publish only the host project (excludes test artifacts from the output).
# The service library is a project reference and is included automatically.
RUN dotnet publish "Source/TvHeadEndM3uProxy/TvHeadEndM3uProxy.csproj" \
    --no-restore -c Release -o /app/publish

# ── Stage 2: runtime ──────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Explicit ENV makes the listen port self-documenting and consistent with the
# reference dashboard Dockerfile convention. Operators may override at runtime
# with -e ASPNETCORE_URLS=http://+:9000 (also update the port mapping).
ENV ASPNETCORE_URLS=http://+:33721

EXPOSE 33721

# Self-probe healthcheck — exec/JSON-array form required (no shell in chiseled image).
# dotnet is on PATH in the aspnet runtime image (/usr/bin/dotnet).
# The probe reads ASPNETCORE_URLS/DOTNET_URLS/appsettings.json to find the port,
# GETs /health, and exits 0 (healthy) or 1 (unhealthy).
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD ["dotnet", "TvHeadEndM3uProxy.dll", "healthcheck"]

# Set non-root user after all COPY instructions (UID 1654, defined by the base image).
USER $APP_UID

ENTRYPOINT ["dotnet", "TvHeadEndM3uProxy.dll"]
