using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 

public class VersionSetter : MonoBehaviour
{
    [SerializeField] TMPro.TMP_Text versionText; 

    void Start()
    {
        string versionNum = BrainCloudManager.Instance.Wrapper.Client.BrainCloudClientVersion;

        versionText.text = "Version: " + versionNum + " - dev"; 
    }
}
