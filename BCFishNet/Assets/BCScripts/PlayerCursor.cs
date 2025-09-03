using FishNet;
using FishNet.Connection;
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
    public int clientId;

    public override void OnStartClient()
    {
        base.OnStartClient();

        clientId = Owner.ClientId;

        if (IsServer)
        {
            // Only send the paint map to the joining client
            RestoreGlobalPaintMap(Owner);
        }
        _enabled = true;
    }
    void Start()
    {
        Transform parentObject = transform.parent;
        if (parentObject == null || parentObject.name != "CursorContainer")
        {
            GameObject targetParent = GameObject.Find("CursorContainer");
            transform.SetParent(targetParent.transform);
            transform.localScale = Vector3.one;
        }

        InitCursor();
        ResetPosition();
    }

    public void InitCursor()
    {
        _rect = GetComponent<RectTransform>();
        _container = transform.parent.gameObject.GetComponent<RectTransform>();

        if (IsOwner)
        {
            string profileId = BCManager.Instance.bc.Client.ProfileId;
            PlayerData playerData = PlayerListItemManager.Instance.GetPlayerDataByProfileId(profileId);
            Color newColor = playerData.Color;
            _cursorImage.color = newColor;
            Debug.Log($"[PlayerCursor] InitCursor for local player {profileId} with color {newColor}");
            
        }
    }

    private float _paintSpawnCooldown = 0.015f; // Adjust delay between spawns (in seconds)
    private float _timeSinceLastPaint = 0f;
    private bool _enabled = false;

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
        Debug.Log($"[PlayerCursor] UpdateSplatScale: {scale}");
        _splatScale = scale;
        this.transform.localScale = Vector3.one * scale;
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
        if (_cursorImage.color == Color.white)
        {
            return;
        }

        PaintSplat paint = Instantiate(_paintPrefab, _container);
        paint.Initialize(position, _cursorImage.color, rotation, scale);

        if (PlayerListItemManager.Instance.SaveGlobalPaintData(paint))
        {
            ObserversRpcSpawnPaint(position, _cursorImage.color, rotation, scale);
        }
        else
        {
            Destroy(paint.gameObject);
        }
    }

    [ObserversRpc]
    private void ObserversRpcSpawnPaint(Vector3 position, Color color, float rotation, float scale)
    {
        // Prevent duplicate spawn on server (which already spawned it)
        if (IsServer)
            return;

        PaintSplatData paintSplatData = new PaintSplatData
        {
            color = color,
            anchoredPosition = position,
            rotation = rotation,
            scale = scale
        };

        if (PlayerListItemManager.Instance.SaveGlobalPaintData(paintSplatData))
        {
            PaintSplat paint = Instantiate(_paintPrefab, _container);
            paint.Initialize(position, color, rotation, scale);
        }
    }


    public void RestoreGlobalPaintMap(NetworkConnection conn = null)
    {
        if (base.IsHost)
        {
            _enabled = false; // Disable cursor updates while restoring paint
            if (conn != null)
                StartCoroutine(RestoreGlobalPaintCoroutine_Target(conn));
            else
                StartCoroutine(RestoreGlobalPaintCoroutine());
        }
    }

    private IEnumerator RestoreGlobalPaintCoroutine()
    {
        yield return new WaitForSeconds(0.15f);

        var paintDataList = PlayerListItemManager.Instance.GetGlobalPaintData();
        Transform container = UIContainerCache.GetCursorContainer();

        Debug.Log($"[PlayerCursor] Restoring {paintDataList.Count} global paint splats");

        int count = 0;
        foreach (var data in paintDataList)
        {
            // Use saved rotation and scale if available, otherwise fallback
            float rotation = data.rotation != 0f ? data.rotation : Random.Range(0f, 360f);
            float scale = data.scale != 0f ? data.scale : 1f;
            ObserversRpcSpawnPaint(data.anchoredPosition, data.color, rotation, scale);
            count++;
            if (count % 200 == 0)
            {
                yield return null;
            }
        }
        yield return null;

        _enabled = true; // Re-enable cursor updates after restoring paint
    }

    private IEnumerator RestoreGlobalPaintCoroutine_Target(NetworkConnection conn)
    {
        yield return new WaitForSeconds(0.15f);

        var paintDataList = PlayerListItemManager.Instance.GetGlobalPaintData();
        Transform container = UIContainerCache.GetCursorContainer();

        Debug.Log($"[PlayerCursor] Restoring {paintDataList.Count} global paint splats to joining client");

        int count = 0;
        foreach (var data in paintDataList)
        {
            float rotation = data.rotation != 0f ? data.rotation : Random.Range(0f, 360f);
            float scale = data.scale != 0f ? data.scale : 1f;
            TargetRpcSpawnPaint(conn, data.anchoredPosition, data.color, rotation, scale);
            count++;
            if (count % 200 == 0)
            {
                yield return null;
            }
        }
        yield return null;

        _enabled = true;
    }

    [TargetRpc]
    private void TargetRpcSpawnPaint(NetworkConnection conn, Vector3 position, Color color, float rotation, float scale)
    {
        PaintSplatData paintSplatData = new PaintSplatData
        {
            color = color,
            anchoredPosition = position,
            rotation = rotation,
            scale = scale
        };

        if (PlayerListItemManager.Instance.SaveGlobalPaintData(paintSplatData))
        {
            PaintSplat paint = Instantiate(_paintPrefab, _container);
            paint.Initialize(position, color, rotation, scale);
        }
    }

    [ObserversRpc]
    public void ChangeColor(Color color)
    {
        _cursorImage.color = color;
    }

    public void ResetPosition()
    {
        _rect.anchoredPosition = Vector2.one * 10000f; // Move offscreen
    }
}
