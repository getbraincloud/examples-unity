using BrainCloud;
using TMPro;
using UnityEngine;

/// <summary>
/// Sends information when a dropdown menu value is changed based on the DropdownMenu enum selected
/// </summary>
public enum DropdownMenus{Socket,Channel,Compression, LobbyType}
public class DropdownMenuCallback : MonoBehaviour
{
    public DropdownMenus TargetMenu;
    private TMP_Dropdown _dropdown;

    private void Awake()
    {
        _dropdown = GetComponent<TMP_Dropdown>();
        OnValueChange();
    }
    //Called from dropdown menu's value change event
    public void OnValueChange()
    {
        switch (TargetMenu)
        {
            case DropdownMenus.Channel:
                PlayerPrefs.SetInt(Settings.ChannelKey, _dropdown.value);
                break;
            case DropdownMenus.Socket:
                StateManager.Instance.Protocol = (RelayConnectionType)_dropdown.value + 1;
                break;
            case DropdownMenus.Compression:
                BrainCloudManager.Instance._relayCompressionType = (RelayCompressionTypes)_dropdown.value;
                GameManager.Instance.SendUpdateRelayCompressionType();
                break;
            case DropdownMenus.LobbyType:
                BrainCloudManager.Instance.LobbyType = (RelayLobbyTypes) _dropdown.value;
                 break;
        }   
    }
}
