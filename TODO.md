# TODO — where we left off

Status snapshot as of the session that built the shop-purchase-as-location system
(commit `daad444`). Read this before picking work back up.

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
  - **Total: 387 locations.**
- **`ItemTable.cs`**: 370 items — 289 BomberUpgrade + 54 CrewEquipment (same catalogue
  as the shop locations, but received via AP instead of bought) + 27 utility items
  (Funds/Intel tiers, per-skill CrewSkillXp, per-chapter MissionUnlock "clearance",
  InstantRepair, InstantHeal).
- **`ItemRewarder.cs`**: all 8 categories implemented against real game APIs
  (`BomberUpgradeConfig.SetUpgrade`, `Crewman.SetEquippedFor`, `SaveData.SetMissionPlayed`,
  `Repairable.Repair`, `Crewman.MagicallyResurrect`). Verified live.
- **DeathLink**: `DeathLinkHandler.KillPlayer()` wired into `Plugin.Update()`, actually
  kills crew via `CrewmanLifeStatus.InstantKill()`, guarded against re-broadcasting what
  it just received.
- Generators for both tables live in `tools/` (`gen_item_table.py`, `gen_location_table.py`)
  — re-run these if the catalogues change (e.g. a DLC gets enabled) rather than hand-editing.

## What's NOT done yet

### 1. The actual `.apworld` (biggest remaining piece)

No real Archipelago world exists yet — only the throwaway 10-location filler world used
to prove the connection worked. Needs a proper Python world with:

- `item_name_to_id` / `location_name_to_id` matching the 370 items / 387 locations above
  exactly (names and IDs must match this C# code byte-for-byte).
- **Itempool balancing**: 370 unique items for 387 locations — 17 short. Pad with extra
  copies of filler-ish items (Funds/Intel/CrewSkillXp tiers), not with more upgrades
  (those should stay 1:1 with their location so getting one doesn't feel redundant).
- **Region/access rules**: decide how much of the real chapter-gating (BRT → C01 → C02
  → ... → C08, DLC1 separately) to encode as AP logic vs. leaving ungated (see the
  session's discussion: the real game already enforces this order independently of AP,
  so locations don't strictly need item-gating to be logically reachable — but decide
  deliberately, don't leave it undecided by accident).
- **Item classification**: decide progression/useful/filler per item. Current thinking:
  MissionUnlock = useful (never required, since nothing needs them — see above),
  non-cosmetic BomberUpgrade/CrewEquipment = useful, cosmetic Livery = filler (per
  earlier design choice), Funds/Intel/CrewSkillXp/InstantRepair/InstantHeal = filler.
- **Goal/completion condition**: presumably reaching `C08_KEY`.
- Manifest (`archipelago.json`), options (even if just an empty options dataclass),
  and a player YAML template.
- Once built: regenerate a real seed, swap out the current throwaway test server/world,
  re-test the full loop (connect → real check → real item) end to end.

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

1. Read `Archipelago/LocationTable.cs` and `Archipelago/ItemTable.cs` for the exact
   names/IDs/payloads the `.apworld` must mirror.
2. Start the `.apworld` from the itempool-balancing and access-rule decisions above —
   those are design calls, not just typing.
3. Test loop: generate seed → local `ArchipelagoServer.exe` → launch game → UnityExplorer
   C# console for quick spot-checks (see this session's transcript for example snippets:
   dumping catalogues/requirements, calling `ItemRewarder.Reward` directly, etc.).
