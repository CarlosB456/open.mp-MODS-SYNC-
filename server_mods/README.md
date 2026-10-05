# open.mp ModSync Server - Asset Staging Directory

Framework: **open.mp 1.5.9.0**  
Client Version: **0.4.0 - R1**  
Architect & Developer: **eLdarqO**  
Documentation Reference: See [../docs/SERVER_OWNER_MODDING_GUIDE.md](../docs/SERVER_OWNER_MODDING_GUIDE.md)

---

## 1. Directory Structure

Place all server-side assets into the appropriate subfolder below. The ModSync CDN server indexes these folders automatically.

```
server_mods/
|-- server_info.json         # Server metadata: name, unique server ID, required client version
|-- vehicles/
|   |-- replacements/        # Replace vanilla vehicles (e.g. emperor.dff, coach.dff, copcarla.dff)
|   `-- additions/           # Add new vehicles without altering vanilla assets (e.g. suzuki_spresso.dff)
|-- weapons/
|   |-- replacements/        # Replace vanilla weapons (e.g. bat.dff, colt45.dff, deagle.dff, m4.dff)
|   `-- additions/           # Add new weapons / custom attachments mapped via artconfig.txt
|-- skins/
|   |-- replacements/        # Replace vanilla ped skins (e.g. swat.dff, army.dff, lapd1.dff)
|   `-- additions/           # Add new custom skins with unique IDs (20000+) (e.g. swat_custom.dff)
|-- objects/
|   |-- replacements/        # Replace map and prop models
|   `-- additions/           # Add new custom objects and props mapped via artconfig.txt
|-- cleo/
|   |-- scripts/             # CLEO scripts (.cs for legacy bytecode, .js for CLEO Redux)
|   |-- plugins/             # CLEO runtime plugins (.cleo)
|   |-- text/                # Localized GXT string tables (.fxt)
|   |-- config/              # INI configuration files (.ini)
|   `-- audio/               # Script-triggered audio files (.wav, .mp3) mounted in cleo/cleo_audio/
|-- audio/                   # Global sound effects and sirens (.wav, .mp3)
|-- textures/                # Texture dictionaries (e.g. particle.txd, hud.txd)
`-- packs/                   # Complete mod bundles and animation packages (.ifp)
```

---

## 2. Replacements vs Additions

* **`replacements/`**: Overwrites visual models and textures in memory using ModLoader without altering client base game files. Drivable vehicles and functional shooting weapons must be placed here.
* **`additions/`**: Expands the game world with brand-new IDs registered through open.mp `CustomModels.dll` via `artconfig.txt`.
  - Skins: IDs `20000+` (base ID `0-311`)
  - Objects / Props / Weapon Attachments: Negative IDs (`-1001`, `-1002`) with base object ID `19300`. (Note: Vehicle models in additions register as 3D object props, not drivable cars).

---

## 3. CLEO & Script Sync

* **CLEO 4/5**: Place compiled `.cs` bytecode in `cleo/scripts/`.
* **CLEO Redux**: Place `.js` scripts in `cleo/scripts/`.
* **Text Tables**: Place `.fxt` files in `cleo/text/`.
* **Configurations**: Place `.ini` files in `cleo/config/`.
* **Audio FX**: Place `.wav` or `.mp3` files in `cleo/audio/` (staged into `cleo/servers/<server_id>/audio/` and `cleo/cleo_audio/` on the client).

The client launcher automatically isolates all server modifications into `modloader/servers/<server_id>/` and `cleo/servers/<server_id>/`, and cleanly restores player files on session disconnect.

---

## 4. How to Publish Asset Changes

1. Copy your `.dff`, `.txd`, `.col`, `.js`, `.cs`, or audio files into the subdirectories above.
2. Run `index_mods.bat` from the server root directory (or invoke `ModSyncServer.exe --reindex`).
3. If the server is currently running, perform a live zero-downtime refresh:
   ```cmd
   curl -X POST http://localhost:8080/api/refresh
   ```
4. Connected clients will download newly added or modified assets on their next connection.
