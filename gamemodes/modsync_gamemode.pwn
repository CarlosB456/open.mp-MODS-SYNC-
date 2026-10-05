#include <open.mp>
#include "modsync.inc"
#include "modsync_net.inc"

main() {
    print("==================================================");
    print("  open.mp 1.5.9 - Chilean ModSync Edition");
    print("  Created & Optimized by eLdarqO");
    print("==================================================");
}

public OnGameModeInit() {
    SetGameModeText("open.mp 1.5.9");
    ModSync_Init("artconfig.txt");
    ModSync_RegisterMods();

    // Default class: Carabineros de Chile - G.O.P.E. (Skin 285) in Pershing Square
    AddPlayerClass(285, 1540.0, -1665.0, 13.5, 90.0, WEAPON_BAT, 1, WEAPON_DEAGLE, 1000, WEAPON_M4, 2500);

    // Showcase vehicles parked in front of spawn point (Pershing Square / LSPD)
    // 1. Hyundai Accent GLS Taxi Chileno (Model 585 - Emperor replacement)
    CreateVehicle(585, 1545.0, -1670.0, 13.5, 0.0, 0, 1, -1);
    Create3DTextLabel("VEHICLE 1: Hyundai Accent GLS Taxi Chileno\nModel: Emperor | ID: 585", 0x00FF88AA, 1545.0, -1670.0, 14.8, 40.0, 0, true);

    // 2. Marcopolo Paradiso 1800 DD G8 Andimar Bus (Model 437 - Coach replacement)
    CreateVehicle(437, 1545.0, -1655.0, 13.5, 0.0, 1, 1, -1);
    Create3DTextLabel("VEHICLE 2: Marcopolo Paradiso 1800 DD G8 Andimar\nModel: Coach | ID: 437", 0x33CCFFAA, 1545.0, -1655.0, 15.5, 40.0, 0, true);

    // 3. Suzuki Spresso (Model 587 - Euros replacement)
    CreateVehicle(587, 1545.0, -1680.0, 13.5, 0.0, 3, 3, -1);
    Create3DTextLabel("VEHICLE 3: Suzuki Spresso WIP\nModel: Euros | ID: 587", 0xFFFF00AA, 1545.0, -1680.0, 14.5, 40.0, 0, true);

    // 4. Radio Patrulla Carabineros SUV (Model 596 - Copcarla replacement)
    CreateVehicle(596, 1545.0, -1690.0, 13.5, 0.0, 2, 2, -1);
    Create3DTextLabel("VEHICLE 4: Radio Patrulla Carabineros SUV\nModel: Copcarla | ID: 596", 0xFFAA00AA, 1545.0, -1690.0, 14.5, 40.0, 0, true);

    return 1;
}

stock ModSync_TriggerNitro(vehicleid, sender_playerid) {
    new json[128];
    format(json, sizeof(json), "{\"vehicleId\":%d,\"sender\":%d}", vehicleid, sender_playerid);
    ModSync_EmitGlobalEvent("SERVER_NITRO_TRIGGER", json);
    return 1;
}

public OnPlayerRequestClass(playerid, classid) {
    SetPlayerPos(playerid, 1540.0, -1665.0, 13.5);
    SetPlayerCameraPos(playerid, 1535.0, -1665.0, 14.5);
    SetPlayerCameraLookAt(playerid, 1545.0, -1665.0, 13.5);
    return 1;
}

public OnPlayerConnect(playerid) {
    ModSync_OnPlayerConnect(playerid);
    return 1;
}

public OnPlayerSpawn(playerid) {
    SetPlayerPos(playerid, 1538.0, -1665.0, 13.5);
    SetPlayerFacingAngle(playerid, 90.0);
    SetCameraBehindPlayer(playerid);

    // Equip Carabineros skin
    SetPlayerSkin(playerid, 285);

    // Equip armory including Chilean Flag melee weapon
    ResetPlayerWeapons(playerid);
    GivePlayerWeapon(playerid, WEAPON_BAT, 1);
    GivePlayerWeapon(playerid, WEAPON_DEAGLE, 1000);
    GivePlayerWeapon(playerid, WEAPON_M4, 2500);
    GivePlayerWeapon(playerid, WEAPON_SNIPER, 500);
    SetPlayerArmedWeapon(playerid, WEAPON_BAT);

    SendClientMessage(playerid, 0x00FF88AA, "==================================================");
    SendClientMessage(playerid, 0x00FF88AA, "  open.mp 1.5.9 ModSync - Developer: eLdarqO     ");
    SendClientMessage(playerid, 0xFFFFFFFF, "Synchronized Chilean Modpack active:");
    SendClientMessage(playerid, 0x33CCFFAA, "- Hyundai Accent Taxi (585) | Marcopolo Andimar Bus (437)");
    SendClientMessage(playerid, 0xFFFF00AA, "- Suzuki Spresso (587)      | Carabineros SUV (596)");
    SendClientMessage(playerid, 0x00FF88AA, "- Bandera Chilena (Arma / Bat) | Carabineros G.O.P.E. HD (20001)");
    SendClientMessage(playerid, 0xFFFFFFFF, "Commands: /taxi, /bus, /spresso, /car, /flag, /skin, /nitro, /boost");
    SendClientMessage(playerid, 0x00FF88AA, "==================================================");
    return 1;
}

public OnPlayerFinishedDownloading(playerid, virtualworld) {
    ModSync_OnPlayerFinishedDownloading(playerid);
    SendClientMessage(playerid, 0x00FF88AA, "[ModSync] All server mods and expansion additions synchronized.");
    return 1;
}

public OnPlayerCommandText(playerid, cmdtext[]) {
    new Float:x, Float:y, Float:z, Float:a;
    GetPlayerPos(playerid, x, y, z);
    GetPlayerFacingAngle(playerid, a);

    if (strcmp(cmdtext, "/mods", true) == 0 || strcmp(cmdtext, "/help", true) == 0) {
        SendClientMessage(playerid, 0x00FF88AA, "=== open.mp 1.5.9 ModSync Commands ===");
        SendClientMessage(playerid, 0xFFFFFFFF, "Vehicles: /taxi (585), /bus (437), /spresso (587), /car (596)");
        SendClientMessage(playerid, 0xFFFFFFFF, "Weapons:  /flag or /bandera (Chilean Flag Melee)");
        SendClientMessage(playerid, 0xFFFFFFFF, "Additions: /customskin (20001), /flagobj (-1001), /spressoobj (-1002)");
        SendClientMessage(playerid, 0xFFFFFFFF, "Skins:     /skins, /skin <id>");
        SendClientMessage(playerid, 0xFFFFFFFF, "Other:     /nitro, /boost");
        return 1;
    }

    if (strcmp(cmdtext, "/taxi", true) == 0 || strcmp(cmdtext, "/hyundai", true) == 0) {
        new veh = CreateVehicle(585, x + 2.0, y + 2.0, z + 0.5, a, 0, 1, -1);
        PutPlayerInVehicle(playerid, veh, 0);
        SendClientMessage(playerid, 0x00FF88AA, "[ModSync] Hyundai Accent GLS Taxi spawned.");
        return 1;
    }

    if (strcmp(cmdtext, "/bus", true) == 0 || strcmp(cmdtext, "/andimar", true) == 0) {
        new veh = CreateVehicle(437, x + 3.0, y + 3.0, z + 0.5, a, 1, 1, -1);
        PutPlayerInVehicle(playerid, veh, 0);
        SendClientMessage(playerid, 0x33CCFFAA, "[ModSync] Marcopolo Paradiso 1800 DD G8 Andimar Bus spawned.");
        return 1;
    }

    if (strcmp(cmdtext, "/spresso", true) == 0 || strcmp(cmdtext, "/suzuki", true) == 0) {
        new veh = CreateVehicle(587, x + 2.0, y + 2.0, z + 0.5, a, 3, 3, -1);
        PutPlayerInVehicle(playerid, veh, 0);
        SendClientMessage(playerid, 0xFFFF00AA, "[ModSync] Suzuki Spresso spawned.");
        return 1;
    }

    if (strcmp(cmdtext, "/car", true) == 0 || strcmp(cmdtext, "/carabineros", true) == 0) {
        new veh = CreateVehicle(596, x + 2.0, y + 2.0, z + 0.5, a, 0, 1, -1);
        PutPlayerInVehicle(playerid, veh, 0);
        SendClientMessage(playerid, 0xFFAA00AA, "[ModSync] Radio Patrulla Carabineros SUV spawned.");
        return 1;
    }

    if (strcmp(cmdtext, "/flag", true) == 0 || strcmp(cmdtext, "/bandera", true) == 0) {
        GivePlayerWeapon(playerid, WEAPON_BAT, 1);
        SetPlayerArmedWeapon(playerid, WEAPON_BAT);
        SendClientMessage(playerid, 0x00FF88AA, "[ModSync] Chilean Flag weapon equipped.");
        return 1;
    }

    if (strcmp(cmdtext, "/flagobj", true) == 0 || strcmp(cmdtext, "/banderaobj", true) == 0) {
        CreateObject(-1001, x + 1.5, y + 1.5, z, 0.0, 0.0, 0.0);
        SendClientMessage(playerid, 0x00FF88AA, "[ModSync] Chilean Flag 3D Object Addition (-1001) spawned.");
        return 1;
    }

    if (strcmp(cmdtext, "/spressoobj", true) == 0) {
        CreateObject(-1002, x + 3.0, y + 3.0, z, 0.0, 0.0, 0.0);
        SendClientMessage(playerid, 0xFFFF00AA, "[ModSync] Suzuki Spresso 3D Object Addition (-1002) spawned.");
        return 1;
    }

    if (strcmp(cmdtext, "/customskin", true) == 0) {
        SetPlayerSkin(playerid, 20001);
        SendClientMessage(playerid, 0x00FF88AA, "[ModSync] Carabineros G.O.P.E. Custom Addition Skin (20001) equipped.");
        return 1;
    }

    if (strcmp(cmdtext, "/skins", true) == 0) {
        SendClientMessage(playerid, 0x00FF88AA, "[ModSync] Carabineros Skins: /skin 280 (lapd1), 281 (sfpd1), 282 (lvpd1), 283 (csher), 284 (lapdm1), 285 (swat), 286 (fbi), 287 (army), 288 (dsher)");
        return 1;
    }

    if (strcmp(cmdtext, "/skin ", true, 6) == 0) {
        new skinid = strval(cmdtext[6]);
        if ((skinid >= 280 && skinid <= 288) || skinid == 20001) {
            SetPlayerSkin(playerid, skinid);
            new msg[64];
            format(msg, sizeof(msg), "[ModSync] Skin changed to ID %d.", skinid);
            SendClientMessage(playerid, 0x00FF88AA, msg);
            return 1;
        } else {
            SendClientMessage(playerid, 0xFF0000AA, "[ModSync] Invalid skin ID. Use /skins to see available Carabineros IDs.");
            return 1;
        }
    }

    if (strcmp(cmdtext, "/nitro", true) == 0 || strcmp(cmdtext, "/boost", true) == 0) {
        if (!IsPlayerInAnyVehicle(playerid)) {
            SendClientMessage(playerid, 0xFF0000AA, "[ModSync] You must be inside a vehicle.");
            return 1;
        }
        new veh = GetPlayerVehicleID(playerid);
        AddVehicleComponent(veh, 1010);
        new Float:vx, Float:vy, Float:vz;
        GetVehicleVelocity(veh, vx, vy, vz);
        if (floatabs(vx) < 0.05 && floatabs(vy) < 0.05) {
            new Float:z_angle;
            GetVehicleZAngle(veh, z_angle);
            vx = 0.6 * -floatsin(z_angle, degrees);
            vy = 0.6 * floatcos(z_angle, degrees);
        } else {
            vx *= 1.6;
            vy *= 1.6;
        }
        SetVehicleVelocity(veh, vx, vy, vz + 0.02);
        ModSync_TriggerNitro(veh, playerid);
        SendClientMessage(playerid, 0x00FF00AA, "[ModSync] Nitro / Velocity boost engaged.");
        return 1;
    }

    return 0;
}
