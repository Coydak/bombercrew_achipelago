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
/// Buying something in the shop only sends the check (so the multiworld knows you reached that
/// location) and is then fully reverted: the slot/crewman is put back to whatever it was
/// equipped with before, and the money/stock spent is refunded. The only way to actually equip
/// an upgrade or piece of gear is to receive it as an Archipelago item (see ItemRewarder), same
/// as any other item in the multiworld. This keeps "location" (the check) and "item" (the
/// reward) properly decoupled instead of the shop just handing you what you paid for.
/// </summary>
public static class ShopHooks
{
    private static bool patched;

    private readonly struct BomberPurchaseState
    {
        public readonly string SlotId;
        public readonly string OldUpgradeName;
        public readonly int BalanceBefore;

        public BomberPurchaseState(string slotId, string oldUpgradeName, int balanceBefore)
        {
            SlotId = slotId;
            OldUpgradeName = oldUpgradeName;
            BalanceBefore = balanceBefore;
        }
    }

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

    [HarmonyPrefix]
    [HarmonyPatch(typeof(BomberUpgradeScreenController), "AttemptPurchase")]
    private static void AttemptPurchasePrefix(
        BomberRequirements.BomberEquipmentRequirement ___m_currentlySelectedRequirement,
        out BomberPurchaseState __state)
    {
        __state = default;

        string slotId = ___m_currentlySelectedRequirement?.GetUniquePartId();
        if (slotId == null) return;

        string oldUpgrade = SaveDataContainer.Instance?.Get()?.GetCurrentBomber()?.GetUpgradeFor(slotId);
        int balanceBefore = SaveDataContainer.Instance?.Get()?.GetBalance() ?? 0;
        __state = new BomberPurchaseState(slotId, oldUpgrade, balanceBefore);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BomberUpgradeScreenController), "AttemptPurchase")]
    private static void AttemptPurchasePostfix(
        EquipmentUpgradeFittableBase ___m_currentlySelectedEquippable,
        BomberPurchaseState __state)
    {
        try
        {
            if (__state.SlotId == null || ___m_currentlySelectedEquippable == null) return;

            var saveData = SaveDataContainer.Instance?.Get();
            var bomberConfig = saveData?.GetCurrentBomber();
            if (saveData == null || bomberConfig == null) return;

            string upgradeName = ___m_currentlySelectedEquippable.name;
            if (bomberConfig.GetUpgradeFor(__state.SlotId) != upgradeName) return; // purchase didn't go through

            // Revert: the purchase attempt only sends a check, it never actually keeps the upgrade.
            var revertTo = string.IsNullOrEmpty(__state.OldUpgradeName)
                ? null
                : BomberUpgradeCatalogueLoader.Instance?.GetCatalogue()?.GetByName(__state.OldUpgradeName);
            bomberConfig.SetUpgrade(__state.SlotId, revertTo);
            saveData.AddBalance(__state.BalanceBefore - saveData.GetBalance());

            long locationId = LocationTable.GetShopPurchaseLocation($"{__state.SlotId}:{upgradeName}");
            if (locationId < 0) return;

            ArchipelagoConsole.LogMessage($"Checked '{upgradeName}' ({__state.SlotId}) - receive it via Archipelago to actually install it.");
            Plugin.BepinLogger.LogMessage($"Purchase-check for '{upgradeName}' in slot '{__state.SlotId}'. Sending location check {locationId}.");
            Plugin.ArchipelagoClient.CheckLocation(locationId);
        }
        catch (System.Exception ex)
        {
            Plugin.BepinLogger.LogError($"Error in ShopHooks.AttemptPurchasePostfix: {ex}");
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
