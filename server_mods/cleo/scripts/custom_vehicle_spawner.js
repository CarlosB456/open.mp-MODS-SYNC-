/// <reference path=".config/sa.d.ts" />
// ModSync Chilean Vehicle & Weapon Spawner for open.mp
// Safe trigger: F10 key (VK_F10 = 121) to completely eliminate conflict with SA-MP keys/movement.

wait(3000);
try {
    showTextBox("ModSync Chile: Presiona F10 para alternar vehiculos y armamento chileno");
} catch (e) {}

let currentPreset = 0;
const PRESETS = [
    { vehId: 585, vehName: "Hyundai Accent GLS 1997 Taxi Chileno", weaponId: 5, weaponName: "Bandera Chilena (Arma)", ammo: 1 },
    { vehId: 437, vehName: "Marcopolo Paradiso 1800 DD G8 Andimar Bus", weaponId: 31, weaponName: "M4 Carabineros", ammo: 500 },
    { vehId: 587, vehName: "Suzuki Spresso WIP", weaponId: 24, weaponName: "Deagle 9mm", ammo: 250 },
    { vehId: 596, vehName: "Carabineros Radio Patrulla SUV", weaponId: 34, weaponName: "Sniper Carabineros", ammo: 100 }
];

while (true) {
    wait(50);
    try {
        const isF10 = Pad.IsKeyPressed(121); // F10 key

        if (isF10) {
            const player = new Player(0);
            const char = player.getChar();
            if (char) {
                const preset = PRESETS[currentPreset];
                currentPreset = (currentPreset + 1) % PRESETS.length;

                const pos = char.getCoordinates();
                const heading = char.getHeading();

                const rad = (heading + 90.0) * (Math.PI / 180.0);
                const spawnX = pos.x + Math.cos(rad) * 3.5;
                const spawnY = pos.y + Math.sin(rad) * 3.5;
                const spawnZ = pos.z + 0.5;

                Streaming.RequestModel(preset.vehId);
                Streaming.LoadAllModelsNow();

                const car = Car.Create(preset.vehId, spawnX, spawnY, spawnZ);
                if (car) {
                    car.setHeading(heading);
                    char.warpIntoCar(car);
                }

                // Equip Chilean weapon
                char.giveWeapon(preset.weaponId, preset.ammo);

                showTextBox("ModSync Chile [" + (currentPreset === 0 ? PRESETS.length : currentPreset) + "/4]: " + preset.vehName + " | " + preset.weaponName);

                // Debounce key until released
                while (Pad.IsKeyPressed(121)) {
                    wait(100);
                }
            }
        }
    } catch (err) {}
}
