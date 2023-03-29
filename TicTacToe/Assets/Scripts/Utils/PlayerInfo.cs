using System.Collections.Generic;

public class PlayerInfo
{
    public string PlayerName = "";
    public string ProfileId = "";
    public string Rank = "";
    public string Score = "";

    public PlayerInfo() { }

    public PlayerInfo(Dictionary<string, object> data)
    {
        PlayerName = SafeGet(data, "playerName");

        if (PlayerName.Equals("")) PlayerName = SafeGet(data, "name");

        Score = SafeGet(data, "score");

        ProfileId = SafeGet(data, "playerId");

        if (ProfileId.Equals("")) ProfileId = SafeGet(data, "profileId");

        Rank = SafeGet(data, "rank");
    }

    private string SafeGet(Dictionary<string, object> data, string key)
    {
        try
        {
            return data[key].ToString();
        }
        catch
        {
            return string.Empty;
        }
    }
}