#if defined _inc_modsync
	#endinput
#endif
#define _inc_modsync

#include <a_samp>

#define MAX_MODS 512
#define MAX_MOD_NAME 64
#define MAX_MOD_ID 32

enum E_MOD_TYPE {
    MOD_TYPE_UNKNOWN = 0,
    MOD_TYPE_SKIN,
    MOD_TYPE_VEHICLE,
    MOD_TYPE_WEAPON,
    MOD_TYPE_OBJECT,
    MOD_TYPE_MAP,
    MOD_TYPE_AUDIO,
    MOD_TYPE_SCRIPT,
    MOD_TYPE_TEXTURE
}

enum E_MOD_DATA {
    ModId[MAX_MOD_ID],
    ModName[MAX_MOD_NAME],
    E_MOD_TYPE:ModType,
    BaseModelId,
    NewModelId,
    VirtualWorld,
    bool:ModRequired,
    DffName[64],
    TxdName[64]
}

static ModList[MAX_MODS][E_MOD_DATA];
static TotalMods = 0;
static bool:PlayerHasMods[MAX_PLAYERS];

// Callbacks
forward OnModSyncInitialized(total_mods);
forward OnPlayerModSyncReady(playerid);
forward OnPlayerModSyncFailed(playerid);

/**
 * Register a custom skin model
 */
stock ModSync_AddCharModel(baseid, newid, const dff[], const txd[]) {
    if (TotalMods >= MAX_MODS) return 0;
    
    ModList[TotalMods][ModType] = MOD_TYPE_SKIN;
    ModList[TotalMods][BaseModelId] = baseid;
    ModList[TotalMods][NewModelId] = newid;
    format(ModList[TotalMods][DffName], 64, "%s", dff);
    format(ModList[TotalMods][TxdName], 64, "%s", txd);
    TotalMods++;
    return 1;
}

/**
 * Register a custom object model
 */
stock ModSync_AddSimpleModel(virtualworld, baseid, newid, const dff[], const txd[]) {
    if (TotalMods >= MAX_MODS) return 0;
    
    ModList[TotalMods][ModType] = MOD_TYPE_OBJECT;
    ModList[TotalMods][VirtualWorld] = virtualworld;
    ModList[TotalMods][BaseModelId] = baseid;
    ModList[TotalMods][NewModelId] = newid;
    format(ModList[TotalMods][DffName], 64, "%s", dff);
    format(ModList[TotalMods][TxdName], 64, "%s", txd);
    TotalMods++;
    return 1;
}

/**
 * Register a custom vehicle model
 */
stock ModSync_AddVehicleModel(baseid, newid, const dff[], const txd[]) {
    if (TotalMods >= MAX_MODS) return 0;
    
    ModList[TotalMods][ModType] = MOD_TYPE_VEHICLE;
    ModList[TotalMods][BaseModelId] = baseid;
    ModList[TotalMods][NewModelId] = newid;
    format(ModList[TotalMods][DffName], 64, "%s", dff);
    format(ModList[TotalMods][TxdName], 64, "%s", txd);
    TotalMods++;
    return 1;
}

/**
 * Initializes and registers custom models
 */
stock ModSync_Init(const config_path[] = "artconfig.txt") {
    #pragma unused config_path
    printf("[ModSync] Initializing ModSync Core Engine (v1.5.9)...");
    TotalMods = 0;
    
    // Register Chilean custom expansion additions
    ModSync_AddCharModel(285, 20001, "swat_custom.dff", "swat_custom.txd");
    ModSync_AddSimpleModel(-1, 19300, -1001, "chilean_flag.dff", "chilean_flag.txd");
    ModSync_AddSimpleModel(-1, 19300, -1002, "suzuki_spresso.dff", "suzuki_spresso.txd");

    CallLocalFunction("OnModSyncInitialized", "i", TotalMods);
    return 1;
}

stock ModSync_RegisterMods() {
    new registered = 0;
    for (new i = 0; i < TotalMods; i++) {
        if (ModList[i][ModType] == MOD_TYPE_SKIN) {
            AddCharModel(ModList[i][BaseModelId], ModList[i][NewModelId], ModList[i][DffName], ModList[i][TxdName]);
            registered++;
        }
        else if (ModList[i][ModType] == MOD_TYPE_OBJECT) {
            AddSimpleModel(ModList[i][VirtualWorld], ModList[i][BaseModelId], ModList[i][NewModelId], ModList[i][DffName], ModList[i][TxdName]);
            registered++;
        }
        else if (ModList[i][ModType] == MOD_TYPE_VEHICLE) {
            AddSimpleModel(-1, ModList[i][BaseModelId], ModList[i][NewModelId], ModList[i][DffName], ModList[i][TxdName]);
            registered++;
        }
    }
    printf("[ModSync] Successfully registered %d custom assets with open.mp artwork pipeline.", registered);
    return registered;
}

stock ModSync_OnPlayerConnect(playerid) {
    PlayerHasMods[playerid] = false;
    return 1;
}

stock ModSync_OnPlayerFinishedDownloading(playerid) {
    PlayerHasMods[playerid] = true;
    CallLocalFunction("OnPlayerModSyncReady", "i", playerid);
    return 1;
}

stock ModSync_IsPlayerReady(playerid) {
    return PlayerHasMods[playerid];
}
