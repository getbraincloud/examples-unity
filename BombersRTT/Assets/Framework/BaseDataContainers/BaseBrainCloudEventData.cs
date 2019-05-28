
namespace Gameframework
{
    public class BaseBrainCloudEventData
    {
        public string fromPlayerId;
        public string toPlayerId;
        public string eventType;
        public string evId;

        public string leaderboardId;

        public int versionId;
        public long createdAt;

        public BaseBrainCloudEventData() { }

        public BaseBrainCloudEventData(BaseBrainCloudEventData data)
        {
            leaderboardId = data.leaderboardId;
            versionId = data.versionId;
            createdAt = data.createdAt;
            fromPlayerId = data.fromPlayerId;
            toPlayerId = data.toPlayerId;
            eventType = data.eventType;
            evId = data.evId;
        }
    }
}