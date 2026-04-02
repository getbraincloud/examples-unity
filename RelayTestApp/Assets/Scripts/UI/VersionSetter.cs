using UnityEngine;

/// <summary>
/// Displays the application version string.
/// Place this component on a Canvas object that is always visible across all screens
/// (e.g. a persistent overlay Canvas or a root-level GameObject with DontDestroyOnLoad).
/// Using OnEnable ensures the text refreshes whenever the parent object is re-activated.
/// </summary>
public class VersionSetter : MonoBehaviour
{
    [SerializeField] TMPro.TMP_Text versionText;

    void OnEnable()
    {
        if (versionText != null)
            versionText.text = "Version: " + Application.version;
    }
}
