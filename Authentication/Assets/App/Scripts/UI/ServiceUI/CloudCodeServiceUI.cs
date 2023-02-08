using BrainCloud;
using BrainCloud.JsonFx.Json;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CloudCodeServiceUI : MonoBehaviour, IServiceUI
{
    private static Dictionary<string, string> CLOUD_CODE_SCRIPTS = new Dictionary<string, string>
    {
        { "HelloWorld", "{\n    \"name\" : \"John Smith\",\n    \"age\" : 21\n}" },
        { "IncrementGlobalStat", "{\n    \"globalStat\" : \"PLAYER_COUNT\",\n    \"incrementAmount\" : 1\n}" },
        { "IncrementPlayerStat", "{\n    \"playerStat\" : \"experiencePoints\",\n    \"incrementAmount\" : 1\n}" },
    };

    [Header("Main")]
    [SerializeField] private CanvasGroup UICanvasGroup = default;
    [SerializeField] private TMP_Dropdown ScriptDropdown = default;
    [SerializeField] private TMP_InputField ScriptJsonField = default;
    [SerializeField] private Button RunButton = default;

    public bool IsInteractable
    {
        get { return UICanvasGroup.interactable; }
        set { UICanvasGroup.interactable = value; }
    }

    private int current = -1;
    private BrainCloudScript scriptService = default;
    private List<string> scriptNames = default;

    #region Unity Messages

    private void Awake()
    {
        ScriptJsonField.text = string.Empty;
    }

    private void OnEnable()
    {
        ScriptDropdown.onValueChanged.AddListener(OnScriptDropdown);
        RunButton.onClick.AddListener(OnRunButton);
    }

    private void Start()
    {
        scriptService = BCManager.ScriptService;

        scriptNames = new List<string>();
        foreach(string name in CLOUD_CODE_SCRIPTS.Keys)
        {
            scriptNames.Add(name);
        }

        ScriptDropdown.AddOptions(scriptNames);
        OnScriptDropdown(0);
    }

    private void OnDisable()
    {
        ScriptDropdown.onValueChanged.RemoveAllListeners();
        RunButton.onClick.RemoveAllListeners();
    }

    private void OnDestroy()
    {
        scriptService = null;
    }

    #endregion

    #region UI

    private void OnScriptDropdown(int option)
    {
        current = option;
        ScriptJsonField.text = CLOUD_CODE_SCRIPTS[scriptNames[option]];
    }

    private void OnRunButton()
    {
        try
        {
            if (!ScriptJsonField.text.IsNullOrEmpty())
            {
                string jsonData = ScriptJsonField.text;
                if (JsonReader.Deserialize(jsonData) is ICollection json && json.Count > 0)
                {
                    IsInteractable = false;
                    string scriptName = scriptNames[current];
                    scriptService.RunScript(scriptName, JsonWriter.Serialize(json),
                                            BCManager.CreateSuccessCallback($"{scriptName} Script Ran Successfully", OnRunScriptFinished),
                                            BCManager.CreateFailureCallback($"{scriptName} Script Failed", OnRunScriptFinished));
                }
                else
                {
                    Debug.LogError("Json Data is not formatted properly!");
                    return;
                }
            }
        }
        catch
        {
            Debug.LogError($"Cannot run script! Please check your Json data and try again.");
            throw;
        }
    }

    private void OnRunScriptFinished()
    {
        ScriptDropdown.value = current;

        IsInteractable = true;
    }

    #endregion

    #region brainCloud

    private void OnHelloWorldScript_Success(string response, object _)
    {
        //TODO: Show message proper in Log
    }

    #endregion
}
