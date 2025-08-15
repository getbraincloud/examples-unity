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
            Debug.LogWarning($"Content height {contentHeight} exceeds max height {maxHeight}. Adjusting to max height.");

            Vector2 size = viewport.sizeDelta;
            size.y = 0;
            viewport.sizeDelta = size;
            Debug.Log($"Setting viewport height to: 0 (due to content exceeding max height)");
        }
        else
        {

            Vector2 size = viewport.sizeDelta;
            size.y = newHeight;
            viewport.sizeDelta = size;
            Debug.Log($"Setting viewport height to: {newHeight}");
        }

        layoutElement.preferredHeight = Mathf.Max(newHeight, MIN_HEIGHT);
    }
}