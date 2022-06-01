using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BrainCloud.LitJson;
using System.Text;
using UnityEngine.UI; 

public class TextLogger : MonoBehaviour
{
    [SerializeField] Text logText;
    [SerializeField] RectTransform logTextParent;
    [SerializeField] ContentSizeFitter sizeFitter;
    [SerializeField] Button clearLogButton; 

    Text newlog;
    bool bContentSizeSet = false;

    public static TextLogger instance; 

    void Awake()
    {
        instance = this; 
    }

    private void Update()
    {
        //Setting the size of the TextLogger content on first frame to ensure first log doesn't get cut off or lock you out of scrolling
        if(newlog != null && bContentSizeSet == false)
        {
            if (newlog.gameObject.GetComponent<RectTransform>().sizeDelta != new Vector2(0.0f, 0.0f) && bContentSizeSet == false)
            {
                logTextParent.sizeDelta = newlog.gameObject.GetComponent<RectTransform>().sizeDelta;
                bContentSizeSet = true;
            }
        }
    }

    public void AddLogJson(string json, string requestName = "")
    {
        StringBuilder sb = new StringBuilder();
        JsonWriter writer = new JsonWriter(sb);
        writer.PrettyPrint = true;
        JsonMapper.ToJson(JsonMapper.ToObject(json), writer);

        newlog = Instantiate(logText, logTextParent);
        newlog.text = requestName + " RESPONSE:" + sb.ToString() + "\n";
    }

    public void OnClearLogClick()
    {
        Text[] logs = logTextParent.gameObject.GetComponentsInChildren<Text>();

        foreach(Text log in logs)
        {
            Destroy(log.gameObject);
        }

        bContentSizeSet = false;
        newlog = null;
    }
}
