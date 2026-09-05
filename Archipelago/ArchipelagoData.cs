using System.Collections.Generic;
using Newtonsoft.Json;

namespace BC_archipelago.Archipelago;

public class ArchipelagoData
{
    public string Uri;
    public string SlotName;
    public string Password;
    public int Index;

    public List<long> CheckedLocations;

    /// <summary>
    /// Highest tier unlocked so far for each progressive bomber upgrade line (see ItemRewarder's
    /// ProgressiveLines), keyed by line id. Receiving a copy of a line's item bumps this by one;
    /// it only ever grows, so any tier at or below it stays purchasable in the shop forever (see
    /// ShopHooks.AttemptPurchasePrefix / ItemRewarder.IsProgressiveUpgradeUnlocked) - the game's
    /// own save data only tracks what's currently equipped, not which tiers have been unlocked.
    /// </summary>
    public Dictionary<string, int> ProgressiveUpgradeCounts = new();

    /// <summary>
    /// Crew equipment unlocked for purchase so far, keyed the same way as CrewEquipment item
    /// payloads ("gearType:equipmentName"). Receiving a crew equipment item only unlocks it here
    /// (see ItemRewarder.ApplyCrewEquipment) rather than equipping it directly - the player then
    /// equips it (or re-equips any other previously-unlocked piece, freely, any number of times)
    /// through the normal crew quarters purchase flow (see ShopHooks.
    /// HandleEquipmentPurchaseAttempt / ItemRewarder.IsCrewEquipmentUnlocked).
    /// </summary>
    public HashSet<string> UnlockedCrewEquipment = new();

    /// <summary>
    /// seed for this archipelago data. Can be used when loading a file to verify the session the player is trying to
    /// load is valid to the room it's connecting to. [JsonProperty] forces this private field into
    /// ToString()'s JSON (Newtonsoft only serializes public members by default) so it round-trips
    /// through ArchipelagoPersistence's save-file companion.
    /// </summary>
    [JsonProperty]
    private string seed;

    public string Seed => seed;

    /// <summary>
    /// The seed found in the save file's Archipelago companion data at load time (see
    /// ArchipelagoPersistence), not yet validated against whichever room we actually connect to.
    /// Compared against <see cref="Seed"/> once connected; on a mismatch, CheckedLocations/
    /// ProgressiveUpgradeCounts are reset instead of trusting progress from a different seed.
    /// Not itself persisted - it only exists to carry that comparison across the gap between
    /// loading a save and connecting (whichever happens first).
    /// </summary>
    [JsonIgnore]
    public string SaveFileSeed;

    private Dictionary<string, object> slotData;

    public bool NeedSlotData => slotData == null;

    /// <summary>
    /// Whether DeathLink was enabled by the Archipelago slot data.
    /// </summary>
    public bool DeathLinkEnabled { get; private set; }

    public ArchipelagoData()
    {
        Uri = "localhost";
        SlotName = "Player1";
        CheckedLocations = new();
    }

    public ArchipelagoData(string uri, string slotName, string password)
    {
        Uri = uri;
        SlotName = slotName;
        Password = password;
        CheckedLocations = new();
    }

    /// <summary>
    /// assigns the slot data and seed to our data handler. any necessary setup using this data can be done here.
    /// </summary>
    /// <param name="roomSlotData">slot data of your slot from the room</param>
    /// <param name="roomSeed">seed name of this session</param>
    public void SetupSession(Dictionary<string, object> roomSlotData, string roomSeed)
    {
        slotData = roomSlotData;
        seed = roomSeed;

        if (roomSlotData != null &&
            roomSlotData.TryGetValue("death_link", out var deathLinkValue) &&
            deathLinkValue != null)
        {
            DeathLinkEnabled = bool.TryParse(deathLinkValue.ToString(), out var parsed) && parsed;
        }
    }

    /// <summary>
    /// returns the object as a json string to be written to a file which you can then load
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }
}