using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatsContainerUI : MonoBehaviour
{
    [SerializeField] private TMP_Text StatNameLabel;
    [SerializeField] private TMP_Text StatValueLabel;
    [SerializeField] private Button IncrementButton;
    [SerializeField] private Image BackgroundSeparationVisual;

    public string StatName
    {
        get { return StatNameLabel.text; }
        set { StatNameLabel.text = value; }
    }

    public long Value
    {
        get { return statValue; }
        set
        {
            statValue = value;
            StatValueLabel.text = statValue.ToString("N0");
        }
    }

    public Action IncrementButtonAction;

    private long statValue = 0;

    #region Unity Messages

    private void OnEnable()
    {
        IncrementButton.onClick.AddListener(OnIncrementButton);
    }

    private void Start()
    {
        Value = 0;
        IncrementButton.enabled = false;
    }

    private void OnDisable()
    {
        IncrementButton.onClick.RemoveAllListeners();
    }

    private void OnDestroy()
    {
        IncrementButtonAction = null;
    }

    #endregion

    #region UI Functionality

    public void SetGameObjectName(string name)
    {
        const string STATS_GO_NAME_FORMAT = "{0}StatsContainer";

        char[] arr = name.Where(c => (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) || c == '-')).ToArray();
        name = new string(arr).ToLower();

        gameObject.name = string.Format(STATS_GO_NAME_FORMAT, name);
    }

    public void ShowSeparation(bool showBackground)
    {
        BackgroundSeparationVisual.enabled = showBackground;
    }

    private void OnIncrementButton()
    {
        IncrementButtonAction?.Invoke();
    }

    #endregion
}
