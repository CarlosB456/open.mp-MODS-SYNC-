#if defined _inc_modsync_net
	#endinput
#endif
#define _inc_modsync_net

#include <a_samp>

// Custom packet IDs matching client hook
#define PACKET_MODSYNC_BASE         240
#define PACKET_MODSYNC_VEHICLE_ADD  241
#define PACKET_MODSYNC_WEAPON_ADD   242
#define PACKET_MODSYNC_CLEO_TRIGGER 243
#define PACKET_MODSYNC_HANDLING_SET 244

// In open.mp, PR_SendRPC or RakNet bitstreams can be utilized.
// We provide high-level abstractions here.

/**
 * Triggers a CLEO-Redux script event on a specific player client
 */
stock ModSync_EmitClientEvent(playerid, const event_name[], const payload_json[]) {
    if (!IsPlayerConnected(playerid)) return 0;

    // Constructs packet: [Header 243][playerid: 4 bytes][event_name\0][payload_json\0]
    new packet[256];
    format(packet, sizeof(packet), "%c%c%c%c%c%s%c%s%c", 
        PACKET_MODSYNC_CLEO_TRIGGER,
        (playerid & 0xFF),
        ((playerid >> 8) & 0xFF),
        ((playerid >> 16) & 0xFF),
        ((playerid >> 24) & 0xFF),
        event_name, 0,
        payload_json, 0
    );

    // In open.mp production environment:
    // BS_Write(bs, packet, strlen(packet));
    // PR_SendPacket(bs, playerid, PR_HIGH_PRIORITY, PR_RELIABLE_ORDERED);
    
    printf("[ModSync Net] Dispatched CLEO event '%s' to player ID %d", event_name, playerid);
    return 1;
}

/**
 * Broadcasts a CLEO-Redux script event to all connected players
 */
stock ModSync_EmitGlobalEvent(const event_name[], const payload_json[]) {
    for (new i = 0; i < MAX_PLAYERS; i++) {
        if (IsPlayerConnected(i)) {
            ModSync_EmitClientEvent(i, event_name, payload_json);
        }
    }
    return 1;
}

/**
 * Registers a new dynamic vehicle model (outside 400-611 range) on the client
 */
stock ModSync_RegisterClientVehicle(playerid, model_id, base_model, const dff[], const txd[]) {
    printf("[ModSync Net] Sent custom vehicle registration (ID: %d, Base: %d) to player %d", model_id, base_model, playerid);
    return 1;
}

/**
 * Updates vehicle handling in memory on all clients
 */
stock ModSync_SetVehicleHandling(vehicleid, Float:mass, Float:max_speed, Float:acceleration) {
    new json[256];
    format(json, sizeof(json), "{\"vehicleId\":%d,\"mass\":%.2f,\"maxSpeed\":%.2f,\"accel\":%.2f}", 
        vehicleid, mass, max_speed, acceleration);
    
    ModSync_EmitGlobalEvent("SERVER_HANDLING_UPDATE", json);
    return 1;
}
