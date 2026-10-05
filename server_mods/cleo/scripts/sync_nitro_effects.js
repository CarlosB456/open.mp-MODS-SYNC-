/// <reference path=".config/sa.d.ts" />
// Synchronized Vehicle Nitrous & Visual FX

wait(1000);

if (typeof globalThis.ModSync !== 'undefined' && globalThis.ModSync) {
    globalThis.ModSync.on("SERVER_NITRO_TRIGGER", (data, senderPlayerId) => {
        try {
            const vehicleId = data.vehicleId;
            showTextBox("ModSync: Synchronized Nitrous Activated!");
        } catch (e) {}
    });
}

while (true) {
    wait(1000);
}
