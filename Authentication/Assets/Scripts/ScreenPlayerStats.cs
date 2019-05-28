using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BrainCloud;
using BrainCloud.Common;
using BrainCloud.LitJson;

public class ScreenPlayerStats : BCScreen
{
    protected class PlayerStatistic
    {
        public string name;
        public long value;
        public string increment = "0";
    }
    IDictionary<string, PlayerStatistic> m_stats = new Dictionary<string, PlayerStatistic>();
    
    public ScreenPlayerStats(BrainCloudWrapper bc) : base(bc) { }
    
    public override void Activate()
    {
        _bc.PlayerStateService.ReadUserState(ReadPlayerStateSuccess, Failure_Callback);
        m_mainScene.AddLogNoLn("[ReadPlayerState]... ");
    }
    
    private void ReadPlayerStateSuccess(string json, object cb)
    {
        m_mainScene.AddLog("SUCCESS");
        m_mainScene.AddLogJson(json);
        m_mainScene.AddLog("");

        JsonData jObj = JsonMapper.ToObject(json);
        JsonData jStats = jObj["data"]["statistics"];
        IDictionary dStats = jStats as IDictionary;
        if (dStats != null)
        {
            foreach (string key in dStats.Keys)
            {
                PlayerStatistic stat = new PlayerStatistic();
                stat.name = (string) key;
                JsonData value = (JsonData) dStats[key];
                
                // silly that LitJson can't upcast an int to a long...
                stat.value = value.IsInt ? (int) value : (long) value;
                
                m_stats[stat.name] = stat;
            }
        }
    }
    
    public override void OnScreenGUI()
    {
        int minLeftWidth = 120;
        
        GUILayout.BeginHorizontal();
        GUILayout.Box("Player Stat Name", GUILayout.MinWidth(minLeftWidth));
        GUILayout.Box("Player Stat Value");
        GUILayout.EndHorizontal();
        
        foreach (PlayerStatistic ps in m_stats.Values)
        {
            GUILayout.BeginVertical();
            GUILayout.Space(5);
            GUILayout.EndVertical();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(ps.name, GUILayout.MinWidth(minLeftWidth));
            GUILayout.Box(ps.value.ToString());
            GUILayout.EndHorizontal();
            
            // increment
            GUILayout.BeginHorizontal();
            GUILayout.Space(minLeftWidth);
            ps.increment = GUILayout.TextField(ps.increment, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Increment"))
            {
                long valueAsLong = 0;
                double valueAsDouble = 0;
                if (long.TryParse(ps.increment, out valueAsLong)
                    || double.TryParse(ps.increment, out valueAsDouble))
                {
                	_bc.PlayerStatisticsService.IncrementUserStats(
               	    	"{ '" + ps.name +"':" + ps.increment +"}",
                    	Success_Callback, Failure_Callback);
               		m_mainScene.AddLogNoLn("[IncrementStat]... ");
                }
            }
            GUILayout.EndHorizontal();
        }
    }
    
    public override void Success_Callback(string json, object cbObject)
    {
        base.Success_Callback(json, cbObject);
        
        //{"status":200,"data":{"statisticsExceptions":{},"milestones":{},"experiencePoints":0,"quests":{},"experienceLevel":0,"statistics":{"wood":75}}}
        JsonData jObj = JsonMapper.ToObject(json);
        JsonData jStats = jObj["data"]["statistics"];
        IDictionary dStats = jStats as IDictionary;
        if (dStats != null)
        {
            foreach (string key in dStats.Keys)
            {
                JsonData value = (JsonData) dStats[key];
                long valueAsLong = value.IsInt ? (int) value : (long) value;
                m_stats[key].value = valueAsLong;
            }
        }
    }
    
}
