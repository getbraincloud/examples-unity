
using TMPro;
using UnityEngine;

public class VersionPrefab : MonoBehaviour
{
    [SerializeField] private TMP_Text _appVersionText, _brainCloudVersionText;
    void OnEnable()
    {
        _appVersionText.text = "App:" + Application.version;
        _brainCloudVersionText.text = "bC:" + BCManager.Wrapper.Client.BrainCloudClientVersion;
    }
}