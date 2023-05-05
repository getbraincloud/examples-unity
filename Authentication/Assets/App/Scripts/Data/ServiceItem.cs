using UnityEngine;

/// <summary>
/// <see cref="ScriptableObject"/> on loading prefabs and scripts to show brainCloud service examples.
/// </summary>
[CreateAssetMenu(fileName = "ServiceItem", menuName = "ScriptableObjects/Service Item")]
public class ServiceItem : ScriptableObject
{
    [SerializeField] private string ServiceName = string.Empty;
    [SerializeField, TextArea(10, 50)] private string ServiceDescription = string.Empty;
    [SerializeField] private string ServiceAPILink = string.Empty;
    [SerializeField] private ContentUIBehaviour ServicePrefab = default;

    /// <summary>
    /// Name of the service.
    /// </summary>
    public string Name => ServiceName;

    /// <summary>
    /// Description of the service.
    /// </summary>
    public string Description => ServiceDescription;

    /// <summary>
    /// Link to the API reference of the service.
    /// </summary>
    public string APILink => ServiceAPILink;

    /// <summary>
    /// The prefab that showcases this service.
    /// </summary>
    public ContentUIBehaviour Prefab => ServicePrefab;
}
