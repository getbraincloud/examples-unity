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
    public GameObject Shockwave;
    public Canvas MatchCanvas;

    [HideInInspector] public Image LocalCursor;   
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
                LocalCursor.enabled = true;    
            }
            
            _newPosition = GetMousePosition();
            BrainCloudManager.Instance.MouseMoved(_newPosition);
            if (Input.GetMouseButtonDown(0))
            {
                SpawnShockwave(_newPosition);
            }
        }
        else
        {
            if (!Cursor.visible)
            {
                Cursor.visible = true;
                LocalCursor.enabled = false;
            }
        }
        UpdateAllCursorsMovement();
    }

    public void SpawnShockwave(Vector2 _newPosition)
    {
        //Sending info to BC
        BrainCloudManager.Instance.Shockwave(_newPosition);
        
        //Get in world position + offset 
        _newPosition = Camera.main.ScreenToWorldPoint(_newPosition);
        _newPosition -= _shockwaveOffset;
        
        _newShockwave = Instantiate(Shockwave, _newPosition, Quaternion.identity);
        
        //Update shockwave list
        StateManager.Instance.ShockwavePositions.Add(_newShockwave);
        //Adjusting shockwave color to what user settings are
        _shockwaveParticle = _newShockwave.GetComponent<ParticleSystem>().main;
        _shockwaveParticle.startColor = _userColor;
    }
    private void UpdateAllCursorsMovement()
    {
        Lobby lobby = StateManager.Instance.CurrentLobby;
        var cursorList = GameManager.Instance.CursorList; 
        for (int i = 0; i < cursorList.Count; i++)
        {
            cursorList[i].transform.localPosition = lobby.Members[i].MousePosition;
        }
    }
    ///Returns 'true' if we touched or hovering on this gameObject.
    private bool IsPointerOverUIElement()
    {
        return CheckForRayCastHit(GetEventSystemRaycastResults());
    }
    ///Returns 'true' if we touched or hovering on this gameObject.
    private bool CheckForRayCastHit(List<RaycastResult> eventSystemRayCastResults )
    {
        for(int index = 0;  index < eventSystemRayCastResults.Count; index ++)
        {
            RaycastResult curRaysastResult = eventSystemRayCastResults [index];
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
