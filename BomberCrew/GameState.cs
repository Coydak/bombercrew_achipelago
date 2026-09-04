extern alias game;

using System.Collections.Generic;
using System.Linq;
using game;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BC_archipelago.BomberCrew;

/// <summary>
/// Helpers for querying the current state of Bomber Crew without directly touching Archipelago logic.
/// All game types are accessed through the <c>game</c> extern alias so they do not conflict with
/// the Newtonsoft.Json types embedded in <c>Assembly-CSharp.dll</c>.
/// </summary>
public static class GameState
{
    /// <summary>
    /// Returns true when the player is probably flying a mission right now.
    /// </summary>
    public static bool IsInMission
    {
        get
        {
            try
            {
                return GameFlow.Instance != null && GameFlow.Instance.GetIsInMissionProbable();
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Returns true when a mission is ending or the game is loading.
    /// </summary>
    public static bool IsLoadingOrTransitioning
    {
        get
        {
            try
            {
                return GameFlow.Instance != null && GameFlow.Instance.IsLoading();
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Returns true when it is safe to apply received Archipelago items (airbase / debrief, not in mission).
    /// </summary>
    public static bool CanApplyItemsSafely
    {
        get
        {
            try
            {
                if (GameFlow.Instance == null) return false;
                if (GameFlow.Instance.GetIsInMissionProbable()) return false;
                if (GameFlow.Instance.IsLoading()) return false;

                // A live bomber in the scene means we are in a mission or briefing.
                if (BomberSpawn.Instance != null && BomberSpawn.Instance.GetBomberSystems() != null)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Returns the name of the active Unity scene.
    /// </summary>
    public static string ActiveSceneName
    {
        get
        {
            try
            {
                return SceneManager.GetActiveScene().name;
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    /// <summary>
    /// Returns the reference name of the currently selected campaign mission, or empty string.
    /// This is used to map in-game missions to Archipelago location IDs.
    /// </summary>
    public static string CurrentMissionReference
    {
        get
        {
            try
            {
                var mission = GameFlow.Instance?.GetCurrentMissionInfo()?.GetCurrentlySelectedMissionDetails();
                return mission?.m_missionReferenceName ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    /// <summary>
    /// Returns all currently alive crewmen from the persistent roster.
    /// </summary>
    public static IEnumerable<Crewman> GetAliveCrewmen()
    {
        IEnumerable<Crewman> Enumerate()
        {
            var container = CrewContainer.Instance;
            if (container == null) yield break;

            int count = container.GetCurrentCrewCount();
            for (int i = 0; i < count; i++)
            {
                var crewman = container.GetCrewman(i);
                if (crewman != null && !crewman.IsDead())
                    yield return crewman;
            }
        }

        try
        {
            return Enumerate();
        }
        catch
        {
            return System.Linq.Enumerable.Empty<Crewman>();
        }
    }

    /// <summary>
    /// Returns the first alive <see cref="CrewmanAvatar"/> found in the mission scene, if any.
    /// </summary>
    public static CrewmanAvatar GetFirstAliveCrewmanAvatar()
    {
        try
        {
            return Object.FindObjectsOfType<CrewmanAvatar>()
                .FirstOrDefault(avatar => avatar != null && avatar.GetHealthState() != null && !avatar.IsBailedOut());
        }
        catch
        {
            return null;
        }
    }
}
