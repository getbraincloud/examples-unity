using BrainCloud;
using BrainCloud.JsonFx.Json;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// <para>
/// Example of how a user's statistics can be handled via brainCloud's PlayerStatistics service.
/// </para>
/// 
/// <seealso cref="BrainCloudPlayerStatistics"/>
/// </summary>
/// API Link: https://getbraincloud.com/apidocs/apiref/?csharp#capi-playerstats
public class PlayerStatsServiceUI : ContentUIBehaviour
{
    [Header("Main")]
    [SerializeField] private Transform StatsContent = default;
    [SerializeField] private StatsContainerUI StatsContainerTemplate = default;

    [Header("Info Row")]
    [SerializeField] private GameObject InfoRow = default;
    [SerializeField] private TMP_Text InfoLabel = default;
    [SerializeField] private TMP_Text ErrorLabel = default;

    private Dictionary<string, StatsContainerUI> userStatContainers { get; set; }
    private BrainCloudPlayerStatistics userStatsService = default;

    #region Unity Messages

    protected override void Start()
    {
        userStatContainers = new Dictionary<string, StatsContainerUI>();
        userStatsService = BCManager.PlayerStatisticsService;

        StatsContainerTemplate.StatName = string.Empty;
        StatsContainerTemplate.Value = 0;
        StatsContainerTemplate.gameObject.SetActive(false);

        InitializeUI();

        base.Start();
    }

    protected override void OnDestroy()
    {
        userStatContainers.Clear();
        userStatsService = null;

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

        userStatsService.ReadAllUserStats(OnSuccess("Loading Player Stats...", OnReadUserStats_Success),
                                          OnFailure("ReadAllUserStats Failed", IsInteractableCheck));
    }

    private void IsInteractableCheck()
    {
        if (userStatContainers.Count > 0)
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
        if (!userStatContainers.IsNullOrEmpty())
        {
            foreach (string key in userStatContainers.Keys)
            {
                userStatContainers[key].IncrementButtonAction = null;
                Destroy(userStatContainers[key].gameObject);
            }

            userStatContainers.Clear();
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
            container.IncrementButtonAction = () => OnIncrementUserStats(key);

            userStatContainers.Add(key, container);
        }
    }

    private void OnIncrementUserStats(string userStatName)
    {
        IsInteractable = false;

        string json = "{ \"" + userStatName + "\" : 1 }";

        userStatsService.IncrementUserStats(json, OnSuccess("Incremented Stat", OnIncrementUserStat_Success),
                                                  OnFailure("IncrementUserStats Failed", IsInteractableCheck));
    }

    #endregion

    #region brainCloud

    private void OnReadUserStats_Success(string response)
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

    private void OnIncrementUserStat_Success(string response)
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
