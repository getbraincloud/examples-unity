using BrainCloud;
using BrainCloud.JsonFx.Json;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    private const int MAX_NUMBER_OF_CUSTOM_ENTITIES = 10;
    private const string DEFAULT_EMPTY_FIELD = "---";

    [Header("Main")]
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

    private int currentMax = 0;
    private int currentIndex = 0;
    private CustomEntity[] customEntities = default;
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
        //UpdateButton.onClick.AddListener(OnUpdateButton);
        //DeleteButton.onClick.AddListener(OnDeleteButton);
        NewButton.onClick.AddListener(OnNewButton);
    }

    protected override void Start()
    {
        customEntities = new CustomEntity[MAX_NUMBER_OF_CUSTOM_ENTITIES];
        customEntityService = BCManager.CustomEntityService;

        InitializeUI();

        base.Start();
    }

    private void OnDisable()
    {
        //UpdateButton.onClick.RemoveAllListeners();
        //DeleteButton.onClick.RemoveAllListeners();
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

        //UpdateButton.gameObject.SetActive(false);
        //DeleteButton.gameObject.SetActive(false);
        NewButton.gameObject.SetActive(true);

        HandleGetCustomEntities();
    }

    private void ClearFields()
    {
        IDField.text = DEFAULT_EMPTY_FIELD;
        TypeField.text = DEFAULT_EMPTY_FIELD;
        HockeyStatsUI.ResetUI();
        RPGUI.ResetUI();
    }

    private void OnNewButton()
    {
        //IsInteractable = false;
        //
        //customEntities = new CustomEntity(new HockeyPlayerData(NameField.text, AgeField.text));
        //
        //customEntityService.CreateEntity(customEntities.EntityType,
        //                                 customEntities.Data.Serialize(),
        //                                 customEntities.ACL.ToJsonString(),
        //                                 null,
        //                                 customEntities.IsOwned,
        //                                 OnSuccess("Created Entity for User", OnCreateEntity_Success),
        //                                 OnFailure("CreateEntity Failed", UpdateUIInformation));
    }

    private void OnUpdateButton()
    {
        //IsInteractable = false;
        //
        //if (customEntities.EntityID.IsEmpty())
        //{
        //    Debug.LogWarning($"Entity ID is blank...");
        //    ClearFields();
        //    IsInteractable = true;
        //    return;
        //}
        //
        //customEntities.SetData(new HockeyPlayerData(NameField.text, AgeField.text, 0, 0));
        //
        //customEntityService.UpdateEntity(customEntities.EntityType,
        //                                 customEntities.EntityID,
        //                                 -1,
        //                                 customEntities.Data.Serialize(),
        //                                 customEntities.ACL.ToJsonString(),
        //                                 null,
        //                                 OnSuccess("Updated Entity for User", OnUpdateEntity_Success),
        //                                 OnFailure("UpdateEntity Failed", UpdateUIInformation));
    }

    private void OnDeleteButton()
    {
        //IsInteractable = false;
        //
        //if (customEntities.EntityID.IsEmpty())
        //{
        //    Debug.LogWarning($"Entity ID is blank...");
        //    ClearFields();
        //    IsInteractable = true;
        //    return;
        //}
        //
        //customEntityService.DeleteEntity(customEntities.EntityType,
        //                                 customEntities.EntityID,
        //                                 -1,
        //                                 OnSuccess("Deleted Entity for User", OnDeleteEntity_Success),
        //                                 OnFailure("DeleteEntity Failed", UpdateUIInformation));
    }

    private void UpdateUIInformation()
    {
        //if (!customEntities.EntityID.IsEmpty())
        //{
        //    IDField.text = customEntities.EntityID;
        //    TypeField.text = customEntities.EntityType;
        //}
        //else
        //{
        //    IDField.text = DEFAULT_EMPTY_FIELD;
        //    TypeField.text = DEFAULT_EMPTY_FIELD;
        //}
        //
        //NameField.text = customEntities.GetData<HockeyPlayerData>().Name;
        //AgeField.text = customEntities.GetData<HockeyPlayerData>().Goals.ToString();

        IsInteractable = true;
    }

    #endregion

    #region brainCloud

    private void HandleGetCustomEntities()
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

        customEntityService.GetEntityPage("",
                                          JsonWriter.Serialize(context),
                                          OnSuccess("GetEntityPage Success", OnGetPage_Success),
                                          OnFailure("GetEntityPage Failed", UpdateUIInformation));
    }

    private void OnGetPage_Success(string response)
    {
        var responseObj = JsonReader.Deserialize(response) as Dictionary<string, object>;
        var dataObj = responseObj["data"] as Dictionary<string, object>;
        var resultsObj = dataObj["results"] as Dictionary<string, object>;

        if (resultsObj["items"] is not Dictionary<string, object>[] data || data.Length <= 0)
        {
            Debug.LogWarning("No custom entities were found that are owned for this user.");
            ClearFields();
            IsInteractable = true;
            return;
        }

        //customEntities.IsOwned = true;
        //customEntities.Deserialize(data[0]);

        //UpdateButton.gameObject.SetActive(true);
        //DeleteButton.gameObject.SetActive(true);
        NewButton.gameObject.SetActive(false);

        UpdateUIInformation();
    }

    private void OnCreateEntity_Success(string response)
    {
        var data = (JsonReader.Deserialize(response) as Dictionary<string, object>)["data"] as Dictionary<string, object>;
        string entityID = (string)data["entityId"];

        BCManager.EntityService.GetEntity(entityID, OnSuccess("Updating Local Entity Data...", OnGetEntity_Success), OnFailure(string.Empty, UpdateUIInformation));
    }

    private void OnUpdateEntity_Success(string response)
    {
        var data = (JsonReader.Deserialize(response) as Dictionary<string, object>)["data"] as Dictionary<string, object>;
        string entityID = (string)data["entityId"];

        BCManager.EntityService.GetEntity(entityID, OnSuccess("Updating Local Entity Data...", OnGetEntity_Success), OnFailure(string.Empty, UpdateUIInformation));
    }

    private void OnDeleteEntity_Success(string response)
    {
        //customEntities = new CustomEntity(new HockeyPlayerData());

        ClearFields();
        IsInteractable = true;
    }

    private void OnGetEntity_Success(string response)
    {
        var data = (JsonReader.Deserialize(response) as Dictionary<string, object>)["data"] as Dictionary<string, object>;

        //customEntities.IsOwned = true;
        //customEntities.Deserialize(data);

        //UpdateButton.gameObject.SetActive(true);
        //DeleteButton.gameObject.SetActive(true);
        NewButton.gameObject.SetActive(false);

        UpdateUIInformation();
    }

    #endregion
}
