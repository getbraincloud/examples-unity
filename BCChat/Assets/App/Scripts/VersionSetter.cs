using TMPro;
using UnityEngine;

/// <summary>
/// Place on any <see cref="TMP_Text"/> to display which version of brainCloud is being used.
/// </summary>
public class VersionSetter : MonoBehaviour
{
    [SerializeField]
    private bool showAppVersion = false;

    private void Start()
    {
        TextMeshProUGUI versionText = GetComponent<TextMeshProUGUI>();

        if(showAppVersion)
            versionText.text = $"v{Application.version}";
        else
            versionText.text = $"{BrainCloud.Version.GetVersion()}";
    }
}
