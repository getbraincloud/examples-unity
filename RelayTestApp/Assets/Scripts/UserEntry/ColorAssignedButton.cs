using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorAssignedButton : MonoBehaviour
{
    public GameColors ButtonColor;
    
    public void ChangeColor()
    {
        GameManager.Instance.UpdateLocalColorChange(ButtonColor);
    }
}
