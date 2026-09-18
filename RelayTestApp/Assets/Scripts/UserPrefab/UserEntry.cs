using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class UserEntry : MonoBehaviour
{
    public TMP_Text UsernameText;
    public Image UserDotImage;
    public Button UserDotButton; // opens the colour popup — only wired/interactable on the local player's own row
    public Toggle UserMaskToggle;
    // "HOST" text badge — not a crown icon, matching the other RelayTestApp clients' convention.
    public TMP_Text HostBadgeText;
    public TMP_Text YouBadgeText;
    // pill containers to toggle — toggling the text alone would leave the background always/never visible
    public GameObject HostBadgeRoot;
    public GameObject YouBadgeRoot;
    public TMP_Text StatusText;
    public TMP_Text PingText;
    public TMP_Text RankText;
}
