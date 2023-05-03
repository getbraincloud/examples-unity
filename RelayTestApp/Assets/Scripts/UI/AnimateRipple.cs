using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// AnimateRipple manipulates image and Rect Transform scale to perform a fading ripple animation with a white circle .png image
/// </summary>
public class AnimateRipple : MonoBehaviour
{
    private float _timeMultiplier = 4;
    public Image _image;
    private RectTransform _rectTransform;

    public Color RippleColor
    {
        get => _image.color;
        set => _image.color = value;
    }
    private void OnEnable()
    {
        StartAnimating();
    }
    
    private void StartAnimating()
    {
        _rectTransform = GetComponent<RectTransform>();
        _rectTransform.localScale = Vector3.zero;
        
        _image = GetComponent<Image>();
        _image.color = new Color
        (
            _image.color.r,
            _image.color.g,
            _image.color.b,
            255 // resetting alpha
        );
        
        StartCoroutine(Ripple());
    }

    IEnumerator Ripple()
    {
        float scale = 0;
        for (float i = 2; i >= 0; i -= Time.deltaTime * _timeMultiplier)
        {
            //Start fading..
            if (i <= 1)
            {
                _image.color = new Color
                (
                    _image.color.r,
                    _image.color.g,
                    _image.color.b,
                    i
                );      
            }

            scale += Time.deltaTime * _timeMultiplier;
            _rectTransform.localScale = new Vector3(scale, scale, scale);
            yield return null;
        }

        yield return null;
        Destroy(gameObject);
    }
}
