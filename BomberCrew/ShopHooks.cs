extern alias game;

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
/// Crew equipment now follows the exact same unlock-gated model as bomber upgrades (see
/// HandleEquipmentPurchaseAttempt): buying always sends the check on first attempt, but only
/// actually equips once the piece has been unlocked via a received Archipelago item. Once
/// unlocked, it stays unlocked forever, so the player can freely (re-)equip any previously-
/// unlocked piece on any crewman, any number of times, through the normal vanilla purchase flow.
/// </summary>
public static class ShopHooks
{
    private static bool patched;

    // Both bomber upgrade and crew equipment rows have four distinct states now.
    private static readonly Color NotPurchasedTint = Color.white;
    private static readonly Color PurchasedTint = new(1f, 0.75f, 0.25f);
    private static readonly Color EquipableTint = new(0.4f, 1f, 0.4f);
    private static readonly Color EquipableUntriedTint = new(0.3f, 0.75f, 1f);

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
    /// Bomber upgrade rows have four distinct states, combining checked and unlocked
    /// independently (an item can unlock a tier before the player ever attempts to buy that
    /// specific slot/tier combo, so these aren't mutually exclusive):
    /// - white: never attempted (no check sent) and not unlocked.
    /// - orange: attempted (check sent), but the progressive line hasn't unlocked this tier yet
    ///   via a received Archipelago item - buying does nothing until it's unlocked.
    /// - blue: unlocked but never attempted on this exact slot - buying it now both sends a new
    ///   check AND actually installs it (see ItemRewarder.IsProgressiveUpgradeUnlocked /
    ///   ShopHooks.AttemptPurchasePrefix).
    /// - green: unlocked AND already attempted on this slot - buying again just re-installs it,
    ///   no new check.
    ///
    /// Crew equipment rows use the exact same four states, keyed on ItemRewarder.
    /// IsCrewEquipmentUnlocked instead of IsProgressiveUpgradeUnlocked.
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
                SetTextColor(nameSetter, ResolveTint(purchased, equipable));
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

                bool purchased = ArchipelagoClient.ServerData.CheckedLocations.Contains(locationId);
                bool equipable = ItemRewarder.IsCrewEquipmentUnlocked(equipment.GetGearType(), equipment.name);
                SetTextColor(nameSetter, ResolveTint(purchased, equipable));
            }
        }
        catch (System.Exception ex)
        {
            Plugin.BepinLogger.LogError($"Error in ShopHooks.SetUpGraphicsPostfix: {ex}");
        }
    }

    private static Color ResolveTint(bool purchased, bool equipable)
    {
        if (equipable) return purchased ? EquipableTint : EquipableUntriedTint;
        return purchased ? PurchasedTint : NotPurchasedTint;
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
    // Crew equipment - same unlock-gated model as bomber upgrades: buying always sends the
    // check on first attempt, but only actually equips once the piece has been unlocked via a
    // received Archipelago item (ItemRewarder.IsCrewEquipmentUnlocked). Once unlocked, it stays
    // unlocked forever, so the player can freely (re-)equip any previously-unlocked piece on any
    // crewman, any number of times, through the normal vanilla purchase flow (real funds/stock
    // checks). No revert-after-the-fact needed anymore, same as AttemptPurchasePrefix.
    // ---------------------------------------------------------------------

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CrewQuartersScreenController), "PurchaseEquipment", typeof(CrewmanEquipmentBase))]
    private static bool PurchaseEquipmentPrefix(CrewmanEquipmentBase equipment, Crewman ___m_currentlySelectedCrewman)
    {
        return HandleEquipmentPurchaseAttempt(equipment, ___m_currentlySelectedCrewman);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CrewQuartersScreenController), "PurchaseEquipmentAll", typeof(CrewmanEquipmentBase))]
    private static bool PurchaseEquipmentAllPrefix(CrewmanEquipmentBase equipment)
    {
        // No single "already equipped" crewman to check against here - fleet-wide purchases are
        // idempotent enough (vanilla just no-ops per-crewman for anyone who already has it).
        return HandleEquipmentPurchaseAttempt(equipment, null);
    }

    private static bool HandleEquipmentPurchaseAttempt(CrewmanEquipmentBase equipment, Crewman selectedCrewman)
    {
        try
        {
            if (equipment == null) return true;

            long locationId = LocationTable.GetShopPurchaseLocation($"{equipment.GetGearType()}:{equipment.name}");
            if (locationId < 0) return true; // not tracked by Archipelago

            if (selectedCrewman != null && selectedCrewman.GetEquippedFor(equipment.GetGearType()) == equipment)
            {
                return false; // already equipped, matches vanilla's own no-op for that case
            }

            bool alreadyChecked = ArchipelagoClient.ServerData.CheckedLocations.Contains(locationId);
            if (!alreadyChecked)
            {
                ArchipelagoConsole.LogMessage($"Checked '{equipment.name}' for the first time.");
                Plugin.BepinLogger.LogMessage($"Purchase-check for equipment '{equipment.name}'. Sending location check {locationId}.");
                Plugin.ArchipelagoClient.CheckLocation(locationId);
            }

            if (ItemRewarder.IsCrewEquipmentUnlocked(equipment.GetGearType(), equipment.name))
            {
                return true; // unlocked via Archipelago - let vanilla purchase/equip run normally
            }

            ArchipelagoConsole.LogMessage($"'{equipment.name}' isn't unlocked yet - receive it via Archipelago first.");
            return false; // not yet unlocked: buying never actually equips it
        }
        catch (System.Exception ex)
        {
            Plugin.BepinLogger.LogError($"Error in ShopHooks.HandleEquipmentPurchaseAttempt: {ex}");
            return false;
        }
    }
}
