using UnityEngine;
using UnityEngine.UI;

namespace BrainCloudPhotonExample
{
    /// <summary>
    /// Sets the text of the UI object
    /// </summary>
    public class VersionText : MonoBehaviour
    {
        private void Awake()
        {
            var text = GetComponent<Text>();
            if (text) text.text = ((TextAsset)Resources.Load("Version")).text.ToString();
        }
    }
}
