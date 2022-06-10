using UnityEngine;
using System.Collections;
using BrainCloud.LitJson;
using System.Collections.Generic;

public class ScreenGlobalStats : BCScreen 
{

    Dictionary<string, GlobalStat> m_globalStats;

    [SerializeField] GlobalStat globalStatPrefab; 
    [SerializeField] Transform gStatPefabParent;

    private void Awake()
    {
        if (HelpMessage == null)
        {
            HelpMessage = "The global stats screen will display all global stats defined within the \"Global Statistics\" page under the \"Statistics Rules\" tab of the brainCloud portal.\n\n" +
                          "Pressing the increment button next to each global stat will increment that stat by 1.\n\n" +
                          "Global stats are accessible by any user and can be monitored on the Global Statistics page under the Global Monitoring " +
                          "tab in the brainCloud Portal.";
        }

        if (HelpURL == null)
        {
            HelpURL = "https://getbraincloud.com/apidocs/apiref/?cloudcode#capi-globalstats";
        }
    }

    public override void Activate()
    {
        GameEvents.instance.onIncrementGlobalStat += IncrementGlobalStat;
        GameEvents.instance.onInstantiateGlobalStats += InstantiateGlobalStats; 

        m_globalStats = new Dictionary<string, GlobalStat>();
        BrainCloudInterface.instance.ReadAllGlobalStats();

        if (HelpMessage == null)
        {
            HelpMessage = "The global stats screen will display all global stats defined within the \"Global Statistics\" page under the \"Statistics Rules\" tab of the brainCloud portal.\n\n" +
                          "Pressing the increment button next to each global stat will increment that stat by 1.\n\n" +
                          "Global stats are accessible by any user and can be monitored on the Global Statistics page under the Global Monitoring " +
                          "tab in the brainCloud Portal.";
        }

        if (HelpURL == null)
        {
            HelpURL = "https://getbraincloud.com/apidocs/apiref/?cloudcode#capi-globalstats";
        }
    }

    public void InstantiateGlobalStats()
    {
        DataManager dataManager = DataManager.instance; 

        foreach(string key in dataManager.GlobalStats.Keys)
        {
            GlobalStat newStat = Instantiate(globalStatPrefab, gStatPefabParent);

            if(newStat != null)
            {
                m_globalStats[key] = newStat;
                m_globalStats[key].SetStatName(key);
                m_globalStats[key].SetStatValue(dataManager.GlobalStats[key]);
            }
        }
    }

    public void IncrementGlobalStat(string globalStatName)
    {
        if (!m_globalStats.ContainsKey(globalStatName))
            return;

        DataManager dataManager = DataManager.instance;

        m_globalStats[globalStatName].SetStatValue(dataManager.GlobalStats[globalStatName]);
    }

    protected override void OnDisable()
    {
        if(m_globalStats != null)
        {
            foreach (GlobalStat stat in m_globalStats.Values)
            {
                Destroy(stat.gameObject);
            }

            m_globalStats.Clear();
            m_globalStats = null;
        }

        GameEvents.instance.onIncrementGlobalStat -= IncrementGlobalStat;
        GameEvents.instance.onInstantiateGlobalStats -= InstantiateGlobalStats;
    }
}
