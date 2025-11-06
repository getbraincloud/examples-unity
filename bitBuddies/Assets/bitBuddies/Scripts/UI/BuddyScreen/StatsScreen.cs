using TMPro;
using UnityEngine;

public class StatsScreen : PopUpUI
{
	[SerializeField] private TMP_Text _loveEarnedText;
	[SerializeField] private TMP_Text _coinsEarnedText;
	[SerializeField] private TMP_Text _levelText;
	[SerializeField] private TMP_Text _buddyBlingText;

	public void OnEnable()
	{
		var buddysRoom = FindAnyObjectByType<BuddysRoom>();
		_loveEarnedText.text = buddysRoom.AppChildrenInfo.loveEarnedInLifetime.ToString();
		_coinsEarnedText.text = buddysRoom.AppChildrenInfo.coinsEarnedInLifetime.ToString();
		_levelText.text = buddysRoom.AppChildrenInfo.buddyLevel.ToString();
		_buddyBlingText.text = buddysRoom.AppChildrenInfo.buddyBling.ToString();
	}
}
