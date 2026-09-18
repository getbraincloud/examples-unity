using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Wraps the 40-swatch colour grid as an on-demand popup instead of an always-visible block —
/// opened by clicking your own colour swatch, closed by picking a colour or clicking the
/// backdrop. Matches the other RelayTestApp clients' lobby UI.
/// </summary>
public class ColorPopupController : MonoBehaviour
{
    public static ColorPopupController Instance { get; private set; }

    public GameObject PopupRoot;   // backdrop + grid container, toggled active/inactive
    public Button OpenButton;      // the local player's own colour swatch
    public Button BackdropButton;  // click-outside-to-close

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        if (OpenButton != null) OpenButton.onClick.AddListener(Show);
        if (BackdropButton != null) BackdropButton.onClick.AddListener(Hide);
        Hide();
    }

    private void OnDisable()
    {
        if (OpenButton != null) OpenButton.onClick.RemoveListener(Show);
        if (BackdropButton != null) BackdropButton.onClick.RemoveListener(Hide);
    }

    public void Show()
    {
        if (PopupRoot != null) PopupRoot.SetActive(true);
    }

    public void Hide()
    {
        if (PopupRoot != null) PopupRoot.SetActive(false);
    }
}
