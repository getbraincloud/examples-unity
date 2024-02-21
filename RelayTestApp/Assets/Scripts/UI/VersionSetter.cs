using UnityEngine;

public class VersionSetter : MonoBehaviour
{
    [SerializeField] TMPro.TMP_Text versionText; 

    void Start()
    {
        string versionNum = BrainCloud.Version.GetVersion();

        versionText.text = "Version: " + versionNum + " - dev"; 
    }
}
