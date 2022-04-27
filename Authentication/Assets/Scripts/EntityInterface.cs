using System;
using System.Collections;
using System.Collections.Generic;
using BrainCloud;
using BrainCloud.Common;
using BrainCloud.JsonFx.Json;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using Unity.UNetWeaver;
using UnityEngine;

/*
 * Entity Interface class demonstrates how to handle requests and responses from braincloud
 *
 * How to create entity
 *   Then store that id
 *
 * How to read entity with stored id
 *
 * How to update entity
 *
 * how to delete entity
 */

[Serializable]
public class EntityInstance
{
    public string Name;
    public string Age;
    
    public string EntityId;
    public string EntityType;
    public ACL Acl;
    //-1 tells the server to create the latest version
    public int Version = -1;

    public DateTime CreatedAt;
    public DateTime UpdatedAt;

    private readonly string DEFAULT_NAME = "Johnny Philharmonica";
    private readonly string DEFAULT_AGE = "49";
    private readonly string DEFAULT_TYPE = "player";
    
    public EntityInstance()
    {
        Name = DEFAULT_NAME;
        Age = DEFAULT_AGE;
        EntityType = DEFAULT_TYPE;
    }
}

public class EntityInterface : MonoBehaviour
{
    [SerializeField]
    private EntityInstance _player = null;
    
    private BrainCloudWrapper _bcWrapper;

    private readonly string PLAYER_ENTITY_TYPE = "player";

    public bool PlayerAssigned;
    public BrainCloudWrapper Wrapper
    {
        set => _bcWrapper = value;
    }

    public EntityInstance Player
    {
        get => _player;
    }

    public void ReadEntity()
    {
        _bcWrapper.PlayerStateService.ReadUserState(OnReadSuccess, OnFailureCallback);
    }

    private void OnReadSuccess(string json, object cb)
    {
        _player = null;
        Debug.Log($"Read Success: {json}");
        Dictionary<string, object> jsonObj = JsonReader.Deserialize(json) as Dictionary<string, object>;
        Dictionary<string, object> data = jsonObj["data"] as Dictionary<string, object>;

        if (!data.ContainsKey("entities"))
        {
            Debug.LogWarning($"No entities were read in, this is a new user.");
            return;
        }
        
        _player = new EntityInstance();
        PlayerAssigned = true;
        
        var listOfEntities = data["entities"] as Dictionary<string, object>[];
        for (int i = 0; i < listOfEntities.Length; i++)
        {
            Dictionary<string, object> entity = listOfEntities[i];
            string listType = entity["entityType"] as string;
            if (listType == PLAYER_ENTITY_TYPE)
            {
                var data1 = entity["data"] as Dictionary<string, object>;
                var entityData = data1["data"] as Dictionary<string, object>;
                _player.Name = entityData["name"] as string;
                _player.Age = entityData["age"] as string;

                _player.EntityId = entity["entityId"] as string;
                _player.EntityType = PLAYER_ENTITY_TYPE;
                _player.Version = (int) entity["version"];
                _player.CreatedAt = Util.BcTimeToDateTime((long) entity["createdAt"]);
                _player.UpdatedAt = Util.BcTimeToDateTime((long) entity["updatedAt"]);
            }
        }
    }

    public void CreateEntity()
    {
        //Clear current entity player
        _player = new EntityInstance();
        
        _bcWrapper.EntityService.CreateEntity
        (
            _player.EntityType,
            CreateJsonEntityData(),
            CreateACLJson(),
            OnCreateEntitySuccess,
            OnFailureCallback
        );
    }

    private void OnCreateEntitySuccess(string json, object cbObject)
    {
        PlayerAssigned = true;
        Debug.Log($"Success callback {json}");
        
        var data = JsonReader.Deserialize(json) as Dictionary<string, object>;
        var data1 = data["data"] as Dictionary<string, object>;
        var data2 = data1["data"] as Dictionary<string, object>;
        var entityData = data2["data"] as Dictionary<string, object>;
        
        _player.Name = entityData["name"] as string;
        _player.Age = entityData["age"] as string;

        _player.EntityId = data1["entityId"] as string;
        _player.EntityType = PLAYER_ENTITY_TYPE;
        _player.Version = (int) data1["version"];
        _player.CreatedAt = Util.BcTimeToDateTime((long) data1["createdAt"]);
        _player.UpdatedAt = Util.BcTimeToDateTime((long) data1["updatedAt"]);
        
        UpdateEntity();
    }

    public void UpdateEntity()
    {
        if (_player.EntityId.IsNullOrEmpty())
        {
            Debug.LogWarning($"Entity ID is blank...");
            return;
        }
        
        _bcWrapper.EntityService.UpdateEntity
        (
            _player.EntityId,
            _player.EntityType,
            CreateJsonEntityData(),
            CreateACLJson(),
            -1,
            OnUpdateEntity,
            OnFailureCallback
        );
    }

    private void OnUpdateEntity(string json, object cbObject)
    {
        Debug.Log($"On Update Response: {json}");
    }

    public void DeleteEntity()
    {
        PlayerAssigned = false;
        
        _bcWrapper.EntityService.DeleteEntity
        (
            _player.EntityId,
            -1,
            OnDeleteEntity,
            OnFailureCallback
        );
        _player = null;
    }

    private void OnDeleteEntity(string json, object cbObject)
    {
        Debug.Log($"On Delete Entity: {json}");
    }

    private void OnSuccessCallback(string json, object cbObject)
    {
        Debug.Log("GREAT SUCCESSS");
    }

    private void OnFailureCallback(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        Debug.Log($"Failure Callback: {statusMessage}");
        Debug.Log($"Failure codes: status code: {statusCode}, reason code: {reasonCode}");
    }

    public string ToJsonString(Dictionary<string,object> in_data)
    {
        return JsonWriter.Serialize(in_data);
    }
    
    string CreateJsonEntityData()
    {
        Dictionary<string, object> entityInfo = new Dictionary<string, object>();
        entityInfo.Add("name", _player.Name);
        entityInfo.Add("age", _player.Age);
        
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
