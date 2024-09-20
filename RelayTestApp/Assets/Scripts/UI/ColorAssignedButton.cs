using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ColorAssignedButton : MonoBehaviour
{
    [SerializeField, Range(0, 7)]
    private int ButtonColor;

    public void Start()
    {
        transform.GetChild(0).GetComponent<Image>().color = GameManager.ReturnUserColor(ButtonColor);
    }

    public void ChangeColor()
    {
        GameManager.Instance.UpdateLocalColorChange(ButtonColor);
    }
}
