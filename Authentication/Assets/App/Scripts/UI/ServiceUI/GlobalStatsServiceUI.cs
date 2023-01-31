using BrainCloud;
using BrainCloud.LitJson;
using System.Collections;
using System.Collections.Generic;
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

    private Dictionary<string, long> globalStats { get; set; }
    private BrainCloudGlobalStatistics globalStatsService = default;

    #region Unity Messages

    private void Start()
    {
        IsInteractable = false;

        StatsContainerTemplate.StatName = "LOADING...";
        StatsContainerTemplate.Value = -1;

        globalStats = new Dictionary<string, long>();
        globalStatsService = BCManager.GlobalStatisticsService;

        globalStatsService.ReadAllGlobalStats(HandleReadAllGlobalStatsSuccess,
                                              BCManager.CreateFailureCallback("ReadAllGlobalStats Failed", HandleReadAllGlobalStatsFailure));
    }

    private void OnDestroy()
    {
        globalStats.Clear();
        globalStatsService = null;
    }

    #endregion

    #region Global Statistics

    private void HandleReadAllGlobalStatsSuccess(string response, object _)
    {
        BCManager.LogMessage("Loading Global Stats...", response);

        JsonData jObj = JsonMapper.ToObject(response);
        JsonData jStats = jObj["data"]["statistics"];
        IDictionary dStats = jStats;
        if (dStats != null)
        {
            bool alternate = false;
            foreach (string key in dStats.Keys)
            {
                JsonData value = (JsonData)dStats[key];
                globalStats.Add(key, value.IsInt ? (int)value : (long)value);

                alternate = !alternate;
                StatsContainerUI container = Instantiate(StatsContainerTemplate, StatsContent, false);
                container.SetGameObjectName(key);
                container.ShowSeparation(alternate);
                container.StatName = key;
                container.Value = globalStats[key];
                container.IncrementButtonAction = () => IncrementGlobalStats(key);
            }
        }

        StatsContainerTemplate.gameObject.SetActive(false);

        IsInteractable = true;
    }

    private void HandleReadAllGlobalStatsFailure()
    {
        StatsContainerTemplate.StatName = "ERROR";
    }

    private void IncrementGlobalStats(string globalStatName)
    {
        IsInteractable = false;

        string jsonData = "{ \"" + globalStatName + "\" : 1 }";

        globalStatsService.IncrementGlobalStats(jsonData, HandleIncrementGlobalStats,
                                                          BCManager.CreateFailureCallback("IncrementGlobalStats Failed"));
    }

    private void HandleIncrementGlobalStats(string response, object _)
    {
        Debug.Log("Incremented Stat");

        //JsonData jObj = JsonMapper.ToObject(response);
        //JsonData jStats = jObj["data"]["statistics"];
        //IDictionary dStats = jStats as IDictionary;
        //
        //if (dStats == null)
        //    return;
        //
        //if (!dStats.Contains(globalStatName))
        //    return;
        //
        //if (!globalStats.ContainsKey(globalStatName))
        //    return;
        //
        //JsonData value = (JsonData)dStats[globalStatName];
        //
        //long valueAsLong = value.IsInt ? (int)value : (long)value;
        //globalStats[globalStatName] = valueAsLong;

        IsInteractable = true;
    }

    #endregion
}
