using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gameframework;

public class ControlsDisplay : BaseBehaviour
{
    public static eControlDisplayTypes GetControlsDisplay()
    {
        eControlDisplayTypes displayIndex = eControlDisplayTypes.Invalid;
#if UNITY_WEBGL
        displayIndex = eControlDisplayTypes.MouseAndKeyboard;
#elif UNITY_IOS || UNITY_ANDROID
        displayIndex = eControlDisplayTypes.Mobile;
#elif UNITY_STANDALONE
        displayIndex = eControlDisplayTypes.Keyboard; // MouseAndKeyboard
#endif
        return displayIndex;
    }
    private void Start()
    {
        int displayIndex = (int)GetControlsDisplay();
        for (int i = 0; i < transform.childCount; ++i)
        {
            transform.GetChild(i).gameObject.SetActive(i == displayIndex);
        }
    }
}

// STAYS IN SYNC WITH THE UI 
public enum eControlDisplayTypes
{
    Invalid = -1,

    Keyboard,
    MouseAndKeyboard,
    Mobile,
    Controller,

    MaxControlDisplayTypes
}
