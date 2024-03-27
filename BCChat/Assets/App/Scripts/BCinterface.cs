using System;
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
    private Text _versionText;
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
        _username = GameObject.Find("username").GetComponent<InputField>();
        _password = GameObject.Find("password").GetComponent<InputField>();
        _channelCode = GameObject.Find("channelid").GetComponent<InputField>();
        _titleMessage = GameObject.Find("title").GetComponent<InputField>();
        _message = GameObject.Find("message").GetComponent<InputField>();
        
        _bcResponseText = GameObject.Find("brainCloudResponse-Text").GetComponent<Text>();
        _versionText = GameObject.Find("Version - Text").GetComponent<Text>();
        _versionText.text = $"Version: {_bc.Client.BrainCloudClientVersion}";
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

    private void OnApplicationQuit()
    {
        if(_bc.Client.Authenticated)
        {
            _bc.Client.LogoutOnApplicationQuit();
        }
    }

    //click authentication button
    public void AuthenticateBC()
    {
        if (_username.text.Equals("") || _password.text.Equals(""))
        {
            SetResponseText("Both User Name and Password fields need to be filled", Color.red);
            return;
        }
        
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
        if (!_bc.Client.Authenticated)
        {
            SetResponseText("Authenticate first before Enabling RTT", Color.red);
            return;
        }
        if (_bc.RTTService.IsRTTEnabled())
        {
            SetResponseText("RTT is enabled..", Color.red);
            return;
        }
        _bc.RTTService.EnableRTT(EnableRTTSuccessCallback, EnableRTTErrorCallback);
        _bc.RTTService.RegisterRTTChatCallback(RTTCallback);
    }
    
    //click disableRTT Button
    public void DisableRTT()
    {
        if (!_bc.RTTService.IsRTTEnabled())
        {
            SetResponseText("RTT is not enabled..", Color.red);
            return;
        }
        _bc.RTTService.DisableRTT();
        SetResponseText("Disabling RTT...", Color.white);
    }
    
    //click post message button 
    public void PostMessage()
    {
        //Check to ensure everything is set up to post message to brainCloud
        if (!CanButtonExecute()) return;
        if (_titleMessage.text.Equals("") || _message.text.Equals(""))
        {
            SetResponseText("Need to fill both 'Title of message' and 'Message' fields", Color.red);
            return;
        }
        
        //Creating a message json for PostChatMessage call
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
        
        // Creating our channelId to send to brainCloud. Note: gl = global channel
        string channelId = _bc.Client.AppId + ":gl:" + _channelCode.text;
        _bc.ChatService.PostChatMessage(channelId, json, true, PostMessageSuccessCallback, PostMessageErrorCallback);
    }
    
    //click connect channel Button
    public void ConnectChannel()
    {
        if (!CanButtonExecute()) return;
        
        string channelId = _bc.Client.AppId + ":gl:" + _channelCode.text;
        int maxReturn = 25;

        _bc.ChatService.ChannelConnect(channelId, maxReturn, ChannelSuccessCallback, EnableRTTErrorCallback);
    }
    
    private void AuthSuccessCallback(string responseData, object cbObject)
    {
        SetResponseText("Authenticate Successful \n " + responseData, Color.white);
    }

    private void AuthErrorCallback(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        SetResponseText("Authenticate Failed \n " + statusMessage, Color.red);
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

        SetResponseText("RTT callback response \n " + display, Color.white);
    }

    private void PostMessageSuccessCallback(string jsonResponse, object cbObject)
    {
        SetResponseText("Post Message Successful !", Color.white);
    }

    private void PostMessageErrorCallback(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        SetResponseText("Post Message Failed: " + statusMessage, Color.red);
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

        SetResponseText("Successfully Enabled RTT \n " + display, Color.white);
    }

    private void EnableRTTErrorCallback(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        Debug.Log(string.Format("[chat Failed] {0}  {1}  {2}", statusCode, reasonCode, statusMessage));
        SetResponseText("Failed to Enable RTT \n " + statusMessage, Color.red);
    }

    private void ChannelSuccessCallback(string responseData, object cbObject)
    {
        Dictionary<string, object> jsonMessage = (Dictionary<string, object>)JsonReader.Deserialize(responseData);
        Dictionary<string, object> jsonData = (Dictionary<string, object>)jsonMessage["data"];

        if (!(jsonData["messages"] is Dictionary<string, object>[] messages))
        {
            SetResponseText("No messages available to display, try Posting a Message", Color.white);
            return;
        }
        
        string display = "";
        foreach (Dictionary<string, object> message in messages)
        {
            foreach(KeyValuePair<string,object> item in message)
            {
                display += item.Key + " : " + JsonWriter.Serialize(item.Value) + "\r\n";
            }
        }
        
        SetResponseText("Channel Successfully Connected \n " + display, Color.white);
    }

    private void SetResponseText(string message, Color textColor)
    {
        Debug.Log(message);
        _bcResponseText.color = textColor;
        _bcResponseText.text = message;
    }

    private bool CanButtonExecute()
    {
        if (!_bc.Client.Authenticated)
        {
            SetResponseText("Need to be Authenticated before this action", Color.red);
            return false;
        }
        if (!_bc.RTTService.IsRTTEnabled())
        {
            SetResponseText("Need to Enable RTT", Color.red);
            return false;
        }
        if (_channelCode.text.Equals(""))
        {
            SetResponseText("Global Channel Code needs to be filled", Color.red);
            return false;
        }
        return true;
    }
}