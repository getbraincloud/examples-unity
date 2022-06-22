using UnityEngine.UI;
using UnityEngine;
namespace Gameframework
{
    public class VersionNumberDisplay : BaseBehaviour
    {
        public const string VERSION_TEXT_FILE = "version";
        public Text VersionText = null;
        // Use this for initialization
        void Start()
        {
            VersionText.text = $"v: {BrainCloud.Version.GetVersion()}";

            DontDestroyOnLoad(gameObject);
        }
    }
}
