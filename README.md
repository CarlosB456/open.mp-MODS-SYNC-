# open.mp ModSync Server Package

High-performance mod synchronization and custom content infrastructure for open.mp (version 1.5.9.0) and client (0.4.0 - R1).

Architect & Developer: **eLdarqO**  
Framework: **open.mp 1.5.9.0 / .NET 10.0**  
License: See [LICENSE.md](LICENSE.md)

---

## 1. Architectural Overview

The ModSync server infrastructure integrates a native .NET 10 CDN server alongside the open.mp multiplayer game server. It provides automated SHA-256 indexing, categorized mod distribution, server isolation, and live synchronization for custom models, textures, CLEO scripts, and audio assets without altering client base files.

```
                              +---------------------------+
                              |   ModSync CDN Server      |
                              |   (.NET 10 / ASP.NET)     |
                              |                           |
                              |   Port: 8080              |
                              |   /manifest.json          |
                              |   /api/download/{id}      |
                              |   /api/health             |
                              +-------------+-------------+
                                            |
                         Hosts SHA-256      |  Serves dynamic
                         verified files     |  manifest.json
                                            |
   +---------------------+      +-----------+-----------+      +---------------------+
   |   open.mp Server    |      |   ModSync Launcher    |      |   GTA San Andreas   |
   |   (omp-server.exe)  |      |   Client (0.4.0-R1)   |      |   Client Directory  |
   |                     |      |                       |      |                     |
   |   Port: 7777        |      |   1. Query manifest   |      |   modloader/        |
   |   - CustomModels    |<---->|   2. Verify SHA-256   +----->|     openmp_server/  |
   |   - LegacyNetwork   |      |   3. Staging / Cache  |      |   cleo/             |
   |   - Pawn Gamemode   |      |   4. Safe Execution   |      |     servers/        |
   +---------------------+      +-----------------------+      +---------------------+
```

---

## 2. Directory Structure

```
.
|-- artconfig.txt               # open.mp custom artwork mapping configuration
|-- bans.json                   # Network ban list
|-- config.json                 # open.mp server runtime configuration
|-- index_mods.bat              # Standalone CLI indexing script
|-- manifest.json               # Pre-indexed asset manifest with SHA-256 hashes
|-- ModSyncServer.exe           # Compiled ModSync CDN Server (.NET 10)
|-- omp-server.exe              # open.mp 1.5.9.0 server executable
|-- start_server.bat            # Dual startup script (CDN + Game Server)
|-- components/                 # open.mp 1.5.9 runtime dynamic libraries (.dll)
|   |-- $CAPI.dll
|   |-- Actors.dll
|   |-- Checkpoints.dll
|   |-- Classes.dll
|   |-- Console.dll
|   |-- CustomModels.dll        # Native open.mp model expansion engine
|   |-- Databases.dll
|   |-- Dialogs.dll
|   |-- GangZones.dll
|   |-- LegacyConfig.dll
|   |-- LegacyNetwork.dll       # Client compatibility and network layer
|   |-- Menus.dll
|   |-- NPCs.dll
|   |-- Objects.dll
|   |-- Pawn.dll
|   |-- Pickups.dll
|   |-- Recordings.dll
|   |-- TextDraws.dll
|   |-- TextLabels.dll
|   |-- Timers.dll
|   |-- Variables.dll
|   `-- Vehicles.dll
|-- gamemodes/                  # Server gamemodes
|   |-- modsync_gamemode.pwn    # Chilean Roleplay & Showcase Gamemode (Pawn source)
|   `-- modsync_gamemode.amx    # Compiled bytecode
|-- filterscripts/              # Additional runtime filterscripts
|   |-- modsync_advanced_fs.pwn # ModSync synchronization filterscript
|   `-- modsync_advanced_fs.amx
|-- models/                     # Custom artwork meshes (.dff) and textures (.txd)
|-- qawno/                      # Pawn compiler (pawncc) and open.mp include headers
|   `-- include/
|       |-- modsync.inc         # ModSync Pawn integration header
|       `-- modsync_net.inc     # Network event RPC headers
|-- server_mods/                # Drop-in categorized mod repository
|   |-- server_info.json        # Server profile and metadata
|   |-- audio/                  # Custom sound effects and sirens (.wav, .mp3)
|   |-- cleo/                   # CLEO bytecode (.cs), Redux (.js), and configs (.ini)
|   |-- objects/                # Object replacements and additions
|   |-- skins/                  # Skin replacements and additions (20001+)
|   |-- vehicles/               # Vehicle replacements and additions (-1001+)
|   `-- weapons/                # Weapon replacements and additions
|-- src/
|   `-- ModSyncServer/          # .NET 10 CDN Server source code
|       |-- Indexer.cs          # Automated directory traversal and SHA-256 calculation
|       |-- Models.cs           # Manifest data transfer contracts
|       |-- Program.cs          # ASP.NET Core Kestrel HTTP endpoints
|       `-- ModSyncServer.csproj
`-- tests/
    `-- ModSyncServer.Tests/    # Automated unit tests for indexing and security
```

---

## 3. Quick Start

### Prerequisites
- Windows 10/11 or Windows Server (x64)
- [.NET 10.0 Runtime or SDK](https://dotnet.microsoft.com/download)
- DirectX 9.0c runtime libraries (for client visual validation)

### Launching the Full Server
Run the automated startup script from the root directory:
```cmd
start_server.bat
```
This batch routine:
1. Boots `ModSyncServer.exe` on `http://0.0.0.0:8080/`. If the executable is absent, it compiles and runs directly via `dotnet run --project src/ModSyncServer/ModSyncServer.csproj`.
2. Delays 2 seconds to allow socket initialization.
3. Launches `omp-server.exe` on UDP port `7777`.

### Re-indexing Server Mods
To recalculate hashes and update `manifest.json` after adding or modifying files in `server_mods/`:
```cmd
index_mods.bat
```
Alternatively, execute the CLI tool directly:
```cmd
ModSyncServer.exe --mods-dir server_mods --reindex
```

---

## 4. Server Configuration

The runtime environment is defined in `config.json`. Critical parameters:

```json
{
    "name": "open.mp 1.5.9 (eLdarqO)",
    "announce": false,
    "artwork": {
        "enable": true,
        "models_path": "models",
        "port": 7777,
        "web_server_bind": "0.0.0.0"
    },
    "network": {
        "bind": "0.0.0.0",
        "port": 7777,
        "allow_037_clients": true,
        "use_omp_encryption": false
    },
    "pawn": {
        "main_scripts": [
            "modsync_gamemode 1"
        ]
    }
}
```

- `"announce": false`: Keeps the server unlisted on master public lists for private synchronization.
- `"web_server_bind": "0.0.0.0"`: Binds the internal artwork web server to all network interfaces.
- `"bind": "0.0.0.0"`: Listens on all interfaces for player UDP packets.

---

## 5. CDN API Endpoints

The ModSync CDN Server exposes the following REST endpoints on port 8080:

| Method | Route | Description |
| :--- | :--- | :--- |
| `GET` | `/` | Service health status, operational metrics, and indexed mod count. |
| `GET` | `/manifest.json` | Full JSON manifest of all available mod assets with SHA-256 hashes. |
| `GET` | `/api/manifest` | Canonical manifest retrieval endpoint for client launchers. |
| `GET` | `/api/health` | Health monitoring endpoint returning timestamp and operational status. |
| `GET` | `/api/refresh` | Dynamically re-indexes `server_mods/` without restarting the process. |
| `GET` | `/api/download/{modId}/{fileName}` | Secure chunked binary download stream for client synchronizers. |

Path traversal attacks (e.g. `..` segments) are sanitized and rejected by default.

---

## 6. Modding Pipeline & Conventions

### Replacements
Place files in `server_mods/<category>/replacements/<mod_name>/`:
- Vehicles: `server_mods/vehicles/replacements/hyundai_accent_taxi/emperor.dff`
- Skins: `server_mods/skins/replacements/carabineros_swat/swat.dff`
- Weapons: `server_mods/weapons/replacements/chilean_flag/bat.dff`

Replacements overwrite game assets in memory using ModLoader, leaving disk game files original.

### Additions (Content Expansion)
Place files in `server_mods/<category>/additions/<mod_name>/`:
- Custom skins: Added with IDs in the `20001`+ range.
- Custom objects and vehicles: Configured with negative IDs (e.g., `-1001`, `-1002`) mapped in `artconfig.txt`.

Example `artconfig.txt`:
```
AddCharModel 285 20001 swat_custom.dff swat_custom.txd
AddSimpleModel -1 19300 -1001 chilean_flag.dff chilean_flag.txd
AddSimpleModel -1 19300 -1002 suzuki_spresso.dff suzuki_spresso.txd
```

### CLEO & Script Sync
Place script assets in `server_mods/cleo/`:
- Bytecode scripts: `cleo/scripts/*.cs`
- Redux scripts: `cleo/scripts/*.js`, `*.ts`
- Text libraries: `cleo/text/*.fxt`
- Configurations: `cleo/config/*.ini`
- Plugins: `cleo/plugins/*.cleo`

---

## 7. Compiling Pawn Scripts

Pawn source files can be compiled using the bundled `qawno` compiler:

```cmd
qawno\pawncc.exe gamemodes\modsync_gamemode.pwn -iqawno\include -ogamemodes\modsync_gamemode.amx
```

---

## 8. Verification & Test Suite

The .NET 10 test suite covers mod indexing, path resolution, SHA-256 calculation, and route safety:

```cmd
dotnet test tests/ModSyncServer.Tests/ModSyncServer.Tests.csproj
```

---

## 9. Credits & Acknowledgments

- **eLdarqO**: Architecture, ModSync CDN Server, custom artwork pipeline, Chilean asset integration, and synchronization protocols.
- **open.mp Team**: The open multiplayer project core and component infrastructure.
