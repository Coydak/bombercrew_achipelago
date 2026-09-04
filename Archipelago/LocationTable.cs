using System.Collections.Generic;

namespace BC_archipelago.Archipelago;

/// <summary>
/// Maps Bomber Crew missions and events to Archipelago location IDs.
/// These IDs must match the location IDs defined in the Bomber Crew Archipelago world.
/// </summary>
public static class LocationTable
{
    /// <summary>
    /// Campaign mission completion locations, keyed by the in-game mission asset name.
    /// Populate this with the real Archipelago location IDs from the world definition.
    /// </summary>
    public static readonly Dictionary<string, long> MissionCompletionByName = new()
    {
        // Example placeholder entries. Replace with real mission names and AP IDs.
        // { "Mission_01", 91001 },
        // { "Mission_02", 91002 },
    };

    /// <summary>
    /// Optional secondary objective locations, keyed by a composite "MissionName:ObjectiveTag".
    /// </summary>
    public static readonly Dictionary<string, long> SecondaryObjective = new()
    {
        // { "Mission_01:PhotoRecon", 92001 },
    };

    /// <summary>
    /// Looks up the Archipelago location ID for a completed campaign mission.
    /// Returns -1 if the mission is not part of the randomizer.
    /// </summary>
    public static long GetMissionCompletionLocation(string missionName)
    {
        return string.IsNullOrEmpty(missionName) || !MissionCompletionByName.TryGetValue(missionName, out var id)
            ? -1
            : id;
    }
}
