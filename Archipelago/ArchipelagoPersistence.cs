extern alias game;

using System;
using System.IO;
using BC_archipelago.Utils;
using game;
using Newtonsoft.Json;

namespace BC_archipelago.Archipelago;

/// <summary>
/// Persists Archipelago progress (checked locations, progressive upgrade tiers, unlocked crew
/// equipment) alongside the game's own save file, and validates it against whichever room is
/// actually connected before trusting it.
///
/// Previously, this state lived only in memory for the process
/// lifetime - every relaunch started from scratch, relying entirely on the Archipelago server's
/// own already-checked-location bookkeeping (which starts fresh unless matched against the same
/// server/session). This ties that state to the game's own save slots instead, the same way the
/// game itself already saves and loads (see BomberCrew_Data's SaveDataContainer, hooked in
/// BomberCrew/SavePersistenceHooks.cs), so switching save files or replaying an old save doesn't
/// silently carry over (or lose) the wrong multiworld's progress.
///
/// File format: a full ArchipelagoData.ToString() JSON blob, written as
/// "&lt;persistentDataPath&gt;/&lt;same prefix+slot the game itself uses&gt;.archipelago.json" -
/// deliberately a separate companion file rather than editing the game's own save file, so a
/// corrupt/missing companion can never affect the real save.
/// </summary>
public static class ArchipelagoPersistence
{
    /// <summary>
    /// Call after SaveDataContainer.Load(int) succeeds. Restores CheckedLocations/
    /// ProgressiveUpgradeCounts/UnlockedCrewEquipment from this slot's companion file (or clears
    /// them if there isn't one - a save with no recorded Archipelago progress yet), then
    /// validates against whichever room is currently connected, if any.
    /// </summary>
    public static void OnSaveLoaded(int slotIndex)
    {
        var data = ArchipelagoClient.ServerData;

        try
        {
            var path = GetCompanionPath(slotIndex);
            var loaded = path != null && File.Exists(path)
                ? JsonConvert.DeserializeObject<ArchipelagoData>(File.ReadAllText(path))
                : null;

            data.CheckedLocations = loaded?.CheckedLocations ?? new();
            data.ProgressiveUpgradeCounts = loaded?.ProgressiveUpgradeCounts ?? new();
            data.UnlockedCrewEquipment = loaded?.UnlockedCrewEquipment ?? new();
            data.SaveFileSeed = loaded?.Seed;
        }
        catch (Exception ex)
        {
            Plugin.BepinLogger.LogError($"Failed to load Archipelago progress for save slot {slotIndex}: {ex}");
            data.CheckedLocations = new();
            data.ProgressiveUpgradeCounts = new();
            data.UnlockedCrewEquipment = new();
            data.SaveFileSeed = null;
        }

        ValidateAgainstCurrentRoom();
    }

    /// <summary>
    /// Call after SaveDataContainer.Save() runs. Only writes a companion file while actually
    /// connected - there's nothing meaningful to persist otherwise, and this avoids clobbering a
    /// previously-saved companion with empty data just because the player happened to save while
    /// temporarily disconnected.
    /// </summary>
    public static void OnSaveWritten(int slotIndex)
    {
        if (!ArchipelagoClient.Authenticated) return;

        try
        {
            var path = GetCompanionPath(slotIndex);
            if (path == null) return;

            File.WriteAllText(path, ArchipelagoClient.ServerData.ToString());
        }
        catch (Exception ex)
        {
            Plugin.BepinLogger.LogError($"Failed to save Archipelago progress for save slot {slotIndex}: {ex}");
        }
    }

    /// <summary>
    /// Call right after a successful Archipelago connection. Handles the case where connecting
    /// happens before a save is loaded (the usual order - the mod's overlay UI is present from
    /// the main menu) as well as after (a save loaded while already connected).
    /// </summary>
    public static void OnConnected()
    {
        ValidateAgainstCurrentRoom();
    }

    /// <summary>
    /// If we know both the room's actual seed and the seed recorded in the loaded save's
    /// companion file, and they don't match, the restored CheckedLocations/ProgressiveUpgradeCounts
    /// belong to a different multiworld - reset them instead of silently trusting the wrong
    /// progress. Does nothing if not connected yet, or nothing was ever loaded from a save.
    /// </summary>
    private static void ValidateAgainstCurrentRoom()
    {
        var data = ArchipelagoClient.ServerData;
        if (!ArchipelagoClient.Authenticated) return;
        if (string.IsNullOrEmpty(data.SaveFileSeed)) return;
        if (data.SaveFileSeed == data.Seed) return;

        ArchipelagoConsole.LogMessage("This save's stored Archipelago progress is for a different seed - starting fresh for this room.");
        data.CheckedLocations = new();
        data.ProgressiveUpgradeCounts = new();
        data.UnlockedCrewEquipment = new();
    }

    private static string GetCompanionPath(int slotIndex)
    {
        var prefix = GameFlow.Instance?.GetGameMode()?.GetSaveDataPrefix();
        if (string.IsNullOrEmpty(prefix)) return null;

        return Path.Combine(UnityEngine.Application.persistentDataPath, $"{prefix}{slotIndex}.archipelago.json");
    }
}
