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

    [HideInInspector] public UserCursor LocalUserCursor;   
    private Vector2 _shockwaveOffset=new Vector2(-8.7f,-5.35f);
    private Vector3 _newPosition;
    private ParticleSystem.MainModule _shockwaveParticle;
    private GameObject _newShockwave;
    private List<Vector2> _localShockwavePositions = new List<Vector2>();

    // Update is called once per frame
    void Update()
    {
        
        if (IsPointerOverUIElement())
        {
            if (Cursor.visible)
            {
                Cursor.visible = false;
                LocalUserCursor.AdjustVisibility(true);
            }
            
            _newPosition = GetMousePosition();
            BrainCloudManager.Instance.LocalMouseMoved(_newPosition);
            if (Input.GetMouseButtonDown(0))
            {
                //Send position of local users input for a shockwave to other users
                BrainCloudManager.Instance.LocalShockwave(_newPosition);
                //Save position locally for us to spawn in UpdateAllShockwaves()
                _localShockwavePositions.Add(_newPosition);
            }
        }
        else
        {
            if (!Cursor.visible)
            {
                Cursor.visible = true;
                LocalUserCursor.AdjustVisibility(false);
            }
        }
        UpdateAllCursorsMovement();
        UpdateAllShockwaves();
    }

    public void UpdateAllShockwaves()
    {
        Lobby lobby = StateManager.Instance.CurrentLobby;
        
        foreach (var member in lobby.Members)
        {
            if (member.AllowSendTo)
            {
                foreach (Vector2 position in member.ShockwavePositions)
                {
                    SetUpShockwave(position,GameManager.ReturnUserColor(member.UserGameColor));
                }   
            }
            
            //Better safe then sorry
            if (member.ShockwavePositions.Count > 0)
            {
                member.ShockwavePositions.Clear();    
            }
        }

        if (GameManager.Instance.CurrentUserInfo.AllowSendTo)
        {
            foreach (var pos in _localShockwavePositions)
            {
                SetUpShockwave(pos,GameManager.ReturnUserColor(GameManager.Instance.CurrentUserInfo.UserGameColor));
            }   
        }

        if (_localShockwavePositions.Count > 0)
        {
            _localShockwavePositions.Clear();
        }
    }

    private void SetUpShockwave(Vector2 position, Color waveColor)
    {
        //Get in world position + offset 
        Vector2 newPosition = Camera.main.ScreenToWorldPoint(position);
        newPosition -= _shockwaveOffset;
        
        _newShockwave = Instantiate(Shockwave, newPosition, Quaternion.identity);
        
        //Adjusting shockwave color to what user settings are
        _shockwaveParticle = _newShockwave.GetComponent<ParticleSystem>().main;
        _shockwaveParticle.startColor = waveColor;    
        StateManager.Instance.Shockwaves.Add(_newShockwave);
    }
    
    private void UpdateAllCursorsMovement()
    {
        Lobby lobby = StateManager.Instance.CurrentLobby;
        for (int i = 0; i < lobby.Members.Count; i++)
        {
            lobby.Members[i].UserCursor.transform.localPosition = lobby.Members[i].MousePosition;
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
