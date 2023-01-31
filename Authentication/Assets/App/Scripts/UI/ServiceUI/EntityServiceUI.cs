using BrainCloud;
using BrainCloud.Common;
using BrainCloud.Entity;
using BrainCloud.JsonFx.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.Experimental.GraphView.GraphView;

/// <summary>
/// Entity Interface class demonstrates how to handle JSON requests and responses from braincloud
/// when handling User Entities.
/// 
/// This includes:
/// - How to create entity
/// - How to read entity with ID received from a JSON response
/// - How to update entity 
/// - How to delete entity
/// 
/// For more info:
/// https://getbraincloud.com/apidocs/portal-usage/user-monitoring/user-entities/
/// https://getbraincloud.com/apidocs/cloud-code-central/cloud-code-tutorials/cloud-code-tutorial3-working-with-entities/
/// </summary>
public class EntityServiceUI : MonoBehaviour, IServiceUI
{
    private const string DEFAULT_EMPTY_FIELD = "---";
    private const string DEFAULT_ENTITY_TYPE = "user";

    [Header("Main")]
    [SerializeField] private CanvasGroup UICanvasGroup = default;
    [SerializeField] private TMP_Text IDField = default;
    [SerializeField] private TMP_Text TypeField = default;
    [SerializeField] private TMP_InputField NameField = default;
    [SerializeField] private TMP_InputField AgeField = default;
    [SerializeField] private Button CreateButton = default;
    [SerializeField] private Button SaveButton = default;
    [SerializeField] private Button DeleteButton = default;

    public bool IsInteractable
    {
        get { return UICanvasGroup.interactable; }
        set { UICanvasGroup.interactable = value; }
    }

    private BCEntity userEntity = default;
    private BrainCloudEntity entityService = default;

    #region Unity Messages

    private void Awake()
    {
        IDField.text = DEFAULT_EMPTY_FIELD;
        TypeField.text = DEFAULT_EMPTY_FIELD;
        NameField.text = string.Empty;
        AgeField.text = string.Empty;
    }

    private void OnEnable()
    {
        CreateButton.onClick.AddListener(OnCreateButton);
        SaveButton.onClick.AddListener(OnSaveButton);
        DeleteButton.onClick.AddListener(OnDeleteButton);
    }

    private void Start()
    {
        entityService = BCManager.EntityService;

        userEntity = BCEntity.Create(DEFAULT_ENTITY_TYPE);

        SaveButton.gameObject.SetActive(false);
        DeleteButton.gameObject.SetActive(false);

        HandleGetUserEntity();
    }

    private void OnDisable()
    {
        CreateButton.onClick.RemoveAllListeners();
        SaveButton.onClick.RemoveAllListeners();
        DeleteButton.onClick.RemoveAllListeners();
    }

    private void OnDestroy()
    {
        entityService = null;
    }

    #endregion

    #region UI Functionality

    private void ResetUIState()
    {
        userEntity = BCEntity.Create(DEFAULT_ENTITY_TYPE);

        IDField.text = DEFAULT_EMPTY_FIELD;
        TypeField.text = DEFAULT_EMPTY_FIELD;
        NameField.text = string.Empty;
        AgeField.text = string.Empty;

        IsInteractable = true;
    }

    private void OnCreateButton()
    {
        IsInteractable = false;

        userEntity = BCEntity.Create(DEFAULT_ENTITY_TYPE, NameField.text, AgeField.text);

        entityService.CreateEntity
        (
            userEntity.EntityType,
            userEntity.DataToJson(),
            userEntity.ACL.ToJsonString(),
            OnCreateEntitySuccess,
            HandleDefaultFailureCallback("CreateEntity Failed")
        );
    }

    private void OnSaveButton()
    {
        IsInteractable = false;

        if (string.IsNullOrEmpty(userEntity.EntityId))
        {
            Debug.LogWarning($"Entity ID is blank...");
            ResetUIState();
            return;
        }

        userEntity.Update(NameField.text, AgeField.text);

        entityService.UpdateEntity
        (
            userEntity.EntityId,
            userEntity.EntityType,
            userEntity.DataToJson(),
            userEntity.ACL.ToJsonString(),
            -1,
            OnUpdateEntitySuccess,
            HandleDefaultFailureCallback("UpdateEntity Failed")
        );
    }

    private void OnDeleteButton()
    {
        IsInteractable = false;

        if (string.IsNullOrEmpty(userEntity.EntityId))
        {
            Debug.LogWarning($"Entity ID is blank...");
            ResetUIState();
            return;
        }

        if (!string.IsNullOrEmpty(userEntity.EntityId))
        {
            entityService.DeleteEntity
            (
                userEntity.EntityId,
                -1,
                OnDeleteEntitySuccess,
                HandleDefaultFailureCallback("DeleteEntity Failed")
            );
        }
    }

    private void UpdateUIInformation()
    {
        if (!string.IsNullOrEmpty(userEntity.EntityId))
        {
            IDField.text = userEntity.EntityId;
            TypeField.text = userEntity.EntityType;
        }
        else
        {
            IDField.text = DEFAULT_EMPTY_FIELD;
            TypeField.text = DEFAULT_EMPTY_FIELD;
        }

        NameField.text = userEntity.Name;
        AgeField.text = userEntity.Age;

        IsInteractable = true;
    }

    #endregion

    #region Entity

    private void HandleGetUserEntity()
    {
        IsInteractable = false;

        var context = new Dictionary<string, object>
        {
            { "pagination", new Dictionary<string, object>
                            {
                                { "rowsPerPage", 50 },
                                { "pageNumber", 1 }
                            }},
            { "searchCriteria", new Dictionary<string, object>
                                {
                                    { "entityType", DEFAULT_ENTITY_TYPE }
                                }},
            { "sortCriteria", new Dictionary<string, object>
                              {
                                  { "createdAt", 1 },
                                  { "updatedAt", -1 }
                              }}
        };

        entityService.GetPage(JsonWriter.Serialize(context), HandleGetPageSuccess,
                                                             HandleDefaultFailureCallback("GetPage Failed"));
    }

    private void HandleGetPageSuccess(string response, object _)
    {
        BCManager.LogMessage("GetPage Success", response);

        var responseObj = JsonReader.Deserialize(response) as Dictionary<string, object>;
        var dataObj = responseObj["data"] as Dictionary<string, object>;
        var resultsObj = dataObj["results"] as Dictionary<string, object>;

        if (resultsObj["items"] is not Dictionary<string, object>[] data || data.Length <= 0)
        {
            Debug.LogWarning("No entities were found for this user.");
            ResetUIState();
            return;
        }

        userEntity.CreateFromJson(data[0]);

        CreateButton.gameObject.SetActive(false);
        SaveButton.gameObject.SetActive(true);
        DeleteButton.gameObject.SetActive(true);

        UpdateUIInformation();
    }

    private void OnCreateEntitySuccess(string response, object _)
    {
        BCManager.LogMessage("Created Entity for User", response);

        var data = (JsonReader.Deserialize(response) as Dictionary<string, object>)["data"] as Dictionary<string, object>;
        string entityID = (string)data["entityId"];

        BCManager.EntityService.GetEntity(entityID, HandleGetEntitySuccess);
    }

    private void OnUpdateEntitySuccess(string response, object _)
    {
        BCManager.LogMessage("Updated Entity for User", response);

        var data = (JsonReader.Deserialize(response) as Dictionary<string, object>)["data"] as Dictionary<string, object>;
        string entityID = (string)data["entityId"];

        BCManager.EntityService.GetEntity(entityID, HandleGetEntitySuccess);
    }

    private void OnDeleteEntitySuccess(string response, object _)
    {
        BCManager.LogMessage("Deleted Entity for User", response);

        userEntity = BCEntity.Create(DEFAULT_ENTITY_TYPE);

        ResetUIState();
    }

    private void HandleGetEntitySuccess(string response, object _)
    {
        BCManager.LogMessage("Updating Local Entity Data...", response);

        var data = (JsonReader.Deserialize(response) as Dictionary<string, object>)["data"] as Dictionary<string, object>;

        userEntity.UpdateFromJSON(data);

        CreateButton.gameObject.SetActive(false);
        SaveButton.gameObject.SetActive(true);
        DeleteButton.gameObject.SetActive(true);

        UpdateUIInformation();
    }

    private FailureCallback HandleDefaultFailureCallback(string errorMessage) =>
        BCManager.CreateFailureCallback(errorMessage, UpdateUIInformation);

    #endregion
}
