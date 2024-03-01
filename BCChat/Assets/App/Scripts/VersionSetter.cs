using TMPro;
using UnityEngine;

/// <summary>
/// Place on any <see cref="TMP_Text"/> to display which version of brainCloud is being used.
/// </summary>
public class VersionSetter : MonoBehaviour
{
    private void Start()
    {
        TextMeshProUGUI versionText = GetComponent<TextMeshProUGUI>();

        versionText.text = $"v{BrainCloud.Version.GetVersion()}";
    }
}
