using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BC_archipelago.Archipelago;
using BC_archipelago.BomberCrew;
using BC_archipelago.Utils;
using UnityEngine;

namespace BC_archipelago;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
public class Plugin : BaseUnityPlugin
{
    public const string PluginGUID = "com.bombercrew.archipelago";
    public const string PluginName = "BC_archipelago";
    public const string PluginVersion = "0.1.0";

    public const string ModDisplayInfo = $"{PluginName} v{PluginVersion}";
    private const string APDisplayInfo = $"Archipelago v{ArchipelagoClient.APVersion}";
    public static ManualLogSource BepinLogger;
    public static ArchipelagoClient ArchipelagoClient;

    // BepInEx config entries
    public static ConfigEntry<string> ConfigHost;
    public static ConfigEntry<string> ConfigSlotName;
    public static ConfigEntry<string> ConfigPassword;
    public static ConfigEntry<bool> ConfigDeathLinkDefault;

    private void Awake()
    {
        // Plugin startup logic
        BepinLogger = Logger;

        BindConfig();

        ArchipelagoClient = new ArchipelagoClient();
        ArchipelagoConsole.Awake();

        MissionHooks.Apply();
        DeathLinkHooks.Apply();

        ArchipelagoConsole.LogMessage($"{ModDisplayInfo} loaded!");
    }

    /// <summary>
    /// Binds BepInEx configuration entries so connection defaults persist between launches.
    /// </summary>
    private void BindConfig()
    {
        ConfigHost = Config.Bind(
            "Connection",
            "Host",
            "localhost",
            "Archipelago server host or URI.");

        ConfigSlotName = Config.Bind(
            "Connection",
            "SlotName",
            "Player1",
            "Your Archipelago slot / player name.");

        ConfigPassword = Config.Bind(
            "Connection",
            "Password",
            "",
            "Archipelago room password (leave blank if none).");

        ConfigDeathLinkDefault = Config.Bind(
            "Gameplay",
            "DeathLinkDefault",
            false,
            "Whether DeathLink should be enabled by default when slot data does not specify it.");

        // Seed the connection data from config so the GUI is pre-filled.
        ArchipelagoClient.ServerData.Uri = ConfigHost.Value;
        ArchipelagoClient.ServerData.SlotName = ConfigSlotName.Value;
        ArchipelagoClient.ServerData.Password = ConfigPassword.Value;
    }

    private void OnGUI()
    {
        // show the mod is currently loaded in the corner
        GUI.Label(new Rect(16, 16, 300, 20), ModDisplayInfo);
        ArchipelagoConsole.OnGUI();

        string statusMessage;
        // show the Archipelago Version and whether we're connected or not
        if (ArchipelagoClient.Authenticated)
        {
            // if your game doesn't usually show the cursor this line may be necessary
            // Cursor.visible = false;

            statusMessage = " Status: Connected";
            GUI.Label(new Rect(16, 50, 300, 20), APDisplayInfo + statusMessage);
        }
        else
        {
            // if your game doesn't usually show the cursor this line may be necessary
            // Cursor.visible = true;

            statusMessage = " Status: Disconnected";
            GUI.Label(new Rect(16, 50, 300, 20), APDisplayInfo + statusMessage);
            GUI.Label(new Rect(16, 70, 150, 20), "Host: ");
            GUI.Label(new Rect(16, 90, 150, 20), "Player Name: ");
            GUI.Label(new Rect(16, 110, 150, 20), "Password: ");

            ArchipelagoClient.ServerData.Uri = GUI.TextField(new Rect(150, 70, 150, 20),
                ArchipelagoClient.ServerData.Uri);
            ArchipelagoClient.ServerData.SlotName = GUI.TextField(new Rect(150, 90, 150, 20),
                ArchipelagoClient.ServerData.SlotName);
            ArchipelagoClient.ServerData.Password = GUI.TextField(new Rect(150, 110, 150, 20),
                ArchipelagoClient.ServerData.Password);

            // requires that the player at least puts *something* in the slot name
            if (GUI.Button(new Rect(16, 130, 100, 20), "Connect") &&
                !ArchipelagoClient.ServerData.SlotName.IsNullOrWhiteSpace())
            {
                // Persist the values the user typed back to config.
                ConfigHost.Value = ArchipelagoClient.ServerData.Uri;
                ConfigSlotName.Value = ArchipelagoClient.ServerData.SlotName;
                ConfigPassword.Value = ArchipelagoClient.ServerData.Password;

                ArchipelagoClient.Connect();
            }
        }
        // this is a good place to create and add a bunch of debug buttons
    }

    private void Update()
    {
        ArchipelagoClient?.ItemRewarder?.Update();

        if (BomberCrew.GameState.IsInMission)
        {
            ArchipelagoClient?.DeathLinkHandler?.KillPlayer();
        }
    }
}
