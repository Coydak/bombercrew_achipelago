extern alias game;

using BC_archipelago.Archipelago;
using game;
using HarmonyLib;

namespace BC_archipelago.BomberCrew;

/// <summary>
/// Hooks the game's own save/load lifecycle (SaveDataContainer) to tie Archipelago progress
/// (checked locations, progressive upgrade tiers) to each save slot - see
/// Archipelago/ArchipelagoPersistence.cs for the actual read/write/validation logic.
/// </summary>
public static class SavePersistenceHooks
{
    private static bool patched;

    public static void Apply()
    {
        if (patched) return;

        var harmony = new Harmony(Plugin.PluginGUID);
        harmony.PatchAll(typeof(SavePersistenceHooks));
        patched = true;

        Plugin.BepinLogger.LogDebug("SavePersistenceHooks applied.");
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SaveDataContainer), "Load", typeof(int))]
    private static void LoadPostfix(int slotIndex, bool __result)
    {
        if (!__result) return;

        ArchipelagoPersistence.OnSaveLoaded(slotIndex);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SaveDataContainer), "Save")]
    private static void SavePostfix(SaveDataContainer __instance)
    {
        ArchipelagoPersistence.OnSaveWritten(__instance.GetCurrentSlot());
    }
}
