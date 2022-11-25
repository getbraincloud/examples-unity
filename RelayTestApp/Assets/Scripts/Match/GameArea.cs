using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

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
    public GameObject Shockwave;
    public Canvas MatchCanvas;
      
    [HideInInspector] public UserCursor LocalUserCursor;
    //Offsets specific for when spawning a shockwave to local user
    public Vector2 _localShockwaveOffset=new Vector2(-8.7f,-5.35f);
    public Vector2 _networkShockwaveOffset =new Vector2(-8.65f,-8.24f);
    //local to network is for shockwave input specifically
    private float _localToNetworkOffset = -310f;
    private Vector2 _newPosition;
    private ParticleSystem.MainModule _shockwaveParticle;
    private GameObject _newShockwave;
    private List<Vector2> _localShockwavePositions = new List<Vector2>();

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
            
            _newPosition = GetMousePosition();
            BrainCloudManager.Instance.LocalMouseMoved(_newPosition);
            if (Input.GetMouseButtonDown(0))
            {
                //Save position locally for us to spawn in UpdateAllShockwaves()
                _localShockwavePositions.Add(_newPosition);
                
                //Position coordinates are different for the nodejs example so I offset it to the right view
                //_newPosition.y += _localToNetworkOffset;
                //Send position of local users input for a shockwave to other users
                BrainCloudManager.Instance.LocalShockwave(_newPosition);
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
        //Get in world position + offset 
        Vector2 newPosition = Camera.main.ScreenToWorldPoint(position);
        
        newPosition -= isUserLocal ? _localShockwaveOffset : _networkShockwaveOffset;
        
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
            if (!lobby.Members[i].UserCursor)
            {
                GameManager.Instance.UpdateCursorList();
            }
            if (!lobby.Members[i].UserCursor.CursorImage.enabled && lobby.Members[i].IsAlive)
            {
                lobby.Members[i].UserCursor.AdjustVisibility(true);
            }
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
        //Vector3 position = new Vector3(mouse.x - (MatchCanvas.pixelRect.width / 2), mouse.y - (MatchCanvas.pixelRect.height / 2));
        Vector3 position = Camera.main.ScreenToViewportPoint(mouse);
        return position;
    }
}
