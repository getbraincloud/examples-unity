using UnityEngine;
using UnityEngine.UI;

public class OpenLinkButton : MonoBehaviour
{
    [SerializeField] private Button LinkButton = default;
    [SerializeField] private string DefaultURL = string.Empty;

    private string url = string.Empty;
    public string URLToOpen
    {
        get => url.IsEmpty() ? DefaultURL : url;
        set => url = value;
    }
    
    private void OnEnable()
    {
        LinkButton.onClick.AddListener(OnLinkButton);
    }

    private void Start()
    {
        if (DefaultURL.IsEmpty())
        {
            enabled = false;
        }
    }

    private void OnDisable()
    {
        LinkButton.onClick.RemoveAllListeners();
    }

    private void OnLinkButton()
    {
        Application.OpenURL(URLToOpen);
    }
}
