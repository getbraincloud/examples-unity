using UnityEngine;
using System.Collections.Generic;
using BrainCloud;
using BrainCloud.Common;
using System;
using BrainCloud.LitJson;
using UnityEngine.UI; 

public class ScreenCloudCode : BCScreen
{
    [SerializeField] Text cloudScriptText;
    [SerializeField] Text placeholderJsonExample; 

    private string m_cloudCodeScript = "";
    private string m_cloudCodeData = "";
    private string m_exampleJSONPlaceholder = ""; 

    private static string[] m_cloudScriptNames;

    public ScreenCloudCode(BrainCloudWrapper bc) : base(bc) { }

    private void Awake()
    {
        m_cloudScriptNames = new string[] { "HelloWorld", "IncrementGlobalStat", "IncrementPlayerStat" };
        OnTemplateChanged(0);
    }

    public override void Activate(BrainCloudWrapper bc)
    {
        _bc = bc;
    }

    public void OnTemplateChanged(int index)
    {
        m_cloudCodeScript = m_cloudScriptNames[index];
        cloudScriptText.text = m_cloudCodeScript;

        switch(m_cloudCodeScript)
        {
            case "HelloWorld":
                placeholderJsonExample.text = "Example Parameters...\n{\"name\" : \"your_name\", \n\"age\" : your_age}";
                break;
            case "IncrementGlobalStat":
                placeholderJsonExample.text = "Example Parameters...\n{\"globalStat\" : \"PLAYER_COUNT\", \n\"incrementAmount\" : 1}";
                break;
            case "IncrementPlayerStat":
                placeholderJsonExample.text = "Example Parameters...\n{\"playerStat\" : \"experiencePoints\", \n\"incrementAmount\" : 1}";
                break;
        }
    }

    public void OnParamEndEdit(string param)
    {
        m_cloudCodeData = param;

        try
        {
            if (m_cloudCodeData.Length > 0)
            {
                JsonMapper.ToObject(m_cloudCodeData);
            }
        }
        catch (Exception e)
        {
            // log and rethrow
            Debug.LogError("FAILED TO PARSE JSON: " + e.ToString());
            throw;
        }
    }

    public void OnRunScriptClick()
    {
        _bc.ScriptService.RunScript(m_cloudCodeScript, m_cloudCodeData, OnRunScriptSuccess, OnRunScriptFailure);
    }

    public void OnRunScriptSuccess(string jsonResponse, object cbObject)
    {
        //Debug.LogWarning(m_cloudCodeScript + " Script ran successfully");
    }

    public void OnRunScriptFailure(int status, int reasonCode, string jsonErrorm, object cbObject)
    {
        Debug.LogError(m_cloudCodeScript + " Script failed to run.");
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

            _bc.ScriptService.RunScript(m_cloudCodeScript, m_cloudCodeData, CloudCode_Success, Failure_Callback);
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
