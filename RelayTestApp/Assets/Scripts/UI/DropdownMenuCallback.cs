using BrainCloud;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Sends information when a dropdown menu value is changed based on the DropdownMenu enum selected
/// </summary>
public enum DropdownMenus{Socket,Channel,Compression, LobbyType}
public class DropdownMenuCallback : MonoBehaviour
{
    public DropdownMenus TargetMenu;
    public TMP_Dropdown Dropdown;

    private void OnEnable()
    {
        Dropdown = GetComponent<TMP_Dropdown>();
        OnValueChange();
    }
    //Called from dropdown menu's value change event
    public void OnValueChange()
    {
        switch (TargetMenu)
        {
            case DropdownMenus.Channel:
                PlayerPrefs.SetInt(Settings.ChannelKey, Dropdown.value);
                break;
            case DropdownMenus.Socket:
                StateManager.Instance.Protocol = (RelayConnectionType)Dropdown.value + 1;
                break;
            case DropdownMenus.Compression:
                BrainCloudManager.Instance._relayCompressionType = (RelayCompressionTypes)Dropdown.value;
                GameManager.Instance.SendUpdateRelayCompressionType();
                break;
            case DropdownMenus.LobbyType:
                BrainCloudManager.Instance.LobbyType = (RelayLobbyTypes) Dropdown.value;
                 break;
        }   
    }
}
