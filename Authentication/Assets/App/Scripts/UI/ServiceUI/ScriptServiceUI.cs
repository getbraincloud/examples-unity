using BrainCloud;
using BrainCloud.JsonFx.Json;
using BrainCloud.JSONHelper;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// <para>
/// Example of how scripts can be executed on brainCloud via the Script service.
/// </para>
/// 
/// <seealso cref="BrainCloudScript"/>
/// </summary>
/// API Link: https://getbraincloud.com/apidocs/apiref/?csharp#capi-script
public class ScriptServiceUI : ContentUIBehaviour
{
    private const string SCRIPT_HELLO_WORLD = "HelloWorld";

    private static Dictionary<string, string> CLOUD_CODE_SCRIPTS = new Dictionary<string, string>
    {
        { SCRIPT_HELLO_WORLD,    "{\n    \"name\" : \"John Smith\",\n    \"age\" : 21\n}" },
        { "IncrementGlobalStat", "{\n    \"globalStat\" : \"PLAYER_COUNT\",\n    \"incrementAmount\" : 1\n}" },
        { "IncrementPlayerStat", "{\n    \"playerStat\" : \"experiencePoints\",\n    \"incrementAmount\" : 1\n}" },
    };

    [Header("Main")]
    [SerializeField] private TMP_Dropdown ScriptDropdown = default;
    [SerializeField] private TMP_InputField ScriptJSONField = default;
    [SerializeField] private Button RunButton = default;

    private int current = -1;
    private BrainCloudScript scriptService = default;
    private List<string> scriptNames = default;

    #region Unity Messages

    protected override void Awake()
    {
        ScriptJSONField.text = string.Empty;

        base.Awake();
    }

    private void OnEnable()
    {
        ScriptDropdown.onValueChanged.AddListener(OnScriptDropdown);
        RunButton.onClick.AddListener(OnRunButton);
    }

    protected override void Start()
    {
        scriptService = BCManager.ScriptService;

        scriptNames = new List<string>();
        foreach(string name in CLOUD_CODE_SCRIPTS.Keys)
        {
            scriptNames.Add(name);
        }

        ScriptDropdown.AddOptions(scriptNames);
        InitializeUI();

        base.Start();
    }

    private void OnDisable()
    {
        ScriptDropdown.onValueChanged.RemoveAllListeners();
        RunButton.onClick.RemoveAllListeners();
    }

    protected override void OnDestroy()
    {
        scriptService = null;

        base.OnDestroy();
    }

    #endregion

    #region UI

    protected override void InitializeUI()
    {
        ScriptDropdown.DisplayNormal();
        ScriptJSONField.DisplayNormal();
        OnScriptDropdown(0);
    }

    private void OnScriptDropdown(int option)
    {
        current = option;
        ScriptJSONField.text = CLOUD_CODE_SCRIPTS[scriptNames[option]];
    }

    private void OnRunButton()
    {
        try
        {
            if (!ScriptJSONField.text.IsEmpty())
            {
                string json = ScriptJSONField.text;
                if (JsonReader.Deserialize(json) is ICollection data)
                {
                    IsInteractable = false;
                    string scriptName = scriptNames[current];
                
                    SuccessCallback onSuccess = scriptName == SCRIPT_HELLO_WORLD ? OnSuccess($"{scriptName} Script Ran Successfully", OnHelloWorldScript_Success)
                                                                                 : OnSuccess($"{scriptName} Script Ran Successfully", OnRunScript_Success);
                
                    scriptService.RunScript(scriptName,
                                            data.Serialize(),
                                            onSuccess, OnFailure($"{scriptName} Script Failed", OnRunScript_Returned));
                }
                else
                {
                    ScriptJSONField.DisplayError();
                    Debug.LogError("JSON Data is not formatted properly!");
                    return;
                }
            }
        }
        catch
        {
            ScriptJSONField.DisplayError();
            Debug.LogError($"Cannot run script! Please check your JSON data and try again.");
            throw;
        }
    }

    #endregion

    #region brainCloud

    private void OnHelloWorldScript_Success(string response)
    {
        const int COLOR_TAG_CHARACTERS = 24;
        const string COLOR_TAG_FORMAT = "<color=#{0}{1}{2}>{3}</color>";

        string message = response.Deserialize("data", "response").GetString("data");

        Color32 letterColor;
        StringBuilder helloWorld = new(message.Length * COLOR_TAG_CHARACTERS);
        foreach (char c in message)
        {
            letterColor = new Color(Random.Range(0.2f, 0.99f), Random.Range(0.2f, 0.99f), Random.Range(0.2f, 0.99f));
            helloWorld.Append(string.Format(COLOR_TAG_FORMAT, letterColor.r.ToString("X2"), letterColor.g.ToString("X2"), letterColor.b.ToString("X2"), c));
        }

        Debug.Log(helloWorld);

        OnRunScript_Returned();
    }

    private void OnRunScript_Success(string response)
    {
        //var data = response.Deserialize("data", "response");

        OnRunScript_Returned();
    }

    private void OnRunScript_Returned()
    {
        ScriptDropdown.value = current;

        IsInteractable = true;
    }

    #endregion
}
