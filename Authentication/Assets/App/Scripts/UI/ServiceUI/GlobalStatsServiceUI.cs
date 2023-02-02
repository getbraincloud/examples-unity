using BrainCloud;
using BrainCloud.JsonFx.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Linq;
using UnityEngine;

public class GlobalStatsServiceUI : MonoBehaviour, IServiceUI
{
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

    private void ManageStatsContainerUI(Dictionary<string, object> statsObj)
    {
        List<string> currentKeys = new List<string>(globalStatContainers.Keys);

        // Add or Create new StatsContainerUI
        foreach (string key in statsObj.Keys)
        {
            long value = Convert.ToInt64(statsObj[key]);
            if (globalStatContainers.ContainsKey(key))
            {
                globalStatContainers[key].Value = value;
            }
            else // Create New StatsContainerUI
            {
                StatsContainerUI container = Instantiate(StatsContainerTemplate, StatsContent);
                container.gameObject.SetName(key, "{0}StatsContainer");
                container.StatName = key;
                container.Value = value;
                container.IncrementButtonAction = () => IncrementGlobalStats(key);

                globalStatContainers.Add(key, container);
            }

            // For removing any that might still exist
            if (currentKeys.Contains(key))
            {
                currentKeys.Remove(key);
            }
        }

        // Destroy StatsContainerUI that no longer have keys
        foreach (string key in currentKeys)
        {
            Destroy(globalStatContainers[key]);
            globalStatContainers.Remove(key);
        }

        // Finally, go through all of the children and separate the backgrounds for better visibility
        bool alternate = false;
        foreach (string key in globalStatContainers.Keys)
        {
            globalStatContainers[key].gameObject.SetActive(true);
            alternate = !alternate;
            globalStatContainers[key].ShowSeparation(alternate);
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
            ManageStatsContainerUI(statsObj);
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
            ManageStatsContainerUI(statsObj);
        }

        IsInteractableCheck();
    }

    #endregion
}
