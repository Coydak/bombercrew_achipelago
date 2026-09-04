# BC_archipelago — Bomber Crew Archipelago Randomizer Client

A BepInEx 5 plugin that adds [Archipelago](https://archipelago.gg/) randomizer support to **Bomber Crew**.

This mod connects your game to an Archipelago multi-world room, receives items from other players, sends checked locations back to the server, and supports DeathLink.

> **Status:** Early development. Phase 1 (build, identity, config) and Phase 3 scaffolding (game assembly references, mission/DeathLink hooks, item rewarder) are complete. The remaining work is to fill in the Archipelago item/location tables for the specific Bomber Crew world definition and finish the in-game reward implementations.

---

## Requirements

- **Bomber Crew** (Steam)
- **BepInEx 5** for Unity (x86 or x86_64, matching your game build)
- **Unity Explorer** (optional but highly recommended for development/debugging)
- An Archipelago room to connect to

---

## Installing BepInEx

BepInEx is the plugin loader that runs this mod.

1. Download the latest **BepInEx 5** release for Unity from  
   <https://github.com/BepInEx/BepInEx/releases>
   - Choose the **x64** or **x86** build that matches your game executable.
2. Extract the ZIP into your **Bomber Crew** game folder (the folder containing `BomberCrew.exe`).
3. Run the game once so BepInEx creates its folder structure (`BepInEx/`, `doorstop_config.ini`, etc.).
4. Close the game.

Your folder should now look similar to:

```
Bomber Crew/
├── BepInEx/
│   ├── config/
│   ├── core/
│   ├── patchers/
│   └── plugins/
├── BomberCrew.exe
└── doorstop_config.ini
```

---

## Installing Unity Explorer

Unity Explorer is an in-game inspector that makes it much easier to find components, methods, and scene objects while developing or debugging.

1. Download the latest **Unity Explorer** release for **BepInEx 5** from  
   <https://github.com/sinai-dev/UnityExplorer/releases>
2. Extract the ZIP contents into `Bomber Crew/BepInEx/plugins/`.
3. Launch the game. In the main menu, press **F7** (default) to open the Unity Explorer window.

> If you use a different loader (e.g., MelonLoader), grab the matching Unity Explorer build instead.

---

## Installing This Mod for dev or debugging

1. Build the project (see below).
2. Copy `BC_archipelago.dll`, `Archipelago.MultiClient.Net.dll`, and `websocket-sharp.dll` from the build output into:

   ```
   Bomber Crew/BepInEx/plugins/BC_archipelago/
   ```

3. Launch Bomber Crew. A small mod label should appear in the top-left corner of the screen.
4. Use the on-screen host / slot / password fields to connect to an Archipelago room.
5. Complete a mission to send a location check, or die to broadcast a DeathLink (when enabled).

---

## Installing This Mod for a release

1. Build the project (see below) or download a release ZIP.
2. Copy the contents of the release folder or ZIP into:

   ```
   Bomber Crew/BepInEx/plugins/BC_archipelago/
   ```

3. Launch Bomber Crew. A small mod label should appear in the top-left corner of the screen.
4. Use the on-screen host / slot / password fields to connect to an Archipelago room.

---

## Building from Source

From the repository root:

```bash
# Restore NuGet packages
dotnet restore

# Debug build (outputs to bin\Debug\BC_archipelago by default)
dotnet build

# Debug build that copies directly into your BepInEx plugins folder
dotnet build -p:BepInExPluginsPath="C:\Path\To\Bomber Crew\BepInEx\plugins"

# Release build (produces bin\Release\BC_archipelago-<Version>.zip)
dotnet build -c Release
```

> **Notes:**
> * The Debug output path defaults to `bin\Debug\BC_archipelago`. To copy directly into a BepInEx plugins folder, pass the `BepInExPluginsPath` property as shown above.
> * The Bomber Crew assembly path defaults to the Steam install at `C:\Program Files (x86)\Steam\steamapps\common\BomberCrew\BomberCrew_Data\Managed`. Override it with `-p:BomberCrewManagedPath="..."` if your install is elsewhere.

---

## Configuration & Usage

- Host, slot, and password are entered in-game through the on-screen GUI.
- Connection status is shown next to the input fields.
- Received items are processed automatically by `ArchipelagoClient`.
- DeathLink is disabled by default; enable it through the mod UI or slot data once implemented.

---

## Useful Links

- [BepInEx documentation](https://docs.bepinex.dev/)
- [Unity Explorer repository](https://github.com/sinai-dev/UnityExplorer)
- [Archipelago MultiClient.Net](https://github.com/ArchipelagoMW/Archipelago.MultiClient.Net)
- [Archipelago](https://archipelago.gg/)

---
