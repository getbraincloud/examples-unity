using UnityEngine;
using UnityEngine.UI;

public class OpenLinkButton : MonoBehaviour
{
    [SerializeField] private Button LinkButton = default;
    [SerializeField] private string URLToOpen = string.Empty;

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(URLToOpen))
        {
            Debug.LogError($"No proper URL was used! String used: {URLToOpen}");
            enabled = false;
            return;
        }

        LinkButton.onClick.AddListener(OnLinkButton);
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
