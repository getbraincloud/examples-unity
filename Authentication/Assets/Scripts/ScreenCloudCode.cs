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
    [SerializeField] InputField exampleJsonInput; 
    [SerializeField] Text placeholderJsonText; 

    private string m_cloudCodeScript = "";
    private string m_cloudCodeData = "";

    private static string[] m_cloudScriptNames { get; set; }

    private void Awake()
    {
        m_cloudScriptNames = new string[] { "HelloWorld", "IncrementGlobalStat", "IncrementPlayerStat" };
        OnTemplateChanged(0);

        if (HelpMessage == null)
        {
            HelpMessage =   "The cloud code screen allows you to choose from three possible cloud code script templates that must be created on the \"Scripts\" page under " +
                            "the \"Cloud Code\" tab in the brainCloud portal.\n\n" +
                            "After selecting the script you would like to run, provide the necessary parameters in JSON format in the Cloud Code Json input field. " +
                            "Finally press the \"Run Script\" button and your results will appear in the log window on the right side of the screen.";
        }

        if (HelpURL == null)
        {
            HelpURL = "https://getbraincloud.com/apidocs/apiref/?cloudcode#capi-script";
        }
    }

    public void OnTemplateChanged(int index)
    {
        m_cloudCodeScript = m_cloudScriptNames[index];
        cloudScriptText.text = m_cloudCodeScript;

        switch(m_cloudCodeScript)
        {
            case "HelloWorld":
                placeholderJsonText.text = "Example Parameters...\n{\"name\" : \"your_name\", \n\"age\" : your_age}";
                m_cloudCodeData = "{\"name\" : \"Tony\", \n\"age\" : 62}";
                exampleJsonInput.text = m_cloudCodeData;
                break;
            case "IncrementGlobalStat":
                placeholderJsonText.text = "Example Parameters...\n{\"globalStat\" : \"PLAYER_COUNT\", \n\"incrementAmount\" : 1}";
                m_cloudCodeData = "{\"globalStat\" : \"PLAYER_COUNT\", \n\"incrementAmount\" : 1}";
                exampleJsonInput.text = m_cloudCodeData;
                break;
            case "IncrementPlayerStat":
                placeholderJsonText.text = "Example Parameters...\n{\"playerStat\" : \"experiencePoints\", \n\"incrementAmount\" : 1}";
                m_cloudCodeData = "{\"playerStat\" : \"experiencePoints\", \n\"incrementAmount\" : 1}";
                exampleJsonInput.text = m_cloudCodeData;
                break;
        }

        CanParseJson(m_cloudCodeData);
    }

    public void CanParseJson(string input)
    {
        try
        {
            if (input.Length > 0)
            {
                JsonMapper.ToObject(input);
            }
        }
        catch (Exception e)
        {
            // log and rethrow
            Debug.LogError("FAILED TO PARSE JSON: " + e.ToString());
            TextLogger.instance.AddLog("Failed to Parse JSON " + e.ToString());
            throw;
        }
    }

    public void OnRunScriptClick()
    {
        m_cloudCodeData = exampleJsonInput.text;

        CanParseJson(m_cloudCodeData);

        BrainCloudInterface.instance.RunCloudCodeScript(m_cloudCodeScript, m_cloudCodeData);
    }
}
