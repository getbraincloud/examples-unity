using BrainCloud.JsonFx.Json;
using System.Collections.Generic;

namespace Gameframework
{
    public class XPData
    {
        public int OriginalLevel = 0;
        public int CurrentLevel = 0;
        public int ExperiencePoints = 0;
        public int PrevThreshold = 0;
        public int NextThreshold = 0;

        public bool XPCapped = false;

        public XPData() { }

        public XPData(XPData in_data)
        {
            CurrentLevel = in_data.CurrentLevel;
            ExperiencePoints = in_data.ExperiencePoints;
            PrevThreshold = in_data.PrevThreshold;
            NextThreshold = in_data.NextThreshold;
            XPCapped = in_data.XPCapped;
        }

        public void Init(string in_scriptResultJson)
        {
            var res = JsonReader.Deserialize<Dictionary<string, object>>(in_scriptResultJson);
            var data = (res[BrainCloudConsts.JSON_DATA] as Dictionary<string, object>)[BrainCloudConsts.JSON_RESPONSE] as Dictionary<string, object>;
            Init(data);
        }

        public void Init(Dictionary<string, object> in_jsonBlob)
        {
            CurrentLevel = (int)in_jsonBlob[BrainCloudConsts.JSON_CURRENT_LEVEL];
            ExperiencePoints = (int)in_jsonBlob[BrainCloudConsts.JSON_EXPERIENCE_POINTS];
            PrevThreshold = (int)in_jsonBlob[BrainCloudConsts.JSON_PREV_THRESHOLD];
            NextThreshold = (int)in_jsonBlob[BrainCloudConsts.JSON_NEXT_THRESHOLD];
            XPCapped = (bool)in_jsonBlob[BrainCloudConsts.JSON_XP_CAPPED];
        }
    }
}