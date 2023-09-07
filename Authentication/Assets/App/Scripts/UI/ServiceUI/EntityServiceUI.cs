using BrainCloud;
using BrainCloud.JSONHelper;
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

    [Header("Main")]
    [SerializeField] private TMP_Text IDField = default;
    [SerializeField] private TMP_Text TypeField = default;
    [SerializeField] private TMP_InputField NameField = default;
    [SerializeField] private TMP_InputField AgeField = default;
    [SerializeField] private Button CreateButton = default;
    [SerializeField] private Button SaveButton = default;
    [SerializeField] private Button DeleteButton = default;

    private Entity userEntity = default;
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
        HandleGetUserEntity();

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
        userEntity = new Entity(new UserData());

        IDField.text = DEFAULT_EMPTY_FIELD;
        TypeField.text = DEFAULT_EMPTY_FIELD;
        NameField.text = string.Empty;
        NameField.DisplayNormal();
        AgeField.text = string.Empty;
        AgeField.DisplayNormal();

        CreateButton.gameObject.SetActive(true);
        SaveButton.gameObject.SetActive(false);
        DeleteButton.gameObject.SetActive(false);
    }

    private void UpdateUIInformation()
    {
        if (!userEntity.entityId.IsEmpty())
        {
            IDField.text = userEntity.entityId;
            TypeField.text = userEntity.GetDataType();
        }
        else
        {
            IDField.text = DEFAULT_EMPTY_FIELD;
            TypeField.text = DEFAULT_EMPTY_FIELD;
        }

        NameField.text = ((UserData)userEntity.data).name;
        AgeField.text = ((UserData)userEntity.data).age;

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
                Debug.LogError($"Please use with a name with at least {MINIMUM_REGISTRATION_NAME_LENGTH} characters.");
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
                    Debug.LogError($"Please use an age of at least {MINIMUM_REGISTRATION_AGE} years old.");
                    return false;
                }
                else if (result > MAXIMUM_REGISTRATION_AGE)
                {
                    AgeField.DisplayError();
                    Debug.LogError("Please use a valid age.");
                    return false;
                }

                return true;
            }

            AgeField.DisplayError();
            Debug.LogError("Please use a valid age.");
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

            userEntity = new Entity(new UserData(inputName, inputAge));

            entityService.CreateEntity(userEntity.GetDataType(),
                                       userEntity.data.Serialize(),
                                       userEntity.acl.ToJsonString(),
                                       OnSuccess("Created Entity for User", OnCreateEntity_Success),
                                       OnFailure("CreateEntity Failed", UpdateUIInformation));
        }
        else if (!inputName.IsEmpty())
        {
            NameField.DisplayError();
            Debug.LogError("Please enter a valid name.");
        }
        else if (!inputAge.IsEmpty())
        {
            AgeField.DisplayError();
            Debug.LogError("Please enter a valid age.");
        }
    }

    private void OnSaveButton()
    {
        string inputName = NameField.text;
        string inputAge = AgeField.text;

        if (userEntity.entityId.IsEmpty())
        {
            Debug.LogError("Entity ID is blank. Has an Entity been created yet?");
            InitializeUI();
            return;
        }
        else if (CheckNameVerification(inputName) && CheckAgeVerification(inputAge))
        {
            IsInteractable = false;

            userEntity.data = new UserData(inputName, inputAge);

            entityService.UpdateEntity(userEntity.entityId,
                                       userEntity.GetDataType(),
                                       userEntity.data.Serialize(),
                                       userEntity.acl.ToJsonString(),
                                       -1,
                                       OnSuccess("Updated Entity for User", OnUpdateEntity_Success),
                                       OnFailure("UpdateEntity Failed", UpdateUIInformation));
        }

        if (inputName.IsEmpty())
        {
            NameField.DisplayError();
            Debug.LogError("Please enter a valid name.");
        }

        if (inputAge.IsEmpty())
        {
            AgeField.DisplayError();
            Debug.LogError("Please enter a valid age.");
        }
    }

    private void OnDeleteButton()
    {
        if (userEntity.entityId.IsEmpty())
        {
            Debug.LogError("Entity ID is blank. Has an Entity been created yet?");
            InitializeUI();
            return;
        }
        else
        {
            IsInteractable = false;

            entityService.DeleteEntity(userEntity.entityId,
                                       -1,
                                       OnSuccess("Deleted Entity for User", OnDeleteEntity_Success),
                                       OnFailure("DeleteEntity Failed", UpdateUIInformation));
        }
    }

    #endregion

    #region brainCloud

    private void HandleGetUserEntity()
    {
        IsInteractable = false;

        string context = new Dictionary<string, object>
        {
            { "pagination", new Dictionary<string, object>
                {
                    { "rowsPerPage", 50 },
                    { "pageNumber", 1 }
                }},
            { "searchCriteria", new Dictionary<string, object>
                {
                    { "entityType", userEntity.GetDataType() }
                }},
            { "sortCriteria", new Dictionary<string, object>
                {
                    { "createdAt", 1 },
                    { "updatedAt", -1 }
                }}
        }.Serialize();

        entityService.GetPage(context,
                              OnSuccess("GetPage Success", OnGetPage_Success),
                              OnFailure("GetPage Failed", UpdateUIInformation));
    }

    private void OnGetPage_Success(string response)
    {
        var data = response.Deserialize("data", "results").GetJSONArray("items");
        if (data.IsNullOrEmpty())
        {
            Debug.LogWarning("No entities were found for this user.");
            InitializeUI();
            IsInteractable = true;
            return;
        }

        userEntity.FromJSONObject(data[0]);

        CreateButton.gameObject.SetActive(false);
        SaveButton.gameObject.SetActive(true);
        DeleteButton.gameObject.SetActive(true);

        UpdateUIInformation();
    }

    private void OnCreateEntity_Success(string response)
    {
        string entityID = response.Deserialize("data").GetString("entityId");

        BCManager.EntityService.GetEntity(entityID, OnSuccess("Updating Local Entity Data...", OnGetEntity_Success));
    }

    private void OnUpdateEntity_Success(string response)
    {
        string entityID = response.Deserialize("data").GetString("entityId");

        BCManager.EntityService.GetEntity(entityID, OnSuccess("Updating Local Entity Data...", OnGetEntity_Success));
    }

    private void OnDeleteEntity_Success(string response)
    {
        userEntity = new Entity(new UserData());

        InitializeUI();
        IsInteractable = true;
    }

    private void OnGetEntity_Success(string response)
    {
        userEntity = response.Deserialize<Entity>("data");

        CreateButton.gameObject.SetActive(false);
        SaveButton.gameObject.SetActive(true);
        DeleteButton.gameObject.SetActive(true);

        UpdateUIInformation();
    }

    #endregion
}
