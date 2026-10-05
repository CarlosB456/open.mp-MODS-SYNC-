#define FILTERSCRIPT
#pragma dynamic 65536
#include <a_samp>
#include "modsync.inc"
#include "modsync_net.inc"

public OnFilterScriptInit() {
    print("==================================================");
    print("  open.mp Advanced ModSync Engine (v2.1)");
    print("  Multi-Vehicle + Multi-Weapon + CLEO Sync");
    print("==================================================");

    ModSync_Init("artconfig.txt");
    ModSync_RegisterMods();
    return 1;
}

public OnPlayerConnect(playerid) {
    ModSync_OnPlayerConnect(playerid);
    return 1;
}

public OnPlayerSpawn(playerid) {
    SendClientMessage(playerid, 0x00FF00AA, "=== open.mp ModSync Commands ===");
    SendClientMessage(playerid, 0xFFFFFFFF, "Vehicles: /car [cop|infernus|bullet|sultan] or /infernus, /bullet");
    SendClientMessage(playerid, 0xFFFFFFFF, "Weapons:  /gun, /m4, /minigun, /deagle, /sniper");
    SendClientMessage(playerid, 0xFFFFFFFF, "Features: /swat (skin), /nitro (synced fx), /mods (help)");
    return 1;
}

public OnPlayerFinishedDownloading(playerid, virtualworld) {
    ModSync_OnPlayerFinishedDownloading(playerid);
    SendClientMessage(playerid, 0x00FF00AA, "[ModSync] All custom assets, vehicles, weapons, and scripts synchronized.");
    return 1;
}

public OnPlayerCommandText(playerid, cmdtext[]) {
    new Float:x, Float:y, Float:z, Float:a;
    GetPlayerPos(playerid, x, y, z);
    GetPlayerFacingAngle(playerid, a);

    // Command: /mods or /help
    if (strcmp(cmdtext, "/mods", true) == 0 || strcmp(cmdtext, "/help", true) == 0) {
        SendClientMessage(playerid, 0x00FF00AA, "=== open.mp ModSync Test Pack Menu ===");
        SendClientMessage(playerid, 0xFFFFFFFF, "Vehicles: /car (cop), /infernus (supercar), /bullet (gt), /sultan (tuner)");
        SendClientMessage(playerid, 0xFFFFFFFF, "Weapons:  /m4 (assault rifle), /minigun (rotary), /deagle (.50 pistol), /sniper");
        SendClientMessage(playerid, 0xFFFFFFFF, "Skins/FX: /swat (tactical skin), /nitro (nitrous sync)");
        return 1;
    }

    // Vehicles
    if (strcmp(cmdtext, "/car", true) == 0 || strcmp(cmdtext, "/cop", true) == 0) {
        new veh = CreateVehicle(596, x + 2.0, y + 2.0, z + 0.5, a, 0, 1, -1);
        PutPlayerInVehicle(playerid, veh, 0);
        SendClientMessage(playerid, 0x00FF00AA, "[ModSync] Spawned FBI Tactical Interceptor SUV (596).");
        return 1;
    }

    if (strcmp(cmdtext, "/infernus", true) == 0 || strcmp(cmdtext, "/car infernus", true) == 0) {
        new veh = CreateVehicle(411, x + 2.0, y + 2.0, z + 0.5, a, 1, 1, -1);
        PutPlayerInVehicle(playerid, veh, 0);
        SendClientMessage(playerid, 0x00FF00AA, "[ModSync] Spawned Supercar Infernus GT (411).");
        return 1;
    }

    if (strcmp(cmdtext, "/bullet", true) == 0 || strcmp(cmdtext, "/car bullet", true) == 0) {
        new veh = CreateVehicle(541, x + 2.0, y + 2.0, z + 0.5, a, 3, 3, -1);
        PutPlayerInVehicle(playerid, veh, 0);
        SendClientMessage(playerid, 0x00FF00AA, "[ModSync] Spawned Bullet GT Racing Edition (541).");
        return 1;
    }

    if (strcmp(cmdtext, "/sultan", true) == 0 || strcmp(cmdtext, "/car sultan", true) == 0) {
        new veh = CreateVehicle(560, x + 2.0, y + 2.0, z + 0.5, a, 2, 2, -1);
        PutPlayerInVehicle(playerid, veh, 0);
        SendClientMessage(playerid, 0x00FF00AA, "[ModSync] Spawned Sultan Street Tuner (560).");
        return 1;
    }

    // Weapons
    if (strcmp(cmdtext, "/gun", true) == 0) {
        GivePlayerWeapon(playerid, WEAPON:24, 250); // Deagle
        GivePlayerWeapon(playerid, WEAPON:31, 500); // M4
        GivePlayerWeapon(playerid, WEAPON:34, 100); // Sniper
        GivePlayerWeapon(playerid, WEAPON:38, 2000); // Minigun
        SendClientMessage(playerid, 0x00FF00AA, "[ModSync] Equipped full synchronized armory (Deagle, M4, Sniper, Minigun).");
        return 1;
    }

    if (strcmp(cmdtext, "/m4", true) == 0) {
        GivePlayerWeapon(playerid, WEAPON:31, 500);
        SendClientMessage(playerid, 0x00FF00AA, "[ModSync] Equipped Tactical M4 Assault Rifle (Slot 31).");
        return 1;
    }

    if (strcmp(cmdtext, "/minigun", true) == 0) {
        GivePlayerWeapon(playerid, WEAPON:38, 2000);
        SendClientMessage(playerid, 0x00FF00AA, "[ModSync] Equipped Heavy Vulcan Rotary Minigun (Slot 38).");
        return 1;
    }

    if (strcmp(cmdtext, "/deagle", true) == 0) {
        GivePlayerWeapon(playerid, WEAPON:24, 250);
        SendClientMessage(playerid, 0x00FF00AA, "[ModSync] Equipped Desert Eagle .50 AE Pistol (Slot 24).");
        return 1;
    }

    if (strcmp(cmdtext, "/sniper", true) == 0) {
        GivePlayerWeapon(playerid, WEAPON:34, 100);
        SendClientMessage(playerid, 0x00FF00AA, "[ModSync] Equipped Tactical Marksman Sniper Rifle (Slot 34).");
        return 1;
    }

    // Skin
    if (strcmp(cmdtext, "/swat", true) == 0) {
        SetPlayerSkin(playerid, 285);
        SendClientMessage(playerid, 0x00FF00AA, "[ModSync] Equipped HD SWAT Operator skin (Model 285).");
        return 1;
    }

    // Nitrous
    if (strcmp(cmdtext, "/nitro", true) == 0) {
        if (!IsPlayerInAnyVehicle(playerid)) {
            SendClientMessage(playerid, 0xFF0000AA, "You must be inside a vehicle to use nitro.");
            return 1;
        }

        new vehicleid = GetPlayerVehicleID(playerid);
        new json[128];
        format(json, sizeof(json), "{\"vehicleId\":%d,\"duration\":4000,\"intensity\":1.8}", vehicleid);

        ModSync_EmitGlobalEvent("SERVER_NITRO_TRIGGER", json);
        SendClientMessage(playerid, 0x00FF00AA, "[ModSync] Nitrous effects synchronized with all nearby players.");
        return 1;
    }

    return 0;
}
