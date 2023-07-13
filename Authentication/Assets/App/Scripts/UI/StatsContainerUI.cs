using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Used to display & interact with global and player statistics.
/// </summary>
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
            StatValueLabel.text = statValue.ToString();
        }
    }

    public Action IncrementButtonAction;

    private long statValue = 0;

    #region Unity Messages

    private void OnEnable()
    {
        IncrementButton.onClick.AddListener(OnIncrementButton);
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
