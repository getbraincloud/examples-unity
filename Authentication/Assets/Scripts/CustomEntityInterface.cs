using System;
using System.Collections;
using System.Collections.Generic;
using BrainCloud;
using BrainCloud.Common;
using BrainCloud.JsonFx.Json;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using UnityEngine;

/*
 * IMPORTANT NOTE: You will need to be on a Development Plus Plan that includes Custom Entities in order to have
 * access in the portal to set this up properly for your app.
 *
 * For more info:
 * https://help.getbraincloud.com/en/articles/3754150-custom-entities-a-scalable-and-flexible-app-data-storage-and-querying-solution
 *
 * Custom Entity Interface demonstrates how to handle JSON requests and responses from braincloud
 * when working with Custom Entities. In this interface you will be shown the following for ONLY the OWNER of the ENTITY:
 *  - How to create entity
 *  - How to read entity with ID received from a JSON response
 *  - How to update entity 
 *  - How to delete entity
 */

[Serializable]
public class CustomEntityInstance
{
    public string FirstName;
    public string LastName;
    public string Position;
    public int Goals;
    public int Assists;
    
    public int TimeToLive;
    public bool IsOwned;
    public string EntityId;
    public string OwnerId;
    public string EntityType;
    /*
     * 0 = private to the owner
     * 1 = readable by all users, but only writeable by the owner
     * 2 = writable by all users
     */
    public ACL Acl;
    //-1 tells the server to create the latest version
    public int Version = -1;

    public DateTime CreatedAt;
    public DateTime UpdatedAt;
    
    private readonly string DEFAULT_FIRST_NAME = "Johnny";
    private readonly string DEFAULT_LAST_NAME = "Bravo";
    private readonly string DEFAULT_POSITION = "forward";
    private readonly string DEFAULT_TYPE = "athletes";
    
    public CustomEntityInstance()
    {
        FirstName = DEFAULT_FIRST_NAME;
        LastName = DEFAULT_LAST_NAME;
        Position = DEFAULT_POSITION;
        EntityType = DEFAULT_TYPE;
        Goals = 0;
        Assists = 0;
    }
}

public class CustomEntityInterface : MonoBehaviour
{
    [SerializeField]
    private CustomEntityInstance _customPlayer;
    public CustomEntityInstance CustomPlayer
    {
        get => _customPlayer;
    }
    
    private BrainCloudWrapper _bcWrapper;
    public BrainCloudWrapper Wrapper
    {
        set => _bcWrapper = value;
    }
    
    public bool PlayerAssigned;
    private readonly string CUSTOM_PLAYER_ENTITY_TYPE = "athletes";
    
    
    public void ReadCustomEntity()
    {
        _bcWrapper.CustomEntityService.GetEntityPage
        (
            "athletes",
            CreateContextJson(),
            OnReadSuccess,
            OnFailureCallback
        );
    }

    private void OnReadSuccess(string json, object cb)
    {
        _customPlayer = null;
        Dictionary<string, object> jsonObj = JsonReader.Deserialize(json) as Dictionary<string, object>;
        Dictionary<string, object> data = jsonObj["data"] as Dictionary<string, object>;
        
        var resultsObj = data["results"] as Dictionary<string, object>;
        var results = resultsObj["items"] as Dictionary<string, object>[];

        if (results == null || results.Length == 0)
        {
            Debug.LogWarning("No entities found that is owned by this user");
            return;
        }
        
        PlayerAssigned = true;
        
        for (int i = 0; i < results.Length; i++)
        {
            Dictionary<string, object> entity = results[i];
            string entityType = entity["entityType"] as string;

            if (entityType == CUSTOM_PLAYER_ENTITY_TYPE)
            {
                _customPlayer = new CustomEntityInstance();
                _customPlayer.EntityId = entity["entityId"] as string;
                _customPlayer.OwnerId = entity["ownerId"] as string;
                _customPlayer.CreatedAt = Util.BcTimeToDateTime((long) entity["createdAt"]);
                _customPlayer.UpdatedAt = Util.BcTimeToDateTime((long) entity["updatedAt"]);

                //Data user stuff
                Dictionary<string, object> entityData = entity["data"] as Dictionary<string, object>;
                if (entityData.Count == 0) return;
                
                _customPlayer.FirstName = entityData["firstName"] as string;
                _customPlayer.LastName = entityData["surName"] as string;
                _customPlayer.Position = entityData["position"] as string;
                _customPlayer.Goals = (int) entityData["goals"];
                _customPlayer.Assists = (int) entityData["assists"];
            }
        }

        TextLogger.instance.AddLogJson(json, "GET CUSTOM ENTITY PAGE");
    }
    
    public void CreateCustomEntity()
    {
        _customPlayer = new CustomEntityInstance();
        _bcWrapper.CustomEntityService.CreateEntity
        (
            CUSTOM_PLAYER_ENTITY_TYPE,
            CreateJsonEntityData(),
            CreateACLJson(),
            null,
            true,
            OnCreateSuccess,
            OnFailureCallback
        );
    }

    private void OnCreateSuccess(string json, object cbObject)
    {
        PlayerAssigned = true;
        
        var jsonObj = JsonReader.Deserialize(json) as Dictionary<string, object>;
        var data = jsonObj["data"] as Dictionary<string, object>;

        _customPlayer.EntityId = data["entityId"] as string;
        _customPlayer.Version = (int) data["version"];
        var aclObj = data["acl"] as Dictionary<string, object>;
        _customPlayer.Acl = aclObj["other"] as ACL;
        _customPlayer.IsOwned = true; 
        _customPlayer.OwnerId = data["ownerId"] as string;
        
        if (data.ContainsValue("timeToLive"))
        {
            _customPlayer.TimeToLive = (int) data["timeToLive"];    
        }
        else
        {
            _customPlayer.TimeToLive = 0;
        }
        
        _customPlayer.CreatedAt = Util.BcTimeToDateTime((long) data["createdAt"]);
        _customPlayer.UpdatedAt = Util.BcTimeToDateTime((long) data["updatedAt"]);

        //Invoking custom entity success event for Screen Enitity custom class
        GameEvents.instance.CreateCustomEntitySuccess();
        TextLogger.instance.AddLogJson(json, "CREATE CUSTOM ENTITY");
    }

    public void UpdateCustomEntity()
    {
        if (_customPlayer.EntityId.IsNullOrEmpty())
        {
            Debug.LogWarning($"Custom Entity ID is missing...");
            return;
        }

        _bcWrapper.CustomEntityService.UpdateEntity
        (
            CUSTOM_PLAYER_ENTITY_TYPE,
            _customPlayer.EntityId,
            -1,     //Use -1 to skip version checking
            CreateJsonEntityData(),
            CreateACLJson(),
            null,
            OnUpdateSuccess,
            OnFailureCallback
        );
    }

    private void OnUpdateSuccess(string json, object cbObject)
    {
        Debug.Log($"Custom Entity is updated !");
        TextLogger.instance.AddLogJson(json, "UPDATE CUSTOM ENTITY");
    }

    public void DeleteCustomEntity()
    {
        PlayerAssigned = false;

        _bcWrapper.CustomEntityService.DeleteEntity
        (
            CUSTOM_PLAYER_ENTITY_TYPE,
            _customPlayer.EntityId,
            -1,     //Use -1 to skip version checking
            OnDeleteSuccess,
            OnFailureCallback
        );
        _customPlayer = null;
    }

    private void OnDeleteSuccess(string json, object cbObject)
    {
        Debug.Log($"Custom Entity is deleted !");

        //Invoking Delete custom entity for Screen entity custom class.
        GameEvents.instance.DeleteCustomEntitySuccess();
        TextLogger.instance.AddLogJson(json, "DELETE CUSTOM ENTITY");
    }
    
    private void OnFailureCallback(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        Debug.Log($"Failure Callback: {statusMessage}");
        Debug.Log($"Failure codes: status code: {statusCode}, reason code: {reasonCode}");
    }
    
    string CreateJsonEntityData()
    {
        Dictionary<string, object> entityInfo = new Dictionary<string, object>();
        
        entityInfo.Add("firstName", _customPlayer.FirstName);
        entityInfo.Add("surName", _customPlayer.LastName);
        entityInfo.Add("position", _customPlayer.Position);
        entityInfo.Add("goals", _customPlayer.Goals);
        entityInfo.Add("assists", _customPlayer.Assists);
        
        
        string value = JsonWriter.Serialize(entityInfo);

        return value;
    }

    string CreateACLJson()
    {
        Dictionary<string, object> aclInfo = new Dictionary<string, object>();
        
        /*
         * 0 = private to the owner
         * 1 = readable by all users, but only writeable by the owner
         * 2 = writable by all users
         */
        aclInfo.Add("other", 1);
        string value = JsonWriter.Serialize(aclInfo);
        return value;
    }

    string CreateContextJson()
    {
        Dictionary<string, object> pagination = new Dictionary<string, object>();
        pagination.Add("rowsPerPage", 50);
        pagination.Add("pageNumber", 1);
        
        Dictionary<string, object> searchCriteria = new Dictionary<string, object>();
        searchCriteria.Add("data.position", "forward");
        
        Dictionary<string, object> sortCriteria = new Dictionary<string, object>();

        Dictionary<string, object> optionCriteria = new Dictionary<string, object>();
        optionCriteria.Add("ownedOnly", true);

        Dictionary<string, object> contextInfo = new Dictionary<string, object>();
        contextInfo.Add("pagination", pagination);
        contextInfo.Add("searchCriteria", searchCriteria);
        contextInfo.Add("sortCriteria", sortCriteria);
        contextInfo.Add("options", optionCriteria);

        string value = JsonWriter.Serialize(contextInfo);
        
        return value;
    }
}
