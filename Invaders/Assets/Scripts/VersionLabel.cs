using UnityEngine;
using TMPro;

public class VersionLabel : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<TextMeshProUGUI>().text = "v" + Application.version;
    }
}
