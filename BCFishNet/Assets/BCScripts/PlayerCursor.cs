using FishNet;
using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    void Update()
{
    if (IsOwner && _enabled)
    {
        Vector2 mousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_container, Input.mousePosition, null, out mousePos);

        _rect.anchoredPosition = mousePos;

        if (Input.GetMouseButton(0))  // GetMouseButton for holding
        {
            _timeSinceLastPaint += Time.deltaTime;
            if (_timeSinceLastPaint >= _paintSpawnCooldown)
            {
                _timeSinceLastPaint = 0f;

                SpawnPaintServer(mousePos);
            }
        }
        else
        {
            _timeSinceLastPaint = _paintSpawnCooldown; // Reset timer when not painting
        }

        // spawn shockwave only on initial click
        if (Input.GetMouseButtonDown(0))
        {
            SpawnShockwaveServer(mousePos);
        }
    }
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
    private void SpawnPaintServer(Vector2 position)
    {
        PaintSplat paint = Instantiate(_paintPrefab, _container);
        paint.Initialize(position, _cursorImage.color);

        PlayerListItemManager.Instance.SaveGlobalPaintData(paint);

        ObserversRpcSpawnPaint(position, _cursorImage.color);
    }

    [ObserversRpc]
    private void ObserversRpcSpawnPaint(Vector3 position, Color color)
    {
        PaintSplat paint = Instantiate(_paintPrefab, _container);
        paint.Initialize(position, color);

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
            PaintSplat paint = Instantiate(_paintPrefab, _container);
            paint.Initialize(data.anchoredPosition, data.color);

            ObserversRpcSpawnPaint(data.anchoredPosition, data.color);
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
