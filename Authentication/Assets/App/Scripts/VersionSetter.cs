using TMPro;
using UnityEngine;

public class VersionSetter : MonoBehaviour
{
    [SerializeField] private BrainCloudManager BCManager = default;

    private void Start()
    {
        TextMeshProUGUI versionText = GetComponent<TextMeshProUGUI>();

        versionText.text = $"v{BCManager.Wrapper.Client.BrainCloudClientVersion}";
    }
}
