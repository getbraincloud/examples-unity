using UnityEngine;

[CreateAssetMenu(fileName = "ServiceItem", menuName = "ScriptableObjects/ServiceItem")]
public class ServiceItem : ScriptableObject
{
    [SerializeField] private string ServiceName = string.Empty;
    [SerializeField, TextArea(10, 50)] private string ServiceDescription = string.Empty;
    [SerializeField] private string ServiceAPILink = string.Empty;
    [SerializeField] private ContentUIBehaviour ServicePrefab = default;

    public string Name => ServiceName;

    public string Description => ServiceDescription;

    public string APILink => ServiceAPILink;

    public ContentUIBehaviour Prefab => ServicePrefab;
}
