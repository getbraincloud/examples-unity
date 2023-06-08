using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RPGDataUI : ContentUIBehaviour
{
    private const int MINIMUM_NAME_LENGTH = 4;
    private const string DEFAULT_POWER_FIELD = "---";

    [Header("Main")]
    [SerializeField] private TMP_InputField NameField = default;
    [SerializeField] private TMP_InputField LevelField = default;
    [SerializeField] private TMP_InputField JobField = default;
    [SerializeField] private TMP_Text PowerField = default;
    [SerializeField] private TMP_InputField HealthField = default;
    [SerializeField] private TMP_InputField StrengthField = default;
    [SerializeField] private TMP_InputField DefenseField = default;

    [Header("Buttons")]
    [SerializeField] private Button UpdateButton = default;
    [SerializeField] private Button DeleteButton = default;

    public Action<RPGData> UpdateButtonAction = default;
    public Action DeleteButtonAction = default;

    private RPGData rpgCharacter = default;

    #region Unity Messages

    protected override void Awake()
    {
        NameField.text = string.Empty;
        LevelField.text = string.Empty;
        JobField.text = string.Empty;
        PowerField.text = DEFAULT_POWER_FIELD;
        HealthField.text = string.Empty;
        StrengthField.text = string.Empty;
        DefenseField.text = string.Empty;

        base.Awake();
    }

    private void OnEnable()
    {
        NameField.onEndEdit.AddListener((name) => CheckNameVerification(name, NameField));
        LevelField.onEndEdit.AddListener((level) => CheckStatVerification(level, RPGData.MIN_LEVEL, RPGData.MAX_LEVEL, LevelField));
        JobField.onEndEdit.AddListener((job) => CheckNameVerification(job.ToLower(), JobField));
        HealthField.onEndEdit.AddListener((health) => CheckStatVerification(health, RPGData.MIN_HEALTH, RPGData.MAX_HEALTH, HealthField));
        StrengthField.onEndEdit.AddListener((strength) => CheckStatVerification(strength, RPGData.MIN_STRENGTH, RPGData.MAX_STRENGTH, StrengthField));
        DefenseField.onEndEdit.AddListener((defense) => CheckStatVerification(defense, RPGData.MIN_DEFENSE, RPGData.MAX_DEFENSE, DefenseField));
        UpdateButton.onClick.AddListener(OnUpdateButton);
        DeleteButton.onClick.AddListener(OnDeleteButton);
    }

    protected override void Start()
    {
        InitializeUI();

        base.Start();
    }

    private void OnDisable()
    {
        NameField.onEndEdit.RemoveAllListeners();
        LevelField.onEndEdit.RemoveAllListeners();
        JobField.onEndEdit.RemoveAllListeners();
        HealthField.onEndEdit.RemoveAllListeners();
        StrengthField.onEndEdit.RemoveAllListeners();
        DefenseField.onEndEdit.RemoveAllListeners();
        UpdateButton.onClick.RemoveAllListeners();
        DeleteButton.onClick.RemoveAllListeners();
    }

    protected override void OnDestroy()
    {
        UpdateButtonAction = null;
        DeleteButtonAction = null;

        base.OnDestroy();
    }

    #endregion

    #region UI

    public void UpdateUI(bool isOwned, RPGData rpgData)
    {
        rpgCharacter = rpgData;
        NameField.text = rpgData.name;
        LevelField.text = rpgData.level.ToString();
        JobField.text = rpgData.job;
        PowerField.text = rpgData.GetPower().ToString();
        HealthField.text = rpgData.health.ToString();
        StrengthField.text = rpgData.strength.ToString();
        DefenseField.text = rpgData.defense.ToString();

        IsInteractable = isOwned;

        if (isOwned)
        {
            NameField.DisplayNormal();
            LevelField.DisplayNormal();
            JobField.DisplayNormal();
            HealthField.DisplayNormal();
            StrengthField.DisplayNormal();
            DefenseField.DisplayNormal();
        }
    }

    protected override void InitializeUI()
    {
        UpdateUI(false, new RPGData());
    }

    private void DisplayError(string error, Selectable problemSelectable = null)
    {
        if (problemSelectable != null)
        {
            problemSelectable.DisplayError();
        }

        Debug.LogError(error);
    }

    private bool CheckNameVerification(string value, TMP_InputField inputField)
    {
        inputField.text = value.Trim();
        if (!inputField.text.IsEmpty())
        {
            if (inputField.text.Length < MINIMUM_NAME_LENGTH)
            {
                DisplayError($"Please use at least {MINIMUM_NAME_LENGTH} characters.", inputField);
                return false;
            }

            if (inputField == NameField)
            {
                rpgCharacter.name = value;
            }
            else // JobField
            {
                rpgCharacter.job = value;
            }

            return true;
        }

        return false;
    }

    private void UpdatePointsDisplay()
    {
        PowerField.text = rpgCharacter.GetPower().ToString();
    }

    private bool CheckStatVerification(string value, int minValue, int maxValue, TMP_InputField inputField)
    {
        inputField.text = value.Trim();
        if (!inputField.text.IsEmpty())
        {
            if (int.TryParse(inputField.text, out int result))
            {
                result = Mathf.Clamp(result, minValue, maxValue);
                inputField.text = result.ToString();

                if (inputField == HealthField)
                {
                    rpgCharacter.health = result;
                }
                else if (inputField == StrengthField)
                {
                    rpgCharacter.strength = result;
                }
                else if (inputField == DefenseField)
                {
                    rpgCharacter.defense = result;
                }
                else // LevelField
                {
                    rpgCharacter.level = result;
                }

                UpdatePointsDisplay();
                return true;
            }

            DisplayError("Please enter a valid number.", inputField);
        }

        return false;
    }

    private void OnUpdateButton()
    {
        if (CheckNameVerification(NameField.text, NameField) &&
            CheckStatVerification(LevelField.text, RPGData.MIN_LEVEL, RPGData.MAX_LEVEL, LevelField) &&
            CheckNameVerification(JobField.text.ToLower(), JobField) &&
            CheckStatVerification(HealthField.text, RPGData.MIN_HEALTH, RPGData.MAX_HEALTH, HealthField) &&
            CheckStatVerification(StrengthField.text, RPGData.MIN_STRENGTH, RPGData.MAX_STRENGTH, StrengthField) &&
            CheckStatVerification(DefenseField.text, RPGData.MIN_DEFENSE, RPGData.MAX_DEFENSE, DefenseField))
        {
            UpdateButtonAction?.Invoke(rpgCharacter);
        }
    }

    private void OnDeleteButton()
    {
        DeleteButtonAction?.Invoke();
    }

    #endregion
}
