using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Base class for shop UI. Handles the close button logic and sets up virtual methods to refresh the shop screen.
/// </summary>
public class Shop : ContentUIBehaviour
{
    [SerializeField] protected Button CloseButton;
    [SerializeField] protected Transform ItemSpawnPoint;

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

    public virtual void SetupShop() { }

    public virtual void RefreshShopScreen()
    {
        SetupShop();
    }

    private void OnCloseButtonPressed()
    {
        Destroy(gameObject);
    }
}
