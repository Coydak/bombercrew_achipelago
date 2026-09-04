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

    private static void ApplyCrewSkillXp(string payload)
    {
        // Expected payload: "SkillName:Amount" or just "Amount" to apply to all crew.
        int amount = 100;
        string skillName = null;

        if (!string.IsNullOrEmpty(payload))
        {
            var parts = payload.Split(':');
            if (parts.Length == 2)
            {
                skillName = parts[0];
                int.TryParse(parts[1], out amount);
            }
            else if (parts.Length == 1)
            {
                int.TryParse(parts[0], out amount);
            }
        }

        foreach (var crewman in GameState.GetAliveCrewmen())
        {
            crewman.GetPrimarySkill()?.AddXP(amount);
            crewman.GetSecondarySkill()?.AddXP(amount);
        }

        ArchipelagoConsole.LogMessage($"Applied {amount} XP to crew.");
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
