﻿namespace BrainCloudUnity.HUD
{

using UnityEngine;
using System.Collections.Generic;
using BrainCloud.JsonFx.Json;

    public class HUDGlobalStats : IHUDElement
    {
        class GlobalStatistic
        {
            public string name;
            public long value;
            public string increment = "0";
        }
        IDictionary<string, GlobalStatistic> m_stats = new Dictionary<string, GlobalStatistic>();
        Vector2 m_scrollPosition = new Vector2(0, 0);


        public void OnHUDActivate()
        {
            RetrieveGlobalStats();
        }

        public void OnHUDDeactivate()
        { }

        public string GetHUDTitle()
        {
            return "Global Stats";
        }

        void RetrieveGlobalStats()
        {
            m_stats.Clear();

            BrainCloudLoginPF.BrainCloud.GlobalStatisticsService.ReadAllGlobalStats(
                ReadGlobalStatsSuccess, ReadGlobalStatsFailure);
        }

        void ReadGlobalStatsSuccess(string json, object cb)
        {
            Dictionary<string,object> jObj = JsonReader.Deserialize< Dictionary<string, object>>(json);
            Dictionary<string, object> data = (Dictionary<string, object>)jObj["data"];
            Dictionary<string, object> stats = (Dictionary<string, object>)data["statistics"];
            if (stats != null)
            {
                foreach (string key in stats.Keys)
                {
                    GlobalStatistic stat = new GlobalStatistic();
                    stat.name = key;
                    stat.value = System.Convert.ToInt64(stats[key]);
                    
                    m_stats[stat.name] = stat;
                }
            }
        }

        void ReadGlobalStatsFailure(int statusCode, int reasonCode, string statusMessage, object cb)
        {
            Debug.LogError("Failed to read global statistics: " + statusMessage);
        }

        public void OnHUDDraw()
        {
            m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            foreach (GlobalStatistic ps in m_stats.Values)
            {
                GUILayout.BeginVertical();
                GUILayout.Space(5);
                GUILayout.EndVertical();

                GUILayout.BeginHorizontal();
                GUILayout.Label(ps.name, GUILayout.MinWidth(125));
                GUILayout.Box(ps.value.ToString());
                GUILayout.EndHorizontal();

                // increment
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                ps.increment = GUILayout.TextField(ps.increment, GUILayout.MinWidth(45));

                if (GUILayout.Button("Increment"))
                {
                    long valueAsLong = 0;
                    double valueAsDouble = 0;
                    if (long.TryParse(ps.increment, out valueAsLong)
                        || double.TryParse(ps.increment, out valueAsDouble))
                    {
                        BrainCloudLoginPF.BrainCloud.GlobalStatisticsService.IncrementGlobalStats(
                            "{ '" + ps.name + "':" + ps.increment + "}",
                            ReadGlobalStatsSuccess, ReadGlobalStatsFailure);
                    }
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
        }

    }
}
