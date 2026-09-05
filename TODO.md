# TODO — where we left off

Status snapshot as of the session that built and verified the real `.apworld`
(`apworld_src/bomber_crew/`). Read this before picking work back up.

## What's done and verified in-game

- **Connection**: mod connects to a local Archipelago server, auth works, DeathLink
  send/receive works (with echo-loop guard).
- **`LocationTable.cs`**:
  - `MissionCompletionByName` — 44 real campaign missions (main campaign 7 chapters +
    training + final, plus DLC1), fires via `MissionHooks` on `MissionFinishCriteria.EndMission`.
  - `ShopPurchaseByKey` — 280 locations (226 bomber upgrade + 54 crew equipment; cosmetic
    Livery is excluded entirely, see below), one per (bomber upgrade slot × compatible
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
  - **Total: 325 locations** (45 mission + 280 shop).
- **`ItemTable.cs`**: 104 items — 23 `BomberUpgradeProgressive` (fleet-wide, tiered) + 54
  CrewEquipment + 27 utility items (Funds/Intel tiers, per-skill CrewSkillXp, per-chapter
  MissionUnlock "clearance", InstantRepair, InstantHeal).
  - **Progressive bomber upgrades**: non-cosmetic upgrades (engines, turrets, fuselage,
    electrical/hydraulic/radar/etc.) are no longer individual per-slot-per-mark items.
    Each upgrade *line* (parallel variants like Standard/Armoured/Light engines are
    separate lines, never merged - see `BomberCrew/ItemRewarder.cs` `ProgressiveLines`)
    is ONE item; receiving another copy bumps that line up one tier, applied fleet-wide
    to every slot of the matching type at once (e.g. one "Progressive EngineStandard"
    upgrades all 4 engines together). A persistent per-line counter
    (`ArchipelagoData.ProgressiveUpgradeCounts`) tracks tiers received, since save data
    only records what's currently equipped. The shop-purchase locations
    (`ShopPurchaseByKey`) are UNCHANGED in spirit - still one location per specific (slot,
    exact mark) combo; only how you *receive* upgrades as items changed, not how buying
    them sends a check. Verified live: two copies of "Progressive EngineStandard"
    correctly installed Mk1 then Mk2 on all 4 engines, no crash.
  - **Cosmetic Livery skins removed entirely** (no items, no locations) - purely cosmetic,
    no gameplay effect, so not worth randomizing. Removed from both generators
    (`tools/gen_item_table.py`, `tools/gen_location_table.py`) and regenerated; the vanilla
    shop/game still has the Livery category as normal, it's just untracked by AP now.
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
- `__init__.py`: single flat "Menu" region containing all 325 real locations, no item-gated
  access rules (deliberate — see the comment there and the reasoning that was previously
  here: the real game enforces chapter/economy gating independently of AP already).
  Itempool = 104 unique item names, with `BomberUpgradeProgressive` items placed as one
  copy per tier (payload `"{lineId}:{tierCount}"` tells `create_items()` how many - see
  `apworld_src/bomber_crew/__init__.py`), padded with filler copies up to 325.
  Classification: the 17 utility items (Funds/Intel/CrewSkillXp/InstantRepair/InstantHeal)
  are `filler`; everything else (BomberUpgradeProgressive, CrewEquipment, MissionUnlock) is
  `useful`.
- **Goal**: a dedicated `address=None` "Victory" event location gated on
  `state.can_reach_location("C08_KEY", player)`. Do NOT lock the Victory event directly onto
  the real `C08_KEY` location (i.e. `get_location("C08_KEY").place_locked_item(...)`) — that
  crashes this Archipelago version's server on load
  (`_speedups.LocationStore.__init__: TypeError: an integer is required`), reproduced and
  confirmed during development. Keep the goal as a separate event location instead.
- Verified end-to-end against a live game + local `ArchipelagoServer.exe`: generated a real
  seed, connected, bought `GunTurret303x2Mk2` in `gun_turret_rear` (a real shop-purchase
  location), server placed `PigeonMk3` there and sent it back, client received it and
  installed it on `survival_pigeon`. Full loop works. (Item/location counts above are from
  a later pass that removed cosmetic Livery entirely and made bomber upgrades progressive -
  re-verify counts against `Archipelago/ItemTable.cs`/`LocationTable.cs` if this drifts.)

Remaining polish, not blocking:

- No player-facing setup docs/YAML template committed yet for others to generate their own
  seed (the dev flow so far is manual: `python tools/gen_apworld_data.py`, zip
  `apworld_src/bomber_crew/` as `.apworld`, install into `custom_worlds/`, generate).
- No options at all yet (no `options_dataclass`) — every game is identical. Could add e.g.
  a "include cosmetic Livery items" toggle, or DLC1-inclusion toggle, later.
- Itempool/classification/goal choices above are v1 opinions, not load-bearing — revisit
  if playtesting says otherwise.

### 2. Two real bugs found and fixed this session

- **Thread-safety crash (serious, fixed)**: `ItemRewarder.Reward()` used to call `Apply()`
  directly for some categories. Received items arrive on the Archipelago client's own
  network thread (`OnItemReceived`, invoked from the websocket's receive callback), and
  `Apply()` touches Unity/game APIs - calling those off the main thread caused a real
  native access violation crash (reproduced twice, confirmed via `crash.dmp`/`error.log`
  next to `BomberCrew.exe`: `ItemRewarder.Apply` called straight from the websocket message
  thread). Fixed: `Reward()` now only ever enqueues (into `pendingItems` or a new
  `instantItems` queue for InstantRepair), and `Apply()` only ever runs from `Update()`
  (main thread), with a `lock` around the queues for thread safety. Verified: no crash
  under a heavy burst of real received items after the fix.
- **Bomber-upgrade weight lockout (serious, fixed)**: the vanilla shop's
  `BomberUpgradeScreenController.AttemptPurchase()` gates on a weight budget (heavy
  equipment needs enough installed engines to carry it) computed from the bomber's
  *current* state. Since items received via Archipelago (especially progressive upgrades)
  install directly and bypass that check, the bomber could end up in an overweight state
  that then made `AttemptPurchase()` silently fail for ANY slot, permanently blocking the
  shop-purchase-as-location mechanism. Fixed: `ShopHooks.AttemptPurchasePrefix` now
  replaces the bomber-upgrade purchase method entirely (`return false`, skips the
  original) instead of letting it run and reverting after - the check always sends
  regardless of funds/weight, nothing is ever actually installed so there's nothing to
  revert. Crew equipment purchases are unaffected (still revert-after-the-fact) since
  there's no weight system on that side. Verified: bomber-upgrade purchases still send
  their check correctly even with several heavy progressive upgrades already installed.

### 3. Known bug, not yet fixed

- `ApplyCrewEquipment` throws a caught `NullReferenceException` for some received
  CrewEquipment items (seen for `HeadgearCapService` and `BootsElectricallyHeated` in
  testing) - doesn't crash (caught in `ItemRewarder.Apply`'s try/catch) but the item
  silently fails to equip. Not yet root-caused; suspect `GameState.GetAliveCrewmen()`
  or the crewman/avatar pairing being in a stale state at the moment of application.
  Needs investigation.

### 4. Not yet real items (lower priority, noted here so it isn't forgotten)

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
