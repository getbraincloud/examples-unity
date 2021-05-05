using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Purpose: To detect when the user is hovering over a UI element. This script needs to be attached to the UI object 
/// Recommend Use with: Image Component
/// </summary>

/*
 * ToDo
 * - Need to disable cursor and place a mouse image where the cursor was
 *  - Need to change color of cursor
 * - Need to spawn a shockwave where the user clicked
 * yes
 */
public class UserMouseArea : MonoBehaviour
{
    public Image NewCursor;
    public Texture2D CursorTexture;
    public GameObject Shockwave;
    public Canvas MatchCanvas;
    
    
    private Vector2 _shockwaveOffset=new Vector2(-8.6f,-5.5f);
    private Vector3 _newPosition;
    private Color _userColor;
    private ParticleSystem.MainModule _shockwaveParticle;
    private GameObject _newShockwave;
    
    // Update is called once per frame
    void Update()
    {
        if (IsPointerOverUIElement())
        {
            if (Cursor.visible)
            {
                Cursor.visible = false;
                NewCursor.enabled = true;    
            }
            

            _newPosition = GetMousePosition();
            NewCursor.transform.localPosition = _newPosition;
            if (Input.GetMouseButtonDown(0))
            {
                _newPosition = Camera.main.ScreenToWorldPoint(_newPosition);
                _newPosition.z = 0;
                _newPosition -= (Vector3)_shockwaveOffset;
                _newShockwave = Instantiate(Shockwave, _newPosition, Quaternion.identity);
                _shockwaveParticle = _newShockwave.GetComponent<ParticleSystem>().main;
                _shockwaveParticle.startColor = _userColor;
            }
        }
        else
        {
            if (!Cursor.visible)
            {
                Cursor.visible = true;
                NewCursor.enabled = false;
            }
            
        }
    }

    private void OnEnable()
    {
        _userColor = GameManager.Instance.ReturnUserColor();
        NewCursor.color = _userColor;
    }

    private void OnDisable()
    {
        NewCursor.enabled = false;
    }

    ///Returns 'true' if we touched or hovering on this gameObject.
    private bool IsPointerOverUIElement()
    {
        return IsPointerOverUIElement(GetEventSystemRaycastResults());
    }
    ///Returns 'true' if we touched or hovering on this gameObject.
    private bool IsPointerOverUIElement(List<RaycastResult> eventSystemRaysastResults )
    {
        for(int index = 0;  index < eventSystemRaysastResults.Count; index ++)
        {
            RaycastResult curRaysastResult = eventSystemRaysastResults [index];
            if (curRaysastResult.gameObject == gameObject)
                return true;
        }
        return false;
    }
    ///Gets all event system raycast results of current mouse or touch position.
    static List<RaycastResult> GetEventSystemRaycastResults()
    {   
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position =  Input.mousePosition;
        List<RaycastResult> raysastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll( eventData, raysastResults );
        return raysastResults;
    }

    private Vector3 GetMousePosition()
    {
        Vector2 mouse = Input.mousePosition;
        Vector3 position = new Vector3(mouse.x - (MatchCanvas.pixelRect.width / 2), mouse.y - (MatchCanvas.pixelRect.height / 2));
        return position;
    }
}
