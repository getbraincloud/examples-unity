using BrainCloud;
using BrainCloud.JsonFx.Json;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// <para>
/// Example of how user data can be handled via brainCloud's Entity service.
/// </para>
/// 
/// <seealso cref="BrainCloudEntity"/>
/// </summary>
/// API Link: https://getbraincloud.com/apidocs/apiref/?csharp#capi-entity
public class EntityServiceUI : ContentUIBehaviour
{
    private const int MINIMUM_REGISTRATION_NAME_LENGTH = 3;
    private const int MINIMUM_REGISTRATION_AGE = 13;
    private const int MAXIMUM_REGISTRATION_AGE = 120;
    private const string DEFAULT_EMPTY_FIELD = "---";
    private const string DEFAULT_ENTITY_TYPE = "user";

    [Header("Main")]
    [SerializeField] private TMP_Text IDField = default;
    [SerializeField] private TMP_Text TypeField = default;
    [SerializeField] private TMP_InputField NameField = default;
    [SerializeField] private TMP_InputField AgeField = default;
    [SerializeField] private Button CreateButton = default;
    [SerializeField] private Button SaveButton = default;
    [SerializeField] private Button DeleteButton = default;

    private BCEntity userEntity = default;
    private BrainCloudEntity entityService = default;

    #region Unity Messages

    protected override void Awake()
    {
        IDField.text = DEFAULT_EMPTY_FIELD;
        TypeField.text = DEFAULT_EMPTY_FIELD;
        NameField.text = string.Empty;
        AgeField.text = string.Empty;

        base.Awake();
    }

    private void OnEnable()
    {
        AgeField.onEndEdit.AddListener(OnAgeFieldEndEdit);
        CreateButton.onClick.AddListener(OnCreateButton);
        SaveButton.onClick.AddListener(OnSaveButton);
        DeleteButton.onClick.AddListener(OnDeleteButton);
    }

    protected override void Start()
    {
        entityService = BCManager.EntityService;

        InitializeUI();

        base.Start();
    }

    private void OnDisable()
    {
        AgeField.onEndEdit.RemoveAllListeners();
        CreateButton.onClick.RemoveAllListeners();
        SaveButton.onClick.RemoveAllListeners();
        DeleteButton.onClick.RemoveAllListeners();
    }

    protected override void OnDestroy()
    {
        entityService = null;

        base.OnDestroy();
    }

    #endregion

    #region UI

    protected override void InitializeUI()
    {
        ClearFields();

        CreateButton.gameObject.SetActive(true);
        SaveButton.gameObject.SetActive(false);
        DeleteButton.gameObject.SetActive(false);

        HandleGetUserEntity();
    }

    private void ClearFields()
    {
        userEntity = BCEntity.Create(DEFAULT_ENTITY_TYPE);

        IDField.text = DEFAULT_EMPTY_FIELD;
        TypeField.text = DEFAULT_EMPTY_FIELD;
        NameField.text = string.Empty;
        NameField.DisplayNormal();
        AgeField.text = string.Empty;
        AgeField.DisplayNormal();
    }

    private void UpdateUIInformation()
    {
        if (!userEntity.EntityId.IsEmpty())
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

    private void OnAgeFieldEndEdit(string value)
    {
        AgeField.text = value.Trim();
        if (!AgeField.text.IsEmpty())
        {
            if (int.TryParse(AgeField.text, out int result))
            {
                result = result < 0 ? 0 : result;
                AgeField.text = result.ToString();
            }
            else
            {
                AgeField.text = string.Empty;
            }
        }
    }

    private bool CheckNameVerification(string value)
    {
        NameField.text = value.Trim();
        if (!NameField.text.IsEmpty())
        {
            if (NameField.text.Length < MINIMUM_REGISTRATION_NAME_LENGTH)
            {
                NameField.DisplayError();
                Logger.LogError($"APP - Please use with a name with at least {MINIMUM_REGISTRATION_NAME_LENGTH} characters.");
                return false;
            }

            return true;
        }

        return false;
    }

    private bool CheckAgeVerification(string value)
    {
        AgeField.text = value.Trim();
        if (!AgeField.text.IsEmpty())
        {
            if (int.TryParse(AgeField.text, out int result))
            {
                if (result < MINIMUM_REGISTRATION_AGE)
                {
                    AgeField.text = result < 0 ? 0.ToString() : AgeField.text;
                    AgeField.DisplayError();
                    Logger.LogError($"APP - Please use an age of at least {MINIMUM_REGISTRATION_AGE} years old.");
                    return false;
                }
                else if (result > MAXIMUM_REGISTRATION_AGE)
                {
                    AgeField.DisplayError();
                    Logger.LogError("APP - Please use a valid age.");
                    return false;
                }

                return true;
            }

            AgeField.DisplayError();
            Logger.LogError("APP - Please use a valid age.");
        }

        return false;
    }

    private void OnCreateButton()
    {
        string inputName = NameField.text;
        string inputAge = AgeField.text;

        if (CheckNameVerification(inputName) && CheckAgeVerification(inputAge))
        {
            IsInteractable = false;

            userEntity = BCEntity.Create(DEFAULT_ENTITY_TYPE, inputName, inputAge);

            entityService.CreateEntity(userEntity.EntityType,
                                       JsonWriter.Serialize(userEntity.DataToJson()),
                                       userEntity.ACL.ToJsonString(),
                                       OnCreateEntity_Success,
                                       OnFailure("CreateEntity Failed"));
        }
        else if (!inputName.IsEmpty())
        {
            NameField.DisplayError();
            Logger.LogError("APP - Please enter a valid name.");
        }
        else if (!inputAge.IsEmpty())
        {
            AgeField.DisplayError();
            Logger.LogError("APP - Please enter a valid age.");
        }
    }

    private void OnSaveButton()
    {
        string inputName = NameField.text;
        string inputAge = AgeField.text;

        if (userEntity.EntityId.IsEmpty())
        {
            Logger.LogError("APP - Entity ID is blank. Has an Entity been created yet?");
            ClearFields();
            return;
        }
        else if (CheckNameVerification(inputName) && CheckAgeVerification(inputAge))
        {
            IsInteractable = false;

            userEntity.Update(inputName, inputAge);

            entityService.UpdateEntity(userEntity.EntityId,
                                       userEntity.EntityType,
                                       JsonWriter.Serialize(userEntity.DataToJson()),
                                       userEntity.ACL.ToJsonString(),
                                       -1,
                                       OnUpdateEntity_Success,
                                       OnFailure("UpdateEntity Failed"));
        }

        if (inputName.IsEmpty())
        {
            NameField.DisplayError();
            Logger.LogError("APP - Please enter a valid name.");
        }

        if (inputAge.IsEmpty())
        {
            AgeField.DisplayError();
            Logger.LogError("APP - Please enter a valid age.");
        }
    }

    private void OnDeleteButton()
    {
        if (userEntity.EntityId.IsEmpty())
        {
            Logger.LogError("APP - Entity ID is blank. Has an Entity been created yet?");
            ClearFields();
            return;
        }
        else
        {
            IsInteractable = false;

            entityService.DeleteEntity(userEntity.EntityId,
                                       -1,
                                       OnDeleteEntity_Success,
                                       OnFailure("DeleteEntity Failed"));
        }
    }

    #endregion

    #region brainCloud

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

        entityService.GetPage(JsonWriter.Serialize(context),
                              OnGetPage_Success,
                              OnFailure("GetPage Failed"));
    }

    private void OnGetPage_Success(string response, object _)
    {
        BCManager.LogMessage("GetPage Success", response);

        var responseObj = JsonReader.Deserialize(response) as Dictionary<string, object>;
        var dataObj = responseObj["data"] as Dictionary<string, object>;
        var resultsObj = dataObj["results"] as Dictionary<string, object>;

        if (resultsObj["items"] is not Dictionary<string, object>[] data || data.Length <= 0)
        {
            Logger.LogError("APP - No entities were found for this user.");
            ClearFields();
            IsInteractable = true;
            return;
        }

        userEntity.CreateFromJson(data[0]);

        CreateButton.gameObject.SetActive(false);
        SaveButton.gameObject.SetActive(true);
        DeleteButton.gameObject.SetActive(true);

        UpdateUIInformation();
    }

    private void OnCreateEntity_Success(string response, object _)
    {
        BCManager.LogMessage("Created Entity for User", response);

        var data = (JsonReader.Deserialize(response) as Dictionary<string, object>)["data"] as Dictionary<string, object>;
        string entityID = (string)data["entityId"];

        BCManager.EntityService.GetEntity(entityID, OnGetEntity_Success);
    }

    private void OnUpdateEntity_Success(string response, object _)
    {
        BCManager.LogMessage("Updated Entity for User", response);

        var data = (JsonReader.Deserialize(response) as Dictionary<string, object>)["data"] as Dictionary<string, object>;
        string entityID = (string)data["entityId"];

        BCManager.EntityService.GetEntity(entityID, OnGetEntity_Success);
    }

    private void OnDeleteEntity_Success(string response, object _)
    {
        BCManager.LogMessage("Deleted Entity for User", response);

        userEntity = BCEntity.Create(DEFAULT_ENTITY_TYPE);

        ClearFields();
        IsInteractable = true;
    }

    private void OnGetEntity_Success(string response, object _)
    {
        BCManager.LogMessage("Updating Local Entity Data...", response);

        var data = (JsonReader.Deserialize(response) as Dictionary<string, object>)["data"] as Dictionary<string, object>;

        userEntity.UpdateFromJSON(data);

        CreateButton.gameObject.SetActive(false);
        SaveButton.gameObject.SetActive(true);
        DeleteButton.gameObject.SetActive(true);

        UpdateUIInformation();
    }

    private FailureCallback OnFailure(string errorMessage) =>
        BCManager.CreateFailureCallback(errorMessage, UpdateUIInformation);

    #endregion
}
