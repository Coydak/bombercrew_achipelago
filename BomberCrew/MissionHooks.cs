extern alias game;

using game;
using HarmonyLib;

namespace BC_archipelago.BomberCrew;

/// <summary>
/// Harmony patches that detect mission completion and forward location checks to the Archipelago client.
/// </summary>
public static class MissionHooks
{
    private static bool patched;

    /// <summary>
    /// Applies the mission-finish patch.
    /// </summary>
    public static void Apply()
    {
        if (patched) return;

        var harmony = new Harmony(Plugin.PluginGUID);
        harmony.PatchAll(typeof(MissionHooks));
        patched = true;

        Plugin.BepinLogger.LogDebug("MissionHooks applied.");
    }

    /// <summary>
    /// Called once <see cref="MissionFinishCriteria.EndMission"/> has run. This is the single
    /// chokepoint that handles mission success, abort, and bomber destruction.
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(MissionFinishCriteria), "EndMission")]
    private static void EndMissionPostfix()
    {
        try
        {
            var currentMission = GameFlow.Instance?.GetCurrentMissionInfo();
            if (currentMission == null) return;

            var log = currentMission.GetMissionLog();
            if (log == null) return;

            // We only send location checks for successful missions.
            if (!log.IsComplete()) return;

            string missionRef = GameState.CurrentMissionReference;
            if (string.IsNullOrEmpty(missionRef)) return;

            long locationId = Archipelago.LocationTable.GetMissionCompletionLocation(missionRef);
            if (locationId < 0)
            {
                Plugin.BepinLogger.LogDebug($"Mission '{missionRef}' completed but has no Archipelago location mapped.");
                return;
            }

            Plugin.BepinLogger.LogMessage($"Mission '{missionRef}' completed. Sending location check {locationId}.");
            Plugin.ArchipelagoClient.CheckLocation(locationId);
        }
        catch (System.Exception ex)
        {
            Plugin.BepinLogger.LogError($"Error in MissionHooks.EndMissionPostfix: {ex}");
        }
    }
}
