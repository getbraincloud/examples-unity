using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Shop : ContentUIBehaviour
{
	[SerializeField] private Button CloseButton;
	[SerializeField] private Transform ToySpawnPoint;
	[SerializeField] private BuyToyBenchUI BuyToyBenchPrefab;

	protected override void Awake()
	{
		base.Awake();
		CloseButton.onClick.AddListener(OnCloseButtonPressed);

		InitializeUI();
	}

	protected override void InitializeUI()
	{
		RefreshShopScreen();
	}
	
	public void RefreshShopScreen()
	{
		
	}
	
	private void OnCloseButtonPressed()
	{
		Destroy(gameObject);
	}
}
