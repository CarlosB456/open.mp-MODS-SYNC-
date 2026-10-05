using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ModSyncServer.Models;

public class ManifestFile
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("size_bytes")]
    public long SizeBytes { get; set; }

    [JsonPropertyName("sha256")]
    public string Sha256 { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("install_path")]
    public string? InstallPath { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }
}

public class ManifestMod
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.5.9";

    [JsonPropertyName("author")]
    public string Author { get; set; } = "eLdarqO";

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("base_model_id")]
    public int? BaseModelId { get; set; }

    [JsonPropertyName("new_model_id")]
    public int? NewModelId { get; set; }

    [JsonPropertyName("is_addition")]
    public bool IsAddition { get; set; }

    [JsonPropertyName("required")]
    public bool Required { get; set; } = true;

    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "replacement";

    [JsonPropertyName("files")]
    public List<ManifestFile> Files { get; set; } = [];
}

public class ServerManifest
{
    [JsonPropertyName("server_id")]
    public string ServerId { get; set; } = "chile_police_roleplay";

    [JsonPropertyName("server_name")]
    public string ServerName { get; set; } = "open.mp 1.5.9 ModSync - Chilean Edition";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "open.mp 1.5.9 ModSync - Chilean Edition";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.5.9";

    [JsonPropertyName("author")]
    public string Author { get; set; } = "eLdarqO";

    [JsonPropertyName("credits")]
    public string Credits { get; set; } = "eLdarqO";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "Fully synchronized Chilean vehicle, skin, object and expansion pack for open.mp.";

    [JsonPropertyName("required_launcher_version")]
    public string RequiredLauncherVersion { get; set; } = "0.4.0 - R1";

    [JsonPropertyName("mods")]
    public List<ManifestMod> Mods { get; set; } = [];
}
