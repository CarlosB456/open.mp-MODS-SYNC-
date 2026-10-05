using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
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
        Assert.Equal("vehicles", mod.Category);
        Assert.Equal("replacements", mod.Mode);
        Assert.False(mod.IsAddition);
        Assert.Equal(585, mod.BaseModelId);
        Assert.Equal(2, mod.Files.Count);

        string expectedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("dummy dff content"))).ToLowerInvariant();
        ManifestFile dffFile = Assert.Single(mod.Files, f => f.Path == "emperor.dff");
        Assert.Equal(expectedHash, dffFile.Sha256);
        Assert.Equal("dff", dffFile.Type);
        Assert.Equal("vehicles", dffFile.Category);
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
        Assert.Equal("skins", mod.Category);
        Assert.Equal("additions", mod.Mode);
        Assert.True(mod.IsAddition);
        Assert.Equal(20001, mod.NewModelId);
        Assert.Equal(285, mod.BaseModelId);
    }

    [Fact]
    public void BuildManifest_IndexesWeaponReplacements_DetectsWeaponBaseId()
    {
        string weaponDir = Path.Combine(_tempDir, "weapons", "replacements", "chilean_flag");
        Directory.CreateDirectory(weaponDir);

        File.WriteAllText(Path.Combine(weaponDir, "bat.dff"), "bat dff content");
        File.WriteAllText(Path.Combine(weaponDir, "bat.txd"), "bat txd content");

        ServerManifest manifest = ModIndexer.BuildManifest(_tempDir, "test_server");

        Assert.Single(manifest.Mods);
        ManifestMod mod = manifest.Mods[0];
        Assert.Equal("weapon_rep_chilean_flag", mod.Id);
        Assert.Equal("weapon", mod.Type);
        Assert.Equal("weapons", mod.Category);
        Assert.Equal(336, mod.BaseModelId);
        Assert.False(mod.IsAddition);
        Assert.Equal(2, mod.Files.Count);
        Assert.Contains(mod.Files, f => f.Type == "dff");
        Assert.Contains(mod.Files, f => f.Type == "txd");
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
        var cleoMod = Assert.Single(manifest.Mods, m => m.Id == "cleo_server_sync" && m.Type == "script");
        Assert.Equal("cleo", cleoMod.Category);
        Assert.Equal("js", cleoMod.Files[0].Type);

        var audioMod = Assert.Single(manifest.Mods, m => m.Id == "audio_server_sync" && m.Type == "audio");
        Assert.Equal("audio", audioMod.Category);
        Assert.Equal("wav", audioMod.Files[0].Type);
    }

    [Fact]
    public void BuildFileLookup_DistinguishesCleoAndAudioSirenWithoutCollision()
    {
        string cleoAudioDir = Path.Combine(_tempDir, "cleo", "audio");
        string serverAudioDir = Path.Combine(_tempDir, "audio");
        Directory.CreateDirectory(cleoAudioDir);
        Directory.CreateDirectory(serverAudioDir);

        string cleoSiren = Path.Combine(cleoAudioDir, "siren.wav");
        string serverSiren = Path.Combine(serverAudioDir, "siren.wav");
        File.WriteAllText(cleoSiren, "cleo siren content");
        File.WriteAllText(serverSiren, "server siren content");

        ServerManifest manifest = ModIndexer.BuildManifest(_tempDir, "test_server");
        var lookup = ModIndexer.BuildFileLookup(_tempDir, manifest);

        Assert.True(lookup.ContainsKey("cleo_server_sync/audio/siren.wav"));
        Assert.True(lookup.ContainsKey("audio_server_sync/siren.wav"));

        Assert.Equal(Path.GetFullPath(cleoSiren), lookup["cleo_server_sync/audio/siren.wav"]);
        Assert.Equal(Path.GetFullPath(serverSiren), lookup["audio_server_sync/siren.wav"]);
    }

    [Fact]
    public void DownloadModFile_RejectsPathTraversalAttempts_ReturnsBadRequest()
    {
        var result1 = Program.DownloadModFile(_tempDir, "test_mod", "../../../secret.txt");
        var statusResult1 = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result1);
        Assert.Equal(StatusCodes.Status400BadRequest, statusResult1.StatusCode);

        var result2 = Program.DownloadModFile(_tempDir, "../escape", "model.dff");
        var statusResult2 = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result2);
        Assert.Equal(StatusCodes.Status400BadRequest, statusResult2.StatusCode);

        var result3 = Program.DownloadModFile(_tempDir, "test_mod", "C:\\Windows\\win.ini");
        var statusResult3 = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result3);
        Assert.Equal(StatusCodes.Status400BadRequest, statusResult3.StatusCode);

        var result4 = Program.DownloadModFile(_tempDir, "invalid/mod", "model.dff");
        var statusResult4 = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result4);
        Assert.Equal(StatusCodes.Status400BadRequest, statusResult4.StatusCode);
    }

    [Fact]
    public void DownloadModFile_ServesExactFileUsingFileLookup()
    {
        string subDir = Path.Combine(_tempDir, "vehicles", "replacements", "taxi");
        Directory.CreateDirectory(subDir);
        string targetFile = Path.Combine(subDir, "model.dff");
        File.WriteAllText(targetFile, "dff byte content");

        ServerManifest manifest = ModIndexer.BuildManifest(_tempDir, "test_server");
        var lookup = ModIndexer.BuildFileLookup(_tempDir, manifest);

        var result = Program.DownloadModFile(_tempDir, "vehicle_rep_taxi", "model.dff", lookup);
        var fileResult = Assert.IsType<PhysicalFileHttpResult>(result);

        Assert.Equal(Path.GetFullPath(targetFile), fileResult.FileName);
        Assert.Equal("application/octet-stream", fileResult.ContentType);
    }

    [Fact]
    public void DownloadModFile_ReturnsNotFound_WhenFileDoesNotExist()
    {
        var result = Program.DownloadModFile(_tempDir, "valid_mod", "non_existent.dff");
        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, statusResult.StatusCode);
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

    [Fact]
    public void IsSafeChildPath_CorrectlyValidatesSubpaths()
    {
        string safeChild = Path.Combine(_tempDir, "sub", "file.txt");
        string unsafeChild = Path.Combine(_tempDir, "..", "secret.txt");

        Assert.True(Program.IsSafeChildPath(_tempDir, safeChild));
        Assert.False(Program.IsSafeChildPath(_tempDir, unsafeChild));
    }
}
