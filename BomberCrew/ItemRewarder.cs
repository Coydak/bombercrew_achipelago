extern alias game;

using System;
using System.Collections.Generic;
using System.Linq;
using BC_archipelago.Archipelago;
using BC_archipelago.Utils;
using game;

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
    /// </summary>
    public void Reward(ItemDefinition item)
    {
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

    private static void ApplyBomberUpgrade(string payload)
    {
        // Placeholder: real implementation needs the upgrade catalogue and slot mapping.
        ArchipelagoConsole.LogMessage($"TODO: install bomber upgrade '{payload}'");
    }

    private static void ApplyCrewEquipment(string payload)
    {
        // Placeholder: real implementation needs the equipment catalogue and a target crewman.
        ArchipelagoConsole.LogMessage($"TODO: equip crew with '{payload}'");
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

    private static void ApplyMissionUnlock(string payload)
    {
        // Placeholder: real implementation will patch save data or campaign tags.
        ArchipelagoConsole.LogMessage($"TODO: unlock mission/tag '{payload}'");
    }

    private static void ApplyInstantRepair()
    {
        // Placeholder: iterate BomberSystems sections and repair them.
        ArchipelagoConsole.LogMessage("TODO: instant repair");
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
