using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RPGDataUI : ContentUIBehaviour
{
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
        NameField.text = rpgData.Name;
        LevelField.text = rpgData.Level.ToString();
        JobField.text = rpgData.Job;
        PowerField.text = rpgData.GetPower().ToString();
        HealthField.text = rpgData.Health.ToString();
        StrengthField.text = rpgData.Strength.ToString();
        DefenseField.text = rpgData.Defense.ToString();

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

    private void OnUpdateButton()
    {
        UpdateButtonAction?.Invoke(new RPGData(name: NameField.text,
                                               job: JobField.text,
                                               level: int.Parse(LevelField.text),
                                               health: int.Parse(HealthField.text),
                                               strength: int.Parse(StrengthField.text),
                                               defense: int.Parse(DefenseField.text)));
    }

    private void OnDeleteButton()
    {
        DeleteButtonAction?.Invoke();
    }

    #endregion

    #region brainCloud

    private void OnServiceFunction()
    {
        //
    }

    #endregion
}
