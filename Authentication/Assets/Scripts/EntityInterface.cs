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
 * Entity Interface class demonstrates how to handle JSON requests and responses from braincloud
 * when handling User Entities.
 * This includes:
 *
 *  - How to create entity
 *  - How to read entity with ID received from a JSON response
 *  - How to update entity 
 *  - How to delete entity
 *
 * For more info:
 * https://getbraincloud.com/apidocs/portal-usage/user-monitoring/user-entities/
 * https://getbraincloud.com/apidocs/cloud-code-central/cloud-code-tutorials/cloud-code-tutorial3-working-with-entities/
 */

[Serializable]
public class EntityInstance
{
    public string Name;
    public string Age;
    
    public string EntityId;
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

    private readonly string DEFAULT_NAME = "Johnny Bravo";
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

    public EntityInstance Player
    {
        get => _player;
    }

    private BrainCloudWrapper _bcWrapper;

    public BrainCloudWrapper Wrapper
    {
        set => _bcWrapper = value;
    }
    
    private readonly string PLAYER_ENTITY_TYPE = "player";

    public void GetPage()
    {
        string context = CreateGetPageContext();
        _bcWrapper.EntityService.GetPage(context, OnGetPageSuccess, OnFailureCallback);
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
            OnUpdateEntitySuccess,
            OnFailureCallback
        );
    }

    public void DeleteEntity()
    {
        _bcWrapper.EntityService.DeleteEntity
        (
            _player.EntityId,
            -1,
            OnDeleteEntitySuccess,
            OnFailureCallback
        );

        _player = null;
    }
    public string CreateGetPageContext()
    {
        Dictionary<string, object> pagination = new Dictionary<string, object>();
        pagination.Add("rowsPerPage", 50);
        pagination.Add("pageNumber", 1);

        Dictionary<string, object> searchCriteria = new Dictionary<string, object>();
        searchCriteria.Add("entityType", "player");

        Dictionary<string, object> sortCriteria = new Dictionary<string, object>();
        sortCriteria.Add("createdAt", 1);
        sortCriteria.Add("updatedAt", -1);

        Dictionary<string, object> context = new Dictionary<string, object>();
        context.Add("pagination", pagination);
        context.Add("searchCriteria", searchCriteria);
        context.Add("sortCriteria", sortCriteria);

        string contextJson = JsonWriter.Serialize(context);

        return contextJson;
    }

    public string CreateJsonEntityData()
    {
        Dictionary<string, object> entityInfo = new Dictionary<string, object>();
        entityInfo.Add("name", _player.Name);
        entityInfo.Add("age", _player.Age);
        
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
        aclInfo.Add("other", 2);
        string value = JsonWriter.Serialize(aclInfo);
        return value;
    }


    //*************** Success Callbacks ***************

    private void OnGetPageSuccess(string response, object cbObject)
    {
        Debug.Log("Success");

        _player = null;

        Dictionary<string, object> responseObj = JsonReader.Deserialize(response) as Dictionary<string, object>;
        Dictionary<string, object> dataObj = responseObj["data"] as Dictionary<string, object>;
        Dictionary<string, object> resultsObj = dataObj["results"] as Dictionary<string, object>;
        var itemsObj = resultsObj["items"] as Dictionary<string, object>[];

        if (itemsObj == null)
        {
            Debug.LogWarning("No entities were found for this user.");
            GameEvents.instance.GetUserEntityPageSuccess();
            return;
        }

        for (int i = 0; i < itemsObj.Length; i++)
        {
            _player = new EntityInstance();

            Dictionary<string, object> itemIndexObj = itemsObj[i];

            _player.EntityId = itemIndexObj["entityId"] as string;
            _player.EntityType = itemIndexObj["entityType"] as string;
            _player.Version = (int)itemIndexObj["version"];
            _player.CreatedAt = Util.BcTimeToDateTime((long)itemIndexObj["createdAt"]);
            _player.UpdatedAt = Util.BcTimeToDateTime((long)itemIndexObj["updatedAt"]);

            Dictionary<string, object> entityDataObj = itemIndexObj["data"] as Dictionary<string, object>;

            _player.Name = entityDataObj["name"] as string;
            _player.Age = entityDataObj["age"] as string;
        }

        GameEvents.instance.GetUserEntityPageSuccess();
    }

    private void OnCreateEntitySuccess(string json, object cbObject)
    {
        Debug.Log($"Success callback {json}");
        
        var data = JsonReader.Deserialize(json) as Dictionary<string, object>;
        var data1 = data["data"] as Dictionary<string, object>;
        
        _player.EntityId = data1["entityId"] as string;
        _player.EntityType = PLAYER_ENTITY_TYPE;
        _player.Version = (int) data1["version"];
        _player.CreatedAt = Util.BcTimeToDateTime((long) data1["createdAt"]);
        _player.UpdatedAt = Util.BcTimeToDateTime((long) data1["updatedAt"]);
        
        UpdateEntity();

        GameEvents.instance.CreateUserEntitySuccess(); 
    }

    private void OnUpdateEntitySuccess(string json, object cbObject)
    {
        Debug.Log($"Entity is updated !");
    }

    private void OnDeleteEntitySuccess(string json, object cbObject)
    {
        Debug.Log($"Entity is deleted !");

        GameEvents.instance.DeleteUserEntitySuccess(); 
    }


    //*************** Failure Callbacks ***************
    private void OnFailureCallback(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        Debug.Log($"Failure Callback: {statusMessage}");
        Debug.Log($"Failure codes: status code: {statusCode}, reason code: {reasonCode}");
    }


    #region Stuff to Remove
    public void ReadEntity()
    {
        _bcWrapper.PlayerStateService.ReadUserState(OnReadSuccess, OnFailureCallback);
    }
    private void OnReadSuccess(string json, object cb)
    {
        //FrancoTODO: look into why I null this.
        //_player = null;
        Dictionary<string, object> jsonObj = JsonReader.Deserialize(json) as Dictionary<string, object>;
        Dictionary<string, object> data = jsonObj["data"] as Dictionary<string, object>;

        if (!data.ContainsKey("entities"))
        {
            Debug.LogWarning($"No entities were read in, this is a new user.");
            return;
        }
        
        var listOfEntities = data["entities"] as Dictionary<string, object>[];
        for (int i = 0; i < listOfEntities.Length; i++)
        {
            Dictionary<string, object> entity = listOfEntities[i];
            string listType = entity["entityType"] as string;
            
            if (listType == PLAYER_ENTITY_TYPE)
            {
                var data1 = entity["data"] as Dictionary<string, object>;
                var entityData = data1["data"] as Dictionary<string, object>;
                _player = new EntityInstance();
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
    public void GetEntitiesByType()
    {
        _bcWrapper.EntityService.GetEntitiesByType(PLAYER_ENTITY_TYPE, OnGetEntitiesByTypeSuccess, OnFailureCallback);
    }
    private void OnGetEntitiesByTypeSuccess(string response, object cbObject)
    {
        Debug.Log(string.Format("Success | {0}", response));
    }
    #endregion

}
