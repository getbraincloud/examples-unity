
using TMPro;
using UnityEngine;

public class VersionPrefab : MonoBehaviour
{
    [SerializeField] private TMP_Text _appVersionText, _brainCloudVersionText;

    private void Start()
    {
        _appVersionText.text = "App:" + Application.version;
        _brainCloudVersionText.text = "bC:" + BCManager.Instance.bc.Client.BrainCloudClientVersion;
    }
}