extern alias game;

using BC_archipelago.Archipelago;
using game;
using HarmonyLib;

namespace BC_archipelago.BomberCrew;

/// <summary>
/// Harmony patches that detect local player death and broadcast DeathLink events.
/// </summary>
public static class DeathLinkHooks
{
    private static bool patched;

    public static void Apply()
    {
        if (patched) return;

        var harmony = new Harmony(Plugin.PluginGUID);
        harmony.PatchAll(typeof(DeathLinkHooks));
        patched = true;

        Plugin.BepinLogger.LogDebug("DeathLinkHooks applied.");
    }

    /// <summary>
    /// Fires when a crewman dies in-mission.
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CrewmanLifeStatus), "InstantKill")]
    private static void CrewmanInstantKillPostfix()
    {
        BroadcastDeathLink("A crewman was killed in action.");
    }

    /// <summary>
    /// Fires when the bomber is destroyed.
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(MissionLog), "SetBomberDestroyed")]
    private static void BomberDestroyedPostfix()
    {
        BroadcastDeathLink("The bomber was destroyed.");
    }

    private static void BroadcastDeathLink(string cause)
    {
        try
        {
            if (!ArchipelagoClient.Authenticated) return;

            var handler = Plugin.ArchipelagoClient?.DeathLinkHandler;
            if (handler == null) return;

            handler.SendDeathLink(cause);
        }
        catch (System.Exception ex)
        {
            Plugin.BepinLogger.LogError($"Error broadcasting death link: {ex}");
        }
    }
}
