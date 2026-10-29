using UnityEngine;

public class LoadingOverlay : MonoBehaviour
{
    public enum LoadingOverlayType
    {
        LoadingBar,
        Spinner,
        Both
    }

    [Header("Config")]
    [SerializeField]
    private LoadingOverlayType _loadingType;

    public LoadingBar loadingBar;
    [SerializeField]
    private GameObject _spinner;

    private void Start()
    {
        OnLoadingTypeChanged();
    }

    private void OnLoadingTypeChanged()
    {
        switch (_loadingType)
        {
            case LoadingOverlayType.LoadingBar:
                _spinner.gameObject.SetActive(false);
                loadingBar.gameObject.SetActive(true);
                break;
            case LoadingOverlayType.Spinner:
                _spinner.gameObject.SetActive(true);
                loadingBar.gameObject.SetActive(false);
                break;
            case LoadingOverlayType.Both:
                _spinner.gameObject.SetActive(true);
                loadingBar.gameObject.SetActive(true);
                break;
        }
    }

    public void SetLoadingValue(float value)
    {
        if (loadingBar != null)
        {
            loadingBar.SetLoadingValue(value);
        }
    }

    public void SetLoadingType(LoadingOverlayType loadingType)
    {
        _loadingType = loadingType;
        OnLoadingTypeChanged();
    }

}
