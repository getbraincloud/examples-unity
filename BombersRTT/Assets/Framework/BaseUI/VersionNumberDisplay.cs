using TMPro;

namespace Gameframework
{
    public class VersionNumberDisplay : BaseBehaviour
    {
        public const string VERSION_TEXT_FILE = "version";
        public TextMeshProUGUI VersionText = null;

        void Start()
        {
            VersionText.text = $"v: {BrainCloud.Version.GetVersion()}";

            DontDestroyOnLoad(gameObject);
        }
    }
}
