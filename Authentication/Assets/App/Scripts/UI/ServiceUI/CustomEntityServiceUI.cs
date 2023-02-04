using BrainCloud;
using BrainCloud.Entity;
using BrainCloud.JsonFx.Json;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CustomEntityServiceUI : MonoBehaviour, IServiceUI
{
    private const string DEFAULT_EMPTY_FIELD = "---";
    private const string DEFAULT_ENTITY_TYPE = "athlete";

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

    private BCCustomEntity customEntity = default;
    private BrainCloudCustomEntity customEntityService = default;

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
        customEntityService = BCManager.CustomEntityService;

        customEntity = BCCustomEntity.Create(DEFAULT_ENTITY_TYPE);

        SaveButton.gameObject.SetActive(false);
        DeleteButton.gameObject.SetActive(false);

        HandleGetCustomEntity();
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
        customEntityService = null;
    }

    #endregion

    #region UI

    private void ResetUIState()
    {
        customEntity = BCCustomEntity.Create(DEFAULT_ENTITY_TYPE);

        IDField.text = DEFAULT_EMPTY_FIELD;
        TypeField.text = DEFAULT_EMPTY_FIELD;
        NameField.text = string.Empty;
        AgeField.text = string.Empty;

        IsInteractable = true;
    }

    private void OnCreateButton()
    {
        IsInteractable = false;

        customEntity = BCCustomEntity.Create(DEFAULT_ENTITY_TYPE, NameField.text, AgeField.text);

        customEntityService.CreateEntity(customEntity.EntityType,
                                         JsonWriter.Serialize(customEntity.DataToJson()),
                                         customEntity.ACL.ToJsonString(),
                                         null,
                                         customEntity.IsOwned,
                                         OnCreateEntitySuccess,
                                         HandleDefaultFailureCallback("CreateEntity Failed"));
    }

    private void OnFetchButton()
    {
        // TODO: Will restructure this to make more sense...
    }

    private void OnSaveButton()
    {
        IsInteractable = false;

        if (customEntity.EntityId.IsNullOrEmpty())
        {
            Debug.LogWarning($"Entity ID is blank...");
            ResetUIState();
            return;
        }

        customEntity.Update(NameField.text, AgeField.text, AgeField.text);

        customEntityService.UpdateEntity(customEntity.EntityType,
                                         customEntity.EntityId,
                                         -1,
                                         JsonWriter.Serialize(customEntity.DataToJson()),
                                         customEntity.ACL.ToJsonString(),
                                         null,
                                         OnUpdateEntitySuccess,
                                         HandleDefaultFailureCallback("UpdateEntity Failed"));
    }

    private void OnDeleteButton()
    {
        IsInteractable = false;

        if (customEntity.EntityId.IsNullOrEmpty())
        {
            Debug.LogWarning($"Entity ID is blank...");
            ResetUIState();
            return;
        }

        customEntityService.DeleteEntity(customEntity.EntityType,
                                         customEntity.EntityId,
                                         -1,
                                         OnDeleteEntitySuccess,
                                         HandleDefaultFailureCallback("DeleteEntity Failed"));
    }

    private void UpdateUIInformation()
    {
        if (!string.IsNullOrEmpty(customEntity.EntityId))
        {
            IDField.text = customEntity.EntityId;
            TypeField.text = customEntity.EntityType;
        }
        else
        {
            IDField.text = DEFAULT_EMPTY_FIELD;
            TypeField.text = DEFAULT_EMPTY_FIELD;
        }

        NameField.text = customEntity.Name;
        AgeField.text = customEntity.Goals.ToString();

        IsInteractable = true;
    }

    #endregion

    #region Entity

    private void HandleGetCustomEntity()
    {
        IsInteractable = false;

        var context = new Dictionary<string, object>
        {
            { "pagination", new Dictionary<string, object>
                {
                    { "rowsPerPage", 50 },
                    { "pageNumber", 1 }
                }},
            { "optionCriteria", new Dictionary<string, object>
                {
                    { "ownedOnly", true }
                }},
            { "sortCriteria", new Dictionary<string, object>
                {
                    { "createdAt", 1 },
                    { "updatedAt", -1 }
                }}
        };

        customEntityService.GetEntityPage(DEFAULT_ENTITY_TYPE,
                                          JsonWriter.Serialize(context),
                                          HandleGetPageSuccess,
                                          HandleDefaultFailureCallback("GetEntityPage Failed"));
    }

    private void HandleGetPageSuccess(string response, object _)
    {
        BCManager.LogMessage("GetEntityPage Success", response);

        var responseObj = JsonReader.Deserialize(response) as Dictionary<string, object>;
        var dataObj = responseObj["data"] as Dictionary<string, object>;
        var resultsObj = dataObj["results"] as Dictionary<string, object>;

        if (resultsObj["items"] is not Dictionary<string, object>[] data || data.Length <= 0)
        {
            Debug.LogWarning("No custom entities were found that are owned for this user.");
            ResetUIState();
            return;
        }

        customEntity.CreateFromJson(true, data[0]);

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

        customEntity = BCCustomEntity.Create(DEFAULT_ENTITY_TYPE);

        ResetUIState();
    }

    private void HandleGetEntitySuccess(string response, object _)
    {
        BCManager.LogMessage("Updating Local Entity Data...", response);

        var data = (JsonReader.Deserialize(response) as Dictionary<string, object>)["data"] as Dictionary<string, object>;

        customEntity.UpdateFromJSON(true, data);

        CreateButton.gameObject.SetActive(false);
        SaveButton.gameObject.SetActive(true);
        DeleteButton.gameObject.SetActive(true);

        UpdateUIInformation();
    }

    private FailureCallback HandleDefaultFailureCallback(string errorMessage)
    {
        errorMessage = string.IsNullOrEmpty(errorMessage) ? "Failure" : errorMessage;
        return (status, reasonCode, jsonError, _) =>
        {
            BCManager.LogError(errorMessage, status, reasonCode, jsonError);
            UpdateUIInformation();
            IsInteractable = reasonCode != 40570; // Custom Entities not enabled for app
        };
    }

    #endregion
}
