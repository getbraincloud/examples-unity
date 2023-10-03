using UnityEngine;

public class SafeArea : MonoBehaviour
{
    private Canvas canvas = null;
    private RectTransform rectTransform = null;

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        rectTransform = GetComponent<RectTransform>();
    }

    private void Start()
    {
        var safeArea = Screen.safeArea;
        var anchorMin = safeArea.position;
        var anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= canvas.pixelRect.width;
        anchorMin.y /= canvas.pixelRect.height;
        anchorMax.x /= canvas.pixelRect.width;
        anchorMax.y /= canvas.pixelRect.height;

        
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
    }

    private void OnDestroy()
    {
        canvas = null;
    }
}
