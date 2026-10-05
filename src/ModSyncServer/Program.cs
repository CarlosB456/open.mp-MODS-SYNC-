using System;
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

        ServerManifest manifest = ModIndexer.BuildManifest(resolvedModsDir);
        string manifestJson = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(manifestCachePath, manifestJson);
        Console.WriteLine($"[INFO] Successfully indexed {manifest.Mods.Count} mods into {manifestCachePath}");

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
            mods_count = manifest.Mods.Count,
            status = "operational"
        }));

        // Manifest endpoints
        app.MapGet("/manifest.json", () => Results.Text(manifestJson, "application/json"));
        app.MapGet("/manifest", () => Results.Text(manifestJson, "application/json"));
        app.MapGet("/api/manifest", () => Results.Text(manifestJson, "application/json"));

        // Health monitoring
        app.MapGet("/api/health", () => Results.Ok(new
        {
            status = "healthy",
            version = Version,
            developer = Credits,
            timestamp = DateTime.UtcNow
        }));

        // Dynamic reload endpoint
        app.MapGet("/api/refresh", () =>
        {
            manifest = ModIndexer.BuildManifest(resolvedModsDir);
            manifestJson = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(manifestCachePath, manifestJson);
            Console.WriteLine($"[INFO] Manifest refreshed dynamically. Total mods: {manifest.Mods.Count}");
            return Results.Ok(new { status = "refreshed", mods_count = manifest.Mods.Count });
        });

        // CDN binary asset distribution endpoints
        app.MapGet("/api/download/{modId}/{*fileName}", (string modId, string fileName) => DownloadModFile(resolvedModsDir, modId, fileName));
        app.MapGet("/manifest.json/api/download/{modId}/{*fileName}", (string modId, string fileName) => DownloadModFile(resolvedModsDir, modId, fileName));
        app.MapGet("/manifest/api/download/{modId}/{*fileName}", (string modId, string fileName) => DownloadModFile(resolvedModsDir, modId, fileName));

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[INFO] ModSync CDN Server active and listening on http://0.0.0.0:{port}/");
        Console.ResetColor();

        app.Run();
    }

    public static IResult DownloadModFile(string resolvedModsDir, string modId, string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Contains(".."))
        {
            return Results.BadRequest("Invalid file path requested.");
        }

        // Search in categorized server_mods directory
        string candidate = FindModFile(resolvedModsDir, modId, fileName);
        if (!string.IsNullOrEmpty(candidate) && File.Exists(candidate))
        {
            return Results.File(candidate, "application/octet-stream", Path.GetFileName(candidate));
        }

        // Fallback search in local mod_files directory if present
        string legacyCandidate = Path.Combine(AppContext.BaseDirectory, "mod_files", modId, fileName);
        if (File.Exists(legacyCandidate))
        {
            return Results.File(legacyCandidate, "application/octet-stream", Path.GetFileName(legacyCandidate));
        }

        return Results.NotFound($"Requested mod asset '{fileName}' for mod '{modId}' was not found.");
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
        var targetFileName = Path.GetFileName(fileName);
        if (!Directory.Exists(modsDir))
        {
            return string.Empty;
        }

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
                    if (part.Length > 3 && normMatch.Contains(part, StringComparison.OrdinalIgnoreCase))
                    {
                        return match;
                    }
                }
            }
            return matches[0];
        }

        return string.Empty;
    }
}
