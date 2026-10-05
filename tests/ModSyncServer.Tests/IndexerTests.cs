using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using ModSyncServer;
using ModSyncServer.Models;
using ModSyncServer.Services;
using Xunit;

namespace ModSyncServer.Tests;

public class IndexerTests : IDisposable
{
    private readonly string _tempDir;

    public IndexerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "modsync_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try
            {
                Directory.Delete(_tempDir, true);
            }
            catch
            {
                // Best-effort cleanup
            }
        }
    }

    [Fact]
    public void BuildManifest_WithNonExistentDirectory_ReturnsEmptyManifest()
    {
        string nonExistent = Path.Combine(_tempDir, "does_not_exist");
        ServerManifest manifest = ModIndexer.BuildManifest(nonExistent, "test_server");

        Assert.NotNull(manifest);
        Assert.Equal("test_server", manifest.ServerId);
        Assert.Empty(manifest.Mods);
    }

    [Fact]
    public void BuildManifest_IndexesVehicleReplacements_Correctly()
    {
        string vehicleDir = Path.Combine(_tempDir, "vehicles", "replacements", "hyundai_accent_taxi");
        Directory.CreateDirectory(vehicleDir);

        string dffPath = Path.Combine(vehicleDir, "emperor.dff");
        string txdPath = Path.Combine(vehicleDir, "emperor.txd");
        File.WriteAllText(dffPath, "dummy dff content");
        File.WriteAllText(txdPath, "dummy txd content");

        ServerManifest manifest = ModIndexer.BuildManifest(_tempDir, "test_server");

        Assert.Single(manifest.Mods);
        ManifestMod mod = manifest.Mods[0];
        Assert.Equal("vehicle_rep_hyundai_accent_taxi", mod.Id);
        Assert.Equal("vehicle", mod.Type);
        Assert.Equal("replacements", mod.Mode);
        Assert.Equal(585, mod.BaseModelId);
        Assert.Equal(2, mod.Files.Count);

        string expectedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("dummy dff content"))).ToLowerInvariant();
        ManifestFile dffFile = Assert.Single(mod.Files, f => f.Path == "emperor.dff");
        Assert.Equal(expectedHash, dffFile.Sha256);
        Assert.Equal("modloader/openmp_test_server/vehicles/hyundai_accent_taxi/emperor.dff", dffFile.InstallPath);
    }

    [Fact]
    public void BuildManifest_IndexesAdditions_WithCustomModelIds()
    {
        string skinDir = Path.Combine(_tempDir, "skins", "additions", "carabineros_gope_custom");
        Directory.CreateDirectory(skinDir);

        File.WriteAllText(Path.Combine(skinDir, "swat_custom.dff"), "skin dff");
        File.WriteAllText(Path.Combine(skinDir, "swat_custom.txd"), "skin txd");

        ServerManifest manifest = ModIndexer.BuildManifest(_tempDir, "test_server");

        Assert.Single(manifest.Mods);
        ManifestMod mod = manifest.Mods[0];
        Assert.Equal("skin_add_carabineros_gope_custom", mod.Id);
        Assert.Equal("skin", mod.Type);
        Assert.Equal("additions", mod.Mode);
        Assert.Equal(20001, mod.NewModelId);
    }

    [Fact]
    public void BuildManifest_IndexesCleoAndAudio_Properly()
    {
        string cleoDir = Path.Combine(_tempDir, "cleo");
        string audioDir = Path.Combine(_tempDir, "audio");
        Directory.CreateDirectory(cleoDir);
        Directory.CreateDirectory(audioDir);

        File.WriteAllText(Path.Combine(cleoDir, "modsync_sdk.js"), "console.log('sdk');");
        File.WriteAllText(Path.Combine(audioDir, "siren.wav"), "RIFF dummy audio");

        ServerManifest manifest = ModIndexer.BuildManifest(_tempDir, "test_server");

        Assert.Equal(2, manifest.Mods.Count);
        Assert.Contains(manifest.Mods, m => m.Id == "cleo_server_sync" && m.Type == "script");
        Assert.Contains(manifest.Mods, m => m.Id == "audio_server_sync" && m.Type == "audio");
    }

    [Fact]
    public void DownloadModFile_RejectsPathTraversalAttempts()
    {
        var result = Program.DownloadModFile(_tempDir, "test_mod", "../../../secret.txt");
        Assert.NotNull(result);
    }

    [Fact]
    public void FindModFile_LocatesExistingModFile()
    {
        string subDir = Path.Combine(_tempDir, "vehicles", "replacements", "taxi");
        Directory.CreateDirectory(subDir);
        string targetFile = Path.Combine(subDir, "model.dff");
        File.WriteAllText(targetFile, "content");

        string found = Program.FindModFile(_tempDir, "vehicle_rep_taxi", "model.dff");
        Assert.Equal(targetFile, found);
    }

    [Fact]
    public void BuildManifest_FiltersOutUnauthorizedExtensions()
    {
        string weaponDir = Path.Combine(_tempDir, "weapons", "replacements", "m4_pack");
        Directory.CreateDirectory(weaponDir);

        File.WriteAllText(Path.Combine(weaponDir, "m4.dff"), "valid dff");
        File.WriteAllText(Path.Combine(weaponDir, "unauthorized.exe"), "malicious content");
        File.WriteAllText(Path.Combine(weaponDir, "notes.tmp"), "temp notes");

        ServerManifest manifest = ModIndexer.BuildManifest(_tempDir, "test_server");

        var mod = Assert.Single(manifest.Mods);
        Assert.Single(mod.Files);
        Assert.Equal("m4.dff", mod.Files[0].Path);
    }

    [Fact]
    public void BuildManifest_HandlesEmptyCategorySubfolders_Gracefully()
    {
        string emptyDir = Path.Combine(_tempDir, "objects", "additions", "empty_object");
        Directory.CreateDirectory(emptyDir);

        ServerManifest manifest = ModIndexer.BuildManifest(_tempDir, "test_server");
        Assert.Empty(manifest.Mods);
    }

    [Fact]
    public void FindModFile_ReturnsEmptyForNonExistentFile()
    {
        string found = Program.FindModFile(_tempDir, "unknown_mod", "does_not_exist.dff");
        Assert.Empty(found);
    }
}

