using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class AnimateSplatter : MonoBehaviour
{
    public Image _image;
    private RectTransform _rectTransform;

    private float lifespan = -1.0f;
    private float splatterDuration = 0.3f;
    private float fadeDuration = 2.0f;

    public Color SplatterColor
    {
        get => _image.color;
        set => _image.color = value;
    }

    private void OnEnable()
    {
        _rectTransform = GetComponent<RectTransform>();
        _rectTransform.localScale = Vector3.zero;

        _image = GetComponent<Image>();

        transform.eulerAngles = new Vector3(0, 0, Random.Range(0, 360));

        StartCoroutine(AppearAnimation());
    }

    private IEnumerator AppearAnimation()
    {
        float age = 0.0f;
        while(age <= splatterDuration)
        {
            age += Time.deltaTime;
            _rectTransform.localScale = Vector3.one * (age / splatterDuration);
            _image.color = SetAlpha(_image.color, Mathf.RoundToInt(Mathf.Max(age / splatterDuration, 0.5f) * 255));
            yield return null;
        }
        _rectTransform.localScale = Vector3.one;
        _image.color = SetAlpha(_image.color, 255);

        if(lifespan > 0)
        {
            yield return new WaitForSeconds(lifespan);
            StartCoroutine(DisappearAnimation());
        }
    }

    private IEnumerator DisappearAnimation()
    {
        float age = 0.0f;
        while(age <= fadeDuration)
        {
            age += Time.deltaTime;
            yield return null;
        }
        Destroy(this.gameObject);
    }

    private Color SetAlpha(Color oldColor, int newAlpha)
    {
        return new Color(oldColor.r, oldColor.g, oldColor.b, newAlpha);
    }
}
