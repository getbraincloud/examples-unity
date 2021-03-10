using BrainCloud;
using BrainCloud.JsonFx.Json;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Facebook.Unity;
using System;

public class BCfblogin : MonoBehaviour
{
    public BrainCloudWrapper _bc;

    Text bcreturn;
    InputField username;
    InputField password;
    InputField profileid;

    //private Camera mainCamera;
    // Start is called before the first frame update
    void Start()
    {
        bcreturn = GameObject.Find("bcreturn").GetComponent<Text>();
        username = GameObject.Find("username").GetComponent<InputField>();
        password = GameObject.Find("password").GetComponent<InputField>();
        profileid = GameObject.Find("profileid").GetComponent<InputField>();
    }

    // Update is called once per frame
    void Update()
    { }

    private void Awake()
    {
        //brainCloud init
        DontDestroyOnLoad(gameObject);
        _bc = gameObject.AddComponent<BrainCloudWrapper>();
        _bc.WrapperName = gameObject.name;
        _bc.Init();

        //Facebook init
        if (!FB.IsInitialized)
        {
            // Initialize the Facebook SDK
            FB.Init(InitCallback, OnHideUnity);
        }
        else
        {
            // Already initialized, signal an app activation App Event
            FB.ActivateApp();
        }
    }

    private void InitCallback()
    {
        if (FB.IsInitialized)
        {
            // Signal an app activation App Event
            FB.ActivateApp();
            // Continue with Facebook SDK
            // ...

            if (AccessToken.CurrentAccessToken != null)
            {
                Debug.Log(AccessToken.CurrentAccessToken.ToString());
                bcreturn.GetComponent<Text>().text = "CurrentAccessToken: \n " + AccessToken.CurrentAccessToken.ToString();
            }
        }
        else
        {
            Debug.Log("Failed to Initialize the Facebook SDK");
        }
    }

    private void OnHideUnity(bool isGameShown)
    {
        if (!isGameShown)
        {
            // Pause the game - we will need to hide
            Time.timeScale = 0;
        }
        else
        {
            // Resume the game - we're getting focus again
            Time.timeScale = 1;
        }
    }



    //click Facebooklogin Button
    public void Facebooklogin()
    {
        //_bc.RTTService.EnableRTT(BrainCloud.RTTConnectionType.WEBSOCKET, peercSuccess_BCcall, peercError_BCcall);
        //_bc.RTTService.RegisterRTTEventCallback(rttSuccess_BCcall);
        var perms = new List<string>() { "public_profile", "email" };
        FB.LogInWithReadPermissions(perms, AuthCallback);
        //FB.Android.RetrieveLoginStatus(LoginStatusCallback);
        //FB.Canvas.Pay("756158598324071");
    }

    private void AuthCallback(ILoginResult result)
    {
        Debug.Log(result.ToString());
        bcreturn.GetComponent<Text>().text = "LoginWithTrackingPreference callback \n " + result.ToString();

        if (FB.IsLoggedIn)
        {
            // AccessToken class will have session details
            var aToken = Facebook.Unity.AccessToken.CurrentAccessToken;
            // Print current access token's User ID
            Debug.Log("IsLoggedIn: userid: " + aToken.UserId);
            Debug.Log("IsLoggedIn: TokenString: " + aToken.TokenString);

            //auth user to bc
            _bc.AuthenticateFacebook(aToken.UserId, aToken.TokenString, true, peercSuccess_BCcall, peercError_BCcall);


            // Print current access token's granted permissions
            foreach (string perm in aToken.Permissions)
            {
                Debug.Log("IsLoggedIn: perm: " + perm);
            }
        }
        else
        {
            Debug.Log("User cancelled login");
        }
    }

    private void LoginStatusCallback(ILoginStatusResult result)
    {
        if (!string.IsNullOrEmpty(result.Error))
        {
            Debug.Log("Error Express : " + result.Error);
        }
        else if (result.Failed)
        {
            Debug.Log("Failure Express : Access Token could not be retrieved");
        }
        else
        {
            // Successfully logged user in
            // A popup notification will appear that says "Logged in as <User Name>"
            Debug.Log("Success: " + result.AccessToken.UserId);
        }
    }




    //click limitedfacebooklogin button
    public void LimitedFBlogin()
    {
        //FB.Mobile.LoginWithTrackingPreference(LoginTracking.LIMITED, new List<string>() { "public_profile", "email", "user_friends" }, "nonce123", this.HandleResult);
        FB.Mobile.LoginWithTrackingPreference(LoginTracking.LIMITED, new List<string>() { "public_profile", "email" }, "nonce123", this.HandleResult);
    }

    protected void HandleResult(IResult result)
    {
        Debug.Log(result.ToString());
        bcreturn.GetComponent<Text>().text = "LoginWithTrackingPreference callback \n " + result.ToString();

        if (FB.IsLoggedIn)
        {
            var profile = FB.Mobile.CurrentProfile();
            if (profile != null)
            {
                Debug.Log("limited login user profile \n "+ profile.ToString());
            }

            var authToken = FB.Mobile.CurrentAuthenticationToken();

            Debug.Log("limited login user CurrentAuthenticationToken \n " + authToken.ToString());

            Debug.Log("limited login user ClientToken \n " + FB.ClientToken.ToString());


            //auth user to bc with OIDC token
            //_bc.AuthenticateFacebook(profile.UserID, authToken.TokenString, true, peercSuccess_BCcall, peercError_BCcall);

            _bc.AuthenticateFacebookLimited(profile.UserID, authToken.TokenString, true, peercSuccess_BCcall, peercError_BCcall);

            //// this line code will not work for limited
            //// AccessToken class will have session details
            //var aToken = Facebook.Unity.AccessToken.CurrentAccessToken;
            
            //// Print current access token's User ID
            //Debug.Log("IsLoggedIn: userid: " + aToken.UserId);
            //Debug.Log("IsLoggedIn: TokenString: " + aToken.TokenString);

            ////auth user to bc
            //_bc.AuthenticateFacebook(aToken.UserId, aToken.TokenString, true, peercSuccess_BCcall, peercError_BCcall);


            //// Print current access token's granted permissions
            //foreach (string perm in aToken.Permissions)
            //{
            //    Debug.Log("IsLoggedIn: perm: " + perm);
            //}
        }
        else
        {
            Debug.Log("User cancelled login");
        }
    }





    //click getsociallb Button
    public void GetSocialLB()
    {
        string leaderboardId = "default";
        bool replaceName = false;
        SuccessCallback successCallback = (response, cbObject) =>
        {
            Debug.Log(string.Format("Success | {0}", response));
            bcreturn.GetComponent<Text>().text = "socaillb success \n " + response;
        };
        FailureCallback failureCallback = (status, code, error, cbObject) =>
        {
            Debug.Log(string.Format("Failed | {0}  {1}  {2}", status, code, error));
            bcreturn.GetComponent<Text>().text = "sociallb fail \n " + error;
        };

        _bc.LeaderboardService.GetSocialLeaderboard(leaderboardId, replaceName, successCallback, failureCallback);
    }





    //click graph api Button
    public void GetGraphAPI()
    {
        //test Graph API
        Debug.Log("bc graph api version : --- "+ FB.GraphApiVersion + " \n");

        //IDictionary<string, string> openWith = new Dictionary<string, string>();
        //// Add some elements to the dictionary. There are no
        //// duplicate keys, but some of the values are duplicates.
        //openWith.Add("txt", "notepad.exe");
        //openWith.Add("bmp", "paint.exe");
        //openWith.Add("dib", "paint.exe");
        //openWith.Add("rtf", "wordpad.exe");



        //openWith.Add("access_token", "2731168760532542|W7y0BHtK4I5vCcFOzB1P7l8JDhE");
        //openWith.Add("reason", reason.DENIED_REFUND.ToString());
        ////this call only returen dispute suceess, no too much info
        ////FB.API("/3720199854759826/dispute", HttpMethod.POST, GraphCallback, openWith);

        ////should use this get payment method to read more info
        //openWith.Add("fields", "disputes,actions,tax,country,items,user");
        //FB.API("/3720199854759826", HttpMethod.GET, GraphCallback, openWith);

        FB.API("/me", HttpMethod.GET, GraphCallback);
    }
    private enum reason { GRANTED_REPLACEMENT_ITEM, DENIED_REFUND, BANNED_USER };
    private void GraphCallback(IGraphResult result)
    {
        Debug.Log("bc graph api result " + JsonWriter.Serialize(result));
        bcreturn.GetComponent<Text>().text = "graph api result: \n " + JsonWriter.Serialize(result);
    }



    //NOTE: this callback will BE called when send event to this user, IS NOT CALLED when RegisterRTTEventCallback()
    public void rttSuccess_BCcall(string responseData)
    {
        Debug.Log("bc RTT register event rttSuccess_BCcall Ssuccess call back");
        bcreturn.GetComponent<Text>().text = "registerRTTEventCallback rttSuccess_BCcall success \n " + responseData;

        Dictionary<string, object> jsonMessage = (Dictionary<string, object>)JsonReader.Deserialize(responseData);
        Dictionary<string, object> jsonData = (Dictionary<string, object>)jsonMessage["data"];
        Dictionary<string, object> eventData = (Dictionary<string, object>)jsonData["eventData"];

  
        eventData.Add("addition", 5);

    

        string display = "";

        foreach (KeyValuePair<string, object> item in eventData)
        {
            //display += item.Key + " : " + JsonWriter.Serialize(item.Value) + "\r\n";
            display += item.Key + " : " + item.Value + "\r\n";
        }


        Debug.Log("bc event call back get the signle TurnNumber value from send event eventData--" + eventData["TurnNumber"]);
        Debug.Log("bc event call back get the values from send event eventData--" + eventData.Values.GetEnumerator());
        bcreturn.GetComponent<Text>().text = "success \n " + display;
    }

    string productURL;
    //private Func<AuthenticationToken> lToken;

    public void BuyProduct()
    {
        if (!string.IsNullOrEmpty(this.productURL))
        {
            Debug.Log("buyproduct url is not empty: "+ this.productURL);
            FB.Canvas.Pay(this.productURL, quantity: 2, callback: PayCallback);
        }
        else
        {
            Debug.Log("buyproduct url is Empty: " + this.productURL);
            FB.Canvas.Pay(product: "https://sharedprod.braincloudservers.com/fbproductservice?gameId=13229&itemId=barBundle1Imp&priceId=3", quantity: 2, callback: PayCallback);
        }
    }

    private void PayCallback(IPayResult response)
    {
        //Debug.Log(response.RawResult);
        //using dictonary instead
        Debug.Log(response.ResultDictionary);
        

        //var resultJson = response.ResultDictionary;
        //Dictionary<string, object> signedRequestD = (Dictionary<string, object>)resultJson["signed_request"];
        //string signedRequest = JsonWriter.Serialize(signedRequestD);

        string responseS = JsonWriter.Serialize(response.ResultDictionary);
        Dictionary<string, object> responseJ = (Dictionary<string, object>)JsonReader.Deserialize(responseS);

        string signedRequest = responseJ["signed_request"].ToString();

        bcreturn.GetComponent<Text>().text = "success \n " + responseJ["signed_request"].ToString();


        //bcreturn.GetComponent<Text>().text = "success \n " + JsonWriter.Serialize(response.ResultDictionary).ToString();
        //bcreturn.GetComponent<Text>().text = "success \n " + response.ResultDictionary.ToString();

        //test verify (gtting signed_request from screen)
        string storeid = "facebook";
        string receiptdata = "{\"signedRequest\":\""+ signedRequest +"\"}";
        _bc.AppStoreService.VerifyPurchase(storeid, receiptdata, peercSuccess_BCcall, peercError_BCcall);

    }

    public void peercSuccess_BCcall(string responseData, object cbObject)
    {
        Debug.Log("bc success call back" + responseData);
        bcreturn.GetComponent<Text>().text = "success \n " + responseData;

        Dictionary<string, object> jsonMessage = (Dictionary<string, object>)JsonReader.Deserialize(responseData);
 
        Dictionary<string, object> jsonData = (Dictionary<string, object>)jsonMessage["data"];
        if (jsonData.ContainsKey("productInventory")){
            Debug.Log("bc productInventory" + jsonData["productInventory"].ToString());

            Dictionary<string, object> [] products = (Dictionary<string, object>[])jsonData["productInventory"];
            Debug.Log("bc productInventory product index 0 ---" + products[0].ToString());
            //profileid.GetComponent<InputField>().text = products[0].ToString();

            string display = "";
            foreach (Dictionary<string, object> product in products)
            {
                foreach (KeyValuePair<string, object> item in product)
                {
                    display += item.Key + " : " + JsonWriter.Serialize(item.Value) + "\r\n";
                    //display += item.Key + " : " + item.Value + "\r\n";

                    //test to set the last fbUrl as the product to buy, or set an array later
                    if(item.Key == "fbUrl")
                    {
                        this.productURL = item.Value.ToString();
                        Debug.Log("bc productInventory get product fbUrl---" + item.Value);
                    }
                }
            }

            Debug.Log("bc productInventory this.productURL---" + this.productURL);
            Debug.Log("bc productInventory data item---" + display);
            profileid.GetComponent<InputField>().text = display;
        }
    }

    public void peercError_BCcall(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        Debug.Log("bc error call back" + statusMessage);
        bcreturn.GetComponent<Text>().text = "fail \n " + statusMessage;
    }
}
