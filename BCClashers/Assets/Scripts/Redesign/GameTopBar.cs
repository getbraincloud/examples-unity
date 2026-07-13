using TMPro;
using UnityEngine;

/// <summary>
/// Drives the persistent gameplay top bar (username / match timer host / exit).
/// Mirrors the shared MainMenu TopBar so both screens read as one UI.
/// </summary>
public class GameTopBar : MonoBehaviour
{
    [Tooltip("Shows the logged-in player's name, same as the MainMenu top bar.")]
    public TMP_Text UsernameText;

    private void Start()
    {
        if (UsernameText == null) return;

        //GameManager owns the signed-in user; fall back to the saved prefs name if it isn't up yet.
        string username = GameManager.Instance != null && GameManager.Instance.CurrentUserInfo != null
            ? GameManager.Instance.CurrentUserInfo.Username
            : PlayerPrefs.GetString(Settings.UsernameKey, string.Empty);

        UsernameText.text = username;
    }
}
