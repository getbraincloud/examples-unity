using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VersionSetter : MonoBehaviour
{
    [SerializeField] BCConfig bcConfig; 
    [SerializeField] Text versionText;

    private void Awake()
    {
        BrainCloudWrapper bc = bcConfig.GetBrainCloud();
        string versionNum = bc.Client.BrainCloudClientVersion;

        versionText.text = "Version: " + versionNum;
    }
}
