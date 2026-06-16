# BC_archipelago — Bomber Crew Archipelago Randomizer Client

A BepInEx 5 plugin that adds [Archipelago](https://archipelago.gg/) randomizer support to **Bomber Crew**.

This mod connects your game to an Archipelago multi-world room, receives items from other players, sends checked locations back to the server, and supports DeathLink.

> **Status:** Early development / template-based. Placeholder values (plugin GUID, game name, version) still need to be finalized before a release build.

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

1. Build the project (see below)
2. Copy the BC_archipelago.dll and Archipelago.MultiClient.Net.dll from My Game\BepInEx\plugins\BC_archipelago into:

   ```
   Bomber Crew/BepInEx/plugins
   ```

3. Launch Bomber Crew. A small mod label should appear in the top-left corner of the screen.
4. Use the on-screen host / slot / password fields to connect to an Archipelago room.

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

# Debug build
dotnet build

# Release build (produces bin\Release\BC_archipelago-<Version>.zip)
dotnet build -c Release
```

> **Note:** The Debug configuration writes output to `C:\My Game\BepInEx\plugins\BC_archipelago` by default. Create that directory or change the path in `ArchipelagoBepin5Template.csproj` if it does not exist.

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
