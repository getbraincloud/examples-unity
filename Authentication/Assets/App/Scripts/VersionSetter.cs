using TMPro;
using UnityEngine;

public class VersionSetter : MonoBehaviour
{
    private void Start()
    {
        TextMeshProUGUI versionText = GetComponent<TextMeshProUGUI>();

        versionText.text = $"v{BCManager.Wrapper.Client.BrainCloudClientVersion}";
    }
}
