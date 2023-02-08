using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServiceItem : ScriptableObject
{
    [SerializeField] private string ServiceName = string.Empty;
    [SerializeField] private string ServiceDescription = string.Empty;
    [SerializeField] private string ServiceAPILink = string.Empty;
    [SerializeField] private GameObject ServicePrefab = default;

    public string Name => ServiceName;

    public string Description => ServiceDescription;

    public string APILink => ServiceAPILink;

    public GameObject Prefab => ServicePrefab;
}
