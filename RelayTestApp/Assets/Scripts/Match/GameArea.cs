using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Features:
/// - How to use brain cloud lobby members to translate for gameplay
/// - Update game area in runtime for network and local users
/// - Create shockwave gameobjects from both local and network user inputs
/// - Update Cursor locations from both local and network users inputs
/// - Offsets are due to the spacing difference from Node js example found at this link -> http://getbraincloud.com/devdemos/relaytestapp
/// </summary>

public class GameArea : MonoBehaviour
{
    public AnimateRipple ShockwaveAnimation;
    [HideInInspector] public UserCursor LocalUserCursor;
    //Offsets are to adjust the image closer to the cursor's point
    private Vector2 _mouseOffset = new Vector2(60, -580);
    private Vector2 _shockwaveOffset = new Vector2(-35, 30);
    
    //local to network is for shockwave input specifically
    private Vector2 _newPosition;
    private ParticleSystem.MainModule _shockwaveParticle;
    private GameObject _newShockwave;
    private List<Vector2> _localShockwavePositions = new List<Vector2>();
    private Vector2 bottomLeftPositionGameArea = new Vector2(920, 300);

    // Update is called once per frame
    private void Update()
    {
        if (IsPointerOverUIElement())
        {
            if (Cursor.visible)
            {
                Cursor.visible = false;
                LocalUserCursor.AdjustVisibility(true);
            }

            _newPosition = Input.mousePosition;
            _newPosition.x -= bottomLeftPositionGameArea.x;
            _newPosition.y -= bottomLeftPositionGameArea.y;
            BrainCloudManager.Instance.LocalMouseMoved(_newPosition + _mouseOffset);
            if (Input.GetMouseButtonDown(0))
            {
                //Save position locally for us to spawn in UpdateAllShockwaves()
                _localShockwavePositions.Add(_newPosition+ _mouseOffset);
                
                //Send position of local users input for a shockwave to other users
                BrainCloudManager.Instance.LocalShockwave(_newPosition+ _mouseOffset);
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

    private void OnDisable()
    {
        if (!Cursor.visible)
        {
            Cursor.visible = true;    
        }
    }
    
    private void UpdateAllShockwaves()
    {
        Lobby lobby = StateManager.Instance.CurrentLobby;
        
        foreach (var member in lobby.Members)
        {
            if (member.AllowSendTo)
            {
                foreach (Vector2 position in member.ShockwavePositions)
                {
                    SetUpShockwave(position, GameManager.ReturnUserColor(member.UserGameColor), false);
                }   
            }
            
            //Clear the list so there's no backlog of input positions
            if (member.ShockwavePositions.Count > 0)
            {
                member.ShockwavePositions.Clear();    
            }
        }

        if (GameManager.Instance.CurrentUserInfo.AllowSendTo)
        {
            foreach (var pos in _localShockwavePositions)
            {
                SetUpShockwave(pos, GameManager.ReturnUserColor(GameManager.Instance.CurrentUserInfo.UserGameColor),true);
            }   
        }
        //Clear the list so there's no backlog of input positions
        if (_localShockwavePositions.Count > 0)
        {
            _localShockwavePositions.Clear();
        }
    }

    private void SetUpShockwave(Vector2 position, Color waveColor, bool isUserLocal)
    {
        var newShockwave = Instantiate(ShockwaveAnimation, Vector3.zero, Quaternion.identity, GameManager.Instance.UserCursorParent.transform);
        RectTransform UITransform = newShockwave.GetComponent<RectTransform>();
        Vector2 minMax = new Vector2(0, 1);
        
        UITransform.anchorMin = minMax;
        UITransform.anchorMax = minMax;
        UITransform.pivot = new Vector2(0.5f, 0.5f);;
        newShockwave.RippleColor = waveColor;
        UITransform.anchoredPosition = position + _shockwaveOffset;
        
        StateManager.Instance.Shockwaves.Add(newShockwave.gameObject);
    }
    
    private void UpdateAllCursorsMovement()
    {
        Lobby lobby = StateManager.Instance.CurrentLobby;
        for (int i = 0; i < lobby.Members.Count; i++)
        {
            if (!lobby.Members[i].UserCursor)
            {
                GameManager.Instance.UpdateCursorList();
            }
            if (GameManager.Instance.CurrentUserInfo.ID != lobby.Members[i].ID && 
                !lobby.Members[i].UserCursor.CursorImage.enabled && 
                lobby.Members[i].IsAlive)
            {
                lobby.Members[i].UserCursor.AdjustVisibility(true);
            }

            lobby.Members[i].CursorTransform.anchoredPosition = lobby.Members[i].MousePosition;
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
}
