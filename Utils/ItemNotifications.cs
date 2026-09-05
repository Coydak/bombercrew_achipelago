using System.Collections.Generic;
using UnityEngine;

namespace BC_archipelago.Utils;

/// <summary>
/// Small fading toast stack shown whenever an item is received via Archipelago, independent of
/// the scrolling ArchipelagoConsole log (which auto-hides and isn't meant as a notification
/// feed). Follows the same immediate-mode OnGUI style as ArchipelagoConsole.
/// </summary>
public static class ItemNotifications
{
    private const float DisplaySeconds = 4f;

    // Capped so a burst of received items can never stack past the bottom of the screen -
    // oldest entries are dropped in favour of newer ones instead of overflowing.
    private const int MaxVisible = 5;

    private static readonly List<(string Text, float ExpiresAt)> active = new();

    private static Texture2D backgroundTexture;
    private static readonly GUIStyle style = new()
    {
        alignment = TextAnchor.MiddleCenter,
        fontStyle = FontStyle.Bold,
        wordWrap = true,
    };

    public static void Show(string text)
    {
        if (active.Count >= MaxVisible)
        {
            active.RemoveAt(0);
        }
        active.Add((text, Time.time + DisplaySeconds));
    }

    public static void OnGUI()
    {
        active.RemoveAll(entry => Time.time >= entry.ExpiresAt);
        if (active.Count == 0) return;

        EnsureStyleReady();

        var width = (int)(Screen.width * 0.24f);
        var height = (int)(Screen.height * 0.045f);
        var spacing = (int)(Screen.height * 0.008f);
        var x = Screen.width - width - 16;
        var y = 16;

        foreach (var entry in active)
        {
            var rect = new Rect(x, y, width, height);
            GUI.Box(rect, GUIContent.none, style);

            // Outline the text (draw offset in black, then in white) so it stays legible
            // whatever's behind it in the 3D scene, on top of the solid background box.
            var textStyle = style;
            var padded = new Rect(rect.x + 6, rect.y, rect.width - 12, rect.height);
            var prevColor = GUI.color;
            GUI.color = Color.black;
            GUI.Label(new Rect(padded.x + 1, padded.y + 1, padded.width, padded.height), entry.Text, textStyle);
            GUI.color = Color.white;
            GUI.Label(padded, entry.Text, textStyle);
            GUI.color = prevColor;

            y += height + spacing;
        }
    }

    private static void EnsureStyleReady()
    {
        if (backgroundTexture != null) return;

        backgroundTexture = new Texture2D(1, 1);
        backgroundTexture.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.75f));
        backgroundTexture.Apply();

        style.normal.background = backgroundTexture;
        style.normal.textColor = Color.white;
        style.fontSize = (int)(Screen.height * 0.02f);
    }
}
