# ModSync Server - Guia para Duenos de Servidores

Version: **0.3.8-A-R1**  
Creditos: **eLdarqO**

Este sistema permite implementar mods y scripts CLEO simplemente **copiando y pegando** los archivos en las carpetas organizadas.

---

## 1. Estructura de Carpetas (Copiar y Pegar)

Coloca tus mods en la carpeta correspondiente dentro de `server_mods/`:

```
server_mods/
├── server_info.json         # Datos del servidor: nombre, ID unico, version
├── vehicles/
│   ├── replacements/        # Reemplazar vehiculos originales (ej: copcarla.dff, copcarla.txd)
│   └── additions/           # AGREGAR vehiculos nuevos sin tocar los originales (ej: patrol_suv.dff)
├── weapons/
│   ├── replacements/        # Reemplazar armas originales (ej: m4.dff, m4.txd)
│   └── additions/           # AGREGAR armas nuevas sin alterar las originales (ej: famae_saf.dff)
├── skins/
│   ├── replacements/        # Reemplazar skins de personajes (ej: swat.dff, swat.txd)
│   └── additions/           # AGREGAR skins nuevos con IDs unicos (20001+) (ej: swat1.dff)
├── objects/
│   ├── replacements/        # Reemplazar objetos de mapa
│   └── additions/           # AGREGAR objetos nuevos (ej: roadblock.dff)
├── cleo/
│   ├── scripts/             # Scripts CLEO (.cs para CLEO clasico, .js para CLEO Redux)
│   ├── plugins/             # Plugins CLEO (.cleo)
│   ├── text/                # Tablas de texto (.fxt)
│   ├── config/              # Archivos de configuracion (.ini)
│   └── audio/               # Sonidos y efectos para scripts (.wav, .mp3)
├── audio/                   # Efectos de sonido y sirenas (.wav, .mp3)
├── textures/                # Diccionarios de texturas (.txd)
├── plugins/                 # Plugins ASI de cliente (.asi)
└── packs/                   # Paquetes completos de mods
```

---

## 2. Expansion de Contenido vs Reemplazo

* **Carpeta `replacements/`**: El mod reemplaza visualmente el modelo existente sin tocar los archivos de instalacion del juego (ModLoader lo hace dinamicamente en memoria).
* **Carpeta `additions/`**: Agrega contenido expandido completamente nuevo. El indexador le asigna automaticamente un ID unico compatible con la pipeline de open.mp (`AddCharModel` para skins, `AddSimpleModel` con base ID segura 19300 para objetos, armas y vehiculos adjuntos) sin alterar ningun archivo original de GTA San Andreas.

---

## 3. Soporte Universal de CLEO

Compatible con:
- CLEO Clasico: Scripts compilados `.cs` ejecutados por CLEO 5.
- CLEO Redux: Scripts `.js` / `.ts`.
- Plugins: Bibliotecas `.cleo` en `cleo/plugins/`.
- Tablas de texto: `.fxt` en `cleo/text/`.
- Configuracion: `.ini` en `cleo/config/`.
- Efectos de sonido: `.wav` y `.mp3` en `cleo/audio/` (montados en `cleo/cleo_audio/`).

Ademas, el cliente realiza **backup automatico** de los archivos personales del jugador antes de montar cualquier sesion de servidor, garantizando que jamas se sobreescriba ni se borre ningun archivo personal al salir o rotar de servidor.

---

## 4. Como Publicar los Cambios

Despues de pegar tus archivos:
1. Haz doble clic en `server/update_mods.bat` (o el servidor CDN lo hara automaticamente al iniciar o al consultar `GET /api/refresh`).
2. El sistema calculara los hashes SHA-256, generara el `manifest.json` y el `artconfig.txt` para open.mp.
3. Los jugadores descargaran los archivos organizados por el identificador de tu servidor (`server_id`).
4. Para volver al GTA original sin mods del servidor, el usuario puede ejecutar el launcher con `--deactivate`.
