using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class TabEntry
{
    public Button Button;
    public GameObject Content;
}

/// <summary>
/// Small reusable tab strip: N (button, content) pairs, exactly one content active at a time.
/// Used for the Main Menu's LEADERBOARD/CHAT tabs, the Lobby's CHAT/LEADERBOARDS/INFO tabs, and
/// the Lobby chat panel's THIS LOBBY/GLOBAL sub-tabs.
/// </summary>
public class TabGroupController : MonoBehaviour
{
    public TabEntry[] Tabs;
    public int DefaultIndex = 0;

    private static readonly Color ActiveColour = new Color(0.30f, 0.45f, 0.90f);
    private static readonly Color InactiveColour = new Color(0.20f, 0.22f, 0.30f);

    private void OnEnable()
    {
        for (int i = 0; i < Tabs.Length; i++)
        {
            int index = i;
            Tabs[i].Button.onClick.AddListener(() => Select(index));
        }
        Select(DefaultIndex);
    }

    private void OnDisable()
    {
        foreach (var t in Tabs)
            t.Button.onClick.RemoveAllListeners();
    }

    public void Select(int index)
    {
        for (int i = 0; i < Tabs.Length; i++)
        {
            Tabs[i].Content.SetActive(i == index);
            var colours = Tabs[i].Button.colors;
            colours.normalColor = i == index ? ActiveColour : InactiveColour;
            Tabs[i].Button.colors = colours;
        }
    }
}
