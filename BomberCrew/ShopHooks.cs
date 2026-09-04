extern alias game;

using BC_archipelago.Archipelago;
using game;
using HarmonyLib;

namespace BC_archipelago.BomberCrew;

/// <summary>
/// Harmony patches that turn real in-game shop purchases (bomber upgrades, crew equipment)
/// into Archipelago location checks. This is the counterpart to receiving BomberUpgrade/
/// CrewEquipment items via AP: the same catalogue of (slot, upgrade) / (gear type, equipment)
/// combinations is both something you can be given directly by the multiworld, and something
/// you can earn a check for by actually buying/equipping it in the normal game UI.
/// </summary>
public static class ShopHooks
{
    private static bool patched;

    public static void Apply()
    {
        if (patched) return;

        var harmony = new Harmony(Plugin.PluginGUID);
        harmony.PatchAll(typeof(ShopHooks));
        patched = true;

        Plugin.BepinLogger.LogDebug("ShopHooks applied.");
    }

    /// <summary>
    /// Fires after a bomber upgrade purchase attempt. AttemptPurchase silently no-ops if the
    /// player can't afford it (funds/intel/weight), so success is confirmed by checking whether
    /// the slot's installed upgrade now actually matches what was being bought.
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(BomberUpgradeScreenController), "AttemptPurchase")]
    private static void AttemptPurchasePostfix(
        BomberRequirements.BomberEquipmentRequirement ___m_currentlySelectedRequirement,
        EquipmentUpgradeFittableBase ___m_currentlySelectedEquippable)
    {
        try
        {
            if (___m_currentlySelectedRequirement == null || ___m_currentlySelectedEquippable == null) return;

            string slotId = ___m_currentlySelectedRequirement.GetUniquePartId();
            string upgradeName = ___m_currentlySelectedEquippable.name;

            string installed = SaveDataContainer.Instance?.Get()?.GetCurrentBomber()?.GetUpgradeFor(slotId);
            if (installed != upgradeName) return; // purchase didn't go through

            long locationId = LocationTable.GetShopPurchaseLocation($"{slotId}:{upgradeName}");
            if (locationId < 0) return;

            Plugin.BepinLogger.LogMessage($"Purchased '{upgradeName}' for slot '{slotId}'. Sending location check {locationId}.");
            Plugin.ArchipelagoClient.CheckLocation(locationId);
        }
        catch (System.Exception ex)
        {
            Plugin.BepinLogger.LogError($"Error in ShopHooks.AttemptPurchasePostfix: {ex}");
        }
    }

    /// <summary>
    /// Fires after a single-crewman equipment purchase. Success is confirmed by checking whether
    /// that crewman now actually has the equipment equipped (the method silently no-ops if the
    /// player can't afford it).
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CrewQuartersScreenController), "PurchaseEquipment", typeof(CrewmanEquipmentBase))]
    private static void PurchaseEquipmentPostfix(
        CrewmanEquipmentBase equipment,
        Crewman ___m_currentlySelectedCrewman)
    {
        try
        {
            if (equipment == null || ___m_currentlySelectedCrewman == null) return;
            if (___m_currentlySelectedCrewman.GetEquippedFor(equipment.GetGearType()) != equipment) return;

            CheckEquipmentPurchase(equipment);
        }
        catch (System.Exception ex)
        {
            Plugin.BepinLogger.LogError($"Error in ShopHooks.PurchaseEquipmentPostfix: {ex}");
        }
    }

    /// <summary>
    /// Fires after a whole-crew equipment purchase ("equip all"). Success is confirmed the same
    /// way: at least one crewman now actually has the equipment equipped.
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CrewQuartersScreenController), "PurchaseEquipmentAll", typeof(CrewmanEquipmentBase))]
    private static void PurchaseEquipmentAllPostfix(CrewmanEquipmentBase equipment)
    {
        try
        {
            if (equipment == null) return;

            bool anyEquipped = false;
            foreach (var crewman in GameState.GetAliveCrewmen())
            {
                if (crewman.GetEquippedFor(equipment.GetGearType()) == equipment)
                {
                    anyEquipped = true;
                    break;
                }
            }

            if (!anyEquipped) return;

            CheckEquipmentPurchase(equipment);
        }
        catch (System.Exception ex)
        {
            Plugin.BepinLogger.LogError($"Error in ShopHooks.PurchaseEquipmentAllPostfix: {ex}");
        }
    }

    private static void CheckEquipmentPurchase(CrewmanEquipmentBase equipment)
    {
        string key = $"{equipment.GetGearType()}:{equipment.name}";
        long locationId = LocationTable.GetShopPurchaseLocation(key);
        if (locationId < 0) return;

        Plugin.BepinLogger.LogMessage($"Equipped '{equipment.name}'. Sending location check {locationId}.");
        Plugin.ArchipelagoClient.CheckLocation(locationId);
    }
}
