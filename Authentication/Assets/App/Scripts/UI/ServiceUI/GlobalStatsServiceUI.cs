using BrainCloud;
using BrainCloud.JsonFx.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// <para>
/// Example of how an app's statistics can be handled via brainCloud's GlobalStatistics service.
/// </para>
/// 
/// <seealso cref="BrainCloudGlobalStatistics"/>
/// </summary>
/// API Link: https://getbraincloud.com/apidocs/apiref/?csharp#capi-globalstats
public class GlobalStatsServiceUI : ContentUIBehaviour
{
    [Header("Main")]
    [SerializeField] private Transform StatsContent = default;
    [SerializeField] private StatsContainerUI StatsContainerTemplate = default;

    private Dictionary<string, StatsContainerUI> globalStatContainers { get; set; }
    private BrainCloudGlobalStatistics globalStatsService = default;

    #region Unity Messages

    protected override void Start()
    {
        StatsContainerTemplate.StatName = "LOADING...";
        StatsContainerTemplate.Value = -1;

        globalStatContainers = new Dictionary<string, StatsContainerUI>();
        globalStatsService = BCManager.GlobalStatisticsService;

        InitializeUI();

        base.Start();
    }

    protected override void OnDestroy()
    {
        globalStatContainers.Clear();
        globalStatsService = null;

        base.OnDestroy();
    }

    #endregion

    #region UI

    protected override void InitializeUI()
    {
        IsInteractable = false;
        globalStatsService.ReadAllGlobalStats(OnSuccess("Loading Global Stats...", OnReadAllGlobalStats_Success),
                                              OnFailure("ReadAllGlobalStats Failed", IsInteractableCheck));
    }

    private void IsInteractableCheck()
    {
        if (StatsContent.childCount > 1)
        {
            IsInteractable = true;
            StatsContainerTemplate.gameObject.SetActive(false);
        }
        else
        {
            IsInteractable = false;
            StatsContainerTemplate.StatName = "ERROR";
            StatsContainerTemplate.gameObject.SetActive(true);
        }
    }

    private void UpdateStatContainers(Dictionary<string, object> statsObj)
    {
        // Destroy all old containers first
        if (!globalStatContainers.IsNullOrEmpty())
        {
            foreach (string key in globalStatContainers.Keys)
            {
                globalStatContainers[key].IncrementButtonAction = null;
                Destroy(globalStatContainers[key].gameObject);
            }

            globalStatContainers.Clear();
        }

        // Create new StatsContainerUIs
        bool alternate = false;
        foreach (string key in statsObj.Keys)
        {
            alternate = !alternate;

            StatsContainerUI container = Instantiate(StatsContainerTemplate, StatsContent);
            container.gameObject.SetActive(true);
            container.gameObject.SetName(key, "{0}StatsContainer");
            container.ShowSeparation(alternate);
            container.StatName = key;
            container.Value = Convert.ToInt64(statsObj[key]);
            container.IncrementButtonAction = () => HandleIncrementGlobalStats(key);

            globalStatContainers.Add(key, container);
        }
    }

    private void HandleIncrementGlobalStats(string globalStatName)
    {
        IsInteractable = false;

        string jsonData = "{ \"" + globalStatName + "\" : 1 }";

        globalStatsService.IncrementGlobalStats(jsonData, OnSuccess("Incremented Stat", OnIncrementGlobalStats_Success),
                                                          OnFailure("IncrementGlobalStats Failed", IsInteractableCheck));
    }

    #endregion

    #region brainCloud

    private void OnReadAllGlobalStats_Success(string response)
    {
        var responseObj = JsonReader.Deserialize(response) as Dictionary<string, object>;
        var dataObj = responseObj["data"] as Dictionary<string, object>;
        var statsObj = dataObj["statistics"] as Dictionary<string, object>;

        if (!statsObj.IsNullOrEmpty())
        {
            UpdateStatContainers(statsObj);
        }

        IsInteractableCheck();
    }

    private void OnIncrementGlobalStats_Success(string response)
    {
        var responseObj = JsonReader.Deserialize(response) as Dictionary<string, object>;
        var dataObj = responseObj["data"] as Dictionary<string, object>;
        var statsObj = dataObj["statistics"] as Dictionary<string, object>;

        if (!statsObj.IsNullOrEmpty())
        {
            UpdateStatContainers(statsObj);
        }

        IsInteractableCheck();
    }

    #endregion
}