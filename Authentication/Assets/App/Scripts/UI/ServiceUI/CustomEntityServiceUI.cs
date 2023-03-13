using BrainCloud;
using BrainCloud.Common;
using BrainCloud.JsonFx.Json;
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

    private static readonly Dictionary<string, string> DATA_TYPES = new Dictionary<string, string>()
    {
        { HockeyStatsData.DataType, "Hockey Player Stats" }, { RPGData.DataType, "RPG Character Stats" }
    };

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

        CurrentEntityLabel.text = string.Format(CURRENT_ENTITY_LABEL_FORMAT, DEFAULT_EMPTY_FIELD, DEFAULT_EMPTY_FIELD);

        HockeyStatsUI.IsInteractable = false;
        RPGUI.IsInteractable = false;
        PreviousButton.interactable = false;
        NextButton.interactable = false;

        HockeyStatsUI.gameObject.SetActive(true);
        RPGUI.gameObject.SetActive(false);

        customEntities.Clear();

        //HandleGetCustomEntities(HockeyStatsData.DataType);
        HandleGetCustomEntities(RPGData.DataType);
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
    }

    private void GetCurrentEntityIndex()
    {
        CurrentEntityLabel.text = string.Format(CURRENT_ENTITY_LABEL_FORMAT, currentIndex + 1, customEntities.Count);

        CustomEntity current = customEntities[currentIndex];

        IDField.text = current.OwnerID;
        TypeField.text = current.EntityType;
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

    #endregion

    #region brainCloud

    private void HandleGetCustomEntities(string entityType)
    {
        int max = Random.Range(5, 15);
        CustomEntity current;
        for (int i = 0; i < max; i++)
        {
            current = new CustomEntity()
            {
                IsOwned = Random.Range(0, 10) > 2,
                Version = -1,
                TimeToLive = -1,
                OwnerID = "",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                ACL = ACL.ReadWrite(),
                Data = Random.Range(0, 10) > 2 ? new HockeyStatsData(name: string.Empty,
                                                                     position: (HockeyStatsData.FieldPosition)Random.Range(0, 6),
                                                                     goals: Random.Range(0, 35),
                                                                     assists: Random.Range(0, 70))
                                               : new RPGData(name: string.Empty,
                                                             job: string.Empty,
                                                             level: Random.Range(1, 99),
                                                             health: Random.Range(100, 9999),
                                                             strength: Random.Range(1, 99),
                                                             defense: Random.Range(0, 99))
            };

            customEntities.Add(current);
        }

        GetCurrentEntityIndex();

        //IsInteractable = false;
        //
        //var context = new Dictionary<string, object>
        //{
        //    { "pagination", new Dictionary<string, object>
        //        {
        //            { "rowsPerPage", 50 },
        //            { "pageNumber", 1 }
        //        }},
        //    { "optionCriteria", new Dictionary<string, object>
        //        {
        //            { "ownedOnly", true }
        //        }},
        //    { "sortCriteria", new Dictionary<string, object>
        //        {
        //            { "createdAt", 1 },
        //            { "updatedAt", -1 }
        //        }}
        //};
        //
        //customEntityService.GetEntityPage(entityType,
        //                                  JsonWriter.Serialize(context),
        //                                  OnSuccess("GetEntityPage Success", OnGetPage_Success),
        //                                  OnFailure("GetEntityPage Failed", () => IsInteractable = false));
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

        CustomEntity newCustomEntity;
        for (int i = 0; i < data.Length; i++)
        {
            newCustomEntity = new CustomEntity
            {
                IsOwned = true
            };

            newCustomEntity.Deserialize(data[i]);
            customEntities.Add(newCustomEntity);
        }

        GetCurrentEntityIndex();
        IsInteractable = true;
    }

    private void OnCreateEntity_Success(string response)
    {
        var data = (JsonReader.Deserialize(response) as Dictionary<string, object>)["data"] as Dictionary<string, object>;
        string entityID = (string)data["entityId"];

        BCManager.EntityService.GetEntity(entityID, OnSuccess("Updating Local Entity Data...", OnGetEntity_Success), OnFailure(string.Empty, () => IsInteractable = true));
    }

    private void OnUpdateEntity_Success(string response)
    {
        var data = (JsonReader.Deserialize(response) as Dictionary<string, object>)["data"] as Dictionary<string, object>;
        string entityID = (string)data["entityId"];

        BCManager.EntityService.GetEntity(entityID, OnSuccess("Updating Local Entity Data...", OnGetEntity_Success), OnFailure(string.Empty, () => IsInteractable = true));
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

        IsInteractable = true;
    }

    #endregion
}
