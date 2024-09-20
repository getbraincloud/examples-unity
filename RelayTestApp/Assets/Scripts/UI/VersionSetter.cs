using UnityEngine;

public class VersionSetter : MonoBehaviour
{
    [SerializeField] TMPro.TMP_Text versionText; 

    void Start()
    {
        string versionNum = Application.version;

        versionText.text = "Version: " + versionNum; 
    }
}
