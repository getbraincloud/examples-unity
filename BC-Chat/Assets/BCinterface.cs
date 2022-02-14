using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BrainCloud.JsonFx.Json;

public class BCinterface : MonoBehaviour
{
    public BrainCloudWrapper _bc;

    Text bcreturn;
    InputField username;
    InputField password;
    InputField channelid;

    //private Camera mainCamera;
    // Start is called before the first frame update
    void Start()
    {
        bcreturn = GameObject.Find("bcreturn").GetComponent<Text>();
        username = GameObject.Find("username").GetComponent<InputField>();
        password = GameObject.Find("password").GetComponent<InputField>();
        channelid = GameObject.Find("channelid").GetComponent<InputField>();
    }

    // Update is called once per frame
    void Update()
    { }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        _bc = gameObject.AddComponent<BrainCloudWrapper>();
        _bc.WrapperName = gameObject.name;
        _bc.Init();
    }

    //click authentication button
    public void AuthenticateBC()
    {
        _bc.AuthenticateUniversal(username.GetComponent<InputField>().text, password.GetComponent<InputField>().text, true, authSuccess_BCcall, authError_BCcall);
    }

    public void authSuccess_BCcall(string responseData, object cbObject)
    {
        Debug.Log("bc auth success----" + responseData);
        bcreturn.GetComponent<Text>().text = "authenticate success \n " + responseData;
    }

    public void authError_BCcall(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        bcreturn.GetComponent<Text>().text = "authenticate fail \n " + statusMessage;
    }


    //click connnect channel Button
    public void ConnectChannel()
    {

        string channelId = channelid.GetComponent<InputField>().text;
        int maxReturn = 25;

        _bc.ChatService.ChannelConnect(channelId, maxReturn, channelSuccess_BCcall, peercError_BCcall);
    }

    //click enableRTT Button
    public void EnablleRTT()
    {
        _bc.RTTService.EnableRTT(BrainCloud.RTTConnectionType.WEBSOCKET, peercSuccess_BCcall, peercError_BCcall);
        _bc.RTTService.RegisterRTTChatCallback(rttSuccess_BCcall);
    }

    public void rttSuccess_BCcall(string responseData)
    {
        Debug.Log("bc RTT register chat success call back");

        Dictionary<string, object> jsonMessage = (Dictionary<string, object>)JsonReader.Deserialize(responseData);
        Dictionary<string, object> jsonData = (Dictionary<string, object>)jsonMessage["data"];

        string display = "";
        foreach (KeyValuePair<string, object> message in jsonData)
        {
            display += message.Key + " : " + JsonWriter.Serialize(message.Value) + "\r\n";

        }

        Debug.Log(display);

        bcreturn.GetComponent<Text>().text = "success \n " + display;

        //bcreturn.GetComponent<Text>().text = "registerRTTPresence success \n " + responseData;
    }


    //click disableRTT Button
    public void DisablleRTT()
    {
        _bc.RTTService.DisableRTT();
        bcreturn.GetComponent<Text>().text = "BC RTT disabled";
    }

    public void peercSuccess_BCcall(string responseData, object cbObject)
    {
        Debug.Log("bc chat success call back "+responseData);

        Dictionary<string, object> jsonMessage = (Dictionary<string, object>)JsonReader.Deserialize(responseData);
        Dictionary<string, object> jsonData = (Dictionary<string, object>)jsonMessage["data"];

        string display="";
        foreach (KeyValuePair<string, object> message in jsonData)
        {
            display += message.Key +" : "+ JsonWriter.Serialize(message.Value) + "\r\n";
            
        }

        Debug.Log(display);

        bcreturn.GetComponent<Text>().text = "success \n " + display;
    }

    public void peercError_BCcall(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        Debug.Log(string.Format("[chat Failed] {0}  {1}  {2}", statusCode, reasonCode, statusMessage));
        bcreturn.GetComponent<Text>().text = "fail \n " + statusMessage;
    }

    public void channelSuccess_BCcall(string responseData, object cbObject)
    {
        Dictionary<string, object> jsonMessage = (Dictionary<string, object>)JsonReader.Deserialize(responseData);
        Dictionary<string, object> jsonData = (Dictionary<string, object>)jsonMessage["data"];
        Dictionary<string, object>[] messages = (Dictionary<string, object>[])jsonData["messages"];

        string display = "";
        foreach (Dictionary<string, object> message in messages)
        {
            foreach(KeyValuePair<string,object> item in message)
            {
                display += item.Key + " : " + JsonWriter.Serialize(item.Value) + "\r\n";
            }
            
        }

        Debug.Log(display);
        bcreturn.GetComponent<Text>().text = "success \n " + display;
    }
}
