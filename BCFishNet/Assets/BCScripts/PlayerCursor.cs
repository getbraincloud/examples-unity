using FishNet;
using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PlayerCursor : NetworkBehaviour
{
    [SerializeField]
    private Image _cursorImage;
    private RectTransform _rect;
    private RectTransform _container;

    public RectTransform Container => _container;

    [SerializeField]
    private Shockwave _shockwavePrefab;

    [SerializeField]
    private PaintSplat _paintPrefab;
    public bool areWeOwner;
    public int clientId;

    public override void OnStartClient()
    {
        base.OnStartClient();

        areWeOwner = IsOwner;
        clientId = Owner.ClientId;

        Transform parentObject = transform.parent;
        if (parentObject == null || parentObject.name != "CursorContainer")
        {
            GameObject targetParent = GameObject.Find("CursorContainer");
            transform.SetParent(targetParent.transform);
            transform.localScale = Vector3.one;
        }

        InitCursor();
        ResetPosition();

        if (IsServer)
        {
            RestoreGlobalPaintMap();
        }
    }

    public void InitCursor()
    {
        _rect = GetComponent<RectTransform>();
        _container = transform.parent.gameObject.GetComponent<RectTransform>();
    }
    private float _paintSpawnCooldown = 0.015f; // Adjust delay between spawns (in seconds)
    private float _timeSinceLastPaint = 0f;
    private bool _enabled = true;

    private Vector2 _lastPaintPosition = Vector2.positiveInfinity;
    private float _splatScale = 1f;
    void Update()
    {
        if (IsOwner && _enabled)
{
    Vector2 localMousePos;
    RectTransformUtility.ScreenPointToLocalPointInRectangle(
        _container,
        Input.mousePosition,
        null,
        out localMousePos
    );

    // 1. Check if inside the container rect
    bool insideContainer = RectTransformUtility.RectangleContainsScreenPoint(
        _container,
        Input.mousePosition,
        null
    );

    // Always move the cursor to the mouse position (local)
    _rect.anchoredPosition = localMousePos;

    // 2. Check if container is the topmost element under mouse
    bool hitContainerTopMost = false;
    if (insideContainer)
    {
        PointerEventData pointer = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, results);

        // container is "valid" only if the first raycast hit is the container itself
        if (results.Count > 0 && results[0].gameObject.transform == _container)
        {
            hitContainerTopMost = true;
        }
    }

    // ---- Painting ----
    if (Input.GetMouseButton(0) && hitContainerTopMost)
    {
        _timeSinceLastPaint += Time.deltaTime;

        if (_timeSinceLastPaint >= _paintSpawnCooldown)
        {
            if (_lastPaintPosition != localMousePos)
            {
                _timeSinceLastPaint = 0f;
                SpawnPaintServer(localMousePos, _splatScale);
                _lastPaintPosition = localMousePos;
            }
            else
            {
                // No movement → keep cooldown maxed
                _timeSinceLastPaint = _paintSpawnCooldown;
            }
        }
    }
    else
    {
        // Reset timer if not actively painting
        _timeSinceLastPaint = _paintSpawnCooldown;
        _lastPaintPosition = Vector2.positiveInfinity;
    }

    // ---- Shockwave ----
    if (Input.GetMouseButtonDown(0) && hitContainerTopMost)
    {
        SpawnShockwaveServer(localMousePos);
    }
}
    }
    
    public void UpdateSplatScale(float scale)
    {
        _splatScale = scale;
    }

    [ServerRpc]
    public void SpawnShockwaveServer(Vector2 position)
    {
        SpawnShockwave(position, _cursorImage.color);
    }

    [ObserversRpc]
    public void SpawnShockwave(Vector2 position, Color color)
    {
        Shockwave shockwave = Instantiate(_shockwavePrefab, _container);
        shockwave.SetColor(color);
        shockwave.SetPosition(position);
    }


    [ServerRpc]
    private void SpawnPaintServer(Vector2 position, float scale)
    {
        float rotation = Random.Range(0f, 360f);

        PaintSplat paint = Instantiate(_paintPrefab, _container);
        paint.Initialize(position, _cursorImage.color, rotation, scale);

        PlayerListItemManager.Instance.SaveGlobalPaintData(paint);

        ObserversRpcSpawnPaint(position, _cursorImage.color, rotation, scale);
    }


    [ObserversRpc]
    private void ObserversRpcSpawnPaint(Vector3 position, Color color, float rotation, float scale)
    {
        // Prevent duplicate spawn on server (which already spawned it)
        if (IsServer)
            return;

        PaintSplat paint = Instantiate(_paintPrefab, _container);
        paint.Initialize(position, color, rotation, scale);

        PlayerListItemManager.Instance.SaveGlobalPaintData(paint);
    }

    public void RestoreGlobalPaintMap()
    {
        _enabled = false; // Disable cursor updates while restoring paint
        StartCoroutine(RestoreGlobalPaintCoroutine());
    }

    private IEnumerator RestoreGlobalPaintCoroutine()
    {
        var paintDataList = PlayerListItemManager.Instance.GetGlobalPaintData();
        Transform container = UIContainerCache.GetCursorContainer();

        Debug.Log($"[PlayerCursor] Restoring {paintDataList.Count} global paint splats");

        foreach (var data in paintDataList)
        {
            // Use saved rotation and scale if available, otherwise fallback
            float rotation = data.rotation != 0f ? data.rotation : Random.Range(0f, 360f);
            float scale = data.scale != 0f ? data.scale : 1f;
            ObserversRpcSpawnPaint(data.anchoredPosition, data.color, rotation, scale);
        }
        yield return null;

        _enabled = true; // Re-enable cursor updates after restoring paint
    }

    [ObserversRpc]
    public void ChangeColor(Color color)
    {
        _cursorImage.color = color;
    }

    public void ResetPosition()
    {
        _rect.anchoredPosition = Vector2.zero;
    }
}
