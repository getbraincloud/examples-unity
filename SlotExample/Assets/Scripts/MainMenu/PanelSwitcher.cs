using UnityEngine;
using System.Collections;

public enum Panel { Login, PlayerSelect, MainMenu }

public class PanelSwitcher : MonoBehaviour
{    
    public GameObject LoginPanel;
    public GameObject PlayerSelectPanel;
    public GameObject MainMenuPanel;

    private GameObject _currentPanel;

    private static PanelSwitcher _instance;
    private bool _isFading;

    private void Awake()
    {
        _instance = this;
    }

    private void OnEnable()
    {
        if (BrainCloudWrapper.GetBC().IsAuthenticated()) SetActivePanel(Panel.MainMenu);
        else SetActivePanel(Panel.Login);
    }

    /// <summary>
    /// Switches to the specified panel immediatly
    /// Sets all other panels inactive
    /// </summary>
    /// <param name="panel"> Panel to set active </param>
    public static void SetActivePanel(Panel panel)
    {
        _instance.SetActivePanelInternal(panel);
    }

    /// <summary>
    /// Fades out current panel and fades in the specified panel
    /// </summary>
    /// <param name="panel"> Panel to fade to </param>
    /// <param name="time"> Total time to fade out and in </param>
    /// <param name="delay"> Delay between fading out and fading in </param>
    public static void SwitchToPanel(Panel panel, float time = 0.6f, float delay = 0.1f)
    {
        _instance.SwitchToPanelInternal(panel, time, delay);
    }

    private void SetActivePanelInternal(Panel panel)
    {
        LoginPanel.SetActive(false);
        PlayerSelectPanel.SetActive(false);
        MainMenuPanel.SetActive(false);

        _currentPanel = GetPanel(panel);
        _currentPanel.SetActive(true);
    }

    private void SwitchToPanelInternal(Panel panel, float time, float delay)
    {
        StartTransition(_currentPanel, GetPanel(panel), time, delay);
    }

    private void StartTransition(GameObject current, GameObject target, float time, float delay)
    {
        StartCoroutine(Transition(current, target, time, delay));
    }

    IEnumerator Transition(GameObject from, GameObject to, float time, float delay)
    {
        _isFading = true;
        StartCoroutine(Fade(from.GetComponent<CanvasGroup>(), false, time / 2));
        while (_isFading) yield return null;
        yield return new WaitForSeconds(delay);
        StartCoroutine(Fade(to.GetComponent<CanvasGroup>(), true, time / 2));
        _currentPanel = to;
    }

    IEnumerator Fade(CanvasGroup canvasGroup, bool isFadingIn, float time)
    {
        _isFading = true;
        canvasGroup.alpha = isFadingIn ? 0f : 1f;
        canvasGroup.interactable = false;
        canvasGroup.gameObject.SetActive(true);

        float startTime = Time.timeSinceLevelLoad;
        float currentPercent = 0f;

        while (currentPercent < 1f)
        {
            currentPercent = (Time.timeSinceLevelLoad - startTime) / time;
            canvasGroup.alpha = isFadingIn ? currentPercent : 1 - currentPercent;
            yield return null;
        }
        _isFading = false;
        canvasGroup.interactable = isFadingIn;
        canvasGroup.gameObject.SetActive(isFadingIn);
    }

    private GameObject GetPanel(Panel panel)
    {
        switch (panel)
        {
            case Panel.Login:
                return LoginPanel;
            case Panel.PlayerSelect:
                return PlayerSelectPanel;
            case Panel.MainMenu:
                return MainMenuPanel;
            default:
                return null;
        }
    }
}
