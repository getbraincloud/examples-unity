using TMPro;
using UnityEngine;

public class StatsScreen : MonoBehaviour
{
    [SerializeField] private TMP_Text _loveEarnedText;
    [SerializeField] private TMP_Text _coinsEarnedText;
    [SerializeField] private TMP_Text _levelText;
    [SerializeField] private TMP_Text _buddyBlingText;

    public void OnEnable()
    {
        var selectedApp = GameManager.Instance.SelectedAppChildrenInfo;
        _loveEarnedText.text = selectedApp.currentXP.ToString();
        _coinsEarnedText.text = selectedApp.coinsEarnedInLifetime.ToString();
        _levelText.text = selectedApp.buddyLevel.ToString();
        _buddyBlingText.text = selectedApp.buddyBling.ToString();
    }
}
