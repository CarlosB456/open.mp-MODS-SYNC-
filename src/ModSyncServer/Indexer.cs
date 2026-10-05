using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using ModSyncServer.Models;

namespace ModSyncServer.Services;

public static class ModIndexer
{
    private static readonly Dictionary<string, int> VehicleBaseIds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["emperor"] = 585,
        ["coach"] = 437,
        ["euros"] = 587,
        ["copcarla"] = 596,
        ["police_ls"] = 596,
        ["infernus"] = 411,
        ["bullet"] = 541,
        ["sultan"] = 560
    };

    private static readonly Dictionary<string, int> SkinBaseIds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["army"] = 287,
        ["csher"] = 283,
        ["dsher"] = 288,
        ["fbi"] = 286,
        ["lapd1"] = 280,
        ["lapdm1"] = 284,
        ["lvpd1"] = 282,
        ["sfpd1"] = 281,
        ["swat"] = 285
    };

    private static readonly Dictionary<string, int> AdditionModelIds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["swat_custom"] = 20001,
        ["carabineros_gope_custom"] = 20001,
        ["chilean_flag"] = -1001,
        ["suzuki_spresso"] = -1002
    };

    public static ServerManifest BuildManifest(string serverModsDir, string serverId = "chile_police_roleplay")
    {
        var manifest = new ServerManifest
        {
            ServerId = serverId,
            Name = "open.mp 1.5.9 ModSync - Chilean Edition",
            Version = "1.5.9",
            Author = "eLdarqO",
            Description = "Fully synchronized Chilean vehicle, skin, object and expansion pack for open.mp.",
            Mods = []
        };

        if (!Directory.Exists(serverModsDir))
        {
            return manifest;
        }

        // 1. Process 3D Categories (vehicles, skins, weapons, objects)
        ProcessCategory(serverModsDir, "vehicles", "vehicle", manifest);
        ProcessCategory(serverModsDir, "skins", "skin", manifest);
        ProcessCategory(serverModsDir, "weapons", "weapon", manifest);
        ProcessCategory(serverModsDir, "objects", "object", manifest);

        // 2. Process CLEO scripts, plugins, and text tables
        ProcessCleo(serverModsDir, manifest);

        // 3. Process Custom Audio assets
        ProcessAudio(serverModsDir, manifest);

        return manifest;
    }

    private static void ProcessCategory(string baseDir, string catName, string typeName, ServerManifest manifest)
    {
        var catDir = Path.Combine(baseDir, catName);
        if (!Directory.Exists(catDir))
        {
            return;
        }

        foreach (var mode in new[] { "replacements", "additions" })
        {
            var modeDir = Path.Combine(catDir, mode);
            if (!Directory.Exists(modeDir))
            {
                continue;
            }

            foreach (var subDir in Directory.GetDirectories(modeDir))
            {
                var modName = Path.GetFileName(subDir);
                var modId = $"{typeName}_{mode[..3]}_{modName}";
                var files = new List<ManifestFile>();

                foreach (var file in Directory.GetFiles(subDir, "*.*", SearchOption.AllDirectories))
                {
                    var ext = Path.GetExtension(file).ToLowerInvariant();
                    if (ext != ".dff" && ext != ".txd" && ext != ".col" && ext != ".cfg" && ext != ".txt")
                    {
                        continue;
                    }

                    var rel = Path.GetRelativePath(subDir, file).Replace('\\', '/');
                    var installRel = mode == "additions"
                        ? $"models/{Path.GetFileName(file)}"
                        : $"modloader/openmp_{manifest.ServerId}/{catName}/{modName}/{rel}";

                    files.Add(new ManifestFile
                    {
                        Path = rel,
                        SizeBytes = new FileInfo(file).Length,
                        Sha256 = ComputeSha256(file),
                        InstallPath = installRel,
                        Category = catName
                    });
                }

                if (files.Count > 0)
                {
                    int? baseId = DetectBaseId(modName, typeName, files);
                    int? newId = mode == "additions" ? DetectNewId(modName, typeName) : null;

                    manifest.Mods.Add(new ManifestMod
                    {
                        Id = modId,
                        Name = $"{(mode == "additions" ? "Added" : "Replacement")} {Capitalize(typeName)} - {modName}",
                        Type = typeName,
                        Version = "1.5.9",
                        Author = "eLdarqO",
                        Description = $"Synchronized {modName} ({mode}).",
                        BaseModelId = baseId,
                        NewModelId = newId,
                        Mode = mode,
                        Files = files
                    });
                }
            }
        }
    }

    private static void ProcessCleo(string baseDir, ServerManifest manifest)
    {
        var cleoDir = Path.Combine(baseDir, "cleo");
        if (!Directory.Exists(cleoDir))
        {
            return;
        }

        var files = new List<ManifestFile>();
        foreach (var file in Directory.GetFiles(cleoDir, "*.*", SearchOption.AllDirectories))
        {
            var ext = Path.GetExtension(file).ToLowerInvariant();
            if (ext != ".js" && ext != ".cs" && ext != ".ini" && ext != ".fxt" && ext != ".wav")
            {
                continue;
            }

            var rel = Path.GetRelativePath(cleoDir, file).Replace('\\', '/');
            files.Add(new ManifestFile
            {
                Path = rel,
                SizeBytes = new FileInfo(file).Length,
                Sha256 = ComputeSha256(file),
                InstallPath = $"cleo/{rel}",
                Category = "cleo"
            });
        }

        if (files.Count > 0)
        {
            manifest.Mods.Add(new ManifestMod
            {
                Id = "cleo_server_sync",
                Name = "Server CLEO Scripts & Plugins",
                Type = "script",
                Version = "1.5.9",
                Author = "eLdarqO",
                Description = "Synchronized server-side CLEO and Redux scripts.",
                Files = files
            });
        }
    }

    private static void ProcessAudio(string baseDir, ServerManifest manifest)
    {
        var audioDir = Path.Combine(baseDir, "audio");
        if (!Directory.Exists(audioDir))
        {
            return;
        }

        var files = new List<ManifestFile>();
        foreach (var file in Directory.GetFiles(audioDir, "*.*", SearchOption.AllDirectories))
        {
            var ext = Path.GetExtension(file).ToLowerInvariant();
            if (ext != ".wav" && ext != ".mp3" && ext != ".ogg")
            {
                continue;
            }

            var rel = Path.GetRelativePath(audioDir, file).Replace('\\', '/');
            files.Add(new ManifestFile
            {
                Path = rel,
                SizeBytes = new FileInfo(file).Length,
                Sha256 = ComputeSha256(file),
                InstallPath = $"audio/{rel}",
                Category = "audio"
            });
        }

        if (files.Count > 0)
        {
            manifest.Mods.Add(new ManifestMod
            {
                Id = "audio_server_sync",
                Name = "Server Audio",
                Type = "audio",
                Version = "1.5.9",
                Author = "eLdarqO",
                Description = "Synchronized audio assets.",
                Files = files
            });
        }
    }

    private static int? DetectBaseId(string name, string typeName, List<ManifestFile> files)
    {
        if (typeName == "vehicle")
        {
            foreach (var kvp in VehicleBaseIds)
            {
                if (name.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value;
                }
                foreach (var file in files)
                {
                    var stem = Path.GetFileNameWithoutExtension(file.Path);
                    if (stem.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase) || stem.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        return kvp.Value;
                    }
                }
            }
        }
        else if (typeName == "skin")
        {
            foreach (var kvp in SkinBaseIds)
            {
                if (name.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value;
                }
                foreach (var file in files)
                {
                    var stem = Path.GetFileNameWithoutExtension(file.Path);
                    if (stem.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase) || stem.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        return kvp.Value;
                    }
                }
            }
        }
        return null;
    }

    private static int DetectNewId(string name, string typeName)
    {
        foreach (var kvp in AdditionModelIds)
        {
            if (name.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
            {
                return kvp.Value;
            }
        }
        return typeName == "skin" ? 20001 : -1002;
    }

    public static string ComputeSha256(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha256.ComputeHash(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Capitalize(string text) =>
        string.IsNullOrEmpty(text) ? text : char.ToUpperInvariant(text[0]) + text[1..];
}
