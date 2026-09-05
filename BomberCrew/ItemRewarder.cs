extern alias game;

using System;
using System.Collections.Generic;
using System.Linq;
using BC_archipelago.Archipelago;
using BC_archipelago.Utils;
using game;
using UnityEngine;

namespace BC_archipelago.BomberCrew;

/// <summary>
/// Applies Archipelago items to the Bomber Crew game state.
/// Items received during a mission are queued and applied once the player returns to base.
/// </summary>
public class ItemRewarder
{
    private readonly object queueLock = new();
    private readonly Queue<ItemDefinition> pendingItems = new();
    private readonly Queue<ItemDefinition> instantItems = new();

    /// <summary>
    /// Enqueues an item to be rewarded. Received items arrive on the Archipelago client's own
    /// network thread (Reward is called from ArchipelagoClient.OnItemReceived, itself invoked by
    /// the websocket's receive callback) - this must NEVER touch Unity/game state directly here,
    /// only queue. Actually applying an item (Apply()) always happens from Update() instead,
    /// which Unity guarantees runs on the main thread; calling Unity APIs off that thread causes
    /// native access violations (reproduced and confirmed crashing the game during development -
    /// Access Violation in ItemRewarder.Apply, called straight from the websocket message thread).
    /// </summary>
    public void Reward(ItemDefinition item)
    {
        lock (queueLock)
        {
            if (item.Category == ItemCategory.InstantRepair)
                instantItems.Enqueue(item);
            else
                pendingItems.Enqueue(item);
        }
    }

    /// <summary>
    /// Call this every frame from the plugin's Update loop (main thread - see Reward's remarks).
    /// Repair items are meaningful only while a bomber is actually in flight, so they bypass the
    /// base-only queue and are applied (or discarded as a no-op) as soon as this next runs.
    /// </summary>
    public void Update()
    {
        List<ItemDefinition> instant = null;
        lock (queueLock)
        {
            if (instantItems.Count > 0)
            {
                instant = new List<ItemDefinition>(instantItems);
                instantItems.Clear();
            }
        }

        if (instant != null)
        {
            foreach (var item in instant) Apply(item);
        }

        TryApplyPending();
    }

    private void TryApplyPending()
    {
        if (!GameState.CanApplyItemsSafely) return;

        while (true)
        {
            ItemDefinition item;
            lock (queueLock)
            {
                if (pendingItems.Count == 0) break;
                item = pendingItems.Dequeue();
            }

            Apply(item);
        }
    }

    private void Apply(ItemDefinition item)
    {
        try
        {
            Plugin.BepinLogger.LogMessage($"Applying item: {item.Name} ({item.Category})");
            ItemNotifications.Show(item.Name);

            switch (item.Category)
            {
                case ItemCategory.Funds:
                    ApplyFunds(item.Payload);
                    break;

                case ItemCategory.Intel:
                    ApplyIntel(item.Payload);
                    break;

                case ItemCategory.BomberUpgrade:
                    ApplyBomberUpgrade(item.Payload);
                    break;

                case ItemCategory.BomberUpgradeProgressive:
                    ApplyBomberUpgradeProgressive(item.Payload);
                    break;

                case ItemCategory.CrewEquipment:
                    ApplyCrewEquipment(item.Payload);
                    break;

                case ItemCategory.CrewSkillXp:
                    ApplyCrewSkillXp(item.Payload);
                    break;

                case ItemCategory.MissionUnlock:
                    ApplyMissionUnlock(item.Payload);
                    break;

                case ItemCategory.InstantRepair:
                    ApplyInstantRepair();
                    break;

                case ItemCategory.InstantHeal:
                    ApplyInstantHeal();
                    break;

                case ItemCategory.Unknown:
                default:
                    Plugin.BepinLogger.LogWarning($"Unknown item category for {item.Name}; skipping.");
                    break;
            }
        }
        catch (Exception ex)
        {
            Plugin.BepinLogger.LogError($"Failed to apply item {item.Name}: {ex}");
        }
    }

    private static void ApplyFunds(string payload)
    {
        if (!int.TryParse(payload, out int amount)) amount = 100;
        SaveDataContainer.Instance?.Get()?.AddBalance(amount);
        ArchipelagoConsole.LogMessage($"Received {amount} funds.");
    }

    private static void ApplyIntel(string payload)
    {
        if (!int.TryParse(payload, out int amount)) amount = 10;
        SaveDataContainer.Instance?.Get()?.AddIntel(amount);
        ArchipelagoConsole.LogMessage($"Received {amount} intel.");
    }

    /// <summary>
    /// Expected payload: "SlotId:UpgradeAssetName", where SlotId is a BomberRequirements
    /// unique part id (e.g. an engine or turret slot) and UpgradeAssetName is the ScriptableObject
    /// asset name of an EquipmentUpgradeFittableBase registered in the bomber upgrade catalogue.
    /// </summary>
    private static void ApplyBomberUpgrade(string payload)
    {
        var parts = payload?.Split(new[] { ':' }, 2);
        if (parts == null || parts.Length != 2)
        {
            Plugin.BepinLogger.LogWarning($"ApplyBomberUpgrade: malformed payload '{payload}' (expected 'SlotId:UpgradeAssetName').");
            return;
        }

        string slotId = parts[0];
        string upgradeName = parts[1];

        var upgrade = BomberUpgradeCatalogueLoader.Instance?.GetCatalogue()?.GetByName(upgradeName);
        if (upgrade == null)
        {
            Plugin.BepinLogger.LogWarning($"ApplyBomberUpgrade: unknown upgrade '{upgradeName}'.");
            return;
        }

        var bomberConfig = SaveDataContainer.Instance?.Get()?.GetCurrentBomber();
        if (bomberConfig == null)
        {
            Plugin.BepinLogger.LogWarning("ApplyBomberUpgrade: no active bomber config to upgrade.");
            return;
        }

        bomberConfig.SetUpgrade(slotId, upgrade);
        ArchipelagoConsole.LogMessage($"Installed bomber upgrade '{upgrade.GetNameTranslated()}' ({slotId}).");
    }

    /// <summary>
    /// Ordered lowest-to-highest tier names for each progressive bomber upgrade line. Parallel
    /// variants (e.g. Standard/Armoured/Light engines) are separate lines, never merged, since
    /// they're a real gameplay tradeoff rather than a strict upgrade path. Keep this in sync with
    /// tools/gen_item_table.py's PROGRESSIVE_LINES (that's what generates ItemTable's entries).
    /// </summary>
    private static readonly Dictionary<string, (BomberUpgradeType Type, string[] Tiers)> ProgressiveLines = new()
    {
        ["EngineStandard"] = (BomberUpgradeType.Engine, new[] { "EngineStandardMk1", "EngineStandardMk2", "EngineStandardMk3", "EngineStandardMk4", "EngineStandardMk5" }),
        ["EngineArmoured"] = (BomberUpgradeType.Engine, new[] { "EngineArmouredMk1", "EngineArmouredMk2", "EngineArmouredMk3", "EngineArmouredMk4", "EngineArmouredMk5" }),
        ["EngineLight"] = (BomberUpgradeType.Engine, new[] { "EngineLightMk1", "EngineLightMk2", "EngineLightMk3" }),
        ["GunTurret303x2"] = (BomberUpgradeType.GunTurret, new[] { "GunTurret303x2Mk1", "GunTurret303x2Mk2", "GunTurret303x2Mk3" }),
        ["GunTurret303x2AmmoFeed"] = (BomberUpgradeType.GunTurret, new[] { "GunTurret303x2Mk1_AmmoFeed", "GunTurret303x2Mk2_AmmoFeed", "GunTurret303x2Mk3_AmmoFeed" }),
        ["GunTurret303x4"] = (BomberUpgradeType.GunTurret, new[] { "GunTurret303x4Mk3", "GunTurret303x4Mk4" }),
        ["GunTurret303x4AmmoFeed"] = (BomberUpgradeType.GunTurret, new[] { "GunTurret303x4Mk3_AmmoFeed" }),
        ["GunTurret50x4"] = (BomberUpgradeType.GunTurret, new[] { "GunTurret50x4Mk3", "GunTurret50x4Mk4" }),
        ["GunTurret50x4AmmoFeed"] = (BomberUpgradeType.GunTurret, new[] { "GunTurret50x4Mk3_AmmoFeed" }),
        ["GunTurret50x2"] = (BomberUpgradeType.GunTurret, new[] { "GunTurret50x2Mk1", "GunTurret50x2Mk2", "GunTurret50x2Mk3" }),
        ["GunTurret50x2AmmoFeed"] = (BomberUpgradeType.GunTurret, new[] { "GunTurret50x2Mk1_AmmoFeed", "GunTurret50x2Mk2_AmmoFeed", "GunTurret50x2Mk3_AmmoFeed" }),
        ["FuselageLightweight"] = (BomberUpgradeType.FuselageMain, new[] { "FuselageLightweightMk1", "FuselageLightweightMk2", "FuselageLightweightMk3", "FuselageLightweightMk4", "FuselageLightweightMk5" }),
        ["FuselageArmoured"] = (BomberUpgradeType.FuselageMain, new[] { "FuselageArmouredMk1", "FuselageArmouredMk2", "FuselageArmouredMk3", "FuselageArmouredMk4", "FuselageArmouredMk5", "FuselageArmouredMk6", "FuselageArmouredMk7" }),
        ["Electrical"] = (BomberUpgradeType.Electrical, new[] { "ElectricalSystemMk1", "ElectricalSystemMk2", "ElectricalSystemMk3", "ElectricalSystemMk4", "ElectricalSystemMk5" }),
        ["Hydraulic"] = (BomberUpgradeType.Hyrdaulic, new[] { "HydraulicSystemMk1", "HydraulicSystemMk2", "HydraulicSystemMk3", "HydraulicSystemMk4" }),
        ["Radar"] = (BomberUpgradeType.Radar, new[] { "RadarMk1", "RadarMk2", "RadarMk3", "RadarMk4", "RadarMk5", "RadarMk6" }),
        ["Extinguisher"] = (BomberUpgradeType.Extinguisher, new[] { "ExtinguisherMk1", "ExtinguisherMk2", "ExtinguisherMk3", "ExtinguisherMk4" }),
        ["EquipmentRack"] = (BomberUpgradeType.EquipmentRack, new[] { "EquipmentRack1", "EquipmentRack2", "EquipmentRack3" }),
        ["OxygenTank"] = (BomberUpgradeType.OxygenTank, new[] { "OxygenTankMk1", "OxygenTankMk2", "OxygenTankMk3" }),
        ["FuelTank"] = (BomberUpgradeType.FuelTank, new[] { "FuelTankMk1", "FuelTankMk2", "FuelTankMk3" }),
        ["FuelTankSelfSealing"] = (BomberUpgradeType.FuelTank, new[] { "FuelTankSelfSealingMk1" }),
        ["SurvivalDinghy"] = (BomberUpgradeType.SurvivalDinghy, new[] { "DinghyMk1", "DinghyMk2", "DinghyMk3" }),
        ["SurvivalPigeon"] = (BomberUpgradeType.SurvivalPigeon, new[] { "PigeonMk1", "PigeonMk2", "PigeonMk3" }),
    };

    /// <summary>
    /// Lines whose Mk1 tier is exactly what the bomber already starts with by default (see
    /// tools/gen_item_table.py's SLOTS default column) - the player never needs an Archipelago
    /// item to obtain that first tier since they already own it, so it counts as unlocked from
    /// the start. Keep in sync with tools/gen_item_table.py's DEFAULT_UNLOCKED_LINES, which
    /// derives the same set and uses it to leave that tier's copy out of the item pool.
    /// </summary>
    private static readonly HashSet<string> DefaultUnlockedLines = new()
    {
        "EngineStandard", "GunTurret303x2", "Electrical", "Hydraulic", "Radar", "EquipmentRack", "OxygenTank", "FuelTank",
    };

    /// <summary>
    /// Reverse lookup from an exact upgrade asset name (e.g. "EngineArmouredMk2") to which
    /// progressive line it belongs to and its 0-based tier index within that line. Built once
    /// from ProgressiveLines.
    /// </summary>
    private static readonly Dictionary<string, (string LineId, int TierIndex)> UpgradeNameToLine =
        ProgressiveLines
            .SelectMany(kv => kv.Value.Tiers.Select((tierName, tierIndex) => (tierName, kv.Key, tierIndex)))
            .ToDictionary(x => x.tierName, x => (x.Key, x.tierIndex));

    /// <summary>
    /// The highest tier unlocked so far for a progressive line, counting DefaultUnlockedLines'
    /// implicit tier-1 unlock even before any item has been received for that line.
    /// </summary>
    private static int GetUnlockedTierCount(string lineId)
    {
        if (ArchipelagoClient.ServerData.ProgressiveUpgradeCounts.TryGetValue(lineId, out int count)) return count;
        return DefaultUnlockedLines.Contains(lineId) ? 1 : 0;
    }

    /// <summary>
    /// True if the given exact upgrade asset name has been unlocked for purchase - i.e. its
    /// progressive line's tier count (received via Archipelago, plus any default-unlocked first
    /// tier) has reached at least this tier. Since the unlock counter only ever grows, any tier at
    /// or below the highest one received stays unlocked forever, so the player can freely switch
    /// back and forth between tiers (and between parallel lines, e.g. Standard/Armoured/Light
    /// engines) via the shop. Used by ShopHooks to gate whether a shop purchase attempt is allowed
    /// to actually install the part.
    /// </summary>
    public static bool IsProgressiveUpgradeUnlocked(string upgradeName)
    {
        if (upgradeName == null || !UpgradeNameToLine.TryGetValue(upgradeName, out var entry)) return false;

        return entry.TierIndex + 1 <= GetUnlockedTierCount(entry.LineId);
    }

    /// <summary>
    /// Expected payload: "{lineId}:{copiesInPool}" (copiesInPool is informational/defensive only -
    /// ProgressiveLines is the source of truth for actual tier names). Each copy received unlocks
    /// the line's next tier for purchase in the bomber upgrade shop (see ShopHooks.
    /// AttemptPurchasePrefix and IsProgressiveUpgradeUnlocked) - it does NOT install anything by
    /// itself. A persistent per-line counter (ArchipelagoData.ProgressiveUpgradeCounts) tracks the
    /// highest tier unlocked so far, starting from 1 instead of 0 for DefaultUnlockedLines; since
    /// it only ever grows, the player can buy (and later switch back to) any already-unlocked
    /// tier, on any slot of the matching type, any number of times, including switching between
    /// parallel lines (e.g. Armoured after Standard).
    /// </summary>
    private static void ApplyBomberUpgradeProgressive(string payload)
    {
        string lineId = payload?.Split(':')[0];
        if (lineId == null || !ProgressiveLines.TryGetValue(lineId, out var line))
        {
            Plugin.BepinLogger.LogWarning($"ApplyBomberUpgradeProgressive: unknown line '{payload}'.");
            return;
        }

        var serverData = ArchipelagoClient.ServerData;
        int count = Math.Min(GetUnlockedTierCount(lineId) + 1, line.Tiers.Length);
        serverData.ProgressiveUpgradeCounts[lineId] = count;

        string upgradeName = line.Tiers[count - 1];
        ArchipelagoConsole.LogMessage($"Unlocked '{lineId}' tier {count}/{line.Tiers.Length} ('{upgradeName}') - buy it in the bomber upgrade shop to install it.");
    }

    /// <summary>
    /// Expected payload: "GearType:EquipmentAssetName", where GearType is a CrewmanGearType name
    /// (Headgear, Oxygen, Vest, Gloves, Boots, Flightsuit) and EquipmentAssetName is the
    /// ScriptableObject asset name of a CrewmanEquipmentBase registered in the crew gear catalogue.
    /// Applies to every currently alive crewman.
    /// </summary>
    private static void ApplyCrewEquipment(string payload)
    {
        var parts = payload?.Split(new[] { ':' }, 2);
        if (parts == null || parts.Length != 2 || !Enum.IsDefined(typeof(CrewmanGearType), parts[0]))
        {
            Plugin.BepinLogger.LogWarning($"ApplyCrewEquipment: malformed payload '{payload}' (expected 'GearType:EquipmentAssetName').");
            return;
        }

        var gearType = (CrewmanGearType)Enum.Parse(typeof(CrewmanGearType), parts[0]);
        string equipmentName = parts[1];
        var equipment = CrewmanGearCatalogueLoader.Instance?.GetCatalogue()?.GetByName(equipmentName);
        if (equipment == null)
        {
            Plugin.BepinLogger.LogWarning($"ApplyCrewEquipment: unknown equipment '{equipmentName}'.");
            return;
        }

        foreach (var crewman in GameState.GetAliveCrewmen())
        {
            crewman.SetEquippedFor(gearType, equipment);
        }

        ArchipelagoConsole.LogMessage($"Equipped crew with '{equipment.GetNamedTextTranslated()}'.");
    }

    /// <summary>
    /// Expected payload: "SkillName:Amount" (SkillName a Crewman.SpecialisationSkill name, e.g.
    /// Piloting, Gunning, Navigator, RadioOp, Engineer, BombAiming, FirstAid, FireFighting) to add
    /// XP only to crew who have that skill as primary or secondary, or just "Amount" to boost
    /// every crewman's primary and secondary skill regardless of type.
    /// </summary>
    private static void ApplyCrewSkillXp(string payload)
    {
        int amount = 100;
        Crewman.SpecialisationSkill? skill = null;

        if (!string.IsNullOrEmpty(payload))
        {
            var parts = payload.Split(':');
            if (parts.Length == 2 && Enum.IsDefined(typeof(Crewman.SpecialisationSkill), parts[0]))
            {
                skill = (Crewman.SpecialisationSkill)Enum.Parse(typeof(Crewman.SpecialisationSkill), parts[0]);
                int.TryParse(parts[1], out amount);
            }
            else
            {
                int.TryParse(parts[0], out amount);
            }
        }

        foreach (var crewman in GameState.GetAliveCrewmen())
        {
            var primary = crewman.GetPrimarySkill();
            var secondary = crewman.GetSecondarySkill();

            if (primary != null && (skill == null || primary.GetSkill() == skill)) primary.AddXP(amount);
            if (secondary != null && (skill == null || secondary.GetSkill() == skill)) secondary.AddXP(amount);
        }

        ArchipelagoConsole.LogMessage(skill == null
            ? $"Applied {amount} XP to crew."
            : $"Applied {amount} {skill} XP to crew.");
    }

    /// <summary>
    /// Expected payload: a mission reference name (see LocationTable), typically a chapter's
    /// "*_KEY" mission. Marks it completed in the save data so the tag it unlocks (and therefore
    /// the next chapter's missions) becomes available without having to fly it.
    /// </summary>
    private static void ApplyMissionUnlock(string payload)
    {
        if (string.IsNullOrEmpty(payload))
        {
            Plugin.BepinLogger.LogWarning("ApplyMissionUnlock: missing mission reference payload.");
            return;
        }

        var saveData = SaveDataContainer.Instance?.Get();
        if (saveData == null)
        {
            Plugin.BepinLogger.LogWarning("ApplyMissionUnlock: no active save data.");
            return;
        }

        saveData.SetMissionPlayed(payload, true, false, null);
        ArchipelagoConsole.LogMessage($"Unlocked mission progress for '{payload}'.");
    }

    /// <summary>
    /// Repairs every currently broken Repairable component on the active bomber. Only meaningful
    /// mid-mission; if there's no live bomber right now, this is a harmless no-op (there's nothing
    /// to repair at base since every mission starts with an undamaged aircraft).
    /// </summary>
    private static void ApplyInstantRepair()
    {
        var bomberSystems = BomberSpawn.Instance?.GetBomberSystems();
        if (bomberSystems == null)
        {
            ArchipelagoConsole.LogMessage("Instant repair received, but there's no bomber in the air right now.");
            return;
        }

        int repaired = 0;
        foreach (var repairable in bomberSystems.GetComponentsInChildren<MonoBehaviour>().OfType<Repairable>())
        {
            if (!repairable.IsBroken()) continue;

            repairable.Repair();
            repaired++;
        }

        ArchipelagoConsole.LogMessage(repaired > 0
            ? $"Instantly repaired {repaired} system(s)."
            : "Instant repair received, but nothing was broken.");
    }

    private static void ApplyInstantHeal()
    {
        foreach (var crewman in GameState.GetAliveCrewmen())
        {
            crewman.MagicallyResurrect();
        }

        ArchipelagoConsole.LogMessage("Healed / resurrected all crew.");
    }
}
