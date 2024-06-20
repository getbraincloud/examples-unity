using TMPro;
using UnityEngine;

namespace Gameframework
{
    public class VersionNumberDisplay : BaseBehaviour
    {
        public const string VERSION_TEXT_FILE = "version";
        public TextMeshProUGUI VersionText = null;

        [SerializeField]
        private bool showAppVersion = false;

        void Start()
        {
            if (showAppVersion) VersionText.text = $"Bombers {Application.version}";
            else VersionText.text = $"brainCloud {BrainCloud.Version.GetVersion()}";

            DontDestroyOnLoad(gameObject);
        }
    }
}
