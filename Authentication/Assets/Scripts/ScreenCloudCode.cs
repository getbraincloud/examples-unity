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

    private static string[] m_cloudScriptNames { get; set; }

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
        BrainCloudInterface.instance.RunCloudCodeScript(m_cloudCodeScript, m_cloudCodeData);
    }

    protected override void OnDisable()
    {

    }
}
