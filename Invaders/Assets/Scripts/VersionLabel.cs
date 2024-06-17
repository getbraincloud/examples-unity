using UnityEngine;
using TMPro;

public class VersionLabel : MonoBehaviour
{
    enum VersionType
    {
        APP,
        BRAINCLOUD
    }

    [SerializeField]
    VersionType versionType = VersionType.APP;

    private void Awake()
    {
        switch (versionType)
        {
            case VersionType.APP:
                GetComponent<TextMeshProUGUI>().text = "v" + Application.version;
                break;
            case VersionType.BRAINCLOUD:
                GetComponent<TextMeshProUGUI>().text = "" + BrainCloud.Version.GetVersion();
                break;
        }
    }
}
