using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToyShop : ContentUIBehaviour
{
	[SerializeField] private Button CloseButton;
	[SerializeField] private Transform ToySpawnPoint;
	[SerializeField] private BuyToyBenchUI BuyToyBenchPrefab;
	private List<ToyBenchInfo> _toyBenchInfos;
	private BuddysRoom _buddysRoom;
	
	private List<BuyToyBenchUI> _toyBenchUIs;

	protected override void Awake()
	{
		base.Awake();
		CloseButton.onClick.AddListener(OnCloseButton);
		_toyBenchInfos = GameManager.Instance.ToyBenchInfos;
		_toyBenchUIs = new List<BuyToyBenchUI>();
		foreach (ToyBenchInfo toyBenchInfo in _toyBenchInfos)
		{
			var info = Instantiate(BuyToyBenchPrefab, ToySpawnPoint);
			info.Init(toyBenchInfo);
			_toyBenchUIs.Add(info);
		}
		InitializeUI();
	}

	protected override void InitializeUI()
	{
		RefreshShopScreen();
	}
	
	public void RefreshShopScreen()
	{
		foreach (BuyToyBenchUI toyBenchUI in _toyBenchUIs)
		{
			toyBenchUI.SetupToyBenchesUI();
		}
	}
	
	private void OnCloseButton()
	{
		Destroy(gameObject);
	}
}
