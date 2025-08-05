using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateUIZ : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        _uiElement = this.gameObject;
    }

    // Update is called once per frame
    void Update()
    {
        if (_uiElement != null)
        {
            _uiElement.transform.Rotate(0, 0, -0.15f);
        }
    }

    private GameObject _uiElement;
}
