# TODO — where we left off

Status snapshot as of the session that built and verified the real `.apworld`
(`apworld_src/bomber_crew/`). Read this before picking work back up.

## What's done and verified in-game

- **Connection**: mod connects to a local Archipelago server, auth works, DeathLink
  send/receive works (with echo-loop guard).
- **`LocationTable.cs`**:
  - `MissionCompletionByName` — 44 real campaign missions (main campaign 7 chapters +
    training + final, plus DLC1), fires via `MissionHooks` on `MissionFinishCriteria.EndMission`.
  - `ShopPurchaseByKey` — 343 locations, one per (bomber upgrade slot × compatible
    catalogue upgrade) and one per crew equipment item, keyed `"slotId:upgradeName"` /
    `"gearType:equipmentName"`. Fires via `BomberCrew/ShopHooks.cs` on real in-game
    purchase (`BomberUpgradeScreenController.AttemptPurchase`,
    `CrewQuartersScreenController.PurchaseEquipment[All]`), confirmed by checking the
    purchase actually succeeded (these methods silently no-op if you can't afford it).
  - **Important design point**: buying something in the shop ONLY sends the check — it is
    then immediately reverted (slot/crewman equip, balance, crew gear stock all restored
    to their pre-purchase values in the Harmony postfix). The upgrade/equipment is never
    actually kept from a purchase. The *only* way to really install/equip something is to
    receive it as an Archipelago item (ItemRewarder). This was a deliberate design choice
    (see session transcript) to keep location (check) and item (reward) properly decoupled,
    since vanilla Bomber Crew has no "owned but not equipped" inventory concept to piggyback
    on and buy-without-equipping isn't a thing in the original UI.
  - **Total: 388 locations** (45 mission + 343 shop).
- **`ItemTable.cs`**: 167 items — 23 `BomberUpgradeProgressive` (fleet-wide, tiered) + 63
  cosmetic Livery `BomberUpgrade` (still individual per-slot) + 54 CrewEquipment + 27
  utility items (Funds/Intel tiers, per-skill CrewSkillXp, per-chapter MissionUnlock
  "clearance", InstantRepair, InstantHeal).
  - **Progressive bomber upgrades**: non-cosmetic upgrades (engines, turrets, fuselage,
    electrical/hydraulic/radar/etc.) are no longer individual per-slot-per-mark items.
    Each upgrade *line* (parallel variants like Standard/Armoured/Light engines are
    separate lines, never merged - see `BomberCrew/ItemRewarder.cs` `ProgressiveLines`)
    is ONE item; receiving another copy bumps that line up one tier, applied fleet-wide
    to every slot of the matching type at once (e.g. one "Progressive EngineStandard"
    upgrades all 4 engines together). A persistent per-line counter
    (`ArchipelagoData.ProgressiveUpgradeCounts`) tracks tiers received, since save data
    only records what's currently equipped. The shop-purchase locations
    (`ShopPurchaseByKey`) are UNCHANGED - still one location per specific (slot, exact
    mark) combo; only how you *receive* upgrades as items changed, not how buying them
    sends a check. Cosmetic Livery items are unaffected (no tiering concept for skins).
    Verified live: two copies of "Progressive EngineStandard" correctly installed Mk1
    then Mk2 on all 4 engines, no crash.
- **`ItemRewarder.cs`**: all 8 categories implemented against real game APIs
  (`BomberUpgradeConfig.SetUpgrade`, `Crewman.SetEquippedFor`, `SaveData.SetMissionPlayed`,
  `Repairable.Repair`, `Crewman.MagicallyResurrect`). Verified live.
- **DeathLink**: `DeathLinkHandler.KillPlayer()` wired into `Plugin.Update()`, actually
  kills crew via `CrewmanLifeStatus.InstantKill()`, guarded against re-broadcasting what
  it just received.
- Generators for both tables live in `tools/` (`gen_item_table.py`, `gen_location_table.py`)
  — re-run these if the catalogues change (e.g. a DLC gets enabled) rather than hand-editing.

## What's NOT done yet

### 1. The real `.apworld` — DONE and verified live

`apworld_src/bomber_crew/` is a real, working Archipelago world (no longer the throwaway
10-location filler):

- `world_data.py` is **generated** (`tools/gen_apworld_data.py`, run from repo root) by
  parsing `Archipelago/ItemTable.cs` and `Archipelago/LocationTable.cs` directly — item/
  location names and ids are guaranteed to match the C# tables exactly, never hand-typed.
  Rerun this generator (then re-zip and reinstall the `.apworld`) any time either table
  changes.
- `__init__.py`: single flat "Menu" region containing all 388 real locations, no item-gated
  access rules (deliberate — see the comment there and the reasoning that was previously
  here: the real game enforces chapter/economy gating independently of AP already).
  Itempool = 167 unique item names, with `BomberUpgradeProgressive` items placed as one
  copy per tier (payload `"{lineId}:{tierCount}"` tells `create_items()` how many - see
  `apworld_src/bomber_crew/__init__.py`), so the base pool is ~222 items, then padded with
  filler copies up to 388. Classification: Livery-slot BomberUpgrade items and the 17
  utility items (Funds/Intel/CrewSkillXp/InstantRepair/InstantHeal) are `filler`; everything
  else (BomberUpgradeProgressive, CrewEquipment, MissionUnlock) is `useful`.
- **Goal**: a dedicated `address=None` "Victory" event location gated on
  `state.can_reach_location("C08_KEY", player)`. Do NOT lock the Victory event directly onto
  the real `C08_KEY` location (i.e. `get_location("C08_KEY").place_locked_item(...)`) — that
  crashes this Archipelago version's server on load
  (`_speedups.LocationStore.__init__: TypeError: an integer is required`), reproduced and
  confirmed during development. Keep the goal as a separate event location instead.
- Verified end-to-end against a live game + local `ArchipelagoServer.exe`: generated a real
  seed (370 items / 388 locations), connected, bought `GunTurret303x2Mk2` in
  `gun_turret_rear` (a real shop-purchase location), server placed `PigeonMk3` there and
  sent it back, client received it and installed it on `survival_pigeon`. Full loop works.

Remaining polish, not blocking:

- No player-facing setup docs/YAML template committed yet for others to generate their own
  seed (the dev flow so far is manual: `python tools/gen_apworld_data.py`, zip
  `apworld_src/bomber_crew/` as `.apworld`, install into `custom_worlds/`, generate).
- No options at all yet (no `options_dataclass`) — every game is identical. Could add e.g.
  a "include cosmetic Livery items" toggle, or DLC1-inclusion toggle, later.
- Itempool/classification/goal choices above are v1 opinions, not load-bearing — revisit
  if playtesting says otherwise.

### 2. Not yet real items (lower priority, noted here so it isn't forgotten)

- `MissionUnlock` payload values are chapter *key* missions only — fine as-is per current
  design, no action needed unless design changes.
- DLC-gated content (Missiles upgrade type, FuselageBombBayDoors upgrades) has zero
  catalogue items in this install and was skipped entirely. Revisit if DLC2 gets enabled
  — rerun the dump scripts, `tools/gen_item_table.py` and `tools/gen_location_table.py`.
- Secondary mission objectives (no casualties, photos, etc.) were considered as a way to
  add more locations but explicitly deprioritized in favor of the shop-purchase-as-location
  approach. Could still be added later as a separate expansion.

### 3. Known rough edges

- `ArchipelagoData` is serialized to JSON but never persisted to disk or validated
  against the connected room's seed on load (pre-existing gap, noted in original README).
- No Disconnect button, no item-received UI notification, console hotkey not configurable
  (pre-existing gaps, noted in original README).
- BepInEx.cfg on the dev machine has `Logging.Disk.LogLevels = All` (turned on mid-session
  for debugging) — fine to leave, just don't be surprised by verbose logs.

## How to pick this back up

The big piece (the `.apworld`) is done. Remaining work is the polish items under
"What's NOT done yet" #1, or moving on to #2/#3.

Dev loop to regenerate/reinstall/test the world after changing either C# table or
`apworld_src/bomber_crew/__init__.py`:

1. If `ItemTable.cs`/`LocationTable.cs` changed: `python tools/gen_apworld_data.py` from
   the repo root (regenerates `apworld_src/bomber_crew/world_data.py`).
2. Zip `apworld_src/bomber_crew/` (the folder itself, so the zip's top level is
   `bomber_crew/...`) and save it as `bomber_crew.apworld` in
   `C:\ProgramData\Archipelago\custom_worlds\` (replacing the old one).
3. `ArchipelagoGenerate.exe --player_files_path Players` from the Archipelago install dir
   (there's already a `Players/bomber_crew.yaml`) to produce a new seed zip in `output/`.
4. `ArchipelagoServer.exe <seed.zip>` to host it (localhost, no password by default).
5. Launch the game, connect, test via UnityExplorer's C# console for spot-checks (see this
   session's transcript for example snippets: dumping catalogues/requirements, calling
   `ItemRewarder.Reward` directly, giving funds/intel, etc.).
