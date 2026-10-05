# open.mp ModSync Server Package

High-performance mod synchronization and custom content infrastructure for open.mp (version 1.5.9.0) and client (0.4.0 - R1).

Architect & Developer: **eLdarqO**  
Framework: **open.mp 1.5.9.0 / .NET 10.0 / ModLoader Sandbox**  
License: See [LICENSE.md](LICENSE.md)  
Comprehensive Technical Guide: See [docs/SERVER_OWNER_MODDING_GUIDE.md](docs/SERVER_OWNER_MODDING_GUIDE.md)

---

## 1. Architectural Overview

The ModSync server infrastructure integrates a high-throughput .NET 10 CDN server alongside the open.mp multiplayer game server. It provides automated SHA-256 cryptographic indexing, categorized mod distribution, server isolation, and live synchronization for custom models, textures, CLEO scripts, and audio assets without altering client base files.

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
   |   Port: 7777 (UDP)  |      |   1. Query manifest   |      |   stream.ini        |
   |   - CustomModels    |<---->|   2. Verify SHA-256   +----->|   modloader/servers/|
   |   - LegacyNetwork   |      |   3. Staging / Cache  |      |   cleo/servers/     |
   |   - Pawn Gamemode   |      |   4. Safe Execution   |      |   (gta3.img clean)  |
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
|   |-- CustomModels.dll        # Native open.mp model expansion engine
|   `-- LegacyNetwork.dll       # Client compatibility and network layer
|-- docs/
|   `-- SERVER_OWNER_MODDING_GUIDE.md  # Comprehensive modding and architecture guide
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
|   |-- audio/                  # Custom sound effects and ambient tracks (.wav, .mp3)
|   |-- cleo/                   # CLEO bytecode (.cs), Redux (.js), and configs (.ini)
|   |   |-- audio/              # Sound effects triggered by CLEO scripts
|   |   |-- config/             # INI configuration files
|   |   |-- scripts/            # JavaScript (.js) and CLEO bytecode (.cs) scripts
|   |   `-- text/               # Localized GXT string tables (.fxt)
|   |-- objects/                # Object replacements and additions
|   |-- skins/                  # Skin replacements and additions (IDs 20000+)
|   |-- textures/               # Custom texture dictionaries (particle.txd, etc.)
|   |-- vehicles/               # Vehicle replacements and additions
|   `-- weapons/                # Weapon replacements and additions
|-- src/
|   `-- ModSyncServer/          # .NET 10 CDN Server source code
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

### Live Zero-Downtime Reload
While the server is running, trigger a live re-index of new assets without restarting:
```cmd
curl -X POST http://localhost:8080/api/refresh
```

---

## 4. Server Owner Capabilities & Production Examples

### A. Replacements vs Additions Architecture

ModSync gives server owners two powerful techniques to customize visual and functional game elements:

1. **Replacements (`server_mods/<category>/replacements/<mod_name>/`)**:
   - Replaces vanilla assets in memory dynamically using ModLoader without altering client base files.
   - **Vehicles**: Inherits native handling, wheel suspension, doors, engine sounds, and physics automatically. Server owners can optionally include custom `handling.cfg`, `vehicles.ide`, `carcols.dat`, and `carmods.dat` inside the mod folder to override vehicle performance.
   - **Weapons**: Replaces 3D model and textures while retaining native fire animations, ammo slots, and reload timings.
   - Example path: `server_mods/vehicles/replacements/hyundai_accent_taxi/emperor.dff`
2. **Additions / Content Expansion (`server_mods/<category>/additions/<mod_name>/`)**:
   - Introduces brand-new 3D assets registered via open.mp `CustomModels.dll` and configured in `artconfig.txt`.
   - **Skins**: Native custom character models using ID range `20000` to `30000` (e.g. `20001` Carabineros GOPE), fully selectable via `SetPlayerSkin()`.
   - **Objects & Props**: Negative IDs (`-1001`, `-1002`) mapped from base object ID `19300`.
   - **Weapons**: Custom 3D meshes attached to character bones via `SetPlayerAttachedObject()`.
   - **Vehicle Models as Objects**: Models registered with `AddSimpleModel` (`-1002 suzuki_spresso`) load as 3D object props (for showroom displays or `AttachObjectToVehicle`), not drivable cars. Drivable cars must use `replacements/`.

Example `artconfig.txt`:
```
; Format: AddCharModel <base_skin_id> <new_id> <dff> <txd>
AddCharModel 285 20001 swat_custom.dff swat_custom.txd

; Format: AddSimpleModel <virtual_world> <base_obj_id> <new_id> <dff> <txd>
AddSimpleModel -1 19300 -1001 chilean_flag.dff chilean_flag.txd
AddSimpleModel -1 19300 -1002 suzuki_spresso.dff suzuki_spresso.txd
```

---

### B. Vehicles: Replacements, Handling & Streaming Memory Budget

#### Directory Structure with Optional Handling Override
```
server_mods/vehicles/replacements/hyundai_accent_taxi/
|-- emperor.dff    # 3D vehicle mesh with chassis_dummy hierarchy
|-- emperor.txd    # Texture dictionary
|-- handling.cfg   # Optional: Custom engine acceleration, braking, center of mass
`-- vehicles.ide   # Optional: Custom wheel radius and animation flags
```

#### Memory Tuning via `stream.ini` & Large Address Aware (LAA)
When serving high-polygon vehicle meshes (e.g. 20 MB+ DFF models), the default 32-bit GTA SA streaming budget (128 MB) can cause models to disappear or textures to flicker.

ModSync configures `stream.ini` in the GTA root:
```ini
memory		2096128
devkit_memory	2096128
vehicles	96
pe_lightchangerate	0.0005
pe_lightingbasecap	0.35
pe_lightingbasemult	0.5
pe_leftx	16
pe_topy		16
pe_rightx	16
pe_bottomy	16
dontbuildpaths
```

* **`memory 2096128` (2047 MB in KB)**: Avoids the signed 32-bit integer overflow bug in GTA SA's memory allocator that occurs if set to exactly 2048 MB (`0x80000000` = negative number).
* **`vehicles 96`**: Boosts the concurrent vehicle streaming pool from 32/48 to 96, eliminating model despawns in crowded multiplayer areas.
* **Large Address Aware (LAA)**: Expands 32-bit address space to 4 GB on 64-bit Windows. Automated by the ModSync Launcher (`0.4.0-R1`).
* **Runtime Guard**: `modsync_sdk.js` writes `2047MB` to memory offset `0x8A5A80` and clears `0x8E4CB4` as an auxiliary runtime safeguard.

---

### C. Weapons: Replacements & Custom 3D Attachments

#### Model Replacements
Drop `.dff` and `.txd` pairs into `server_mods/weapons/replacements/<mod_name>/`:
- `bat.dff` / `bat.txd`: Baseball bat (Base ID: 336)
- `colt45.dff` / `colt45.txd`: 9mm Pistol (Base ID: 346)
- `deagle.dff` / `deagle.txd`: Desert Eagle (Base ID: 348)
- `shotgspa.dff` / `shotgspa.txd`: Combat Shotgun (Base ID: 351)
- `ak47.dff` / `ak47.txd`: AK-47 Assault Rifle (Base ID: 355)
- `m4.dff` / `m4.txd`: M4 Carbine (Base ID: 356)

#### Custom 3D Weapon Additions via Attached Objects
Because GTA SA hardcodes weapon firing anims to base IDs, brand-new weapon models can be added using `AddSimpleModel` and attached to the player's skeletal hierarchy using `SetPlayerAttachedObject`:

```pawn
// Attach custom 3D model to player's right hand (Bone 6)
SetPlayerAttachedObject(
    playerid,
    0,                  // Attachment slot (0-9)
    -1001,              // Model ID from artconfig.txt
    6,                  // Bone index: right hand
    0.08, 0.03, -0.02,  // Position offsets (X, Y, Z)
    180.0, 90.0, 0.0,   // Rotation (Roll, Pitch, Yaw)
    1.0, 1.0, 1.0,      // Scale (X, Y, Z)
    0, 0                // Default material colors
);
```

---

### D. Custom Audio & Sirens

ModSync provides two audio pipelines:

1. **open.mp Native HTTP Audio Streaming (Pawn)**:
   The ModSync CDN hosts audio files over HTTP. Gamemodes stream synchronized 3D directional audio to players:
   ```pawn
   PlayAudioStreamForPlayer(playerid, "http://127.0.0.1:8080/api/download/audio_server_sync/siren.wav", x, y, z, 50.0, 1);
   ```

2. **CLEO Redux Low-Latency Audio**:
   Audio assets placed in `server_mods/cleo/audio/` (such as `siren.wav`) are synchronized directly to `cleo/servers/<server_id>/audio/` and triggered via CLEO Redux:

```javascript
/// <reference path=".config/sa.d.ts" />
// Emergency Siren Trigger via CLEO Redux
const KEY_SIREN = 51; // '3' key (VK_3)
let sirenStream = null;

while (true) {
    wait(50);
    const player = new Player(0);
    const char = player.getChar();
    if (char && char.isSittingInAnyCar() && Pad.IsKeyPressed(KEY_SIREN)) {
        if (!sirenStream) {
            sirenStream = AudioStream.Create("cleo/cleo_audio/siren.wav");
            sirenStream.setLooping(true);
            sirenStream.play();
            showTextBox("Emergency Siren: ACTIVE");
        } else {
            sirenStream.stop();
            sirenStream = null;
            showTextBox("Emergency Siren: OFF");
        }
        while (Pad.IsKeyPressed(KEY_SIREN)) wait(100);
    }
}
```

---

### E. Universal CLEO Scripts & CLEO Redux

ModSync supports both compiled `.cs` bytecode (CLEO 4/5) and modern JavaScript `.js` scripts (CLEO Redux).

#### Production Examples Included in Package:
1. **Vehicle & Weapon Spawner (`custom_vehicle_spawner.js`)**:
   - Press `F10` (`VK_F10 = 121`) to cycle showcase vehicles and weapons.
   - Uses `Streaming.RequestModel()` and `Streaming.LoadAllModelsNow()`.
   - Spawns vehicles via `Car.Create()` and warps the player with `char.warpIntoCar()`.
2. **Nitrous Visual Effects (`sync_nitro_effects.js`)**:
   - Subscribes to server-side RPC events using `globalThis.ModSync.on("SERVER_NITRO_TRIGGER", ...)`.
3. **Custom GXT Text Tables (`.fxt`)**:
   - `server_mods/cleo/text/openmp_chile.fxt`: Localized text strings displayed via `showTextBox()`.
4. **Configuration Files (`.ini`)**:
   - `server_mods/cleo/config/openmp_sync.ini`: Script configuration without recompilation.

---

### F. Custom Animations & Particle Effects

1. **Custom Animations (`.ifp`)**:
   - Place custom `.ifp` packages into `server_mods/packs/<pack_name>/<name>.ifp`.
   - ModLoader intercepts animation blocks at runtime.
   - Pawn gamemodes trigger animations via `ApplyAnimation(playerid, "PED", "WALK_civi", 4.1, 1, 1, 1, 0, 0, 1)`.
2. **Particle Effects (`particle.txd`)**:
   - Place modified particle dictionaries into `server_mods/textures/particle/particle.txd`.
   - Upgrades tire burnout smoke, nitro flames, and weapon muzzle flares.

---

### G. Server Owner Automation & Pawn Integration

```pawn
#include <open.mp>
#include "modsync.inc"

public OnGameModeInit() {
    // 1. Initialize ModSync subsystem
    ModSync_Init("artconfig.txt");

    // 2. Register custom additions with open.mp CustomModels engine
    ModSync_RegisterMods();
    return 1;
}

public OnPlayerFinishedDownloading(playerid, virtualworld) {
    // 3. Mark player as synchronized
    ModSync_OnPlayerFinishedDownloading(playerid);
    SendClientMessage(playerid, 0x00FF88AA, "[ModSync] Server assets synchronized successfully.");
    return 1;
}
```

---

## 5. CDN API Endpoints

The ModSync CDN Server exposes the following REST endpoints on port 8080:

| Method | Route | Description |
| :--- | :--- | :--- |
| `GET` | `/` | Service health status, operational metrics, and indexed mod count. |
| `GET` | `/api/status` | Operational status, version, and mod count. |
| `GET` | `/api/server-info` | Server metadata, launcher version requirement, and pack information. |
| `GET` | `/manifest.json` | Full JSON manifest of all available mod assets with SHA-256 hashes. |
| `GET` | `/api/manifest` | Canonical manifest retrieval endpoint for client launchers. |
| `GET` | `/api/health` | Health monitoring endpoint returning timestamp and operational status. |
| `GET`, `POST` | `/api/refresh` | Dynamically re-indexes `server_mods/` without restarting the process. |
| `GET` | `/api/download/{modId}/{fileName}` | Secure chunked binary download stream for client synchronizers. |

Path traversal attacks (`..` segments, rooted paths, and path separators in query parameters) are strictly sanitized and rejected.

---

## 6. Compiling Pawn Scripts

Pawn source files can be compiled using the bundled `qawno` compiler:

```cmd
qawno\pawncc.exe gamemodes\modsync_gamemode.pwn -iqawno\include -ogamemodes\modsync_gamemode
qawno\pawncc.exe filterscripts\modsync_advanced_fs.pwn -iqawno\include -ofilterscripts\modsync_advanced_fs
```

---

## 7. Verification & Test Suite

The .NET 10 test suite covers mod indexing, path resolution, SHA-256 calculation, and route safety:

```cmd
dotnet test tests/ModSyncServer.Tests/ModSyncServer.Tests.csproj
```

---

## 8. Credits & Acknowledgments

- **eLdarqO**: Architecture, ModSync CDN Server, custom artwork pipeline, Chilean asset integration, and synchronization protocols.
- **open.mp Team**: The open multiplayer project core and component infrastructure.
