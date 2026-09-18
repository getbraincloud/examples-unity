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
/// - Create splatter gameobjects from both local and network user inputs
/// - Update Cursor locations from both local and network users inputs
/// - Normalized coordinates: x=0 left, x=1 right; y=0 top, y=1 bottom (matches all other clients)
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
    private GameObject SplatterAnimation;

    [HideInInspector] public UserCursor LocalUserCursor;
    protected Vector2 _cursorOffset = new Vector2(23, -35);
    protected Vector2 _splatterOffset = new Vector2(5, -3);
    //local to network is for splatter input specifically
    protected Vector2 _newPosition;

    [SerializeField]
    private Transform splatterParent;
    protected GameObject _newSplatter;
    protected List<Vector2> _localSplatterPositions = new List<Vector2>();
    protected List<TeamCodes> _localSplatterCodes = new List<TeamCodes>();
    protected List<float> _localSplatterAngles = new List<float>();
    protected Vector2 bottomLeftPositionGameArea = new Vector2(920, 300);
    private GameMode _currentGameMode;
    private RectTransform _cursorParentRectTransform;
    private float splatterLifespan = -1.0f;
    private float splatterAppear = 1.0f;
    private float splatterDisappear = 1.0f;

    // Hold-to-paint: repeats the same splatter emit on an interval while the button stays held,
    // instead of only firing once per click — matches cpp/react/Godot's RelayTestApp ports.
    private const float HOLD_REPEAT_INTERVAL = 0.15f;
    private float _leftHoldTimer;
    private float _rightHoldTimer;
    private float _middleHoldTimer;

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
                _leftHoldTimer = 0f;
                EmitLeftSplatter();
            }
            else if (Input.GetMouseButton(0))
            {
                _leftHoldTimer += Time.deltaTime;
                if (_leftHoldTimer >= HOLD_REPEAT_INTERVAL)
                {
                    _leftHoldTimer = 0f;
                    EmitLeftSplatter();
                }
            }
            else if (Input.GetMouseButtonDown(1) && _currentGameMode == GameMode.Team)
            {
                _rightHoldTimer = 0f;
                EmitTeamSplatter();
            }
            else if (Input.GetMouseButton(1) && _currentGameMode == GameMode.Team)
            {
                _rightHoldTimer += Time.deltaTime;
                if (_rightHoldTimer >= HOLD_REPEAT_INTERVAL)
                {
                    _rightHoldTimer = 0f;
                    EmitTeamSplatter();
                }
            }
            else if (Input.GetMouseButtonDown(2) && _currentGameMode == GameMode.Team)
            {
                _middleHoldTimer = 0f;
                EmitOpponentSplatter();
            }
            else if (Input.GetMouseButton(2) && _currentGameMode == GameMode.Team)
            {
                _middleHoldTimer += Time.deltaTime;
                if (_middleHoldTimer >= HOLD_REPEAT_INTERVAL)
                {
                    _middleHoldTimer = 0f;
                    EmitOpponentSplatter();
                }
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
        UpdateAllSplatters();
    }

    private void EmitLeftSplatter()
    {
        // Rolled once here, carried on the wire — never re-rolled on receive.
        float angle = UnityEngine.Random.Range(0f, 360f);
        //Save position locally for us to spawn in UpdateAllSplatters()
        _localSplatterPositions.Add(_newPosition);
        _localSplatterCodes.Add(TeamCodes.all);
        _localSplatterAngles.Add(angle);
        if (_currentGameMode == GameMode.FreeForAll)
        {
            //Send position of local users input for a splatter to other users
            BrainCloudManager.Instance.LocalSplatter(_newPosition, angle);
        }
        else
        {
            BrainCloudManager.Instance.SendSplatterToAll(_newPosition, angle);
        }
    }

    private void EmitTeamSplatter()
    {
        float angle = UnityEngine.Random.Range(0f, 360f);
        //Save position locally for us to spawn in UpdateAllSplatters()
        _localSplatterPositions.Add(_newPosition);
        _localSplatterCodes.Add(GameManager.Instance.CurrentUserInfo.Team);
        _localSplatterAngles.Add(angle);
        //Send Position to local players team
        BrainCloudManager.Instance.SendSplatterToTeam(_newPosition, angle);
    }

    private void EmitOpponentSplatter()
    {
        float angle = UnityEngine.Random.Range(0f, 360f);
        //Save position locally for us to spawn in UpdateAllSplatters()
        _localSplatterPositions.Add(_newPosition);
        TeamCodes teamToSend = GameManager.Instance.CurrentUserInfo.Team == TeamCodes.alpha
            ? TeamCodes.beta
            : TeamCodes.alpha;
        _localSplatterCodes.Add(teamToSend);
        _localSplatterAngles.Add(angle);
        //Send Position to opposite team
        BrainCloudManager.Instance.SendSplatterToOpponents(_newPosition, angle);
    }

    protected void SendMousePosition()
    {
        LocalCursorRectTransform.position = Input.mousePosition;

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(_gameAreaTransform,
            Input.mousePosition, null, out Vector3 worldPoint))
        {
            var normalizedPosition = WorldPointToBoardFraction(worldPoint);
            _newPosition = normalizedPosition;
            BrainCloudManager.Instance.LocalMouseMoved(normalizedPosition);
        }
    }

    // GameArea is rotated 270° in the scene, so its local rect isn't screen-aligned. Use the
    // world-space AABB instead — x=0 left/1 right, y=0 top/1 bottom, same as every other client.
    private Vector2 WorldPointToBoardFraction(Vector3 worldPoint)
    {
        GetBoardWorldBounds(out float xMin, out float xMax, out float yMin, out float yMax);
        return new Vector2(
            (worldPoint.x - xMin) / (xMax - xMin),
            (yMax - worldPoint.y) / (yMax - yMin));
    }

    private Vector3 BoardFractionToWorldPoint(Vector2 fraction)
    {
        GetBoardWorldBounds(out float xMin, out float xMax, out float yMin, out float yMax);
        var corners = new Vector3[4];
        _gameAreaTransform.GetWorldCorners(corners);
        return new Vector3(
            Mathf.Lerp(xMin, xMax, fraction.x),
            Mathf.Lerp(yMax, yMin, fraction.y),
            corners[0].z);
    }

    private void GetBoardWorldBounds(out float xMin, out float xMax, out float yMin, out float yMax)
    {
        var corners = new Vector3[4];
        _gameAreaTransform.GetWorldCorners(corners);
        xMin = Mathf.Min(Mathf.Min(corners[0].x, corners[1].x), Mathf.Min(corners[2].x, corners[3].x));
        xMax = Mathf.Max(Mathf.Max(corners[0].x, corners[1].x), Mathf.Max(corners[2].x, corners[3].x));
        yMin = Mathf.Min(Mathf.Min(corners[0].y, corners[1].y), Mathf.Min(corners[2].y, corners[3].y));
        yMax = Mathf.Max(Mathf.Max(corners[0].y, corners[1].y), Mathf.Max(corners[2].y, corners[3].y));
    }

    // Splats/cursors aren't children of GameArea (different, unrotated parents) — re-project
    // the world point into whatever they actually render inside.
    private static Vector2 WorldPointToAnchoredPosition(RectTransform target, Vector3 worldPoint)
    {
        RectTransform parent = target.parent as RectTransform;
        Vector3 localPoint = parent.InverseTransformPoint(worldPoint);
        Rect parentRect = parent.rect;
        Vector2 anchorRef = new Vector2(
            Mathf.Lerp(parentRect.xMin, parentRect.xMax, target.anchorMin.x),
            Mathf.Lerp(parentRect.yMin, parentRect.yMax, target.anchorMin.y));
        return (Vector2)localPoint - anchorRef;
    }

    protected void OnDisable()
    {
        if (!Cursor.visible)
        {
            Cursor.visible = true;
        }
    }

    protected void UpdateAllSplatters()
    {
        // Handle canvas clear (from host clear command)
        if (StateManager.Instance.PendingClearSplatters)
        {
            foreach (var go in StateManager.Instance.Splatters)
                if (go != null) Destroy(go);
            StateManager.Instance.Splatters.Clear();
            StateManager.Instance.AllSplotches.Clear();
            StateManager.Instance.PendingClearSplatters = false;
        }

        // Handle JIP splotch sync — first chunk clears canvas, then all chunks rebuild it
        if (StateManager.Instance.PendingSyncIsFirst)
        {
            foreach (var go in StateManager.Instance.Splatters)
                if (go != null) Destroy(go);
            StateManager.Instance.Splatters.Clear();
            StateManager.Instance.AllSplotches.Clear();
            StateManager.Instance.PendingSyncIsFirst = false;
        }
        if (StateManager.Instance.PendingSyncSplotches.Count > 0)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            foreach (var record in StateManager.Instance.PendingSyncSplotches)
            {
                float? effectiveLifespan = null;
                if (splatterLifespan >= 0)
                {
                    float elapsed = (now - record.StartTimeMs) / 1000f;
                    float remaining = splatterLifespan - elapsed;
                    if (remaining <= 0) continue; // already expired
                    effectiveLifespan = remaining;
                }
                SetUpSplatter(record.Position, record.ColorIndex, record.TeamCode, record.InstigatorCode, effectiveLifespan, record.SenderCxId, record.Angle);
            }
            StateManager.Instance.PendingSyncSplotches.Clear();
        }

        Lobby lobby = StateManager.Instance.CurrentLobby;

        foreach (var member in lobby.Members)
        {
            // Unconditional rendering — a per-viewer mask used to let a client hide a peer's
            // splotches from its own canvas, which Coverage reads directly. Scoring hole, fixed.
            for (int i = 0; i < member.SplatterPositions.Count; ++i)
            {
                float angle = i < member.SplatterAngles.Count ? member.SplatterAngles[i] : 0f;
                if (member.SplatterTeamCodes.Count > 0 && member.InstigatorTeamCodes.Count > 0)
                {
                    SetUpSplatter(member.SplatterPositions[i], member.UserGameColor, member.SplatterTeamCodes[i], member.InstigatorTeamCodes[i], null, member.cxId, angle);
                }
                else
                {
                    SetUpSplatter(member.SplatterPositions[i], member.UserGameColor, senderCxId: member.cxId, angle: angle);
                }
            }

            //Clear the list so there's no backlog of input positions
            if (member.SplatterPositions.Count > 0)
            {
                member.SplatterPositions.Clear();
                member.SplatterTeamCodes.Clear();
                member.InstigatorTeamCodes.Clear();
                member.SplatterAngles.Clear();
            }
        }

        {
            int i = 0;
            foreach (var pos in _localSplatterPositions)
            {
                SetUpSplatter
                (
                    pos,
                    GameManager.Instance.CurrentUserInfo.UserGameColor,
                    _localSplatterCodes[i],
                    GameManager.Instance.CurrentUserInfo.Team,
                    // CurrentUserInfo.cxId is never set locally — read it off the RTT connection.
                    senderCxId: BrainCloudManager.Instance.Wrapper.Client.RTTConnectionID,
                    angle: _localSplatterAngles[i]
                );
                i++;
            }
        }
        //Clear the list so there's no backlog of input positions
        if (_localSplatterPositions.Count > 0)
        {
            _localSplatterPositions.Clear();
            _localSplatterCodes.Clear();
            _localSplatterAngles.Clear();
        }
    }

    // colorIndex is the palette index; team/instigatorTeam control colour override logic.
    // lifespanOverride: null = use configured splatterLifespan; a value overrides (for JIP sync).
    public void SetUpSplatter(Vector2 position, int colorIndex, TeamCodes team = TeamCodes.all, TeamCodes instigatorTeam = TeamCodes.all, float? lifespanOverride = null, string senderCxId = null, float angle = 0f)
    {
        GameObject newSplatter = Instantiate
        (
            SplatterAnimation,
            Vector3.zero,
            Quaternion.identity,
            splatterParent
        );
        RectTransform UITransform = newSplatter.GetComponent<RectTransform>();
        Vector2 minMax = new Vector2(0, 1);
        UITransform.anchorMin = minMax;
        UITransform.anchorMax = minMax;
        UITransform.pivot = new Vector2(0.5f, 0.5f);
        // splatterParent isn't GameArea — project through world space, not GameArea's local rect.
        Vector3 worldPoint = BoardFractionToWorldPoint(position);
        UITransform.anchoredPosition = WorldPointToAnchoredPosition(UITransform, worldPoint) + _splatterOffset;

        Color waveColor = GameManager.ReturnUserColor(colorIndex);
        if (_currentGameMode == GameMode.Team && team == TeamCodes.all)
        {
            waveColor = Color.white;
        }
        AnimateSplatter anim = newSplatter.GetComponent<AnimateSplatter>();
        anim.SetColour(waveColor);
        anim.SetAngle(angle);
        float effectiveLifespan = lifespanOverride.HasValue ? lifespanOverride.Value : splatterLifespan;
        anim.SetLifespan(effectiveLifespan);
        anim.SetAnimationDurations(splatterAppear, splatterDisappear);

        StateManager.Instance.Splatters.Add(newSplatter.gameObject);

        // Record for host-to-JIP-client sync
        StateManager.Instance.AllSplotches.Add(new SplotchRecord
        {
            Position = position,
            ColorIndex = colorIndex,
            TeamCode = team,
            InstigatorCode = instigatorTeam,
            StartTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            SenderCxId = senderCxId,
            Angle = angle
        });
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
            // Cursors aren't children of GameArea either — same world-space projection as splats.
            Vector3 worldPoint = BoardFractionToWorldPoint(lobby.Members[i].MousePosition);
            Vector2 newMousePosition = WorldPointToAnchoredPosition(lobby.Members[i].CursorTransform, worldPoint);

            lobby.Members[i].CursorTransform.anchoredPosition = newMousePosition + _cursorOffset;
        }
    }
    ///Returns 'true' if we touched or hovering on this gameObject.
    protected bool IsPointerOverUIElement()
    {
        return CheckForRayCastHit(GetEventSystemRaycastResults());
    }
    ///Returns 'true' if we touched or hovering on this gameObject.
    protected bool CheckForRayCastHit(List<RaycastResult> eventSystemRayCastResults)
    {
        for (int index = 0; index < eventSystemRayCastResults.Count; index++)
        {
            RaycastResult curRaysastResult = eventSystemRayCastResults[index];
            if (curRaysastResult.gameObject == gameObject)
                return true;
        }
        return false;
    }
    ///Gets all event system raycast results of current mouse or touch position.
    protected static List<RaycastResult> GetEventSystemRaycastResults()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        List<RaycastResult> raysastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raysastResults);
        return raysastResults;
    }

    private void OnGetLifespanCallback(string jsonResponse, object cbObject)
    {
        var response = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        var data = response["data"] as Dictionary<string, object>;
        var property = data["PaintLifespan"] as Dictionary<string, object>;
        float value = Convert.ToSingle(property["value"]);
        splatterLifespan = value;
    }

    private void OnGetAnimDurationsCallback(string jsonResponse, object cbObject)
    {
        var response = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        var data = response["data"] as Dictionary<string, object>;

        var property = data["AppearDuration"] as Dictionary<string, object>;
        float value = Convert.ToSingle(property["value"]);
        splatterAppear = value;

        property = data["DisappearDuration"] as Dictionary<string, object>;
        value = Convert.ToSingle(property["value"]);
        splatterDisappear = value;
    }
}
