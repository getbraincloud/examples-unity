using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class AdjustShockwaveVisibilityToggle : MonoBehaviour
{
    public TMP_Text Username;
    public Toggle Toggle;
    
    //Called from Unity's Toggle Value Change
    public void OnValueChange()
    {
        GameManager.Instance.AdjustUserShockwaveMask(Username.text,Toggle.isOn);
    }
}
