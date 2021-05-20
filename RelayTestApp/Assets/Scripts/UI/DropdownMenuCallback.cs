using BrainCloud;
using TMPro;
using UnityEngine;

/// <summary>
/// Sends information when a dropdown menu value is changed based on the DropdownMenu enum selected
/// </summary>
public enum DropdownMenus{Socket,Channel}
public class DropdownMenuCallback : MonoBehaviour
{
    public DropdownMenus TargetMenu;
    private TMP_Dropdown _dropdown;

    private void Awake()
    {
        _dropdown = GetComponent<TMP_Dropdown>();
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
                StateManager.Instance.protocol = (RelayConnectionType)_dropdown.value + 1;
                break;
        }   
    }
}
