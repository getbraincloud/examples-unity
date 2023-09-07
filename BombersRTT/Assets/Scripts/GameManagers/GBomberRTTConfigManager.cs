using Gameframework;

namespace BrainCloudUNETExample
{

    public class GBomberRTTConfigManager : SingletonBehaviour<GBomberRTTConfigManager>
    {
        public GBomberRTTConfigManager()
        {
        }

        private void Start()
        {
        }

        #region public consts
        public const string ON_SEARCH_RESULTS_UPDATED = "OnSearchResultsUpdated";
        public const string JSON_GOLD_WINGS = "bGoldWings";
        public const string CURRENCY_GOLD_WINGS = "goldWings";
        #endregion
    }
}
