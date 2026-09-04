extern alias game;

using BC_archipelago.Archipelago;
using BC_archipelago.Utils;
using game;
using HarmonyLib;

namespace BC_archipelago.BomberCrew;

/// <summary>
/// Harmony patches that detect local player death and broadcast DeathLink events, and the
/// game-side handler that enacts a DeathLink received from another player.
/// </summary>
public static class DeathLinkHooks
{
    private static bool patched;

    /// <summary>
    /// Set while <see cref="KillLocalCrew"/> is killing crew in response to a received DeathLink,
    /// so the InstantKill postfix below does not turn around and broadcast it right back out.
    /// </summary>
    private static bool isApplyingReceivedDeathLink;

    public static void Apply()
    {
        if (patched) return;

        var harmony = new Harmony(Plugin.PluginGUID);
        harmony.PatchAll(typeof(DeathLinkHooks));
        patched = true;

        DeathLinkHandler.OnKillRequested = KillLocalCrew;

        Plugin.BepinLogger.LogDebug("DeathLinkHooks applied.");
    }

    /// <summary>
    /// Fires when a crewman dies in-mission.
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CrewmanLifeStatus), "InstantKill")]
    private static void CrewmanInstantKillPostfix()
    {
        if (isApplyingReceivedDeathLink) return;

        BroadcastDeathLink("A crewman was killed in action.");
    }

    /// <summary>
    /// Kills every currently alive crewman in the active mission in response to an incoming
    /// DeathLink. Only meaningful mid-mission (there's no live crew to kill at base); the caller
    /// is expected to only invoke <see cref="DeathLinkHandler.KillPlayer"/> while in a mission.
    /// </summary>
    private static void KillLocalCrew(string cause)
    {
        if (!GameState.IsInMission)
        {
            Plugin.BepinLogger.LogDebug("Received DeathLink while not in a mission; nothing to kill.");
            return;
        }

        ArchipelagoConsole.LogMessage($"DeathLink received: {cause}");

        isApplyingReceivedDeathLink = true;
        try
        {
            var crewSpawner = CrewSpawner.Instance;
            if (crewSpawner == null) return;

            foreach (var pairing in crewSpawner.GetAllCrew())
            {
                var lifeStatus = pairing.m_spawnedAvatar?.GetHealthState();
                if (lifeStatus != null && !lifeStatus.IsDead())
                {
                    lifeStatus.InstantKill();
                }
            }
        }
        finally
        {
            isApplyingReceivedDeathLink = false;
        }
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
