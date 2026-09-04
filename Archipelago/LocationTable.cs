using System.Collections.Generic;

namespace BC_archipelago.Archipelago;

/// <summary>
/// Maps Bomber Crew missions and events to Archipelago location IDs.
/// These IDs must match the location IDs defined in the Bomber Crew Archipelago world.
///
/// Mission reference names were extracted at runtime from the game's CampaignStructure
/// ScriptableObject assets (MainCampaign and DLCMP01_Campaign). The main campaign is a
/// fixed sequence of 7 chapters, each gated behind the previous chapter's key mission
/// tag: C0N_M01..M04 (regular missions) plus C0N_KEY (unlocks the "C0N_KEY" tag that the
/// next chapter's missions require), starting from BombRunTraining (unlocks "BRT") and
/// ending with the final mission C08_KEY.
/// </summary>
public static class LocationTable
{
    private const long MainCampaignBase = 9200000;
    private const long Dlc01Base = 9200100;

    /// <summary>
    /// Campaign mission completion locations, keyed by the in-game mission asset name.
    /// </summary>
    public static readonly Dictionary<string, long> MissionCompletionByName = new()
    {
        // Main campaign: tutorial, 7 chapters of 4 missions + 1 key mission each, final key mission.
        { "BombRunTraining", MainCampaignBase + 0 },

        { "C01_M01", MainCampaignBase + 1 },
        { "C01_M02", MainCampaignBase + 2 },
        { "C01_M03", MainCampaignBase + 3 },
        { "C01_M04", MainCampaignBase + 4 },
        { "C01_KEY", MainCampaignBase + 5 },

        { "C02_M01", MainCampaignBase + 6 },
        { "C02_M02", MainCampaignBase + 7 },
        { "C02_M03", MainCampaignBase + 8 },
        { "C02_M04", MainCampaignBase + 9 },
        { "C02_KEY", MainCampaignBase + 10 },

        { "C03_M01", MainCampaignBase + 11 },
        { "C03_M02", MainCampaignBase + 12 },
        { "C03_M03", MainCampaignBase + 13 },
        { "C03_M04", MainCampaignBase + 14 },
        { "C03_KEY", MainCampaignBase + 15 },

        { "C04_M01", MainCampaignBase + 16 },
        { "C04_M02", MainCampaignBase + 17 },
        { "C04_M03", MainCampaignBase + 18 },
        { "C04_M04", MainCampaignBase + 19 },
        { "C04_KEY", MainCampaignBase + 20 },

        { "C05_M01", MainCampaignBase + 21 },
        { "C05_M02", MainCampaignBase + 22 },
        { "C05_M03", MainCampaignBase + 23 },
        { "C05_M04", MainCampaignBase + 24 },
        { "C05_KEY", MainCampaignBase + 25 },

        { "C06_M01", MainCampaignBase + 26 },
        { "C06_M02", MainCampaignBase + 27 },
        { "C06_M03", MainCampaignBase + 28 },
        { "C06_M04", MainCampaignBase + 29 },
        { "C06_KEY", MainCampaignBase + 30 },

        { "C07_M01", MainCampaignBase + 31 },
        { "C07_M02", MainCampaignBase + 32 },
        { "C07_M03", MainCampaignBase + 33 },
        { "C07_M04", MainCampaignBase + 34 },
        { "C07_KEY", MainCampaignBase + 35 },

        { "C08_KEY", MainCampaignBase + 36 }, // final mission

        // DLC1 campaign: 6 missions + 1 key mission, all in a single linear chain.
        { "DLCMP01_C01_M01", Dlc01Base + 0 },
        { "DLCMP01_C01_M02", Dlc01Base + 1 },
        { "DLCMP01_C01_M03", Dlc01Base + 2 },
        { "DLCMP01_C01_M04", Dlc01Base + 3 },
        { "DLCMP01_C01_M05", Dlc01Base + 4 },
        { "DLCMP01_C01_M06", Dlc01Base + 5 },
        { "DLCMP01_C01_KEY", Dlc01Base + 6 },

        // Confirmed live in-game (reference name not present in the CampaignStructure.m_allMissions
        // dump, so it's likely a standalone fixed intro mission rather than part of the procedural
        // mission pool): the very first mission played on a new campaign.
        { "FIRST_MISSION", MainCampaignBase + 200 },
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
