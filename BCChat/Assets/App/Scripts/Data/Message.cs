using BrainCloud.JsonFx.Json;
using BrainCloud.JSONHelper;
using System;
using System.Collections.Generic;

/// <summary>
/// A basic data struct for chat service's message structure.
/// </summary>
[Serializable]
public struct Message : IJSON
{
    #region Consts

    // JSON Properties
    private const string PROPERTY_VERSION    = "ver";
    private const string PROPERTY_CHANNEL_ID = "chId";
    private const string PROPERTY_MESSAGE_ID = "msgId";
    private const string PROPERTY_FROM       = "from";
    private const string PROPERTY_CONTENT    = "content";
    private const string PROPERTY_DATE       = "date";
    private const string PROPERTY_UPDATED_AT = "updatedAt";
    private const string PROPERTY_EXPIRES_AT = "expiresAt";

    #endregion

    [JsonName(PROPERTY_VERSION)]    public int ver;
    [JsonName(PROPERTY_CHANNEL_ID)] public string chId;
    [JsonName(PROPERTY_MESSAGE_ID)] public string msgId;
    [JsonName(PROPERTY_FROM)]       public From from;
    [JsonName(PROPERTY_CONTENT)]    public Content content;
    [JsonName(PROPERTY_DATE)]       public DateTime date;
    [JsonName(PROPERTY_UPDATED_AT)] public DateTime updatedAt;
    [JsonName(PROPERTY_EXPIRES_AT)] public DateTime expiresAt;

    #region IJSON

    public readonly string GetDataType() => typeof(Message).Name.ToLower();

    public readonly Dictionary<string, object> ToJSONObject() => new()
    {
        { PROPERTY_VERSION, ver }, { PROPERTY_CHANNEL_ID, chId }, { PROPERTY_MESSAGE_ID, msgId },
        { PROPERTY_FROM,    from.ToJSONObject() }, { PROPERTY_CONTENT,    content.ToJSONObject() },
        { PROPERTY_DATE,    date }, { PROPERTY_UPDATED_AT, updatedAt }, { PROPERTY_EXPIRES_AT, expiresAt }
    };

    public IJSON FromJSONObject(Dictionary<string, object> obj)
    {
        ver = obj.GetValue<int>(PROPERTY_VERSION);
        chId = obj.GetString(PROPERTY_CHANNEL_ID);
        msgId = obj.GetString(PROPERTY_MESSAGE_ID);
        from = obj.GetJSONObject<From>(PROPERTY_FROM);
        content = obj.GetJSONObject<Content>(PROPERTY_CONTENT);
        date = obj.GetDateTime(PROPERTY_DATE);
        updatedAt = obj.GetDateTime(PROPERTY_UPDATED_AT);
        expiresAt = obj.GetDateTime(PROPERTY_EXPIRES_AT);

        return this;
    }

    #endregion

    #region Message Data

    [Serializable]
    public struct From : IJSON
    {
        #region Consts

        // JSON Properties
        private const string PROPERTY_ID   = "id";
        private const string PROPERTY_NAME = "name";
        private const string PROPERTY_PIC  = "pic";

        #endregion

        [JsonName(PROPERTY_ID)]   public string id;
        [JsonName(PROPERTY_NAME)] public string name;
        [JsonName(PROPERTY_PIC)]  public string pic;

        #region IJSON

        public readonly string GetDataType() => typeof(From).Name.ToLower();

        public readonly Dictionary<string, object> ToJSONObject() => new()
        {
            { PROPERTY_ID, id }, { PROPERTY_NAME, name }, { PROPERTY_PIC, pic }
        };

        public IJSON FromJSONObject(Dictionary<string, object> obj)
        {
            id = obj.GetString(PROPERTY_ID);
            name = obj.GetString(PROPERTY_NAME);
            pic = obj.GetString(PROPERTY_PIC);

            return this;
        }

        #endregion
    }

    [Serializable]
    public struct Content : IJSON
    {
        #region Consts

        // JSON Properties
        private const string PROPERTY_TEXT = "text";
        private const string PROPERTY_RICH = "rich";

        #endregion

        [JsonName(PROPERTY_TEXT)] public string text;
        //[JsonName(PROPERTY_RICH)] public object rich;

        #region IJSON

        public readonly string GetDataType() => typeof(Content).Name.ToLower();

        public readonly Dictionary<string, object> ToJSONObject() => new()
        {
            { PROPERTY_TEXT, text }//, { PROPERTY_RICH, rich }
        };

        public IJSON FromJSONObject(Dictionary<string, object> obj)
        {
            text = obj.GetString(PROPERTY_TEXT);
            //rich = obj[PROPERTY_RICH];

            return this;
        }

        #endregion
    }

    #endregion
}
