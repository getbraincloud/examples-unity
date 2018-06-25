// Copyright 2016 Google Inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using BrainCloud.Common;
using Firebase;
using Firebase.Messaging;
using UnityEngine;


//Note - This example is modified and based on the code found here: https://github.com/firebase/quickstart-unity 
//Be sure to add your google-services.json file, App Id, and App Secret to get this demo to work
//You will also need to import the Firebase Unity SDK https://firebase.google.com/docs/unity/setup 


// Handler for UI buttons on the scene.  Also performs some
// necessary setup (initializing the firebase app, etc) on
// startup.
public
    class AndroidUIHandler : MonoBehaviour
{
    private const int kMaxLogSize = 16382;
    private Vector2 controlsScrollViewVector = Vector2.zero;
    private DependencyStatus dependencyStatus = DependencyStatus.UnavailableOther;
    public GUISkin fb_GUISkin;

    private string firebaseToken = "";
    private string logText = "";
    private Vector2 scrollViewVector = Vector2.zero;
    private readonly bool UIEnabled = true;

    private BrainCloudWrapper _bc;

    // When the app starts, check to make sure that we have
    // the required dependencies to use Firebase, and if not,
    // add them if possible.
    private void Start()
    {
        dependencyStatus = FirebaseApp.CheckDependencies();
        if (dependencyStatus != DependencyStatus.Available)
            FirebaseApp.FixDependenciesAsync().ContinueWith(task =>
            {
                dependencyStatus = FirebaseApp.CheckDependencies();
                if (dependencyStatus == DependencyStatus.Available) InitializeFirebase();
                else
                    Debug.LogError(
                        "Could not resolve all Firebase dependencies: " + dependencyStatus);
            });
        else InitializeFirebase();

        
        
        //Set up brainCloud
        _bc = gameObject.AddComponent<BrainCloudWrapper>();        
        _bc.Init();

        _bc.AlwaysAllowProfileSwitch = true;
    }

    // Setup message event handlers.
    private void InitializeFirebase()
    {
        FirebaseMessaging.MessageReceived += OnMessageReceived;
        FirebaseMessaging.TokenReceived += OnTokenReceived;
        DebugLog("Firebase Messaging Initialized");
    }

    public void OnMessageReceived(object sender, MessageReceivedEventArgs e)
    {
        DebugLog("Received a new message");
        var notification = e.Message.Notification;
        if (notification != null)
        {
            DebugLog("title: " + notification.Title);
            DebugLog("body: " + notification.Body);
        }
        if (e.Message.From.Length > 0)
            DebugLog("from: " + e.Message.From);
        if (e.Message.Data.Count > 0)
        {
            DebugLog("data:");
            foreach (var iter in
                e.Message.Data) DebugLog("  " + iter.Key + ": " + iter.Value);
        }
    }

    public void OnTokenReceived(object sender, TokenReceivedEventArgs token)
    {
        firebaseToken = token.Token;

        DebugLog("Received Registration Token: " + token.Token);
    }

    // End our messaging session when the program exits.
    public void OnDestroy()
    {
        FirebaseMessaging.MessageReceived -= OnMessageReceived;
        FirebaseMessaging.TokenReceived -= OnTokenReceived;
    }

    // Output text to the debug log text field, as well as the console.
    public void DebugLog(string s)
    {
        print(s);
        logText += s + "\n";

        while (logText.Length > kMaxLogSize)
        {
            var index = logText.IndexOf("\n");
            logText = logText.Substring(index + 1);
        }

        scrollViewVector.y = int.MaxValue;
    }

    // Render the log output in a scroll view.
    private void GUIDisplayLog()
    {
        scrollViewVector = GUILayout.BeginScrollView(scrollViewVector);
        GUILayout.Label(logText);
        GUILayout.EndScrollView();
    }

    // Render the buttons and other controls.
    private void GUIDisplayControls()
    {
        if (UIEnabled)
        {
            controlsScrollViewVector =
                GUILayout.BeginScrollView(controlsScrollViewVector);
            GUILayout.BeginVertical();

            float inputWidth = Screen.width < Screen.height ? Screen.width * 0.75f : Screen.width * 0.30f; 
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Profile Id: ", GUILayout.Width( Screen.width * 0.20f));
            GUILayout.TextField(_bc.GetStoredProfileId(), GUILayout.Width(inputWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Token: ", GUILayout.Width(Screen.width * 0.20f));
            GUILayout.TextField(firebaseToken, GUILayout.Width(inputWidth));
            GUILayout.EndHorizontal();

            if (!_bc.Client.IsAuthenticated())
            {
                if (!string.IsNullOrEmpty(_bc.GetStoredAnonymousId()) ||
                    !string.IsNullOrEmpty(_bc.GetStoredProfileId())) {
                    _bc.ResetStoredAnonymousId();
                    _bc.ResetStoredProfileId();
                }

            if (GUILayout.Button("Authenticate"))
                _bc.AuthenticateAnonymous(
                        (response, cbObject) =>
                        {
                            DebugLog(string.Format("brainCloud Authentication Success: {0}", response));
                        },
                        (status, code, error, cbObject) =>
                        {
                            DebugLog(
                                string.Format("brainCloud Authentication Failed: {0} {1} {2}", status, code, error));
                        });
            }
            else
            {
                if (GUILayout.Button("Register"))
                {
                    _bc.PushNotificationService
                        .RegisterPushNotificationDeviceToken(Platform.GooglePlayAndroid, firebaseToken);

                    DebugLog("Registered to brainCloud");
                }
                if (GUILayout.Button("Deregister"))
                {
                    _bc.PushNotificationService
                        .DeregisterPushNotificationDeviceToken(Platform.GooglePlayAndroid, firebaseToken);

                    DebugLog("Deregistered from brainCloud");
                }

                if (GUILayout.Button("SendNormalizedPushNotification"))
                {
                    DebugLog("SendNormalizedPushNotification");

                    _bc.PushNotificationService.SendNormalizedPushNotification(
                        _bc.GetStoredProfileId(),
                        "{ \"body\": \"content of message\", \"title\": \"message title\" }",
                        null,
                        (response, cbObject) => { DebugLog(string.Format("Success: {0}", response)); },
                        (status, code, error, cbObject) =>
                        {
                            DebugLog(string.Format("Failed: {0} {1} {2}", status, code, error));
                        });
                }

                if (GUILayout.Button("SendRichPushNotification"))
                {
                    DebugLog("SendRichPushNotification");

                    _bc.PushNotificationService.SendRichPushNotification(
                        _bc.GetStoredProfileId(),
                        1,
                        (response, cbObject) => { DebugLog(string.Format("Success: {0}", response)); },
                        (status, code, error, cbObject) =>
                        {
                            DebugLog(string.Format("Failed: {0} {1} {2}", status, code, error));
                        });
                }

                if (GUILayout.Button("SendRawPushNotification"))
                {
                    DebugLog("SendRawPushNotification");

                    _bc.PushNotificationService.SendRawPushNotification(
                        _bc.GetStoredProfileId(),
                        "{ \"notification\": { \"body\": \"content of message\", \"title\": \"message title\" }, \"data\": { \"customfield1\": \"customValue1\", \"customfield2\": \"customValue2\" }, \"priority\": \"normal\" }",
                        "{ \"aps\": { \"alert\": { \"body\": \"content of message\", \"title\": \"message title\" }, \"badge\": 0, \"sound\": \"gggg\" } }",
                        "{\"template\": \"content of message\"}",
                        (response, cbObject) => { DebugLog(string.Format("Success: {0}", response)); },
                        (status, code, error, cbObject) =>
                        {
                            DebugLog(string.Format("Failed: {0} {1} {2}", status, code, error));
                        });
                }
            }

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }
    }

    // Render the GUI:
    private void OnGUI()
    {
        GUI.skin = fb_GUISkin;
        if (dependencyStatus != DependencyStatus.Available)
        {
            GUILayout.Label("One or more Firebase dependencies are not present.");
            GUILayout.Label("Current dependency status: " + dependencyStatus);
            return;
        }

        Rect logArea;
        Rect controlArea;

        if (Screen.width < Screen.height)
        {
            // Portrait mode
            controlArea = new Rect(0.0f, 0.0f, Screen.width, Screen.height * 0.5f);
            logArea = new Rect(0.0f, Screen.height * 0.5f, Screen.width, Screen.height * 0.5f);
        }
        else
        {
            // Landscape mode
            controlArea = new Rect(0.0f, 0.0f, Screen.width * 0.5f, Screen.height);
            logArea = new Rect(Screen.width * 0.5f, 0.0f, Screen.width * 0.5f, Screen.height);
        }

        GUILayout.BeginArea(new Rect(0.0f, 0.0f, Screen.width, Screen.height));

        GUILayout.BeginArea(logArea);
        GUIDisplayLog();
        GUILayout.EndArea();

        GUILayout.BeginArea(controlArea);
        GUIDisplayControls();
        GUILayout.EndArea();

        GUILayout.EndArea();
    }
}