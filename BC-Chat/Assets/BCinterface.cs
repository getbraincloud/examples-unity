using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BrainCloud.JsonFx.Json;
using UnityEngine.EventSystems;

/*
 * Guide for setting up braincloud chat: http://help.getbraincloud.com/en/articles/3272685-design-messaging-chat-channels
 */

public class BCinterface : MonoBehaviour
{
    private BrainCloudWrapper _bc;
    private Text _bcResponseText;
    private InputField _username;
    private InputField _password;
    private InputField _channelCode;
    private InputField _titleMessage;
    private InputField _message;
    private EventSystem _eventSystem;
    
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        _bc = gameObject.AddComponent<BrainCloudWrapper>();
        _bc.WrapperName = name;
        _bc.Init();
        _eventSystem = EventSystem.current;
    }
    
    // Start is called before the first frame update
    void Start()
    {
        _bcResponseText = GameObject.Find("brainCloudResponse-Text").GetComponent<Text>();
        _username = GameObject.Find("username").GetComponent<InputField>();
        _password = GameObject.Find("password").GetComponent<InputField>();
        _channelCode = GameObject.Find("channelid").GetComponent<InputField>();
        _titleMessage = GameObject.Find("title").GetComponent<InputField>();
        _message = GameObject.Find("message").GetComponent<InputField>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Selectable next = _eventSystem.currentSelectedGameObject.GetComponent<Selectable>().FindSelectableOnDown();
         
            if (next)
            {
                InputField inputfield = next.GetComponent<InputField>();
                if (inputfield)
                {
                    //if it's an input field, also set the text caret
                    inputfield.OnPointerClick(new PointerEventData(_eventSystem));
                }
                _eventSystem.SetSelectedGameObject(next.gameObject, new BaseEventData(_eventSystem));
            }
        }
    }

    //click authentication button
    public void AuthenticateBC()
    {
        _bc.AuthenticateUniversal
            (
                _username.text,
                _password.text,
                true,
                AuthSuccessCallback,
                AuthErrorCallback
            );
    }
    
    //click enableRTT Button
    public void EnableRTT()
    {
        _bc.RTTService.EnableRTT(BrainCloud.RTTConnectionType.WEBSOCKET, EnableRTTSuccessCallback, EnableRTTErrorCallback);
        _bc.RTTService.RegisterRTTChatCallback(RTTCallback);
    }
    
    //click disableRTT Button
    public void DisablleRTT()
    {
        _bc.RTTService.DisableRTT();
        _bcResponseText.text = "BC RTT disabled";
    }
    
    //click post message button 
    public void PostMessage()
    {
        Dictionary<string, object> messageContent = new Dictionary<string, object>
        {
            {
                "text", "This is an example message"
            }
        };

        Dictionary<string, object> messageCustom = new Dictionary<string, object>
        {
            {
                "title", _titleMessage.text
            },
            {
                "message", _message.text
            }
        };
        
        messageContent.Add("custom", messageCustom);

        string json = JsonWriter.Serialize(messageContent);
        string channelId = _bc.Client.AppId + ":gl:" + _channelCode.text; 
        Debug.Log($"JSON: {json}");
        _bc.ChatService.PostChatMessage(channelId, json, true, PostMessageSuccessCallback, PostMessageErrorCallback);
    }
    
    //click connect channel Button
    public void ConnectChannel()
    {
        string channelId = _bc.Client.AppId + ":gl:" + _channelCode.text;
        int maxReturn = 25;

        _bc.ChatService.ChannelConnect(channelId, maxReturn, ChannelSuccessCallback, EnableRTTErrorCallback);
    }
    
    private void AuthSuccessCallback(string responseData, object cbObject)
    {
        Debug.Log("bc auth success----" + responseData);
        _bcResponseText.text = "authenticate success \n " + responseData;
    }

    private void AuthErrorCallback(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        _bcResponseText.text = "authenticate fail \n " + statusMessage;
    }

    private void RTTCallback(string responseData)
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

        _bcResponseText.text = "success \n " + display;
    }

    private void PostMessageSuccessCallback(string jsonResponse, object cbObject)
    {
        Dictionary<string, object> jsonMessage = (Dictionary<string, object>)JsonReader.Deserialize(jsonResponse);
        Dictionary<string, object> jsonData = (Dictionary<string, object>)jsonMessage["data"];
        
        string response = "Post message successful ! \n Response: ";
        foreach (KeyValuePair<string,object> keyValuePair in jsonData)
        {
            response += keyValuePair.Key + " : " + JsonWriter.Serialize(keyValuePair.Value) + "\r\n";
        }
        Debug.Log($"Post Message Response: {response}");
        _bcResponseText.text = response;
    }

    private void PostMessageErrorCallback(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        _bcResponseText.text = "Post Message Failed: " + statusMessage;
    }

    private void EnableRTTSuccessCallback(string responseData, object cbObject)
    {
        Debug.Log("bc chat success call back " + responseData);

        Dictionary<string, object> jsonMessage = (Dictionary<string, object>)JsonReader.Deserialize(responseData);
        Dictionary<string, object> jsonData = (Dictionary<string, object>)jsonMessage["data"];

        string display = "";
        foreach (KeyValuePair<string, object> message in jsonData)
        {
            display += message.Key +" : "+ JsonWriter.Serialize(message.Value) + "\r\n";
            
        }

        Debug.Log(display);
        _bcResponseText.text = "success \n " + display;
    }

    private void EnableRTTErrorCallback(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        Debug.Log(string.Format("[chat Failed] {0}  {1}  {2}", statusCode, reasonCode, statusMessage));
        _bcResponseText.text = "fail \n " + statusMessage;
    }

    private void ChannelSuccessCallback(string responseData, object cbObject)
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
        _bcResponseText.text = "success \n " + display;
    }
}
