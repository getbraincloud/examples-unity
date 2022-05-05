using System;
using System.Collections;
using System.Collections.Generic;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using BrainCloud.JsonFx.Json;
using UnityEngine;

public class BrainCloudManager : MonoBehaviour
{
    
    private BrainCloudWrapper m_bcWrapper;
    private bool m_dead = false;
    public bool LeavingGame;
    public BrainCloudWrapper Wrapper => m_bcWrapper;
    public static BrainCloudManager Instance;
    private bool _isNewPlayer = false;
    private void Awake()
    {
        m_bcWrapper = GetComponent<BrainCloudWrapper>();
        if (!Instance)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);
    }
    
    private void InitializeBC()
    {
        m_bcWrapper.Init();

        m_bcWrapper.Client.EnableLogging(true);
    }
    
    // Uninitialize brainCloud
    void UninitializeBC()
    {
        if (m_bcWrapper != null)
        {
            m_bcWrapper.Client.ShutDown();
        }
    }

    private void OnApplicationQuit()
    {
        UninitializeBC();
    }

    //Called from Unity Button, attempting to login
    public void Login()
    {
        _isNewPlayer = false;
        string username = MenuManager.Instance.UsernameInputField.text;
        string password = MenuManager.Instance.PasswordInputField.text;
        if (username.IsNullOrEmpty())
        {   
            MenuManager.Instance.AbortToSignIn($"Please provide a username");
            return;
        }
        if (password.IsNullOrEmpty())
        {
            MenuManager.Instance.AbortToSignIn($"Please provide a password");
            return;
        }

        if (!username.Equals(GameManager.Instance.CurrentUserInfo.Username))
        {
            _isNewPlayer = true;
            Debug.Log("hit");
        }
        
        Settings.SaveLogin(username, password);
        InitializeBC();
        // Authenticate with brainCloud
        m_bcWrapper.AuthenticateUniversal(username, password, true, HandlePlayerState, OnFailureCallback, "Login Failed");
    }

    public void SignOut()
    {
        m_bcWrapper.PlayerStateService.Logout();
    }

    public void UpdateEntity()
    {
        m_bcWrapper.EntityService.UpdateEntity
        (
            GameManager.Instance.CurrentUserInfo.EntityId,
            "vikings",
            CreateJsonEntityData(false),
            CreateACLJson(),
            -1
        );
    }

    public void LookForPlayers()
    {
        m_bcWrapper.MatchMakingService.EnableMatchMaking(OnEnableMatchMaking, OnFoundPlayersError);
        //m_bcWrapper.EntityService.GetEntitiesByType("vikings", OnFoundPlayers, OnFoundPlayersError);
    }

    void OnEnableMatchMaking(string jsonResponse, object cbObject)
    {
        m_bcWrapper.MatchMakingService.FindPlayers(1000, 10, OnFoundPlayers, OnFoundPlayersError);
    }

    public void SetPlayerRating()
    {
        m_bcWrapper.MatchMakingService.SetPlayerRating(10);
    }

    void OnFoundPlayers(string jsonResponse, object cbObject)
    {
        //m_bcWrapper.EntityService.GetSharedEntitiesForProfileId("a20735cb-62fa-43f3-956f-690c152755e9", OnFoundEntity, OnFoundPlayersError);

    }

    void OnFoundPlayersError(int status, int reasonCode, string jsonError, object cbObject)
    {
        
    }

    void OnFoundEntity(string jsonResponse, object cbObject)
    {
        
    }
    
    // User authenticated, handle the result
    void HandlePlayerState(string jsonResponse, object cbObject)
    {
        var response = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        var data = response["data"] as Dictionary<string, object>;
        var tempUsername = GameManager.Instance.CurrentUserInfo.Username;
        var userInfo = GameManager.Instance.CurrentUserInfo;
        
        userInfo = new UserInfo
        {
            ID = data["profileId"] as string
        };
        
        // If no username is set for this user, ask for it
        if (!data.ContainsKey("playerName"))
        {
            // Update name for display
            m_bcWrapper.PlayerStateService.UpdateName(tempUsername, OnLoggedIn, OnFailureCallback,
                "Failed to update username to braincloud");
        }
        else
        {
            userInfo.Username = data["playerName"] as string;
            if (userInfo.Username.IsNullOrEmpty())
            {
                userInfo.Username = tempUsername;
            }
            m_bcWrapper.PlayerStateService.UpdateName(userInfo.Username, OnLoggedIn, OnFailureCallback,
                "Failed to update username to braincloud");
        }
    }
    
    // Go back to login screen, with an error message
    void OnFailureCallback(int status, int reasonCode, string jsonError, object cbObject)
    {
        if (m_dead) return;

        m_dead = true;

        string message = cbObject as string;

        MenuManager.Instance.AbortToSignIn($"Message: {message} |||| JSON: {jsonError}");

    }
    
    // User fully logged in. 
    void OnLoggedIn(string jsonResponse, object cbObject)
    {
        //Check if this is a new login, if so then check if this user has entities
        if (!_isNewPlayer)
        {
            if (GameManager.Instance.IsEntityIdValid())
            {
                m_bcWrapper.EntityService.GetEntity
                (
                    GameManager.Instance.CurrentUserInfo.EntityId,
                    OnValidEntityResponse,
                    OnFailureCallback
                );    
            }
        }
        else
        {
            m_bcWrapper.EntityService.GetEntitiesByType
            (
                "vikings",
                OnReadEntitiesResponse,
                OnFailureCallback
            );
        }
    }
    
    void OnValidEntityResponse(string jsonResponse, object cbObject)
    {
        Dictionary<string, object> response = JsonReader.Deserialize(jsonResponse) as Dictionary<string, object>;
        Dictionary<string, object> data1 = response["data"] as Dictionary<string,object>;
        Dictionary<string, object> data2 = data1["data"] as Dictionary<string,object>;
        Dictionary<string, object> entityData = data2["data"] as Dictionary<string,object>;
        
        int defenderSelection = (int) entityData["defenderSelection"];
        int invaderSelection = (int) entityData["invaderSelection"];
        
        GameManager.Instance.UpdateArmySelection(defenderSelection, invaderSelection);
        MenuManager.Instance.UpdateMainMenu();
        MenuManager.Instance.IsLoading = false;
    }

    void OnReadEntitiesResponse(string jsonResponse, object cbObject)
    {
        //Read in the entities, if list is empty than create a new entity.
        Dictionary<string, object> response = JsonReader.Deserialize(jsonResponse) as Dictionary<string, object>;
        Dictionary<string, object> data = response["data"] as Dictionary<string, object>;

        var entities = data["entities"] as Dictionary<string, object>[];
        
        if (entities != null && entities.Length > 0)
        {
            Dictionary<string, object> data2 = entities[0]["data"] as Dictionary<string, object>;
            Dictionary<string, object> entityData = data2["data"] as Dictionary<string, object>;
            
            int defenderSelection = (int) entityData["defenderSelection"];
            int invaderSelection = (int) entityData["invaderSelection"];
        
            GameManager.Instance.UpdateArmySelection(defenderSelection, invaderSelection);
            MenuManager.Instance.UpdateMainMenu();
            MenuManager.Instance.IsLoading = false;
        }
        else
        {
            m_bcWrapper.EntityService.CreateEntity
            (
                "vikings",
                CreateJsonEntityData(true),
                CreateACLJson(),
                OnCreatedEntityResponse,
                OnFailureCallback
            );
        }
    }
    
    
    void OnCreatedEntityResponse(string jsonResponse, object cbObject)
    {
        Dictionary<string, object> response = JsonReader.Deserialize(jsonResponse) as Dictionary<string, object>;
        Dictionary<string, object> jsonData = response["data"] as Dictionary<string, object>;
        string entityId = jsonData["entityId"] as string;
        
        GameManager.Instance.UpdateEntityId(entityId);
        GameManager.Instance.UpdateArmySelection(0, 0);
        MenuManager.Instance.IsLoading = false;
        MenuManager.Instance.UpdateMainMenu();
    }

    string CreateJsonEntityData(bool isDataNew)
    {
        Dictionary<string, object> entityInfo = new Dictionary<string, object>();
        if (isDataNew)
        {
            entityInfo.Add("defenderSelection", 0);
            entityInfo.Add("invaderSelection", 0);    
        }
        else
        {
            UserInfo user = GameManager.Instance.CurrentUserInfo;
            entityInfo.Add("defenderSelection",(int) user.DefendersSelected);
            entityInfo.Add("invaderSelection",(int) user.InvaderSelected);
        }
        
        Dictionary<string, object> jsonData = new Dictionary<string, object>();
        jsonData.Add("data",entityInfo);
        string value = JsonWriter.Serialize(jsonData);

        return value;
    }

    string CreateACLJson()
    {
        Dictionary<string, object> aclInfo = new Dictionary<string, object>();
        aclInfo.Add("other", 2);
        string value = JsonWriter.Serialize(aclInfo);
        return value;
    }
}
