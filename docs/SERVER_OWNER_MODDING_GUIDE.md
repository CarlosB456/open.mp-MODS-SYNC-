# open.mp ModSync Server Owner & Modding Architecture Guide

**Platform Version**: open.mp 1.5.9.0  
**Client Version**: 0.4.0 - R1  
**Architect & Developer**: eLdarqO  
**Framework**: .NET 10.0 / ASP.NET Core Kestrel CDN / ModLoader Sandbox  

---

## 1. Executive Summary & Core Architecture

The open.mp ModSync system provides an enterprise-grade pipeline for delivering, synchronizing, and isolating custom server assets in Grand Theft Auto: San Andreas multiplayer. Traditional SA-MP servers were limited to hardcoded client assets or required manual installation of mod packs, causing client crashes, version mismatches, and asset collisions.

ModSync resolves this through a dual-channel architecture:
1. **High-Performance .NET 10 CDN Server**: Provides SHA-256 cryptographic verification, manifest generation, automated directory indexing, and high-throughput binary chunk distribution over HTTP/REST on port 8080.
2. **Client-Side Isolated Sandbox**: The ModSync launcher synchronizes assets into isolated ModLoader profiles (`modloader/openmp_<server_id>/`), mounts CLEO scripts into sandboxed runtime folders, and guarantees that vanilla game files are never altered or overwritten.

```
+-----------------------------------------------------------------------------+
|                            MODSYNC ARCHITECTURE                             |
+-----------------------------------------------------------------------------+

   SERVER HOST                                CLIENT MACHINE
  +--------------------------+               +-------------------------------+
  |  open.mp Game Server     |               |  GTA San Andreas Directory    |
  |  (omp-server.exe)        |               |                               |
  |  - Port 7777 (UDP)       |<--- Game ---->|  - gta_sa.exe (LAA 4GB)       |
  |  - CustomModels.dll      |     Network   |  - modloader/                 |
  |  - Pawn Gamemode         |               |    `- openmp_<server_id>/     |
  +--------------------------+               |       |-- vehicles/           |
               |                             |       |-- skins/              |
     Shares Asset IDs                        |       |-- weapons/            |
     & Event Protocol                        |       `-- objects/            |
               |                             |  - cleo/                      |
  +--------------------------+               |    |-- scripts/               |
  |  ModSync CDN Server      |               |    |-- cleo_audio/            |
  |  (ModSyncServer.exe)     |               |    `-- text/                  |
  |  - Port 8080 (HTTP)      |<--- HTTP ---->|  ModSync Launcher (0.4.0-R1)  |
  |  - manifest.json         |     Sync      |  - Cryptographic Verification |
  |  - /api/download/{id}    |               |  - Staging & Session Sandbox  |
  +--------------------------+               +-------------------------------+
```

---

## 2. Replacements vs Additions: Architecture and Decision Matrix

ModSync supports two complementary approaches for introducing custom 3D models and textures: **Asset Replacement** and **Asset Addition (Content Expansion)**.

### Replacements Architecture

Asset replacements override base Grand Theft Auto: San Andreas models in memory at runtime via ModLoader. The physical files on the player's disk (`gta3.img`, `gta_int.img`, `player.img`) remain 100% untouched.

* **Target Directory**: `server_mods/<category>/replacements/<mod_name>/`
* **File Requirements**: The `.dff` (geometry) and `.txd` (texture dictionary) filenames must exactly match the internal GTA SA model name (e.g., `infernus.dff`, `infernus.txd`, `swat.dff`, `swat.txd`).
* **Game Integration**: Automatically activates when the game engine requests the model ID.
* **Benefits**:
  - Uses existing vehicle handling, physics configurations, and default sound banks.
  - Retains native animations, weapon reload cycles, and weapon firing rates.
  - Does not consume custom model ID limits.

### Additions (Content Expansion) Architecture

Asset additions expand the game world beyond the original 2004 asset limits. They register brand-new IDs inside the open.mp `CustomModels.dll` pipeline, allowing custom assets to exist alongside original models.

* **Target Directory**: `server_mods/<category>/additions/<mod_name>/`
* **File Requirements**: Arbitrary unique filenames (e.g., `suzuki_spresso.dff`, `chilean_flag.dff`, `swat_custom.dff`).
* **Game Integration**: Registered in `artconfig.txt` and Pawn gamemodes via `AddCharModel` or `AddSimpleModel`.
* **ID Allocation Rules**:
  - **Skins**: ID range `20000` to `30000`. Base model ID must reference an existing vanilla skin (e.g., `285` for SWAT).
  - **Objects, Props, & Weapons**: Negative virtual IDs (e.g., `-1001`, `-1002`) mapped from base object ID `19300` (safe dynamic prop slot).

### Technical Comparison Matrix

| Feature | Replacements (`replacements/`) | Additions (`additions/`) |
| :--- | :--- | :--- |
| Engine Hook | ModLoader memory redirection | open.mp CustomModels engine (`CustomModels.dll`) |
| Storage Location on Client | `modloader/openmp_<server_id>/...` | `models/` staging & ModLoader cache |
| ID Scope | Base GTA SA IDs (Vehicles 400-611, Skins 0-311, Weapons 321-372) | Skin IDs 20000+, Object/Prop IDs 19300+ / Negative IDs |
| Original Asset Access | Overwritten in memory for this session | Original asset remains available concurrently |
| Handling / Sound Config | Uses base vehicle/weapon parameters | Uses assigned base model parameters |
| artconfig.txt Required | No | Yes |

---

## 3. Vehicle Pipeline: Replacements and Handling Architecture

### Directory Layout

```
server_mods/vehicles/
|-- replacements/
|   |-- hyundai_accent_taxi/
|   |   |-- emperor.dff              # Vehicle mesh and dummy hierarchy
|   |   `-- emperor.txd              # Texture dictionary
|   `-- marcopolo_andimar_bus/
|       |-- coach.dff
|       `-- coach.txd
`-- additions/
    `-- suzuki_spresso/
        |-- suzuki_spresso.dff       # Registered with negative ID in artconfig.txt
        `-- suzuki_spresso.txd
```

### 3D Model Dummy Hierarchy Requirements

GTA San Andreas vehicles require a precise dummy node hierarchy. When modeling in 3ds Max, Blender, or ZModeler, all components must branch from `chassis_dummy`:

```
chassis_dummy
|-- chassis                 # Visible main vehicle body mesh
|-- chassis_vlo             # Very Low-poly Optimization model for distant rendering
|-- wheel_rf_dummy          # Right front wheel pivot
|-- wheel_lf_dummy          # Left front wheel pivot
|-- wheel_rb_dummy          # Right back wheel pivot
|-- wheel_lb_dummy          # Left back wheel pivot
|-- door_rf_dummy           # Passenger front door
|   |-- door_rf_ok          # Intact door mesh
|   `-- door_rf_dam         # Damaged door mesh
|-- door_lf_dummy           # Driver front door
|   |-- door_lf_ok
|   `-- door_lf_dam
|-- door_rr_dummy           # Passenger rear door (4-door models)
|-- door_lr_dummy           # Driver rear door (4-door models)
|-- bonnet_dummy            # Engine hood
|-- boot_dummy              # Trunk / luggage compartment
|-- windscreen_dummy        # Front windshield glass
|-- ped_frontseat           # Driver seat coordinate dummy
|-- ped_passenger           # Front passenger seat coordinate dummy
|-- headlights              # Headlight 3D flare coordinates
|-- taillights              # Taillight 3D flare coordinates
`-- exhaust                 # Exhaust particle emitter position
```

### Collision Mesh (`.col`) Integration

Vehicles can include an embedded `.col` block inside the `.dff` file or a separate `.col` file. Key collision criteria:
1. **Spheres**: Collision spheres must encompass the cabin, engine, and bumpers for projectile and damage calculation.
2. **Boxes / Convexhulls**: Floor collision must be positioned precisely above the wheel radii to prevent the vehicle from clipping through road meshes.
3. **Shadow Mesh**: Shadow mesh polygons must be planar and positioned directly under the chassis with surface normal pointing upward.

### Streaming Memory Management & Large Address Aware (LAA)

Modern high-polygon vehicle models (such as 20 MB+ DFF files) can rapidly exhaust the 32-bit GTA SA memory ceiling (originally set to 128 MB streaming memory in 2004).

Symptoms of streaming memory exhaustion:
- Disappearing vehicle bodies leaving only floating drivers and wheels.
- Texture pop-in and gray terrain flashing across the screen.
- Crash with unhandled exception `0xC0000005` at address `0x00538000+`.

**Mandatory Client Tuning**:
1. **Large Address Aware (LAA)**: The `gta_sa.exe` binary must have the LAA header flag enabled, granting 4 GB of virtual address space on 64-bit Windows.
2. **Streaming Memory Budget**: ModSync enforces a 2048 MB memory budget (2047 MB safe ceiling) by writing to memory offset `0x8A5A80` via `modsync_sdk.js`:

```javascript
// Enforce 2048MB memory ceiling (0x8A5A80) in CLEO Redux runtime
if (typeof Memory !== 'undefined') {
    Memory.WriteU32(0x8A5A80, 2047 * 1024 * 1024);
    if (Memory.ReadU32(0x8E4CB4) >= 2047 * 1024 * 1024) {
        Memory.WriteU32(0x8E4CB4, 0);
    }
}
```

---

## 4. Weapons Pipeline: Model Replacements & Custom 3D Attachments

### Weapon Model Replacements

To replace standard weapons, place the corresponding `.dff` and `.txd` into `server_mods/weapons/replacements/<mod_name>/`. The ModSync indexer maps the weapon name to its base ID automatically:

| Weapon Type | DFF Name | TXD Name | Base Weapon ID |
| :--- | :--- | :--- | :--- |
| Baseball Bat | `bat.dff` | `bat.txd` | 336 |
| Colt 45 9mm | `colt45.dff` | `colt45.txd` | 346 |
| Silenced 9mm | `silenced.dff` | `silenced.txd` | 347 |
| Desert Eagle | `desert_eagle.dff` / `deagle.dff` | `desert_eagle.txd` | 348 |
| Standard Shotgun | `chromegun.dff` / `shotgun.dff` | `chromegun.txd` | 349 |
| Sawn-Off Shotgun | `sawnoff.dff` | `sawnoff.txd` | 350 |
| Combat Shotgun | `shotgspa.dff` / `spas12.dff` | `shotgspa.txd` | 351 |
| Micro Uzi | `micro_uzi.dff` / `uzi.dff` | `micro_uzi.txd` | 352 |
| MP5 Submachine Gun | `mp5lng.dff` / `mp5.dff` | `mp5lng.txd` | 353 |
| AK-47 Assault Rifle | `ak47.dff` | `ak47.txd` | 355 |
| M4 Assault Rifle | `m4.dff` | `m4.txd` | 356 |
| Country Rifle | `cuntgun.dff` / `rifle.dff` | `cuntgun.txd` | 357 |
| Sniper Rifle | `sniper.dff` | `sniper.txd` | 358 |
| Rocket Launcher | `rocketla.dff` / `rpg.dff` | `rocketla.txd` | 359 |
| Teargas / Grenade | `teargas.dff` / `grenade.dff` | `teargas.txd` | 343 / 342 |

### Custom 3D Weapon Additions via Attached Objects

Because GTA San Andreas hardcodes weapon animation states to weapon slots 1 through 12, adding a visually distinct weapon without replacing original models is achieved by:
1. Registering the custom weapon model as an object in `artconfig.txt`.
2. Attaching the object to the player character's hand bone using `SetPlayerAttachedObject`.
3. Associating the attachment with an underlying functional weapon ID for firing logic and damage detection.

#### Pawn Implementation Example

```pawn
// Attachment Index (0 to 9)
#define ATTACH_INDEX_WEAPON 0

// Bone constants for character skeletal hierarchy
#define BONE_SPINE        1
#define BONE_HEAD         2
#define BONE_LEFT_ARM     3
#define BONE_RIGHT_ARM    4
#define BONE_LEFT_HAND    5
#define BONE_RIGHT_HAND   6

/**
 * Attaches a custom registered weapon model to the player's right hand.
 * @param playerid The player ID
 * @param custom_model_id Negative ID defined in artconfig.txt (e.g. -1001)
 */
stock EquipCustomWeaponObject(playerid, custom_model_id) {
    // Offset and rotation calibrated for standard pistol / melee grip
    SetPlayerAttachedObject(
        playerid,
        ATTACH_INDEX_WEAPON,
        custom_model_id,
        BONE_RIGHT_HAND,
        0.08, 0.03, -0.02,   // Offset (X, Y, Z) in meters
        180.0, 90.0, 0.0,    // Rotation (Roll, Pitch, Yaw) in degrees
        1.0, 1.0, 1.0,       // Scale (X, Y, Z)
        0, 0                 // Material colors (0 = default textures)
    );
    return 1;
}

stock RemoveCustomWeaponObject(playerid) {
    if (IsPlayerAttachedObjectSlotUsed(playerid, ATTACH_INDEX_WEAPON)) {
        RemovePlayerAttachedObject(playerid, ATTACH_INDEX_WEAPON);
    }
    return 1;
}
```

---

## 5. Custom Audio & Siren Synchronization

### Directory Structure & Distribution

Audio assets are placed in two locations:
1. `server_mods/audio/`: Global ambient tracks and UI sounds.
2. `server_mods/cleo/audio/`: Script-triggered sounds (e.g., police sirens, megaphone calls, custom engine tones).

The ModSync launcher maps files from `server_mods/cleo/audio/` directly into `cleo/cleo_audio/` on the client.

### CLEO Redux Siren Script Implementation

The following complete CLEO Redux JavaScript script (`server_mods/cleo/scripts/sync_sirens.js`) demonstrates loading, looping, and controlling a custom police siren stream with keyboard input:

```javascript
/// <reference path=".config/sa.d.ts" />
/**
 * Synchronized Emergency Siren Controller
 * Author: eLdarqO
 * Platform: open.mp 1.5.9.0 / Client 0.4.0 - R1
 */

const KEY_SIREN_TOGGLE = 51; // Number key '3' (VK_3)
let sirenAudioStream = null;
let isSirenActive = false;

while (true) {
    wait(50);

    const player = new Player(0);
    const char = player.getChar();

    if (!char) continue;

    // Verify player is driving an emergency vehicle
    if (char.isSittingInAnyCar()) {
        const car = char.getCarCarriedBy();
        const modelId = car.getModel();

        // 596: Police LS, 597: Police SF, 598: Police LV, 427: Enforcer, 416: Ambulance
        const isEmergencyVehicle = (modelId === 596 || modelId === 597 || modelId === 598 || modelId === 427 || modelId === 416);

        if (isEmergencyVehicle && Pad.IsKeyPressed(KEY_SIREN_TOGGLE)) {
            isSirenActive = !isSirenActive;

            if (isSirenActive) {
                // Initialize streaming audio from isolated ModSync audio directory
                if (!sirenAudioStream) {
                    sirenAudioStream = AudioStream.Create("cleo/cleo_audio/siren.wav");
                }
                if (sirenAudioStream) {
                    sirenAudioStream.setLooping(true);
                    sirenAudioStream.setVolume(0.85);
                    sirenAudioStream.play();
                    showTextBox("Emergency Siren: ACTIVE");
                }
            } else {
                if (sirenAudioStream) {
                    sirenAudioStream.stop();
                    sirenAudioStream = null;
                    showTextBox("Emergency Siren: OFF");
                }
            }

            // Debounce key
            while (Pad.IsKeyPressed(KEY_SIREN_TOGGLE)) {
                wait(100);
            }
        }
    } else {
        // Player exited vehicle while siren was sounding
        if (isSirenActive) {
            isSirenActive = false;
            if (sirenAudioStream) {
                sirenAudioStream.stop();
                sirenAudioStream = null;
            }
        }
    }
}
```

---

## 6. CLEO Scripts & CLEO Redux Architecture

ModSync provides universal support for both legacy compiled `.cs` bytecode scripts (CLEO 4/5) and modern JavaScript/TypeScript `.js`/`.ts` scripts (CLEO Redux).

### Dual Engine Capability

* **CLEO 4 / 5 (`.cs`)**: Compiled Sanny Builder opcode scripts. Suited for low-level memory patches and legacy modifications.
* **CLEO Redux (`.js`)**: Modern ES6+ JavaScript running in a V8 runtime inside the game process. Supports JSON serialization, asynchronous timers, memory manipulation, and direct interaction with the ModSync SDK.

### Complete CLEO Redux Production Examples

#### 1. Custom Vehicle & Weapon Spawner (`custom_vehicle_spawner.js`)

Cycles through registered Chilean vehicle and weapon presets on key `F10` (`VK_F10 = 121`):

```javascript
/// <reference path=".config/sa.d.ts" />
/**
 * ModSync Chilean Vehicle & Weapon Showcase Spawner
 * Framework: open.mp 1.5.9.0 / Client 0.4.0 - R1
 * Author: eLdarqO
 */

wait(3000);
try {
    showTextBox("ModSync: Press F10 to cycle server showcase vehicles and weapons.");
} catch (e) {}

let currentPresetIndex = 0;
const VEHICLE_PRESETS = [
    { vehId: 585, vehName: "Hyundai Accent GLS Taxi", weaponId: 5, weaponName: "Chilean Flag (Bat)", ammo: 1 },
    { vehId: 437, vehName: "Marcopolo Paradiso 1800 DD Bus", weaponId: 31, weaponName: "M4 Tactical Rifle", ammo: 500 },
    { vehId: 587, vehName: "Suzuki Spresso Addition", weaponId: 24, weaponName: "Desert Eagle 9mm", ammo: 250 },
    { vehId: 596, vehName: "Carabineros Radio Patrulla SUV", weaponId: 34, weaponName: "Sniper Rifle", ammo: 100 }
];

while (true) {
    wait(50);
    try {
        if (Pad.IsKeyPressed(121)) { // F10 Key
            const player = new Player(0);
            const char = player.getChar();

            if (char) {
                const preset = VEHICLE_PRESETS[currentPresetIndex];
                currentPresetIndex = (currentPresetIndex + 1) % VEHICLE_PRESETS.length;

                const pos = char.getCoordinates();
                const heading = char.getHeading();

                // Calculate spawn offset 3.5 meters in front of player
                const rad = (heading + 90.0) * (Math.PI / 180.0);
                const spawnX = pos.x + Math.cos(rad) * 3.5;
                const spawnY = pos.y + Math.sin(rad) * 3.5;
                const spawnZ = pos.z + 0.5;

                // Request and load model into streaming memory
                Streaming.RequestModel(preset.vehId);
                Streaming.LoadAllModelsNow();

                const vehicle = Car.Create(preset.vehId, spawnX, spawnY, spawnZ);
                if (vehicle) {
                    vehicle.setHeading(heading);
                    char.warpIntoCar(vehicle);
                }

                // Equip corresponding weapon
                char.giveWeapon(preset.weaponId, preset.ammo);
                showTextBox("Spawned: " + preset.vehName + " | Equipped: " + preset.weaponName);

                // Debounce key until release
                while (Pad.IsKeyPressed(121)) {
                    wait(100);
                }
            }
        }
    } catch (err) {}
}
```

#### 2. Nitrous Particle Effect & RPC Listener (`sync_nitro_effects.js`)

Listens for the `SERVER_NITRO_TRIGGER` event broadcast from the open.mp Pawn gamemode via the ModSync SDK:

```javascript
/// <reference path=".config/sa.d.ts" />
/**
 * Synchronized Nitrous Particle Emitter
 * Author: eLdarqO
 */

wait(1000);

if (typeof globalThis.ModSync !== 'undefined' && globalThis.ModSync) {
    globalThis.ModSync.on("SERVER_NITRO_TRIGGER", (data, senderPlayerId) => {
        try {
            const vehicleId = data.vehicleId;
            showTextBox("ModSync: Synchronized Nitrous Activated on Vehicle " + vehicleId);
        } catch (e) {}
    });
}

while (true) {
    wait(1000);
}
```

#### 3. Custom GXT Text Tables (`.fxt`)

FXT files provide localized in-game GXT text strings used by scripts and the game HUD.
* **File Location**: `server_mods/cleo/text/openmp_chile.fxt`
* **Format**: Key (up to 7 characters) followed by space and the string value:

```
TAX_01 Hyundai Accent GLS Taxi Chileno
BUS_01 Marcopolo Paradiso 1800 DD Andimar
GOPE_01 Carabineros G.O.P.E. Unidad Tactica
WPN_01 Bandera Chilena de Combate
```

In CLEO Redux, invoke via `showTextBox("~g~" + Text.Get("TAX_01"));`.

#### 4. INI Configuration Files (`.ini`)

INI files provide configurable parameters without modifying script code.
* **File Location**: `server_mods/cleo/config/openmp_sync.ini`
* **Format**:

```ini
[Settings]
EnableCustomVehicles=1
EnableSirens=1
StreamingMemoryMB=2048
DebugMode=0

[Keybinds]
SirenToggleKey=51
VehicleSpawnerKey=121
```

---

## 7. Custom Animations & Visual Particle Effects

### Custom Animations (`.ifp`)

Custom animations allow servers to introduce realistic movement patterns, tactical holding stances, or roleplay emotes without modifying `anim/ped.ifp` directly.

1. **ModLoader Mounting**:
   Place custom `.ifp` files in `server_mods/packs/<pack_name>/<custom_name>.ifp`. ModLoader intercepts `CAnimManager` calls and registers the animation block.
2. **Pawn Server Execution**:
   Load and apply animations using the open.mp native animation API:

```pawn
// Trigger custom animation on player
ApplyAnimation(
    playerid,
    "PED",              // Animation library
    "WALK_civi",         // Animation action name
    4.1,                // Animation speed delta
    1,                  // Loop flag (1 = loop, 0 = single play)
    1,                  // Lock X position
    1,                  // Lock Y position
    0,                  // Freeze on last frame
    0,                  // Time in milliseconds (0 = indefinite)
    1                   // Force network synchronization across all clients
);
```

### Visual Particle Effects & Texture Dictionaries

Server owners can deploy customized particle effects by supplying a modified `particle.txd` in `server_mods/textures/particle/particle.txd`:
* **Smoke Particles**: High-definition tire burnout smoke and nitrous exhaust plumes.
* **Fire & Spark Textures**: Realistic weapon muzzle flashes and explosive debris.
* **Custom HUD Textures**: High-resolution radar disks (`radar_centre.png`, `hud.txd`).

ModLoader automatically gives priority to texture dictionaries inside the server folder, ensuring the player returns to standard textures immediately upon leaving the server.

---

## 8. Server Owner Workflow & Automation Pipeline

```
+-------------------------------------------------------------------------+
|                  STEP-BY-STEP SERVER OWNER WORKFLOW                     |
+-------------------------------------------------------------------------+

  1. DROP-IN FILES
     Copy .dff, .txd, .js, .cs, .wav into server_mods/<category>/...
                          |
                          v
  2. AUTOMATIC INDEXING
     Run index_mods.bat OR ModSyncServer.exe --reindex
     -> Computes SHA-256 hashes
     -> Generates manifest.json
     -> Generates artconfig.txt
                          |
                          v
  3. PAWN INTEGRATION
     Include modsync.inc in gamemode
     Call ModSync_Init() and ModSync_RegisterMods() in OnGameModeInit()
                          |
                          v
  4. START SERVER
     Execute start_server.bat
     -> ModSync CDN starts on port 8080
     -> open.mp server starts on UDP port 7777
                          |
                          v
  5. CLIENT AUTODOWNLOAD & PLAY
     Client launcher connects, downloads diffs, verifies hashes,
     and enters server with isolated sandbox active.
```

### Automation Scripts

* **`index_mods.bat`**: Re-indexes all files in `server_mods/` and outputs `manifest.json`.
* **Zero-Downtime Hot-Reload**: While the server is running, invoke:
  ```
  curl -X POST http://localhost:8080/api/refresh
  ```
  The CDN server re-indexes newly added mods immediately without dropping player connections.

### Gamemode Integration Boilerplate (`modsync.inc`)

```pawn
#include <open.mp>
#include "modsync.inc"

public OnGameModeInit() {
    // 1. Initialize ModSync core subsystem
    ModSync_Init("artconfig.txt");

    // 2. Register custom additions with open.mp CustomModels engine
    ModSync_RegisterMods();

    return 1;
}

public OnPlayerFinishedDownloading(playerid, virtualworld) {
    // 3. Flag player as synchronized
    ModSync_OnPlayerFinishedDownloading(playerid);
    SendClientMessage(playerid, 0x00FF88AA, "ModSync: All custom server assets verified and active.");
    return 1;
}
```

---

## 9. Diagnostic Matrix & Troubleshooting

| Symptom | Probable Cause | Corrective Action |
| :--- | :--- | :--- |
| Vehicles render invisible or flickering | Streaming memory buffer depleted by high-poly models | Ensure `modsync_sdk.js` writes `2047MB` to `0x8A5A80` and verify client executable has the 4GB Large Address Aware (LAA) flag enabled. |
| Model has white/missing textures | Texture names in `.txd` do not match internal `.dff` material names | Open `.dff` in RW Analyze or 3ds Max, check material texture name string, and rename matching raster in `.txd`. |
| Custom addition model ID does not spawn | Incorrect ID range or base model assignment | Skins must use IDs `20000+` and base ID `0-311`. Objects must use base ID `19300` and negative new IDs (`-1001`, `-1002`). |
| CLEO Redux script does not run | Missing TypeScript/JavaScript definitions or runtime syntax error | Check `cleo_redux.log` in GTA root. Verify script starts with coroutine loop (`while (true) { wait(...); }`). |
| Client cannot download assets from CDN | HTTP port 8080 blocked by firewall or router NAT | Allow TCP port 8080 inbound in Windows Firewall. Verify CDN responds via `curl http://localhost:8080/api/health`. |
| Downloaded mods remain after leaving server | Player used non-ModSync launcher | Launch only via ModSync Launcher (`0.4.0-R1`), which automatically isolates files into `modloader/openmp_<server_id>/` and cleans up on exit. |

---

## 10. Credits & Provenance

* **eLdarqO**: Principal Architect and Developer of ModSync Server, CDN engine, packaging pipeline, and Chilean roleplay mod infrastructure.
* **open.mp Team**: The open multiplayer project server runtime and `CustomModels.dll` engine.
