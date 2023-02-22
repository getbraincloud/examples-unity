using BrainCloud;
using BrainCloud.JsonFx.Json;
using System.Collections;
using System.Collections.Generic;
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
    private static Dictionary<string, string> CLOUD_CODE_SCRIPTS = new Dictionary<string, string>
    {
        { "HelloWorld", "{\n    \"name\" : \"John Smith\",\n    \"age\" : 21\n}" },
        { "IncrementGlobalStat", "{\n    \"globalStat\" : \"PLAYER_COUNT\",\n    \"incrementAmount\" : 1\n}" },
        { "IncrementPlayerStat", "{\n    \"playerStat\" : \"experiencePoints\",\n    \"incrementAmount\" : 1\n}" },
    };

    [Header("Main")]
    [SerializeField] private TMP_Dropdown ScriptDropdown = default;
    [SerializeField] private TMP_InputField ScriptJsonField = default;
    [SerializeField] private Button RunButton = default;

    private int current = -1;
    private BrainCloudScript scriptService = default;
    private List<string> scriptNames = default;

    #region Unity Messages

    protected override void Awake()
    {
        ScriptJsonField.text = string.Empty;

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
        ScriptJsonField.DisplayNormal();
        OnScriptDropdown(0);
    }

    private void OnScriptDropdown(int option)
    {
        current = option;
        ScriptJsonField.text = CLOUD_CODE_SCRIPTS[scriptNames[option]];
    }

    private void OnRunButton()
    {
        try
        {
            if (!ScriptJsonField.text.IsEmpty())
            {
                string jsonData = ScriptJsonField.text;
                if (JsonReader.Deserialize(jsonData) is ICollection json && json.Count > 0)
                {
                    IsInteractable = false;
                    string scriptName = scriptNames[current];
                    scriptService.RunScript(scriptName, JsonWriter.Serialize(json),
                                            OnSuccess($"{scriptName} Script Ran Successfully", OnRunScript_Returned),
                                            OnFailure($"{scriptName} Script Failed", OnRunScript_Returned));
                }
                else
                {
                    ScriptJsonField.DisplayError();
                    Logger.Error("#APP - Json Data is not formatted properly!");
                    return;
                }
            }
        }
        catch
        {
            ScriptJsonField.DisplayError();
            Logger.Error($"#APP - Cannot run script! Please check your Json data and try again.");
            throw;
        }
    }

    #endregion

    #region brainCloud

    private void OnHelloWorldScript_Success(string response)
    {
        //TODO: Show message proper in Log
    }

    private void OnRunScript_Returned()
    {
        ScriptDropdown.value = current;

        IsInteractable = true;
    }

    #endregion
}
