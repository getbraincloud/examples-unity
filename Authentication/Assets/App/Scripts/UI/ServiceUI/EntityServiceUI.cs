using BrainCloud;
using BrainCloud.Common;
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
    [SerializeField] private Button FetchButton = default;
    [SerializeField] private Button CreateButton = default;
    [SerializeField] private Button SaveButton = default;
    [SerializeField] private Button DeleteButton = default;

    public bool IsInteractable
    {
        get { return UICanvasGroup.interactable; }
        set { UICanvasGroup.interactable = value; }
    }

    private BCEntity userEntityData = default;
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
        FetchButton.onClick.AddListener(OnFetchButton);
        CreateButton.onClick.AddListener(OnCreateButton);
        SaveButton.onClick.AddListener(OnSaveButton);
        DeleteButton.onClick.AddListener(OnDeleteButton);
    }

    private void Start()
    {
        entityService = BCManager.EntityService;

        userEntityData = BCEntity.CreateEmpty();

        FetchButton.gameObject.SetActive(false);
        SaveButton.gameObject.SetActive(false);
        DeleteButton.gameObject.SetActive(false);

        HandleGetUserEntity();
    }

    private void OnDisable()
    {
        FetchButton.onClick.RemoveAllListeners();
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

    private void OnFetchButton()
    {
        //IsInteractable = false;
    }

    private void OnCreateButton()
    {
        IsInteractable = false;

        if (string.IsNullOrEmpty(NameField.text))
        {
            NameField.text = userEntityData.Name;
        }
        else
        {
            userEntityData.Name = NameField.text;
        }

        if (string.IsNullOrEmpty(AgeField.text))
        {
            AgeField.text = userEntityData.Age;
        }
        else
        {
            userEntityData.Age = AgeField.text;
        }

        userEntityData.EntityType = DEFAULT_ENTITY_TYPE;

        entityService.CreateEntity
        (
            userEntityData.EntityType,
            userEntityData.EntityDataToJSON(),
            userEntityData.ACL.ToJsonString(),
            OnCreateEntitySuccess,
            BCManager.CreateFailureCallback("CreateEntity Failed", UpdateUIInformation)
        );
    }

    private void OnSaveButton()
    {
        IsInteractable = false;

        if (string.IsNullOrEmpty(userEntityData.EntityId))
        {
            Debug.LogWarning($"Entity ID is blank...");
            return;
        }

        entityService.UpdateEntity
        (
            userEntityData.EntityId,
            userEntityData.EntityType,
            userEntityData.EntityDataToJSON(),
            userEntityData.ACL.ToJsonString(),
            -1,
            OnUpdateEntitySuccess,
            BCManager.CreateFailureCallback("UpdateEntity Failed", UpdateUIInformation)
        );
    }

    private void OnDeleteButton()
    {
        IsInteractable = false;

        if (!string.IsNullOrEmpty(userEntityData.EntityId))
        {
            entityService.DeleteEntity
            (
                userEntityData.EntityId,
                -1,
                OnDeleteEntitySuccess,
                BCManager.CreateFailureCallback("DeleteEntity Failed", UpdateUIInformation)
            );

            userEntityData = BCEntity.CreateEmpty();
        }
    }

    private void UpdateUIInformation()
    {
        if (!string.IsNullOrEmpty(userEntityData.EntityId))
        {
            IDField.text = userEntityData.EntityId;
            TypeField.text = userEntityData.EntityType;
            NameField.text = userEntityData.Name;
            AgeField.text = userEntityData.Age;
        }
        else
        {
            IDField.text = DEFAULT_EMPTY_FIELD;
            TypeField.text = DEFAULT_EMPTY_FIELD;
            NameField.text = userEntityData.Name;
            AgeField.text = userEntityData.Age;
        }

        IsInteractable = true;
    }

    #endregion

    #region Entity Functionality

    private void OnCreateEntitySuccess(string response, object _)
    {
        BCManager.LogMessage("Created Entity for User", response);

        var data = (JsonReader.Deserialize(response) as Dictionary<string, object>)["data"] as Dictionary<string, object>;

        userEntityData.EntityType = DEFAULT_ENTITY_TYPE;
        userEntityData.UpdateFromJSON(data);

        FetchButton.gameObject.SetActive(false);
        CreateButton.gameObject.SetActive(false);
        SaveButton.gameObject.SetActive(false);
        DeleteButton.gameObject.SetActive(false);

        OnSaveButton();
    }

    private void OnUpdateEntitySuccess(string response, object _)
    {
        BCManager.LogMessage("Updated Entity for User", response);

        FetchButton.gameObject.SetActive(false);
        CreateButton.gameObject.SetActive(false);
        SaveButton.gameObject.SetActive(true);
        DeleteButton.gameObject.SetActive(true);

        UpdateUIInformation();
    }

    private void OnDeleteEntitySuccess(string response, object _)
    {
        BCManager.LogMessage("Deleted Entity for User", response);

        FetchButton.gameObject.SetActive(false);
        CreateButton.gameObject.SetActive(true);
        SaveButton.gameObject.SetActive(false);
        DeleteButton.gameObject.SetActive(false);

        UpdateUIInformation();
    }

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
                                    { "entityType", "player" }
                                }},
            { "sortCriteria", new Dictionary<string, object>
                              {
                                  { "createdAt", 1 },
                                  { "updatedAt", -1 }
                              }}
        };

        entityService.GetPage(JsonWriter.Serialize(context), HandleGetPageSuccess,
                                                             BCManager.CreateFailureCallback("GetPage Failed", UpdateUIInformation));
    }

    private void HandleGetPageSuccess(string response, object _)
    {
        BCManager.LogMessage("GetPage Success", response);

        userEntityData = BCEntity.CreateEmpty();

        var responseObj = JsonReader.Deserialize(response) as Dictionary<string, object>;
        var dataObj = responseObj["data"] as Dictionary<string, object>;
        var results = dataObj["results"] as Dictionary<string, object>;

        if (results["items"] is not Dictionary<string, object>[] itemsObj || itemsObj.Length <= 0)
        {
            Debug.LogWarning("No entities were found for this user.");
            UpdateUIInformation();
            return;
        }

        userEntityData.CreateFromPageItemJSON(itemsObj[0]);

        UpdateUIInformation();
    }

    #endregion
}
