extern alias game;

using System.Collections.Generic;
using BC_archipelago.Archipelago;
using BC_archipelago.Utils;
using game;
using HarmonyLib;
using UnityEngine;

namespace BC_archipelago.BomberCrew;

/// <summary>
/// Harmony patches that turn real in-game shop purchase *attempts* (bomber upgrades, crew
/// equipment) into Archipelago location checks.
///
/// Attempting a purchase always sends the location check the first time (so the multiworld
/// knows you reached that location), regardless of whether the purchase is actually allowed to
/// go through.
///
/// Bomber upgrades (all progressive - see ItemRewarder.ProgressiveLines) are gated on unlock
/// state: a slot/upgrade combo can only actually be installed once its progressive line has
/// received enough Archipelago items to unlock that tier (ItemRewarder.
/// IsProgressiveUpgradeUnlocked). Once unlocked, a tier stays unlocked forever, so the player can
/// freely buy it (or any lower tier of that line, or a different parallel line, e.g. Armoured
/// after Standard) on any compatible slot, any number of times, through the normal vanilla
/// purchase flow (real funds/weight checks, single-slot install) - see AttemptPurchasePrefix.
/// Anything untracked (currently: cosmetic Livery) is left alone and behaves exactly like
/// vanilla.
///
/// Crew equipment isn't tiered, so it keeps its original design: the original purchase always
/// goes through and is then reverted (equip, balance, stock) - only receiving the item via
/// Archipelago actually equips it fleet-wide.
/// </summary>
public static class ShopHooks
{
    private static bool patched;

    // Bomber upgrade rows have three distinct states; crew equipment rows (no unlock-gated
    // install step) only ever use NotPurchasedTint/PurchasedTint.
    private static readonly Color NotPurchasedTint = Color.white;
    private static readonly Color PurchasedTint = new(1f, 0.75f, 0.25f);
    private static readonly Color EquipableTint = new(0.4f, 1f, 0.4f);

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
    // Visual "already checked" indicator on shop rows
    // ---------------------------------------------------------------------

    // MultiTextSetter (the concrete TextSetter used by these rows) doesn't override SetColor
    // (it's a no-op in the TextSetter base class) - it just fans SetText out to an array of
    // tk2dTextMesh, which DOES implement SetColor. Reach into that array via reflection instead.
    private static readonly System.Reflection.FieldInfo MultiTextSetterMeshesField =
        typeof(MultiTextSetter).GetField("m_allMeshesToSet", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

    private static void SetTextColor(TextSetter setter, Color color)
    {
        if (setter == null) return;

        if (setter is MultiTextSetter multi)
        {
            if (MultiTextSetterMeshesField?.GetValue(multi) is tk2dTextMesh[] meshes)
            {
                foreach (var mesh in meshes)
                {
                    mesh?.SetColor(color);
                }
            }
            return;
        }

        setter.SetColor(color);
    }

    private static readonly System.Reflection.FieldInfo BomberRowFittableField =
        typeof(BomberUpgradePurchaseableSelection).GetField("m_thisFittable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    private static readonly System.Reflection.FieldInfo BomberRowRequirementField =
        typeof(BomberUpgradePurchaseableSelection).GetField("m_requirementSlot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    private static readonly System.Reflection.FieldInfo BomberRowNameField =
        typeof(BomberUpgradePurchaseableSelection).GetField("m_name", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

    private static readonly System.Reflection.FieldInfo CrewRowEquipmentField =
        typeof(CrewQuartersItemSelectButton).GetField("m_equipment", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    private static readonly System.Reflection.FieldInfo CrewRowNameField =
        typeof(CrewQuartersItemSelectButton).GetField("m_itemName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

    /// <summary>
    /// Tints a shop row's name text so the player can tell its state at a glance without reading
    /// a console message. Untracked combos (e.g. cosmetic Livery) are left with their normal
    /// vanilla appearance.
    ///
    /// Bomber upgrade rows have three distinct states:
    /// - white: purchase never attempted (no check sent yet for this exact slot/upgrade combo).
    /// - orange: purchase attempted (check sent), but the progressive line hasn't unlocked this
    ///   tier yet via a received Archipelago item - buying does nothing until it's unlocked.
    /// - green: unlocked - this tier can actually be bought and installed on this slot right now
    ///   (see ItemRewarder.IsProgressiveUpgradeUnlocked / ShopHooks.AttemptPurchasePrefix).
    /// Unlocked takes priority over checked, since an item can unlock a tier before the player
    /// ever attempts to buy that specific slot/tier combo.
    ///
    /// Crew equipment rows have no unlock-gated install step (buying always reverts; only
    /// receiving the item actually equips it fleet-wide), so they only ever show white/orange.
    ///
    /// Patched here rather than on BomberUpgradePurchaseableSelection/CrewQuartersItemSelectButton's
    /// own Refresh() because SelectableFilterButton.SetUpGraphics is the actual last writer of the
    /// row's text color (it runs on every selection/hover/filter state change via
    /// RefreshGraphicsStates(), several of which - e.g. SetSelected() - don't go through the row's
    /// own OnRefresh event at all) - tinting from Refresh() got silently overwritten back to the
    /// vanilla color as soon as the row's selection state changed. Postfixing the true last writer
    /// instead means our tint always wins, however the refresh was triggered.
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SelectableFilterButton), "SetUpGraphics")]
    private static void SetUpGraphicsPostfix(SelectableFilterButton __instance)
    {
        try
        {
            var bomberRow = __instance.GetComponent<BomberUpgradePurchaseableSelection>();
            if (bomberRow != null)
            {
                var fittable = BomberRowFittableField.GetValue(bomberRow) as EquipmentUpgradeFittableBase;
                var requirement = BomberRowRequirementField.GetValue(bomberRow) as BomberRequirements.BomberEquipmentRequirement;
                var nameSetter = BomberRowNameField.GetValue(bomberRow) as TextSetter;
                if (fittable == null || requirement == null || nameSetter == null) return;

                long locationId = LocationTable.GetShopPurchaseLocation($"{requirement.GetUniquePartId()}:{fittable.name}");
                if (locationId < 0) return; // untracked (e.g. cosmetic Livery) - leave vanilla appearance alone

                bool purchased = ArchipelagoClient.ServerData.CheckedLocations.Contains(locationId);
                bool equipable = ItemRewarder.IsProgressiveUpgradeUnlocked(fittable.name);
                SetTextColor(nameSetter, equipable ? EquipableTint : purchased ? PurchasedTint : NotPurchasedTint);
                return;
            }

            var crewRow = __instance.GetComponent<CrewQuartersItemSelectButton>();
            if (crewRow != null)
            {
                var equipment = CrewRowEquipmentField.GetValue(crewRow) as CrewmanEquipmentBase;
                var nameSetter = CrewRowNameField.GetValue(crewRow) as TextSetter;
                if (equipment == null || nameSetter == null) return;

                long locationId = LocationTable.GetShopPurchaseLocation($"{equipment.GetGearType()}:{equipment.name}");
                if (locationId < 0) return;

                SetTextColor(nameSetter, ArchipelagoClient.ServerData.CheckedLocations.Contains(locationId) ? PurchasedTint : NotPurchasedTint);
            }
        }
        catch (System.Exception ex)
        {
            Plugin.BepinLogger.LogError($"Error in ShopHooks.SetUpGraphicsPostfix: {ex}");
        }
    }

    // ---------------------------------------------------------------------
    // Bomber upgrades
    // ---------------------------------------------------------------------

    /// <summary>
    /// Replaces AttemptPurchase (returns false to skip the original) for slot/upgrade
    /// combinations that ARE tracked by Archipelago, in order to always send the location check
    /// on attempt regardless of the vanilla funds/weight gate - but whether the part is actually
    /// installed now depends on whether it's been unlocked via a received Archipelago item
    /// (ItemRewarder.IsProgressiveUpgradeUnlocked). If it has, the prefix returns true and lets
    /// the original method run normally (real funds/weight checks apply, single slot installs,
    /// same as vanilla) - so the player can freely buy back any previously-unlocked tier, on any
    /// slot, any number of times, including switching between parallel lines (e.g. Armoured after
    /// Standard). If it hasn't been unlocked yet, the check still sends but nothing installs.
    ///
    /// Anything NOT tracked by Archipelago (currently: cosmetic Livery, which has no items or
    /// locations at all) is left completely alone - the prefix returns true immediately and the
    /// vanilla method runs normally.
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

            string upgradeName = ___m_currentlySelectedEquippable.name;
            long locationId = LocationTable.GetShopPurchaseLocation($"{slotId}:{upgradeName}");
            if (locationId < 0)
            {
                return true; // not tracked by Archipelago (e.g. cosmetic Livery) - vanilla purchase/install applies as normal
            }

            var bomberConfig = SaveDataContainer.Instance?.Get()?.GetCurrentBomber();
            if (bomberConfig != null && bomberConfig.GetUpgradeFor(slotId) == upgradeName)
            {
                return false; // already equipped, matches vanilla's own no-op for that case
            }

            bool alreadyChecked = ArchipelagoClient.ServerData.CheckedLocations.Contains(locationId);
            if (!alreadyChecked)
            {
                ArchipelagoConsole.LogMessage($"Checked '{upgradeName}' ({slotId}) for the first time.");
                Plugin.BepinLogger.LogMessage($"Purchase-check for '{upgradeName}' in slot '{slotId}'. Sending location check {locationId}.");
                Plugin.ArchipelagoClient.CheckLocation(locationId);
            }

            if (ItemRewarder.IsProgressiveUpgradeUnlocked(upgradeName))
            {
                return true; // unlocked via Archipelago - let vanilla purchase/install run normally (real funds/weight checks)
            }

            ArchipelagoConsole.LogMessage($"'{upgradeName}' ({slotId}) isn't unlocked yet - receive it via Archipelago first.");
            __instance.Refresh(); // keep the shop UI in sync even though nothing was actually bought
            return false; // not yet unlocked: buying never actually installs it
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

            bool alreadyChecked = ArchipelagoClient.ServerData.CheckedLocations.Contains(locationId);
            ArchipelagoConsole.LogMessage(alreadyChecked
                ? $"Already checked '{equipment.name}' before - no new check sent."
                : $"Checked '{equipment.name}' for the first time - receive it via Archipelago to actually equip it.");
            Plugin.BepinLogger.LogMessage($"Purchase-check for equipment '{equipment.name}'. Sending location check {locationId}.");
            Plugin.ArchipelagoClient.CheckLocation(locationId);
        }
        catch (System.Exception ex)
        {
            Plugin.BepinLogger.LogError($"Error in ShopHooks equipment purchase revert: {ex}");
        }
    }
}
