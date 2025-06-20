using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PaintSplat : NetworkBehaviour
{
    [SerializeField] private Image _image;
    private RectTransform _rect;

    private readonly SyncVar<Color> _color = new SyncVar<Color>();
    private readonly SyncVar<Vector2> _anchoredPosition = new SyncVar<Vector2>();

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        // Register listeners
        _color.OnChange += OnColorChanged;
        _anchoredPosition.OnChange += OnPositionChanged;

        // Apply current value (handles late joiners)
        OnColorChanged(Color.clear, _color.Value, false);
        OnPositionChanged(Vector2.zero, _anchoredPosition.Value, false);


        StartCoroutine(WaitAndReparent());
    }

    private IEnumerator WaitAndReparent()
    {
        float timeout = 1.5f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            PlayerCursor[] cursors = FindObjectsOfType<PlayerCursor>();
            foreach (var cursor in cursors)
            {
                if (cursor.clientId == Owner.ClientId)
                {
                    transform.SetParent(cursor.Container, false);
                    Debug.Log($"[PaintSplat] Reparented to PlayerCursor {cursor.clientId}");
                    yield break;
                }
            }

            elapsed += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        Debug.LogWarning($"[PaintSplat] Failed to reparent to PlayerCursor for owner {Owner.ClientId} after timeout");
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        _color.OnChange -= OnColorChanged;
        _anchoredPosition.OnChange -= OnPositionChanged;
    }

    public void OnAnimComplete()
    {

    }

    public void Initialize(Vector2 position, Color color)
    {
        _anchoredPosition.Value = position;
        _color.Value = color;
        
        // Ensure visual update immediately on host/server
        OnColorChanged(Color.clear, color, true);
        OnPositionChanged(Vector2.zero, position, true);

        Debug.Log($"[PaintSplat] Initialize");
    }

    private void OnColorChanged(Color oldColor, Color newColor, bool asServer)
    {
        if (_image != null)
            _image.color = newColor;
        
        Debug.Log($"[PaintSplat] OnColorChanged {newColor}");
    }

    private void OnPositionChanged(Vector2 oldPos, Vector2 newPos, bool asServer)
    {
        if (_rect != null)
            _rect.anchoredPosition = newPos;

        Debug.Log($"[PaintSplat] OnPositionChanged {newPos}");
    }
}
