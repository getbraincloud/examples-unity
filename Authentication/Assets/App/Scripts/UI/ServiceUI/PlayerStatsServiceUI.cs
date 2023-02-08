using BrainCloud;
using BrainCloud.JsonFx.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStatsServiceUI : MonoBehaviour
{
    [Header("Main")]
    [SerializeField] private CanvasGroup UICanvasGroup = default;
    [SerializeField] private Transform StatsContent = default;
    [SerializeField] private StatsContainerUI StatsContainerTemplate = default;

    public bool IsInteractable
    {
        get { return UICanvasGroup.interactable; }
        set { UICanvasGroup.interactable = value; }
    }

    private Dictionary<string, StatsContainerUI> userStatContainers { get; set; }
    private BrainCloudPlayerStatistics userStatsService = default;

    #region Unity Messages

    private void Start()
    {
        StatsContainerTemplate.StatName = "LOADING...";
        StatsContainerTemplate.Value = -1;

        userStatContainers = new Dictionary<string, StatsContainerUI>();
        userStatsService = BCManager.PlayerStatisticsService;

        userStatsService.ReadAllUserStats(OnReadUserStats_Success,
                                          BCManager.CreateFailureCallback("ReadAllUserStats Failed", IsInteractableCheck));

        IsInteractable = false;
    }

    private void OnDestroy()
    {
        userStatContainers.Clear();
        userStatsService = null;
    }

    #endregion

    #region UI

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
            container.IncrementButtonAction = () => HandleIncrementUserStats(key);

            userStatContainers.Add(key, container);
        }
    }

    private void HandleIncrementUserStats(string userStatName)
    {
        IsInteractable = false;

        string jsonData = "{ \"" + userStatName + "\" : 1 }";

        userStatsService.IncrementUserStats(jsonData, OnIncrementUserStat_Success,
                                                      BCManager.CreateFailureCallback("IncrementUserStats Failed", IsInteractableCheck));
    }

    #endregion

    #region brainCloud

    private void OnReadUserStats_Success(string response, object _)
    {
        BCManager.LogMessage("Loading Player Stats...", response);

        var responseObj = JsonReader.Deserialize(response) as Dictionary<string, object>;
        var dataObj = responseObj["data"] as Dictionary<string, object>;
        var statsObj = dataObj["statistics"] as Dictionary<string, object>;

        if (!statsObj.IsNullOrEmpty())
        {
            UpdateStatContainers(statsObj);
        }

        IsInteractableCheck();
    }

    private void OnIncrementUserStat_Success(string response, object _)
    {
        Debug.Log("Incremented Stat");

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
