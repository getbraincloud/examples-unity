using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Shockwave : MonoBehaviour
{
    private Image _image;
    private RectTransform _rect;
    // Start is called before the first frame update
    private void Awake()
    {
        _image = GetComponent<Image>();
        _rect = GetComponent<RectTransform>();
    }

    public void OnAnimComplete()
    {
        Destroy(gameObject);
    }

    public void SetColor(Color color)
    {
        _image.color = color;
    }

    public void SetPosition(Vector2 position)
    {
        _rect.anchoredPosition = position;
    }

}
