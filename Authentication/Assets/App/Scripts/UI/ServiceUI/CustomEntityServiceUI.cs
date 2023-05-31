using BrainCloud;
using BrainCloud.Common;
using BrainCloud.JsonFx.Json;
using BrainCloud.JSONHelper;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Random = UnityEngine.Random;

/// <summary>
/// <para>
/// Example of how custom data can be handled via brainCloud's CustomEntity service.
/// </para>
/// 
/// <seealso cref="BrainCloudCustomEntity"/>
/// </summary>
/// API Link: https://getbraincloud.com/apidocs/apiref/?csharp#capi-customentity
public class CustomEntityServiceUI : ContentUIBehaviour
{
    private const string DEFAULT_EMPTY_FIELD = "---";
    private const string CURRENT_ENTITY_LABEL_FORMAT = "Current Custom Entity: {0}/{1}";

    private static readonly string DAYS_TO_EXPIRE = TimeSpan.FromDays(1).TotalMilliseconds.ToString(); // Expiry time for Custom Entity (default is 1 day)

    private static readonly Dictionary<string, string> DATA_TYPES = new Dictionary<string, string>()
    {
        { HockeyStatsData.DataType, "Hockey Player Stats" }, { RPGData.DataType, "RPG Character Stats" }
    };

    private static readonly HockeyStatsData[] PRE_MADE_HOCKEY_PLAYER_STATS =
    {
        new HockeyStatsData("Gordie Howe",     HockeyStatsData.FieldPosition.RightWing,   801, 1049),
        new HockeyStatsData("Wayne Gretzky",   HockeyStatsData.FieldPosition.Center,      894, 1963),
        new HockeyStatsData("Bobby Orr",       HockeyStatsData.FieldPosition.LeftDefense, 270, 645),
        new HockeyStatsData("Mario Lemieux",   HockeyStatsData.FieldPosition.Center,      690, 1033),
        new HockeyStatsData("Maurice Richard", HockeyStatsData.FieldPosition.RightWing,   544, 422),
        new HockeyStatsData("Jean Beliveau",   HockeyStatsData.FieldPosition.Center,      507, 712),
        new HockeyStatsData("Mark Messier",    HockeyStatsData.FieldPosition.LeftWing,    694, 1193),
        new HockeyStatsData("Guy Lafleur",     HockeyStatsData.FieldPosition.RightWing,   560, 793),
        new HockeyStatsData("Bobby Hull",      HockeyStatsData.FieldPosition.LeftWing,    610, 560),
        new HockeyStatsData("Sidney Crosby",   HockeyStatsData.FieldPosition.Center,      546, 944)
    };

    private static readonly RPGData[] PRE_MADE_RPG_CHARACTERS =
    {
        new RPGData("Willow",           "magician",          2, 123,   7,  2),
        new RPGData("Ali Baba",         "woodcutter",        7, 1458, 17,  5),
        new RPGData("Aang",             "avatar",           12, 183,  11,  6),
        new RPGData("Bilbo Baggins",    "hobbit",           14, 3777, 18,  7),
        new RPGData("San",              "princess",         34, 2538, 22, 22),
        new RPGData("Tyrion Lannister", "hand_of_the_king", 42, 1996, 24, 14),
        new RPGData("Roland Deschain",  "gunslinger",       57, 3770, 52, 23),
        new RPGData("Jareth",           "goblin_king",      69, 4541, 48, 25),
        new RPGData("Mary Poppins",     "nanny",            81, 5892, 99, 42),
        new RPGData("Gandalf",          "wizard",           98, 9500, 80, 94)
    };

    [Header("Main")]
    [SerializeField] private TMP_Text LoadingLabel = default;
    [SerializeField] private TMP_Text ErrorLabel = default;
    [SerializeField] private TMP_Text IDField = default;
    [SerializeField] private TMP_Text TypeField = default;

    [Header("Custom Entity Management")]
    [SerializeField] private HockeyStatsDataUI HockeyStatsUI = default;
    [SerializeField] private RPGDataUI RPGUI = default;
    [SerializeField] private TMP_Text CurrentEntityLabel = default;
    [SerializeField] private Button PreviousButton = default;
    [SerializeField] private Button NextButton = default;

    [Header("Custom Entity Creation")]
    [SerializeField] private TMP_Dropdown EntityTypeDropdown = default;
    [SerializeField] private Button NewButton = default;

    private int currentIndex = 0;
    private string selectedEntityType = string.Empty;
    private List<CustomEntity> customEntities = default;
    private BrainCloudCustomEntity customEntityService = default;

    #region Unity Messages

    protected override void Awake()
    {
        IDField.text = DEFAULT_EMPTY_FIELD;
        TypeField.text = DEFAULT_EMPTY_FIELD;

        base.Awake();
    }

    private void OnEnable()
    {
        HockeyStatsUI.UpdateButtonAction = (hockeyData) => OnUpdateButton(hockeyData);
        RPGUI.UpdateButtonAction = (rpgData) => OnUpdateButton(rpgData);
        HockeyStatsUI.DeleteButtonAction = OnDeleteButton;
        RPGUI.DeleteButtonAction = OnDeleteButton;
        PreviousButton.onClick.AddListener(OnPreviousButton);
        NextButton.onClick.AddListener(OnNextButton);
        EntityTypeDropdown.onValueChanged.AddListener(OnEntityTypeDropdown);
        NewButton.onClick.AddListener(OnNewButton);
    }

    protected override void Start()
    {
        customEntities = new List<CustomEntity>();
        customEntityService = BCManager.CustomEntityService;

        EntityTypeDropdown.AddOptions(new List<string>(DATA_TYPES.Values));

        StartCoroutine(DelayedInitialization());

        base.Start();
    }

    private void OnDisable()
    {
        HockeyStatsUI.UpdateButtonAction = null;
        RPGUI.UpdateButtonAction = null;
        HockeyStatsUI.DeleteButtonAction = null;
        RPGUI.DeleteButtonAction = null;
        PreviousButton.onClick.RemoveAllListeners();
        NextButton.onClick.RemoveAllListeners();
        EntityTypeDropdown.onValueChanged.RemoveAllListeners();
        NewButton.onClick.RemoveAllListeners();
    }

    protected override void OnDestroy()
    {
        customEntityService = null;

        base.OnDestroy();
    }

    #endregion

    #region UI

    protected override void InitializeUI()
    {
        ClearFields();

        LoadingLabel.gameObject.SetActive(true);
        ErrorLabel.gameObject.SetActive(false);

        HockeyStatsUI.IsInteractable = false;
        RPGUI.IsInteractable = false;
        PreviousButton.interactable = false;
        NextButton.interactable = false;

        HockeyStatsUI.gameObject.SetActive(true);
        RPGUI.gameObject.SetActive(false);

        customEntities.Clear();

        OnEntityTypeDropdown(0);
        HandleGetCustomEntities();
    }

    private IEnumerator DelayedInitialization()
    {
        yield return new WaitForEndOfFrame();

        InitializeUI();
    }

    private void ClearFields()
    {
        IDField.text = DEFAULT_EMPTY_FIELD;
        TypeField.text = DEFAULT_EMPTY_FIELD;
        HockeyStatsUI.ResetUI();
        RPGUI.ResetUI();
        CurrentEntityLabel.text = string.Format(CURRENT_ENTITY_LABEL_FORMAT, DEFAULT_EMPTY_FIELD, DEFAULT_EMPTY_FIELD);
    }

    private void GetCurrentEntityIndex()
    {
        if (customEntities.IsNullOrEmpty())
        {
            Debug.LogWarning("No custom entities were found.");
            ClearFields();
            return;
        }

        CurrentEntityLabel.text = string.Format(CURRENT_ENTITY_LABEL_FORMAT, currentIndex + 1, customEntities.Count);

        CustomEntity current = customEntities[currentIndex];

        IDField.text = current.EntityID;
        TypeField.text = current.GetDataType();
        if (current.Data is HockeyStatsData hockeyData)
        {
            HockeyStatsUI.gameObject.SetActive(true);
            HockeyStatsUI.IsInteractable = true;
            RPGUI.gameObject.SetActive(false);
            RPGUI.IsInteractable = false;

            HockeyStatsUI.UpdateUI(current.IsOwned, hockeyData);
        }
        else if (current.Data is RPGData rpgData)
        {
            HockeyStatsUI.gameObject.SetActive(false);
            HockeyStatsUI.IsInteractable = false;
            RPGUI.gameObject.SetActive(true);
            RPGUI.IsInteractable = true;

            RPGUI.UpdateUI(current.IsOwned, rpgData);
        }

        PreviousButton.interactable = currentIndex > 0;
        NextButton.interactable = currentIndex < customEntities.Count - 1;
    }

    private void OnPreviousButton()
    {
        currentIndex = --currentIndex <= 0 ? 0 : currentIndex;
        GetCurrentEntityIndex();
    }

    private void OnNextButton()
    {
        currentIndex = ++currentIndex >= customEntities.Count ? customEntities.Count - 1 : currentIndex;
        GetCurrentEntityIndex();
    }

    private void OnEntityTypeDropdown(int option)
    {
        selectedEntityType = new List<string>(DATA_TYPES.Keys)[option];
    }

    private void OnNewButton()
    {
        IsInteractable = false;

        string dataJson;
        if (selectedEntityType == HockeyStatsData.DataType)
        {
            dataJson = PRE_MADE_HOCKEY_PLAYER_STATS[Random.Range(0, PRE_MADE_HOCKEY_PLAYER_STATS.Length)].Serialize();
        }
        else // RPGData.DataType
        {
            dataJson = PRE_MADE_RPG_CHARACTERS[Random.Range(0, PRE_MADE_RPG_CHARACTERS.Length)].Serialize();
        }

        customEntityService.CreateEntity(selectedEntityType,
                                         dataJson,
                                         new ACL(ACL.Access.ReadWrite).ToJsonString(),
                                         DAYS_TO_EXPIRE,
                                         true,
                                         OnSuccess($"Created Custom Entity ({selectedEntityType})", OnCreateEntity_Success),
                                         OnFailure("CreateEntity Failed", () => IsInteractable = true));
    }

    private void OnUpdateButton(IJSON hockeyData)
    {
        CustomEntity current = customEntities[currentIndex];

        if (current.EntityID.IsEmpty())
        {
            Debug.LogWarning($"Entity ID is blank...");
            ClearFields();
            return;
        }

        IsInteractable = false;

        current.Data = hockeyData;

        customEntityService.UpdateEntity(current.GetDataType(),
                                         current.EntityID,
                                         -1,
                                         current.Data.Serialize(),
                                         current.ACL.ToJsonString(),
                                         current.TimeToLive.Milliseconds.ToString(),
                                         OnSuccess($"Updated Custom Entity ({current.GetDataType()})", OnUpdateEntity_Success),
                                         OnFailure("UpdateEntity Failed", () => IsInteractable = true));
    }

    private void OnDeleteButton()
    {
        CustomEntity current = customEntities[currentIndex];

        if (current.EntityID.IsEmpty())
        {
            Debug.LogWarning($"Entity ID is blank...");
            ClearFields();
            return;
        }

        IsInteractable = false;

        customEntityService.DeleteEntity(current.GetDataType(),
                                         current.EntityID,
                                         -1,
                                         OnSuccess($"Deleted Custom Entity ({current.GetDataType()})", OnDeleteEntity_Success),
                                         OnFailure("DeleteEntity Failed", () => IsInteractable = true));
    }

    #endregion

    #region brainCloud

    private void HandleGetCustomEntities()
    {
        IsInteractable = false;

        string context = JsonWriter.Serialize(new Dictionary<string, object>
        {
            { "pagination", new Dictionary<string, object>
                {
                    { "rowsPerPage", 50 },
                    { "pageNumber", 1 }
                }},
            { "optionCriteria", new Dictionary<string, object>
                {
                    { "ownedOnly", false }
                }},
            { "sortCriteria", new Dictionary<string, object>
                {
                    { "createdAt", 1 },
                    { "updatedAt", -1 }
                }}
        });

        // Get HockeyStatsData first, then RPGData
        SuccessCallback onGetHockeyDataSuccess = OnSuccess($"GetEntityPage ({HockeyStatsData.DataType}) Success", (response) =>
        {
            OnGetPage_Success(response);

            SuccessCallback onGetRPGDataSuccess = OnSuccess($"GetEntityPage ({RPGData.DataType}) Success", (response) =>
            {
                OnGetPage_Success(response);

                if (customEntities.IsNullOrEmpty())
                {
                    Debug.LogWarning("No custom entities were found.");
                    ClearFields();
                }
                else
                {
                    currentIndex = 0;
                    GetCurrentEntityIndex();
                }

                IsInteractable = true;
                LoadingLabel.gameObject.SetActive(false);
                ErrorLabel.gameObject.SetActive(false);
            });

            customEntityService.GetEntityPage(RPGData.DataType,
                                          context,
                                          onGetRPGDataSuccess,
                                          OnFailure($"GetEntityPage ({RPGData.DataType}) Failed", OnGetPage_Failed));
        });

        customEntityService.GetEntityPage(HockeyStatsData.DataType,
                                          context,
                                          onGetHockeyDataSuccess,
                                          OnFailure($"GetEntityPage ({HockeyStatsData.DataType}) Failed", OnGetPage_Failed));
    }

    private void OnGetPage_Success(string response)
    {
        var responseObj = JsonReader.Deserialize(response) as Dictionary<string, object>;
        var dataObj = responseObj["data"] as Dictionary<string, object>;
        var resultsObj = dataObj["results"] as Dictionary<string, object>;

        if (resultsObj["items"] is not Dictionary<string, object>[] data || data.Length <= 0)
        {
            return;
        }

        CustomEntity newCustomEntity;
        for (int i = 0; i < data.Length; i++)
        {
            newCustomEntity = new CustomEntity();
            newCustomEntity.Deserialize(data[i]);
            customEntities.Add(newCustomEntity);
        }
    }

    private void OnGetPage_Failed(ErrorResponse response)
    {
        LoadingLabel.gameObject.SetActive(false);
        ErrorLabel.gameObject.SetActive(true);

        if (response.ReasonCode == 40570 || response.Message.ToLower().Contains("no custom entity config exists"))
        {
            string error = $"Custom Entities has not been enabled for this app on brainCloud (App ID: {BCManager.Client.AppId}).";
            ErrorLabel.text = error;
            Debug.LogError(error);
        }

        IsInteractable = false;
    }

    private void OnCreateEntity_Success(string response)
    {
        var data = (JsonReader.Deserialize(response) as Dictionary<string, object>)["data"] as Dictionary<string, object>;

        CustomEntity newCustomEntity = new CustomEntity();
        newCustomEntity.Deserialize(data);
        customEntities.Add(newCustomEntity);

        currentIndex = customEntities.Count - 1;

        if (newCustomEntity.Data == null || !data.ContainsKey("data"))
        {
            customEntityService.ReadEntity(newCustomEntity.GetDataType(),
                                           newCustomEntity.EntityID,
                                           OnSuccess($"Reading Custom Entity ({newCustomEntity.GetDataType()})", OnReadEntity_Success),
                                           OnFailure("Error Reading Custom Entity", () => IsInteractable = true));
        }
    }

    private void OnUpdateEntity_Success(string response)
    {
        var data = (JsonReader.Deserialize(response) as Dictionary<string, object>)["data"] as Dictionary<string, object>;

        customEntities[currentIndex].Deserialize(data);

        if (customEntities[currentIndex].Data == null || !data.ContainsKey("data"))
        {
            customEntityService.ReadEntity(customEntities[currentIndex].GetDataType(),
                                           customEntities[currentIndex].EntityID,
                                           OnSuccess($"Reading Custom Entity ({customEntities[currentIndex].GetDataType()})", OnReadEntity_Success),
                                           OnFailure("Error Reading Custom Entity", () => IsInteractable = true));
        }
    }

    private void OnDeleteEntity_Success()
    {
        customEntities.RemoveAt(currentIndex);
        OnPreviousButton();

        IsInteractable = true;
    }

    private void OnReadEntity_Success(string response)
    {
        var data = (JsonReader.Deserialize(response) as Dictionary<string, object>)["data"] as Dictionary<string, object>;
        string entityID = data["entityId"] as string;

        ClearFields();

        for (int i = 0; i < customEntities.Count; i++)
        {
            if (entityID == customEntities[i].EntityID)
            {
                currentIndex = i;
                customEntities[currentIndex].Deserialize(data);
                GetCurrentEntityIndex();

                break;
            }
        }

        IsInteractable = true;
    }

    #endregion
}
