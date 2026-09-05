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
    private readonly Queue<ItemDefinition> pendingItems = new();

    /// <summary>
    /// Enqueues an item to be rewarded. If it is safe to apply immediately, it is applied right away.
    /// Repair items are meaningful only while a bomber is actually in flight, so they bypass the
    /// base-only queue and are applied (or discarded as a no-op) the moment they arrive.
    /// </summary>
    public void Reward(ItemDefinition item)
    {
        if (item.Category == ItemCategory.InstantRepair)
        {
            Apply(item);
            return;
        }

        pendingItems.Enqueue(item);
        TryApplyPending();
    }

    /// <summary>
    /// Call this periodically (e.g. from the plugin Update loop) to flush the item queue
    /// once the player is back at base.
    /// </summary>
    public void Update()
    {
        TryApplyPending();
    }

    private void TryApplyPending()
    {
        if (!GameState.CanApplyItemsSafely) return;

        while (pendingItems.Count > 0)
        {
            var item = pendingItems.Dequeue();
            Apply(item);
        }
    }

    private void Apply(ItemDefinition item)
    {
        try
        {
            Plugin.BepinLogger.LogMessage($"Applying item: {item.Name} ({item.Category})");

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
    /// Expected payload: "{lineId}:{tierCount}" (tierCount is informational/defensive only -
    /// ProgressiveLines is the source of truth for actual tier names). Each copy received moves
    /// the line up one tier, applied fleet-wide to every requirement slot whose BomberUpgradeType
    /// matches - e.g. every copy of "Progressive EngineStandard" upgrades all 4 engines at once.
    /// A persistent per-line counter (ArchipelagoData.ProgressiveUpgradeCounts) tracks how many
    /// tiers have been received so far, since the save data only records what's currently equipped
    /// and switching to a different parallel line (e.g. Armoured after Standard) would otherwise
    /// make the current tier ambiguous.
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
        serverData.ProgressiveUpgradeCounts.TryGetValue(lineId, out int count);
        count++;
        serverData.ProgressiveUpgradeCounts[lineId] = count;

        int tierIndex = Math.Min(count, line.Tiers.Length) - 1;
        string upgradeName = line.Tiers[tierIndex];

        var upgrade = BomberUpgradeCatalogueLoader.Instance?.GetCatalogue()?.GetByName(upgradeName);
        var bomberConfig = SaveDataContainer.Instance?.Get()?.GetCurrentBomber();
        if (upgrade == null || bomberConfig == null)
        {
            Plugin.BepinLogger.LogWarning($"ApplyBomberUpgradeProgressive: could not resolve '{upgradeName}' or the active bomber.");
            return;
        }

        var requirements = GameFlow.Instance?.GetGameMode()?.GetBomberRequirements()?.GetRequirements();
        if (requirements == null) return;

        int applied = 0;
        foreach (var requirement in requirements)
        {
            if (requirement.GetUpgradeConfig() != line.Type) continue;

            bomberConfig.SetUpgrade(requirement.GetUniquePartId(), upgrade);
            applied++;
        }

        ArchipelagoConsole.LogMessage($"Progressive '{lineId}' tier {tierIndex + 1}/{line.Tiers.Length}: installed '{upgrade.GetNameTranslated()}' on {applied} slot(s).");
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
