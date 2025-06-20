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
    }

    public void InitCursor()
    {
        _rect = GetComponent<RectTransform>();
        _container = transform.parent.gameObject.GetComponent<RectTransform>();
    }
    private float _paintSpawnCooldown = 0.025f; // Adjust delay between spawns (in seconds)
    private float _timeSinceLastPaint = 0f;

    void Update()
{
    if (IsOwner)
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

        Spawn(paint.gameObject, Owner); // Syncs to all clients
    }

    // Called on server to respawn saved splats for a client
    [ServerRpc]
    public void RestorePaintSplatsForPlayer(int clientId)
    {
        var paintDataList = PlayerListItemManager.Instance.GetPlayerPaintData(clientId);
            Debug.Log("[player cursor] RestorePaintSplatsForPlayer...");
        
        foreach (var data in paintDataList)
        {
            PaintSplat paint = Instantiate(_paintPrefab, _container);
            paint.Initialize(data.anchoredPosition, data.color);
            Spawn(paint.gameObject, Owner);

            Debug.Log("[PlayerListItemManager] RestorePaintSplatsForPlayer...");
        }
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
