using System;
using System.Collections;
using System.Collections.Generic;
using BrainCloud;
using BrainCloud.Common;
using BrainCloud.JsonFx.Json;
using UnityEngine;

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
        Debug.Log("OnReadSuccess " + json);
        _customPlayer = null;
        Dictionary<string, object> jsonObj = JsonReader.Deserialize(json) as Dictionary<string, object>;
        Dictionary<string, object> data = jsonObj["data"] as Dictionary<string, object>;
        
        if (!data.ContainsKey("results"))
        {
            Debug.LogWarning($"No entities were read in, this is a new user.");
            return;
        }

        PlayerAssigned = true;

        var resultsObj = data["results"] as Dictionary<string, object>;
        var results = resultsObj["items"] as Dictionary<string, object>[];
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
    }
    
    public void CreateCustomEntity()
    {
        
    }

    private void OnCreateSuccess(string json, object cbObject)
    {
        
    }
    
    private void OnFailureCallback(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        Debug.Log($"Failure Callback: {statusMessage}");
        Debug.Log($"Failure codes: status code: {statusCode}, reason code: {reasonCode}");
    }
    
    string CreateJsonEntityData()
    {
        Dictionary<string, object> entityInfo = new Dictionary<string, object>();
        //ToDo: Update what data should be sent to update this entity
        //entityInfo.Add("name", _customPlayer.Name);
        //entityInfo.Add("age", _customPlayer.Age);
        
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

    string CreateContextJson()
    {
        Dictionary<string, object> pagination = new Dictionary<string, object>();
        pagination.Add("rowsPerPage", 50);
        pagination.Add("pageNumber", 1);
        
        Dictionary<string, object> searchCriteria = new Dictionary<string, object>();
        searchCriteria.Add("data.position", "forward");
        
        Dictionary<string, object> sortCriteria = new Dictionary<string, object>();
        
        Dictionary<string, object> contextInfo = new Dictionary<string, object>();
        contextInfo.Add("pagination", pagination);
        contextInfo.Add("searchCriteria", searchCriteria);
        contextInfo.Add("sortCriteria", sortCriteria);

        string value = JsonWriter.Serialize(contextInfo);
        
        return value;
    }
}
