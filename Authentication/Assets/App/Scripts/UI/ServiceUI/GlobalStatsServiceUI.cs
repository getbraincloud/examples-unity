using BrainCloud;
using BrainCloud.JSONHelper;
using System;
using System.Collections.Generic;
using TMPro;
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

    [Header("Info Row")]
    [SerializeField] private GameObject InfoRow = default;
    [SerializeField] private TMP_Text InfoLabel = default;
    [SerializeField] private TMP_Text ErrorLabel = default;

    private Dictionary<string, StatsContainerUI> globalStatContainers { get; set; }
    private BrainCloudGlobalStatistics globalStatsService = default;

    #region Unity Messages

    protected override void Start()
    {
        globalStatContainers = new Dictionary<string, StatsContainerUI>();
        globalStatsService = BCManager.GlobalStatisticsService;

        StatsContainerTemplate.StatName = string.Empty;
        StatsContainerTemplate.Value = 0;
        StatsContainerTemplate.gameObject.SetActive(false);

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

        InfoLabel.gameObject.SetActive(true);
        ErrorLabel.gameObject.SetActive(false);
        InfoRow.SetActive(true);

        globalStatsService.ReadAllGlobalStats(OnSuccess("Loading Global Stats...", OnReadAllGlobalStats_Success),
                                              OnFailure("ReadAllGlobalStats Failed", IsInteractableCheck));
    }

    private void IsInteractableCheck()
    {
        if (globalStatContainers.Count > 0)
        {
            IsInteractable = true;
            InfoRow.SetActive(false);
        }
        else
        {
            IsInteractable = false;
            InfoLabel.gameObject.SetActive(false);
            ErrorLabel.gameObject.SetActive(true);
            InfoRow.SetActive(true);
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
            container.gameObject.SetName("{0}StatsContainer", key);
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

        string json = "{ \"" + globalStatName + "\" : 1 }";

        globalStatsService.IncrementGlobalStats(json, OnSuccess("Incremented Stat", OnIncrementGlobalStats_Success),
                                                      OnFailure("IncrementGlobalStats Failed", IsInteractableCheck));
    }

    #endregion

    #region brainCloud

    private void OnReadAllGlobalStats_Success(string response)
    {
        var data = response.Deserialize("data", "statistics");
        if (!data.IsNullOrEmpty())
        {
            UpdateStatContainers(data);
        }

        IsInteractableCheck();
    }

    private void OnIncrementGlobalStats_Success(string response)
    {
        var data = response.Deserialize("data", "statistics");
        if (!data.IsNullOrEmpty())
        {
            UpdateStatContainers(data);
        }

        IsInteractableCheck();
    }

    #endregion
}
