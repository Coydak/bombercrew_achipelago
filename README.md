# BC_archipelago — Bomber Crew Archipelago Randomizer Client

A BepInEx 5 plugin that adds [Archipelago](https://archipelago.gg/) randomizer support to **Bomber Crew**.

This mod connects your game to an Archipelago multi-world room, receives items from other players, sends checked locations back to the server, and supports DeathLink.

> **Status:** Working end-to-end - 325 locations (campaign missions + bomber upgrade/crew equipment shop purchases), 104 items, DeathLink support, and a matching `.apworld` to generate real seeds against. See `TODO.md` for remaining polish/known issues.

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

## Generating and Hosting Your Own Seed

The mod above only handles the *client* side (connecting Bomber Crew to a room). To actually
play, you also need a `bomber_crew.apworld` and a generated seed - here's how, assuming you
already have [Archipelago](https://archipelago.gg/) installed.

### 1. Get the `.apworld`

Build it from source (there's no packaged release yet):

```bash
python tools/gen_apworld_data.py
```

Then zip the `apworld_src/bomber_crew/` folder itself (so the zip's top level is
`bomber_crew/...`, not `apworld_src/...`) and rename the result to `bomber_crew.apworld`.

### 2. Install it

Copy `bomber_crew.apworld` into your Archipelago install's `custom_worlds/` folder, e.g.:

```
C:\ProgramData\Archipelago\custom_worlds\
```

### 3. Create a player YAML

Copy `apworld_src/bomber_crew.yaml` (a ready-to-use template) into your Archipelago install's
`Players/` folder and change `name:` to your own slot name:

```yaml
name: Player1
game: Bomber Crew
requires:
  version: 0.6.7
Bomber Crew:
  progression_balancing: 50
  accessibility: items
  include_dlc1: true  # set to false if you don't own the DLC1 campaign
```

`include_dlc1` is the only option so far (defaults to `true`) - turning it off drops the 7
DLC1 mission locations and its chapter-clearance item from the pool.

### 4. Generate a seed

From your Archipelago install folder:

```
ArchipelagoGenerate.exe --player_files_path Players
```

This produces a seed zip in `output/`.

### 5. Host it

```
ArchipelagoServer.exe output\AP_<your seed>.zip
```

Defaults to `localhost:38281`, no password.

### 6. Connect

Launch Bomber Crew with the mod installed (see above) and enter the host/slot/password in the
on-screen fields.

> For a multiplayer room instead of solo testing, put everyone's YAML in the same `Players/`
> folder before generating, and host the resulting seed somewhere reachable by all players.

If you change `Archipelago/ItemTable.cs` or `Archipelago/LocationTable.cs` in the client mod,
re-run `python tools/gen_apworld_data.py` and repeat steps 1-2 before generating a new seed -
see `TODO.md` for the full dev loop.

---

## Connecting to a Room Hosted on archipelago.gg

Bomber Crew runs on an old Unity/Mono runtime that can't complete a modern TLS handshake, so it
**cannot connect directly** to a `wss://`-secured room like the ones archipelago.gg hosts for
you after an upload (self-hosted rooms via `ArchipelagoServer.exe` work fine as-is, since those
are plain unencrypted `ws://`). You'll see `Connection timed out` / `TLS handshake` errors in
the log if you try.

The fix is a tiny local relay (`tools/ap_ws_relay.py`) that does the TLS handshake for the game:
the mod connects to it locally over plain `ws://` like normal, and the relay - a regular Python
process with a real TLS stack - forwards everything to the actual `wss://` room. No router or
firewall changes needed; it only makes outbound connections.

**One-time setup** (needs [Python 3](https://www.python.org/downloads/) installed):

```bash
pip install websockets
```

**Each time you want to connect to an archipelago.gg room:**

1. Double-click `tools/run_ap_relay.bat` (or run
   `python tools/ap_ws_relay.py --remote <address from the site> --local-port 39000` yourself).
2. Enter the room address the site gave you (e.g. `archipelago.gg:12345`) when prompted.
3. Once it prints `Listening on ws://localhost:39000 -> relaying to ...`, leave that window open.
4. In the mod's Host field, enter `localhost:39000` (not the archipelago.gg address) and connect
   as usual.

Closing the relay window disconnects you - it needs to keep running alongside the game.

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
