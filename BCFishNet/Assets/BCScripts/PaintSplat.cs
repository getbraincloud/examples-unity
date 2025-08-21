using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PaintSplat : MonoBehaviour
{
    [SerializeField] private Image _image;
    private RectTransform _rect;
    public RectTransform RectTransform => _rect;
    public Color Color => _color;
    private Color _color;
    private Vector2 _anchoredPosition;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

    public void OnStart()
    {
        StartCoroutine(WaitAndReparent());
    }

    public void OnAnimComplete()
    {

    }

    private IEnumerator WaitAndReparent()
    {
        float timeout = 1.5f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            Transform container = UIContainerCache.GetCursorContainer();
            if (container != null)
            {
                transform.SetParent(container, false);

                yield break;
            }

            elapsed += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }
    }

    public void Initialize(Vector2 position, Color color, float rotation, float scale)
    {
        _anchoredPosition = position;
        _color = color;

        // Ensure visual update immediately on host/server
        OnColorChanged(Color.clear, color, true);
        OnPositionChanged(Vector2.zero, position, true);

        // Apply rotation and scale
        var rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.localRotation = Quaternion.Euler(0, 0, rotation);
            rect.localScale = Vector3.one * scale;
            Debug.Log($"Initialized PaintSplat at {position} with color {color}, rotation {rotation}, scale {scale}");
        }
    }

    private void OnColorChanged(Color oldColor, Color newColor, bool asServer)
    {
        if (_image != null)
            _image.color = newColor;
    }

    private void OnPositionChanged(Vector2 oldPos, Vector2 newPos, bool asServer)
    {
        if (_rect != null)
            _rect.anchoredPosition = newPos;
    }
}
