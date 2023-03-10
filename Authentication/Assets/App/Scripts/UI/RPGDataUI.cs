using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RPGDataUI : ContentUIBehaviour
{
    public static string DataType => RPGData.DataType;

    private const string DEFAULT_POWER_FIELD = "---";

    [Header("Main")]
    [SerializeField] private TMP_InputField NameField = default;
    [SerializeField] private TMP_InputField LevelField = default;
    [SerializeField] private TMP_InputField JobField = default;
    [SerializeField] private TMP_Text PowerField = default;
    [SerializeField] private TMP_InputField HealthField = default;
    [SerializeField] private TMP_InputField StrengthField = default;
    [SerializeField] private TMP_InputField DefenseField = default;

    [Header("CustomEntityServiceUI Buttons")]
    [SerializeField] private Button UpdateButton = default;
    [SerializeField] private Button DeleteButton = default;

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
        if (UpdateButton.onClick != null || DeleteButton.onClick != null)
        {
            OnDisable();
        }

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
        //

        base.OnDestroy();
    }

    #endregion

    #region UI

    public void UpdateUI(RPGData rpgData)
    {
        NameField.text = rpgData.Name;
        NameField.DisplayNormal();
        LevelField.text = rpgData.Level.ToString();
        LevelField.DisplayNormal();
        JobField.text = rpgData.Job;
        JobField.DisplayNormal();
        PowerField.text = rpgData.GetPower().ToString();
        HealthField.text = rpgData.Health.ToString();
        HealthField.DisplayNormal();
        StrengthField.text = rpgData.Strength.ToString();
        StrengthField.DisplayNormal();
        DefenseField.text = rpgData.Defense.ToString();
        DefenseField.DisplayNormal();
    }

    protected override void InitializeUI()
    {
        UpdateUI(new RPGData());

        UpdateButton.DisplayNormal();
        DeleteButton.DisplayNormal();
    }

    private void OnUpdateButton()
    {
        //
    }

    private void OnDeleteButton()
    {
        //
    }

    #endregion

    #region brainCloud

    private void OnServiceFunction()
    {
        //
    }

    #endregion
}
