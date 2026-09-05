extern alias game;

using System.Collections.Generic;
using BC_archipelago.Archipelago;
using BC_archipelago.Utils;
using game;
using HarmonyLib;

namespace BC_archipelago.BomberCrew;

/// <summary>
/// Harmony patches that turn real in-game shop purchase *attempts* (bomber upgrades, crew
/// equipment) into Archipelago location checks, without actually granting the item locally.
///
/// Buying something in the shop only ever sends the check (so the multiworld knows you reached
/// that location); it never actually keeps the upgrade/equipment. The only way to actually equip
/// something is to receive it as an Archipelago item (see ItemRewarder), same as any other item
/// in the multiworld. This keeps "location" (the check) and "item" (the reward) properly
/// decoupled instead of the shop just handing you what you paid for.
///
/// Bomber upgrades replace the original purchase method outright (see AttemptPurchasePrefix) so
/// the check always sends regardless of the game's own funds/weight gating. Crew equipment
/// instead lets the original purchase go through and then reverts its effects (equip, balance,
/// stock) - there's no equivalent lockout risk there since crew gear has no weight system.
/// </summary>
public static class ShopHooks
{
    private static bool patched;

    private readonly struct EquipmentPurchaseState
    {
        public readonly Dictionary<Crewman, CrewmanEquipmentBase> OldEquipped;
        public readonly int StockBefore;
        public readonly int BalanceBefore;

        public EquipmentPurchaseState(Dictionary<Crewman, CrewmanEquipmentBase> oldEquipped, int stockBefore, int balanceBefore)
        {
            OldEquipped = oldEquipped;
            StockBefore = stockBefore;
            BalanceBefore = balanceBefore;
        }
    }

    public static void Apply()
    {
        if (patched) return;

        var harmony = new Harmony(Plugin.PluginGUID);
        harmony.PatchAll(typeof(ShopHooks));
        patched = true;

        Plugin.BepinLogger.LogDebug("ShopHooks applied.");
    }

    // ---------------------------------------------------------------------
    // Bomber upgrades
    // ---------------------------------------------------------------------

    /// <summary>
    /// Replaces AttemptPurchase entirely (returns false to skip the original) instead of just
    /// reverting its effects afterward. The original method gates on funds AND a weight budget
    /// (heavy equipment needs enough installed engines to carry it) computed from the bomber's
    /// *current* state - once any slot is force-installed by a received Archipelago item into an
    /// overweight configuration (which bypasses that gate entirely, see ItemRewarder), every
    /// future purchase attempt for ANY slot would silently fail the weight check and never send
    /// its location check again. Reimplementing the check-sending ourselves, with no funds/weight
    /// involved at all, avoids that lockout - buying in the shop should always be able to send a
    /// check, since it never actually keeps the upgrade anyway.
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(BomberUpgradeScreenController), "AttemptPurchase")]
    private static bool AttemptPurchasePrefix(
        BomberUpgradeScreenController __instance,
        BomberRequirements.BomberEquipmentRequirement ___m_currentlySelectedRequirement,
        EquipmentUpgradeFittableBase ___m_currentlySelectedEquippable)
    {
        try
        {
            string slotId = ___m_currentlySelectedRequirement?.GetUniquePartId();
            if (slotId == null || ___m_currentlySelectedEquippable == null) return true; // nothing selected, let vanilla handle it

            var bomberConfig = SaveDataContainer.Instance?.Get()?.GetCurrentBomber();
            string upgradeName = ___m_currentlySelectedEquippable.name;
            if (bomberConfig != null && bomberConfig.GetUpgradeFor(slotId) == upgradeName)
            {
                return false; // already equipped, matches vanilla's own no-op for that case
            }

            long locationId = LocationTable.GetShopPurchaseLocation($"{slotId}:{upgradeName}");
            if (locationId >= 0)
            {
                ArchipelagoConsole.LogMessage($"Checked '{upgradeName}' ({slotId}) - receive it via Archipelago to actually install it.");
                Plugin.BepinLogger.LogMessage($"Purchase-check for '{upgradeName}' in slot '{slotId}'. Sending location check {locationId}.");
                Plugin.ArchipelagoClient.CheckLocation(locationId);
            }

            __instance.Refresh(); // keep the shop UI in sync even though nothing was actually bought
            return false; // skip the original entirely: no funds/weight gate, no install, nothing to revert
        }
        catch (System.Exception ex)
        {
            Plugin.BepinLogger.LogError($"Error in ShopHooks.AttemptPurchasePrefix: {ex}");
            return false;
        }
    }

    // ---------------------------------------------------------------------
    // Crew equipment (single crewman)
    // ---------------------------------------------------------------------

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CrewQuartersScreenController), "PurchaseEquipment", typeof(CrewmanEquipmentBase))]
    private static void PurchaseEquipmentPrefix(
        CrewmanEquipmentBase equipment,
        Crewman ___m_currentlySelectedCrewman,
        out EquipmentPurchaseState __state)
    {
        __state = default;
        if (equipment == null || ___m_currentlySelectedCrewman == null) return;

        var oldEquipped = new Dictionary<Crewman, CrewmanEquipmentBase>
        {
            [___m_currentlySelectedCrewman] = ___m_currentlySelectedCrewman.GetEquippedFor(equipment.GetGearType())
        };
        int stockBefore = SaveDataContainer.Instance?.Get()?.GetStockForCrewGear(equipment) ?? 0;
        int balanceBefore = SaveDataContainer.Instance?.Get()?.GetBalance() ?? 0;
        __state = new EquipmentPurchaseState(oldEquipped, stockBefore, balanceBefore);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CrewQuartersScreenController), "PurchaseEquipment", typeof(CrewmanEquipmentBase))]
    private static void PurchaseEquipmentPostfix(CrewmanEquipmentBase equipment, EquipmentPurchaseState __state)
    {
        RevertAndCheckEquipmentPurchase(equipment, __state);
    }

    // ---------------------------------------------------------------------
    // Crew equipment (whole crew at once)
    // ---------------------------------------------------------------------

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CrewQuartersScreenController), "PurchaseEquipmentAll", typeof(CrewmanEquipmentBase))]
    private static void PurchaseEquipmentAllPrefix(CrewmanEquipmentBase equipment, out EquipmentPurchaseState __state)
    {
        __state = default;
        if (equipment == null) return;

        var oldEquipped = new Dictionary<Crewman, CrewmanEquipmentBase>();
        foreach (var crewman in GameState.GetAliveCrewmen())
        {
            oldEquipped[crewman] = crewman.GetEquippedFor(equipment.GetGearType());
        }

        int stockBefore = SaveDataContainer.Instance?.Get()?.GetStockForCrewGear(equipment) ?? 0;
        int balanceBefore = SaveDataContainer.Instance?.Get()?.GetBalance() ?? 0;
        __state = new EquipmentPurchaseState(oldEquipped, stockBefore, balanceBefore);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CrewQuartersScreenController), "PurchaseEquipmentAll", typeof(CrewmanEquipmentBase))]
    private static void PurchaseEquipmentAllPostfix(CrewmanEquipmentBase equipment, EquipmentPurchaseState __state)
    {
        RevertAndCheckEquipmentPurchase(equipment, __state);
    }

    private static void RevertAndCheckEquipmentPurchase(CrewmanEquipmentBase equipment, EquipmentPurchaseState state)
    {
        try
        {
            if (equipment == null || state.OldEquipped == null) return;

            var saveData = SaveDataContainer.Instance?.Get();
            if (saveData == null) return;

            bool anyEquipped = false;
            foreach (var pair in state.OldEquipped)
            {
                if (pair.Key.GetEquippedFor(equipment.GetGearType()) == equipment) anyEquipped = true;
            }

            // Revert every captured crewman back to what they had before, regardless of success -
            // the purchase attempt only sends a check, it never actually keeps the equipment.
            foreach (var pair in state.OldEquipped)
            {
                pair.Key.SetEquippedFor(equipment.GetGearType(), pair.Value);
            }

            saveData.ModifyStockForCrewGear(equipment, state.StockBefore - saveData.GetStockForCrewGear(equipment));
            saveData.AddBalance(state.BalanceBefore - saveData.GetBalance());

            if (!anyEquipped) return;

            long locationId = LocationTable.GetShopPurchaseLocation($"{equipment.GetGearType()}:{equipment.name}");
            if (locationId < 0) return;

            ArchipelagoConsole.LogMessage($"Checked '{equipment.name}' - receive it via Archipelago to actually equip it.");
            Plugin.BepinLogger.LogMessage($"Purchase-check for equipment '{equipment.name}'. Sending location check {locationId}.");
            Plugin.ArchipelagoClient.CheckLocation(locationId);
        }
        catch (System.Exception ex)
        {
            Plugin.BepinLogger.LogError($"Error in ShopHooks equipment purchase revert: {ex}");
        }
    }
}
