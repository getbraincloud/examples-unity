using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ColorAssignedButton : MonoBehaviour
{
    public GameColors ButtonColor;

    public void Start()
    {
        transform.GetChild(0).GetComponent<Image>().color = GameManager.ReturnUserColor(ButtonColor);
    }

    public void ChangeColor()
    {
        GameManager.Instance.UpdateLocalColorChange(ButtonColor);
    }
}
