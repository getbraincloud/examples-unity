using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class ScrollViewMaxSize : MonoBehaviour
{
    public RectTransform viewport;

    public RectTransform content;
    public LayoutElement layoutElement;
    public float maxHeight = 355.0f; // cap in pixels

    private const float MIN_HEIGHT = 60.0f; // minimum height in pixels
    void LateUpdate()
    {
        // Match viewport height to content height until max is reached
        float contentHeight = content.rect.height;
        float newHeight = Mathf.Min(contentHeight, maxHeight);

        if (contentHeight > newHeight)
        {   
            Vector2 size = viewport.sizeDelta;
            size.y = 0;
            viewport.sizeDelta = size;
        }
        else
        {
            Vector2 size = viewport.sizeDelta;
            size.y = newHeight;
            viewport.sizeDelta = size;
        }

        layoutElement.preferredHeight = Mathf.Max(newHeight, MIN_HEIGHT);
    }
}