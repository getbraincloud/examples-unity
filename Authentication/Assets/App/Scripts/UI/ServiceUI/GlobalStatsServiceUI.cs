using BrainCloud;
using BrainCloud.JsonFx.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

public class GlobalStatsServiceUI : MonoBehaviour, IServiceUI
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

    private Dictionary<string, StatsContainerUI> globalStatContainers { get; set; }
    private BrainCloudGlobalStatistics globalStatsService = default;

    #region Unity Messages

    private void Start()
    {
        StatsContainerTemplate.StatName = "LOADING...";
        StatsContainerTemplate.Value = -1;

        globalStatContainers = new Dictionary<string, StatsContainerUI>();
        globalStatsService = BCManager.GlobalStatisticsService;

        globalStatsService.ReadAllGlobalStats(HandleReadAllGlobalStatsSuccess,
                                              BCManager.CreateFailureCallback("ReadAllGlobalStats Failed", IsInteractableCheck));

        IsInteractable = false;
    }

    private void OnDestroy()
    {
        globalStatContainers.Clear();
        globalStatsService = null;
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

    private void SetUpStatsContainerUI(Dictionary<string, object> statsObj)
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
            container.IncrementButtonAction = () => IncrementGlobalStats(key);

            globalStatContainers.Add(key, container);
        }
    }

    #endregion

    #region Global Statistics

    private void HandleReadAllGlobalStatsSuccess(string response, object _)
    {
        BCManager.LogMessage("Loading Global Stats...", response);

        var responseObj = JsonReader.Deserialize(response) as Dictionary<string, object>;
        var dataObj = responseObj["data"] as Dictionary<string, object>;
        var statsObj = dataObj["statistics"] as Dictionary<string, object>;

        if (!statsObj.IsNullOrEmpty())
        {
            SetUpStatsContainerUI(statsObj);
        }

        IsInteractableCheck();
    }

    private void IncrementGlobalStats(string globalStatName)
    {
        IsInteractable = false;

        string jsonData = "{ \"" + globalStatName + "\" : 1 }";

        globalStatsService.IncrementGlobalStats(jsonData, HandleIncrementGlobalStats,
                                                          BCManager.CreateFailureCallback("IncrementGlobalStats Failed", IsInteractableCheck));
    }

    private void HandleIncrementGlobalStats(string response, object _)
    {
        Debug.Log("Incremented Stat");

        var responseObj = JsonReader.Deserialize(response) as Dictionary<string, object>;
        var dataObj = responseObj["data"] as Dictionary<string, object>;
        var statsObj = dataObj["statistics"] as Dictionary<string, object>;

        if (!statsObj.IsNullOrEmpty())
        {
            SetUpStatsContainerUI(statsObj);
        }

        IsInteractableCheck();
    }

    #endregion
}
