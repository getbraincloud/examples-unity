using UnityEngine;
using System.Collections.Generic;
using BrainCloud;
using BrainCloud.Common;
using System;
using LitJson;

public class ScreenCloudCode : BCScreen
{
    private string m_cloudCodeScript = "";
    private string m_cloudCodeData = "";

    public override void Activate()
    {
    }

    public override void OnScreenGUI()
    {
        GUILayout.BeginVertical();
        GUILayout.BeginHorizontal();
        GUILayout.Label("CloudCode Templates");
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("HelloWorld"))
        {
            m_cloudCodeScript = "HelloWorld";
            m_cloudCodeData = "{}";
        }
        if (GUILayout.Button("GlobalStats"))
        {
            m_cloudCodeScript = "GlobalStats";
            m_cloudCodeData = "{}";
        }
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        GUILayout.BeginVertical();
        GUILayout.BeginHorizontal();
        GUILayout.Label("CloudCode Script");
        m_cloudCodeScript = GUILayout.TextField(m_cloudCodeScript);
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        GUILayout.BeginVertical();
        GUILayout.BeginHorizontal();
        GUILayout.Label("CloudCode JSON");
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        GUILayout.BeginVertical();
        m_cloudCodeData = GUILayout.TextArea(m_cloudCodeData, GUILayout.MinHeight(150));
        GUILayout.EndVertical();

        GUILayout.BeginVertical();
        GUILayout.Space(20);
        GUILayout.EndVertical();

        GUILayout.BeginVertical();
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Run Cloud Code"))
        {
            m_mainScene.AddLogNoLn("[RunScript]...");
            try
            {
                if (m_cloudCodeData.Length > 0)
                {
                    JsonMapper.ToObject(m_cloudCodeData);
                }
            }
            catch(Exception e)
            {
                // log and rethrow
                m_mainScene.AddLog("FAILED TO PARSE JSON: " + e.ToString());
                m_mainScene.AddLog("");
                throw;
            }

            BrainCloudWrapper.GetBC().ScriptService.RunScript(m_cloudCodeScript, m_cloudCodeData, CloudCode_Success, Failure_Callback);
        }
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

    }

    private void CloudCode_Success(string json, object cb)
    {
        m_mainScene.AddLog("SUCCESS");
        m_mainScene.AddLogJson(json);
        m_mainScene.AddLog("");
    }
}
