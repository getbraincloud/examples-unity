using BrainCloud.JsonFx.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Cursor = UnityEngine.Cursor;

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
    public RectTransform LocalCursorRectTransform;
    private RectTransform _gameAreaTransform;
    public RectTransform GameAreaTransform
    {
        get => _gameAreaTransform;
    }

    [SerializeField]
    private GameObject ShockwaveAnimation;

    [HideInInspector] public UserCursor LocalUserCursor;
    protected Vector2 _cursorOffset = new Vector2(30, -30);
    protected Vector2 _shockwaveOffset = new Vector2(15, -6);
    //local to network is for shockwave input specifically
    protected Vector2 _newPosition;

    [SerializeField]
    private Transform shockwaveParent;
    protected GameObject _newShockwave;
    protected List<Vector2> _localShockwavePositions = new List<Vector2>();
    protected List<TeamCodes> _localShockwaveCodes = new List<TeamCodes>();
    protected Vector2 bottomLeftPositionGameArea = new Vector2(920, 300);
    private GameMode _currentGameMode;
    private RectTransform _cursorParentRectTransform;
    private float shockwaveLifespan = -1.0f;
    private float shockwaveAppear = 1.0f;
    private float shockwaveDisappear = 1.0f;

    private void OnEnable()
    {
        _currentGameMode = GameManager.Instance.GameMode;
        _cursorParentRectTransform = GameManager.Instance.UserCursorParent.GetComponent<RectTransform>();
        _gameAreaTransform = GetComponent<RectTransform>();
        string[] properties = new string[] { "PaintLifespan" };
        BrainCloudManager.Instance.Wrapper.GlobalAppService.ReadSelectedProperties(properties, OnGetLifespanCallback, null);
        properties = new string[] { "AppearDuration", "DisappearDuration" };
        BrainCloudManager.Instance.Wrapper.GlobalAppService.ReadSelectedProperties(properties, OnGetAnimDurationsCallback, null);
    }

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

            SendMousePosition();
            if (Input.GetMouseButtonDown(0))
            {
                //Save position locally for us to spawn in UpdateAllShockwaves()
                _localShockwavePositions.Add(_newPosition);
                _localShockwaveCodes.Add(TeamCodes.all);
                if (_currentGameMode == GameMode.FreeForAll)
                {
                    //Send position of local users input for a shockwave to other users
                    BrainCloudManager.Instance.LocalShockwave(_newPosition);
                }
                else
                {
                    BrainCloudManager.Instance.SendShockwaveToAll(_newPosition);
                }
            }
            else if (Input.GetMouseButtonDown(1) && _currentGameMode == GameMode.Team)
            {
                //Save position locally for us to spawn in UpdateAllShockwaves()
                _localShockwavePositions.Add(_newPosition);
                _localShockwaveCodes.Add(GameManager.Instance.CurrentUserInfo.Team);
                //Send Position to local players team
                BrainCloudManager.Instance.SendShockwaveToTeam(_newPosition);
            }
            else if (Input.GetMouseButtonDown(2) && _currentGameMode == GameMode.Team)
            {
                //Save position locally for us to spawn in UpdateAllShockwaves()
                _localShockwavePositions.Add(_newPosition);
                TeamCodes TeamToSend = GameManager.Instance.CurrentUserInfo.Team == TeamCodes.alpha
                    ? TeamCodes.beta
                    : TeamCodes.alpha;
                _localShockwaveCodes.Add(TeamToSend);
                //Send Position to opposite team
                BrainCloudManager.Instance.SendShockwaveToOpponents(_newPosition);
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

    protected void SendMousePosition()
    {
        LocalCursorRectTransform.position = Input.mousePosition;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_gameAreaTransform,
        LocalCursorRectTransform.position, null, out var localPosition))
        {
            var gameAreaRect = _gameAreaTransform.rect;
            var normalizedPosition = new Vector2
            (
                (localPosition.y - gameAreaRect.y) / gameAreaRect.height,
                (localPosition.x - gameAreaRect.x) / gameAreaRect.width
            );
            _newPosition = normalizedPosition;
            BrainCloudManager.Instance.LocalMouseMoved(normalizedPosition);
        }
    }
    
    protected void OnDisable()
    {
        if (!Cursor.visible)
        {
            Cursor.visible = true;    
        }
    }
    
    protected void UpdateAllShockwaves()
    {
        Lobby lobby = StateManager.Instance.CurrentLobby;
        
        foreach (var member in lobby.Members)
        {
            if (member.AllowSendTo)
            {
                for(int i= 0; i < member.ShockwavePositions.Count; ++i)
                {
                    if(member.ShockwaveTeamCodes.Count > 0 && member.InstigatorTeamCodes.Count > 0)
                    {
                        SetUpShockwave(member.ShockwavePositions[i], GameManager.ReturnUserColor(member.UserGameColor), member.ShockwaveTeamCodes[i], member.InstigatorTeamCodes[i]);                            
                    }
                    else
                    {
                        SetUpShockwave(member.ShockwavePositions[i], GameManager.ReturnUserColor(member.UserGameColor));
                    }
                } 
            }
            
            //Clear the list so there's no backlog of input positions
            if (member.ShockwavePositions.Count > 0)
            {
                member.ShockwavePositions.Clear();    
                member.ShockwaveTeamCodes.Clear();
                member.InstigatorTeamCodes.Clear();
            }
        }

        if (GameManager.Instance.CurrentUserInfo.AllowSendTo)
        {
            int i = 0;
            foreach (var pos in _localShockwavePositions)
            {
                SetUpShockwave
                (
                    pos,
                    GameManager.ReturnUserColor(GameManager.Instance.CurrentUserInfo.UserGameColor),
                    _localShockwaveCodes[i],
                    GameManager.Instance.CurrentUserInfo.Team
                );  
                i++;
            }   
        }
        //Clear the list so there's no backlog of input positions
        if (_localShockwavePositions.Count > 0)
        {
            _localShockwavePositions.Clear();
            _localShockwaveCodes.Clear();
        }
    }

    protected void SetUpShockwave(Vector2 position, Color waveColor, TeamCodes team = TeamCodes.all, TeamCodes instigatorTeam = TeamCodes.all)
    {
        GameObject newShockwave = Instantiate
        (
            ShockwaveAnimation,
            Vector3.zero,
            Quaternion.identity,
            shockwaveParent
        );
        RectTransform UITransform = newShockwave.GetComponent<RectTransform>();
        Vector2 minMax = new Vector2(0, 1);
        UITransform.anchorMin = minMax;
        UITransform.anchorMax = minMax;
        UITransform.pivot = new Vector2(0.5f, 0.5f);
        RectTransform gameAreaTransform = GameManager.Instance.GameArea.GameAreaTransform;
        Rect gameAreaRect = gameAreaTransform.rect;
        var newPosition = new Vector2(
            gameAreaRect.height * position.x,
            gameAreaRect.width * -position.y
        );
        UITransform.anchoredPosition = newPosition + _shockwaveOffset;

        if (_currentGameMode == GameMode.Team && team == TeamCodes.all)
        {
            waveColor = Color.white;
        }
        AnimateSplatter anim = newShockwave.GetComponent<AnimateSplatter>();
        anim.SetColour(waveColor);
        anim.SetLifespan(shockwaveLifespan);
        anim.SetAnimationDurations(shockwaveAppear, shockwaveDisappear);

        StateManager.Instance.Shockwaves.Add(newShockwave.gameObject);
    }
    
    protected void UpdateAllCursorsMovement()
    {
        Lobby lobby = StateManager.Instance.CurrentLobby;
        for (int i = 0; i < lobby.Members.Count; i++)
        {
            if (!lobby.Members[i].UserCursor)
            {
                GameManager.Instance.UpdateCursorList();
            }
            if (GameManager.Instance.CurrentUserInfo.ProfileID != lobby.Members[i].ProfileID &&
                !lobby.Members[i].UserCursor.CursorImage.enabled &&
                lobby.Members[i].IsAlive)
            {
                lobby.Members[i].UserCursor.AdjustVisibility(true);
            }
            RectTransform gameAreaTransform = GameManager.Instance.GameArea.GameAreaTransform;
            Rect gameAreaRect = gameAreaTransform.rect;

            Vector2 newMousePosition = new Vector2(
                gameAreaRect.height * lobby.Members[i].MousePosition.x,
                gameAreaRect.width * -lobby.Members[i].MousePosition.y
            );

            lobby.Members[i].CursorTransform.anchoredPosition = newMousePosition + _cursorOffset;
        }
    }
    ///Returns 'true' if we touched or hovering on this gameObject.
    protected bool IsPointerOverUIElement()
    {
        return CheckForRayCastHit(GetEventSystemRaycastResults());
    }
    ///Returns 'true' if we touched or hovering on this gameObject.
    protected bool CheckForRayCastHit(List<RaycastResult> eventSystemRayCastResults )
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
    protected static List<RaycastResult> GetEventSystemRaycastResults()
    {   
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position =  Input.mousePosition;
        List<RaycastResult> raysastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll( eventData, raysastResults );
        return raysastResults;
    }

    private void OnGetLifespanCallback(string jsonResponse, object cbObject)
    {
        var response = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        var data = response["data"] as Dictionary<string, object>;
        var property = data["PaintLifespan"] as Dictionary<string, object>;
        float value = Convert.ToSingle(property["value"]);
        shockwaveLifespan = value;
    }

    private void OnGetAnimDurationsCallback(string jsonResponse, object cbObject)
    {
        var response = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        var data = response["data"] as Dictionary<string, object>;

        var property = data["AppearDuration"] as Dictionary<string, object>;
        float value = Convert.ToSingle(property["value"]);
        shockwaveAppear = value;

        property = data["DisappearDuration"] as Dictionary<string, object>;
        value = Convert.ToSingle(property["value"]);
        shockwaveDisappear = value;
    }
}
