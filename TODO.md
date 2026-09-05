# TODO — where we left off

Status snapshot after the session that reworked bomber upgrades into an unlock-based system,
added the 3-color shop indicator, removed default-baseline Mk1 items, and added a Disconnect
button + item-received toast. Read this before picking work back up.

## What's done and verified in-game

- **Connection**: mod connects to a local Archipelago server, auth works, DeathLink
  send/receive works (with echo-loop guard). A **Disconnect** button now appears next to the
  connection status once connected (`Plugin.cs OnGUI()`, calls the now-public
  `ArchipelagoClient.Disconnect()`).
- **Item-received feedback**: every item applied via `ItemRewarder.Apply()` now also pops a
  fading toast (`Utils/ItemNotifications.cs`) top-right of the screen - solid dark background,
  bold white text with a black outline for legibility over any scene, capped at 5 stacked
  entries (oldest dropped first) so a burst of received items can never overflow past the
  bottom of the screen. Independent of the scrolling `ArchipelagoConsole` debug log.
- **`LocationTable.cs`**:
  - `MissionCompletionByName` — 44 real campaign missions (main campaign 7 chapters +
    training + final, plus DLC1), fires via `MissionHooks` on `MissionFinishCriteria.EndMission`.
  - `ShopPurchaseByKey` — 280 locations (226 bomber upgrade + 54 crew equipment; cosmetic
    Livery is excluded entirely), one per (bomber upgrade slot × compatible catalogue upgrade)
    and one per crew equipment item, keyed `"slotId:upgradeName"` / `"gearType:equipmentName"`.
    Fires via `BomberCrew/ShopHooks.cs` on a real in-game purchase *attempt* — the check always
    sends the first time regardless of whether the purchase is actually allowed to go through.
  - **Total: 325 locations** (45 mission + 280 shop).
- **`ItemTable.cs`**: 104 items — 23 `BomberUpgradeProgressive` + 54 CrewEquipment + 27 utility
  items (Funds/Intel tiers, per-skill CrewSkillXp, per-chapter MissionUnlock "clearance",
  InstantRepair, InstantHeal).
  - **Progressive bomber upgrades are now unlock-based, not force-installed.** Each upgrade
    *line* (parallel variants like Standard/Armoured/Light engines are separate lines, see
    `BomberCrew/ItemRewarder.cs` `ProgressiveLines`) is one item; receiving a copy only unlocks
    that line's next tier (`ArchipelagoData.ProgressiveUpgradeCounts`, now semantically "highest
    tier unlocked") - it does **not** install anything by itself anymore. The player then buys
    an unlocked tier through the normal vanilla shop flow (`ShopHooks.AttemptPurchasePrefix`
    returns `true` to let the real purchase run - real funds/weight checks, single-slot
    install) any time, on any slot, any number of times, including switching back to a lower
    tier or a different parallel line. This replaced the old "receive = force-install
    fleet-wide" design, which had no way to revert to a previously-unlocked tier/line.
  - **Default-baseline Mk1 items removed from the pool**: lines whose Mk1 tier is exactly what
    the bomber starts with by default (EngineStandard, GunTurret303x2, Electrical, Hydraulic,
    Radar, EquipmentRack, OxygenTank, FuelTank — see `tools/gen_item_table.py`'s
    `DEFAULT_UNLOCKED_LINES`, derived from `SLOTS`' default column) have one fewer copy in the
    pool than they have tiers, since the player already owns that first tier. Those lines start
    already unlocked at tier 1 (`ItemRewarder.DefaultUnlockedLines`) so the shop shows them as
    equipable (green) from the very start of a new game.
  - **Cosmetic Livery skins remain excluded entirely** (no items, no locations) - purely
    cosmetic, no gameplay effect.
  - **Shop UI now shows 3 states per bomber-upgrade row** (`ShopHooks.SetUpGraphicsPostfix`):
    white = never attempted, orange = attempted (checked) but not yet unlocked, green =
    unlocked/equipable right now. Crew equipment rows only ever show white/orange (no
    unlock-gated install step exists for crew - buying always reverts, only receiving equips).
- **`ItemRewarder.cs`**: all 8 categories implemented against real game APIs. Verified live.
- **DeathLink**: `DeathLinkHandler.KillPlayer()` wired into `Plugin.Update()`, guarded against
  re-broadcasting what it just received.
- Generators for both tables live in `tools/` (`gen_item_table.py`, `gen_location_table.py`,
  `gen_apworld_data.py`) — re-run these (in that order, then re-zip/reinstall the `.apworld`
  and regenerate a seed) any time either C# table changes.

## What's NOT done yet

### 1. Known bug — FIXED this session

- `ApplyCrewEquipment`/`ApplyCrewSkillXp` used to throw a caught `NullReferenceException` for
  some received items (seen for `HeadgearCapService`/`BootsElectricallyHeated`, and separately
  for CrewSkillXp items) - didn't crash (caught in `ItemRewarder.Apply`'s try/catch) but the
  item silently failed to apply. Root-caused by decompiling `CrewContainer`/`Crewman` via
  `ilspycmd` (offline, no live repro needed): `CrewContainer.GetCurrentCrewCount()` does
  `Singleton<SaveDataContainer>.Instance.Get().GetActiveCrewmen().Count` - if no save is loaded
  yet (`Get()` returns null), this throws immediately. `GameState.CanApplyItemsSafely`
  (`BomberCrew/GameState.cs`) gated on mission/loading/bomber-in-scene state but never checked
  whether a save was actually loaded, so items received right after connecting (e.g. at the
  main menu, before loading a save) got applied too early and hit this. Fixed by adding a
  `SaveDataContainer.Instance?.Get() == null` check to `CanApplyItemsSafely`. Also fixed
  `GameState.GetAliveCrewmen()`'s try/catch, which wrapped only the call to a local iterator
  method (a no-op guard - the method body only runs lazily per `foreach` step in the *caller*,
  long after the try/catch returned) - it now guards each per-crewman step individually instead,
  as defense-in-depth for the `InstantHeal`/`InstantRepair` immediate-queue path which bypasses
  `CanApplyItemsSafely` on purpose (repairs are meant to work mid-mission).

### 2. Persistence / seed validation — DONE, needs live verification

- `ArchipelagoData` (`CheckedLocations`/`ProgressiveUpgradeCounts`) is now tied to the game's
  own save slots instead of living only in memory. Implementation:
  - `BomberCrew/SavePersistenceHooks.cs` — Harmony postfixes on `SaveDataContainer.Load(int)`
    and `SaveDataContainer.Save()` (found via `ilspycmd`-decompiling `Assembly-CSharp.dll`
    offline - `SaveDataContainer` is a thin JSON read/write wrapper over `Application.
    persistentDataPath + GameMode.GetSaveDataPrefix() + slotIndex + ".dat"`, so a companion
    file next to it was the natural approach rather than editing the real save file).
  - `Archipelago/ArchipelagoPersistence.cs` — writes/reads a companion file
    `<same prefix+slot>.archipelago.json` (full `ArchipelagoData.ToString()` JSON) alongside
    each save slot. `OnSaveWritten` only writes while actually connected (so saving while
    briefly disconnected can't clobber previously-persisted progress with nothing).
    `OnSaveLoaded` restores `CheckedLocations`/`ProgressiveUpgradeCounts` from that slot's
    companion file (or clears them if there isn't one), and records the seed it was tied to
    (`ArchipelagoData.SaveFileSeed`).
  - Seed validation (`ArchipelagoPersistence.ValidateAgainstCurrentRoom`, called both right
    after a save loads and right after a successful connect, since either can happen first):
    if the save's recorded seed doesn't match the room actually connected to,
    `CheckedLocations`/`ProgressiveUpgradeCounts` are reset instead of trusting progress from a
    different multiworld. `ArchipelagoData.Seed` is a public getter added over the existing
    private `seed` field (now `[JsonProperty]`-forced into serialization, since Newtonsoft only
    serializes public members by default and this field previously silently never round-tripped
    at all).
  - **Verified live**: companion file (`BC_SaveSlotV2_3.archipelago.json` next to the real save,
    under `%userprofile%\AppData\LocalLow\Runner Duck\Bomber Crew\`) is created on save with the
    correct `CheckedLocations`/`ProgressiveUpgradeCounts`/seed; reload + reconnect on the same
    seed restores progress with no errors and no seed-mismatch warning (as expected for a
    matching seed). Not yet exercised: an actual seed *mismatch* (loading a save tied to a
    different room) - the reset-on-mismatch path is implemented but unconfirmed live.

### 3. apworld polish (not blocking)

- **Player-facing setup doc — DONE**: `README.md` now has a "Generating and Hosting Your Own
  Seed" section (build `.apworld` from source, install into `custom_worlds/`, YAML template,
  generate, host, connect). `apworld_src/bomber_crew.yaml` is a ready-to-copy player YAML
  template committed to the repo (kept as a sibling of `bomber_crew/`, not inside it, so it
  doesn't end up bundled into the `.apworld` zip).
- **DLC1-inclusion toggle — DONE**: `apworld_src/bomber_crew/__init__.py` now has an
  `options_dataclass` (`BomberCrewOptions`) with `include_dlc1` (`DefaultOnToggle`, on by
  default to match prior behavior). When off, the 7 `DLCMP01_*` mission locations and the
  single DLC1-gated item ("DLC1 Chapter Clearance") are excluded from `create_regions()`/
  `create_items()` - `location_name_to_id`/`item_name_to_id` stay the full static per-AP-
  convention id tables regardless of the option; only the per-seed subset changes. Verified via
  a real 2-player local generation (one with the option on, one off) and inspecting the spoiler
  log: DLC1 locations/item only appear for the player with it enabled. Caught and fixed one bug
  along the way: `_is_dlc1_item(payload)` crashed on items with no payload (e.g. InstantRepair/
  InstantHeal) until guarded against `None`.
- **Livery toggle — explicitly dropped, do not revisit without being asked**: cosmetic Livery
  skins were deliberately removed entirely from `ItemTable.cs`/`LocationTable.cs` earlier this
  project ("Enleve les Livery des checks & items possible"), and adding an opt-in toggle back
  was proposed and then explicitly declined ("Laisse tomber"). Livery stays untracked by AP.
- Itempool/classification/goal choices are v1 opinions, not load-bearing — revisit if
  playtesting says otherwise.

### 4. Not yet real items (lower priority, deprioritized on purpose)

- DLC-gated content (Missiles upgrade type, FuselageBombBayDoors upgrades) has zero catalogue
  items in this install and was skipped entirely. Revisit if DLC2 gets enabled — rerun
  `tools/gen_item_table.py` and `tools/gen_location_table.py`.
- Secondary mission objectives (no casualties, photos, etc.) were explicitly deprioritized in
  favor of the shop-purchase-as-location approach. Could still be added later as a separate
  expansion.

## How to pick this back up

Dev loop to regenerate/reinstall/test the world after changing `ItemTable.cs`/`LocationTable.cs`
or `apworld_src/bomber_crew/__init__.py`:

1. `python tools/gen_apworld_data.py` from the repo root (regenerates
   `apworld_src/bomber_crew/world_data.py`).
2. Zip `apworld_src/bomber_crew/` (the folder itself, so the zip's top level is
   `bomber_crew/...`) and save it as `bomber_crew.apworld` in
   `C:\ProgramData\Archipelago\custom_worlds\` (replacing the old one).
3. `ArchipelagoGenerate.exe --player_files_path Players` from the Archipelago install dir
   (there's already a `Players/bomber_crew.yaml`) to produce a new seed zip in `output/`.
4. `ArchipelagoServer.exe <seed.zip>` to host it (localhost, no password by default).
5. Copy the built `BC_archipelago.dll` (from `bin/Debug/BC_archipelago/`) into
   `C:\Program Files (x86)\Steam\steamapps\common\BomberCrew\BepInEx\plugins\BC_archipelago\`
   (stop `BomberCrew.exe` first if it's running — the DLL is locked while it's loaded).
6. Launch the game, connect (`Player1`, `localhost:38281` by default), test live. For deep
   game-API questions, `ilspycmd` (installed as a dotnet global tool) can decompile
   `BomberCrew_Data/Managed/Assembly-CSharp.dll` offline — `ilspycmd -l c` lists all classes,
   `ilspycmd -t <TypeName>` decompiles one type — often faster than live UnityExplorer probing
   for questions about what a game API does or whether a given system exists.
