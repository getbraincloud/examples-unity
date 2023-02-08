/// <summary>
/// 
/// </summary>
public interface IServiceUI
{
    public bool IsInteractable { get; set; }

/* Template to copy for classes that make use of this interface
    [Header("Main")]
    [SerializeField] private CanvasGroup UICanvasGroup = default;
 
    public bool IsInteractable
    {
        get { return UICanvasGroup.interactable; }
        set { UICanvasGroup.interactable = value; }
    }

    private BrainCloudService bcService = default;

    #region Unity Messages

    private void Awake()
    {

    }

    private void OnEnable()
    {

    }

    private void Start()
    {
        bcService = BCManager.Service;
    }

    private void OnDisable()
    {

    }

    private void OnDestroy()
    {
        bcService = null;
    }

    #endregion

    #region UI

    private void OnInteractable()
    {

    }

    #endregion

    #region brainCloud

    private void OnServiceFunction()
    {

    }

    #endregion
*/
}
