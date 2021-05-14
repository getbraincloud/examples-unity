using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum ToggleType {Reliable,Ordered}
public class ToggleCallback : MonoBehaviour
{
    public ToggleType ToggleType;

    private Toggle _toggle;

    private void Awake()
    {
        _toggle = GetComponent<Toggle>();
    }

    //called from unity's toggle component
    public void OnValueChange()
    {
        Settings.SetPlayerPrefBool(ToggleType.ToString(),_toggle.isOn);
    }
}
