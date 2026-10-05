using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModSyncServer.Models;
using ModSyncServer.Services;

namespace ModSyncServer;

public class Program
{
    private const string Version = "1.5.9";
    private const string Credits = "eLdarqO";

    private static readonly object SyncLock = new();
    private static ServerManifest _currentManifest = new();
    private static Dictionary<string, string> _fileLookup = new(StringComparer.OrdinalIgnoreCase);

    public static void Main(string[] args)
    {
        int port = 8080;
        string serverModsPath = "server_mods";
        bool reindexOnly = false;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].Equals("--port", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                int.TryParse(args[++i], out port);
            }
            else if (args[i].Equals("--mods-dir", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                serverModsPath = args[++i];
            }
            else if (args[i].Equals("--reindex", StringComparison.OrdinalIgnoreCase))
            {
                reindexOnly = true;
            }
        }

        string resolvedModsDir = ResolveModsDirectory(serverModsPath);
        string manifestCachePath = Path.Combine(Path.GetDirectoryName(resolvedModsDir) ?? Directory.GetCurrentDirectory(), "manifest.json");

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("==================================================");
        Console.WriteLine($"  open.mp ModSync Server v{Version}");
        Console.WriteLine($"  open.mp & {Credits}");
        Console.WriteLine("==================================================");
        Console.ResetColor();
        Console.WriteLine($"[INFO] Mod directory resolved: {resolvedModsDir}");

        RefreshState(resolvedModsDir, manifestCachePath);
        Console.WriteLine($"[INFO] Successfully indexed {_currentManifest.Mods.Count} mods into {manifestCachePath}");

        if (reindexOnly)
        {
            Console.WriteLine("[INFO] Re-indexing complete. Exiting.");
            return;
        }

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(port);
        });

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            });
        });

        var app = builder.Build();
        app.UseCors();

        // Service root & status
        app.MapGet("/", () => Results.Ok(new
        {
            service = "open.mp ModSync CDN Server",
            version = Version,
            developer = Credits,
            mods_count = _currentManifest.Mods.Count,
            status = "operational"
        }));

        app.MapGet("/api/status", () => Results.Ok(new
        {
            status = "online",
            version = Version,
            credits = Credits,
            mods_count = _currentManifest.Mods.Count
        }));

        app.MapGet("/api/server-info", () => Results.Ok(new
        {
            server_id = _currentManifest.ServerId,
            server_name = _currentManifest.ServerName,
            version = Version,
            credits = Credits,
            mod_count = _currentManifest.Mods.Count,
            required_launcher = _currentManifest.RequiredLauncherVersion
        }));

        // Manifest endpoints
        app.MapGet("/manifest.json", () => Results.Text(GetManifestJson(), "application/json"));
        app.MapGet("/manifest", () => Results.Text(GetManifestJson(), "application/json"));
        app.MapGet("/api/manifest", () => Results.Text(GetManifestJson(), "application/json"));

        // Health monitoring
        app.MapGet("/api/health", () => Results.Ok(new
        {
            status = "healthy",
            version = Version,
            developer = Credits,
            timestamp = DateTime.UtcNow
        }));

        // Dynamic reload endpoints (supporting both GET and POST)
        app.MapGet("/api/refresh", () => HandleRefresh(resolvedModsDir, manifestCachePath));
        app.MapPost("/api/refresh", () => HandleRefresh(resolvedModsDir, manifestCachePath));

        // CDN binary asset distribution endpoints
        app.MapGet("/api/download/{modId}/{*fileName}", (string modId, string fileName) => DownloadModFile(resolvedModsDir, modId, fileName, _fileLookup));
        app.MapGet("/manifest.json/api/download/{modId}/{*fileName}", (string modId, string fileName) => DownloadModFile(resolvedModsDir, modId, fileName, _fileLookup));
        app.MapGet("/manifest/api/download/{modId}/{*fileName}", (string modId, string fileName) => DownloadModFile(resolvedModsDir, modId, fileName, _fileLookup));

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[INFO] ModSync CDN Server active and listening on http://0.0.0.0:{port}/");
        Console.ResetColor();

        app.Run();
    }

    private static IResult HandleRefresh(string resolvedModsDir, string manifestCachePath)
    {
        RefreshState(resolvedModsDir, manifestCachePath);
        Console.WriteLine($"[INFO] Manifest refreshed dynamically. Total mods: {_currentManifest.Mods.Count}");
        return Results.Ok(new { status = "refreshed", mods_count = _currentManifest.Mods.Count, version = Version });
    }

    private static void RefreshState(string resolvedModsDir, string manifestCachePath)
    {
        lock (SyncLock)
        {
            _currentManifest = ModIndexer.BuildManifest(resolvedModsDir);
            _fileLookup = ModIndexer.BuildFileLookup(resolvedModsDir, _currentManifest);
            string manifestJson = JsonSerializer.Serialize(_currentManifest, new JsonSerializerOptions { WriteIndented = true });
            try
            {
                File.WriteAllText(manifestCachePath, manifestJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] Could not persist manifest to {manifestCachePath}: {ex.Message}");
            }
        }
    }

    private static string GetManifestJson()
    {
        lock (SyncLock)
        {
            return JsonSerializer.Serialize(_currentManifest, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    public static IResult DownloadModFile(string resolvedModsDir, string modId, string fileName, IReadOnlyDictionary<string, string>? lookup = null)
    {
        if (string.IsNullOrWhiteSpace(modId) || string.IsNullOrWhiteSpace(fileName))
        {
            return Results.BadRequest("Mod ID and file name are required.");
        }

        if (modId.Contains("..") || fileName.Contains("..") ||
            modId.Contains('/') || modId.Contains('\\') ||
            fileName.Contains(':') || Path.IsPathRooted(fileName))
        {
            return Results.BadRequest("Invalid file path requested.");
        }

        var cleanFileName = fileName.TrimStart('/', '\\').Replace('\\', '/');

        // 1. Primary lookup using O(1) indexed cache
        if (lookup != null)
        {
            var keyWithSubpath = $"{modId}/{cleanFileName}".ToLowerInvariant();
            if (lookup.TryGetValue(keyWithSubpath, out var indexedPath) && File.Exists(indexedPath))
            {
                if (IsSafeChildPath(resolvedModsDir, indexedPath))
                {
                    return Results.File(indexedPath, GetContentType(indexedPath), Path.GetFileName(indexedPath));
                }
            }

            var keyWithFileName = $"{modId}/{Path.GetFileName(cleanFileName)}".ToLowerInvariant();
            if (lookup.TryGetValue(keyWithFileName, out var fallbackIndexedPath) && File.Exists(fallbackIndexedPath))
            {
                if (IsSafeChildPath(resolvedModsDir, fallbackIndexedPath))
                {
                    return Results.File(fallbackIndexedPath, GetContentType(fallbackIndexedPath), Path.GetFileName(fallbackIndexedPath));
                }
            }
        }

        // 2. Direct directory resolution under resolvedModsDir
        string candidate = FindModFile(resolvedModsDir, modId, cleanFileName);
        if (!string.IsNullOrEmpty(candidate) && File.Exists(candidate))
        {
            if (IsSafeChildPath(resolvedModsDir, candidate))
            {
                return Results.File(candidate, GetContentType(candidate), Path.GetFileName(candidate));
            }
            return Results.BadRequest("Access outside mod directory denied.");
        }

        // 3. Fallback search in local mod_files directory if present
        string safeLegacyDir = Path.Combine(AppContext.BaseDirectory, "mod_files");
        string legacyCandidate = Path.Combine(safeLegacyDir, modId, cleanFileName);
        if (File.Exists(legacyCandidate) && IsSafeChildPath(safeLegacyDir, legacyCandidate))
        {
            return Results.File(legacyCandidate, GetContentType(legacyCandidate), Path.GetFileName(legacyCandidate));
        }

        return Results.NotFound($"Requested mod asset '{fileName}' for mod '{modId}' was not found.");
    }

    public static bool IsSafeChildPath(string parentDir, string childPath)
    {
        try
        {
            var fullParent = Path.GetFullPath(parentDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var fullChild = Path.GetFullPath(childPath);
            return fullChild.StartsWith(fullParent, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public static string GetContentType(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".dff" or ".txd" or ".col" => "application/octet-stream",
            ".wav" => "audio/wav",
            ".mp3" => "audio/mpeg",
            ".ogg" => "audio/ogg",
            ".js" => "application/javascript",
            ".json" => "application/json",
            ".ini" or ".txt" or ".fxt" or ".cfg" => "text/plain",
            _ => "application/octet-stream"
        };
    }

    public static string ResolveModsDirectory(string candidate)
    {
        var locations = new[]
        {
            candidate,
            Path.Combine(AppContext.BaseDirectory, candidate),
            Path.Combine(Directory.GetCurrentDirectory(), candidate),
            Path.Combine(Directory.GetCurrentDirectory(), "server_mods"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", candidate),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "server_mods"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", candidate),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "server_mods"),
            Path.Combine(Directory.GetCurrentDirectory(), "server", candidate),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", candidate),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "server_mods"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", candidate),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "server_mods")
        };

        foreach (var loc in locations)
        {
            if (Directory.Exists(loc))
            {
                return Path.GetFullPath(loc);
            }
        }

        return Path.GetFullPath(candidate);
    }

    public static string FindModFile(string modsDir, string modId, string fileName)
    {
        if (!Directory.Exists(modsDir) || string.IsNullOrWhiteSpace(fileName))
        {
            return string.Empty;
        }

        var cleanFileName = fileName.TrimStart('/', '\\').Replace('\\', '/');

        // Deterministic matching based on mod conventions
        if (modId.Equals("cleo_server_sync", StringComparison.OrdinalIgnoreCase))
        {
            var cand = Path.Combine(modsDir, "cleo", cleanFileName);
            if (File.Exists(cand)) return Path.GetFullPath(cand);
        }
        else if (modId.Equals("audio_server_sync", StringComparison.OrdinalIgnoreCase))
        {
            var cand = Path.Combine(modsDir, "audio", cleanFileName);
            if (File.Exists(cand)) return Path.GetFullPath(cand);
        }
        else
        {
            var parts = modId.Split('_');
            if (parts.Length >= 3)
            {
                var cat = parts[0] switch
                {
                    "vehicle" => "vehicles",
                    "skin" => "skins",
                    "weapon" => "weapons",
                    "object" => "objects",
                    _ => parts[0]
                };
                var mode = parts[1] == "add" ? "additions" : "replacements";
                var sub = string.Join('_', parts[2..]);
                var cand = Path.Combine(modsDir, cat, mode, sub, cleanFileName);
                if (File.Exists(cand)) return Path.GetFullPath(cand);
            }
        }

        // Direct modId folder or direct file fallback
        var directCand = Path.Combine(modsDir, modId, cleanFileName);
        if (File.Exists(directCand)) return Path.GetFullPath(directCand);

        var fileOnlyCand = Path.Combine(modsDir, cleanFileName);
        if (File.Exists(fileOnlyCand)) return Path.GetFullPath(fileOnlyCand);

        // Safe filename search (without wildcard injection)
        var targetFileName = Path.GetFileName(cleanFileName);
        if (!string.IsNullOrEmpty(targetFileName) && !targetFileName.Contains('*') && !targetFileName.Contains('?'))
        {
            var matches = Directory.GetFiles(modsDir, targetFileName, SearchOption.AllDirectories);
            if (matches.Length == 1)
            {
                return matches[0];
            }
            if (matches.Length > 1)
            {
                var parts = modId.Split('_', StringSplitOptions.RemoveEmptyEntries);
                foreach (var match in matches)
                {
                    var normMatch = match.Replace('\\', '/');
                    foreach (var part in parts)
                    {
                        if (part.Length > 3 && !part.Equals("server", StringComparison.OrdinalIgnoreCase) && normMatch.Contains(part, StringComparison.OrdinalIgnoreCase))
                        {
                            return match;
                        }
                    }
                }
                return matches[0];
            }
        }

        return string.Empty;
    }
}
