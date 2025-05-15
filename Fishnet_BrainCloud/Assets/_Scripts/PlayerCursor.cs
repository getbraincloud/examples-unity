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

    [SerializeField]
    private Shockwave _shockwavePrefab;

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
    // Update is called once per frame
    void Update()
    {
        if (IsOwner)
        {
            Vector2 mousePos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_container, Input.mousePosition, null, out mousePos);

            _rect.anchoredPosition = mousePos;

            if (Input.GetMouseButtonDown(0))
            {
                //spawn shockwave
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
