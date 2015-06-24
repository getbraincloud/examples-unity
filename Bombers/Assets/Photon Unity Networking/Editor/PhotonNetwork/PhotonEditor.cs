// ----------------------------------------------------------------------------
// <copyright file="PhotonEditor.cs" company="Exit Games GmbH">
//   PhotonNetwork Framework for Unity - Copyright (C) 2011 Exit Games GmbH
// </copyright>
// <summary>
//   MenuItems and in-Editor scripts for PhotonNetwork.
// </summary>
// <author>developer@exitgames.com</author>
// ----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ExitGames.Client.Photon;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;


public class PunWizardText
{
    public string WindowTitle = "PUN Wizard";
    public string SetupWizardWarningTitle = "Warning";
    public string SetupWizardWarningMessage = "You have not yet run the Photon setup wizard! Your game won't be able to connect. See Windows -> Photon Unity Networking.";
    public string MainMenuButton = "Main Menu";

    public string SetupWizardTitle = "PUN Setup";
    public string SetupWizardInfo = "Thanks for importing Photon Unity Networking.\nThis window should set you up.\n\n<b>•</b> To use an existing Photon Cloud App, enter your AppId.\n<b>•</b> To register an account or access an existing one, enter the account’s mail address.\n<b>•</b> To use Photon OnPremise, skip this step.";
    public string EmailOrAppIdLabel = "AppId or Email";
    public string GoButton = "Go";
    public string SkipButton = "Skip";



    public string UsePhotonLabel = "Using the Photon Cloud is free for development. If you don't have an account yet, enter your email and register.";
    public string SendButton = "Send";

    public string SignedUpAlreadyLabel = "I am already signed up. Let me enter my AppId.";
    public string SetupButton = "Setup";
    public string RegisterByWebsiteLabel = "I want to register by a website.";
    public string AccountWebsiteButton = "Open account website";
    public string SelfHostLabel = "I want to host my own server. Let me set it up.";
    public string SelfHostSettingsButton = "Open self-hosting settings";
    public string MobileExportNoteLabel = "Build for mobiles impossible. Get PUN+ or Unity Pro for mobile.";
    public string MobilePunPlusExportNoteLabel = "PUN+ available. Using native sockets for iOS/Android.";
    public string EmailInUseLabel = "The provided e-mail-address has already been registered.";
    public string EmailRegisteredError = "registered";
    public string KnownAppIdLabel = "Ah, I know my Application ID. Get me to setup.";
    public string SeeMyAccountLabel = "Mh, see my account page";
    public string SelfHostSettingButton = "Open self-hosting settings";
    public string OopsLabel = "Oops!";
    public string SeeMyAccountPage = "";
    public string CancelButton = "Cancel";
    public string PhotonCloudConnect = "Connect to Photon Cloud";
    public string SetupOwnHostLabel = "Setup own Photon Host";
    public string PUNWizardLabel = "Photon Unity Networking (PUN) Wizard";
    public string SettingsButton = "Settings";
    public string SetupServerCloudLabel = "Setup wizard for setting up your own server or the cloud.";
    public string WarningPhotonDisconnect = "";
    public string ConverterLabel = "Converter";
    public string StartButton = "Start";
    public string UNtoPUNLabel = "Converts pure Unity Networking to Photon Unity Networking.";
    public string SettingsFileLabel = "Settings File";
    public string LocateSettingsButton = "Locate settings asset";
    public string SettingsHighlightLabel = "Highlights the used photon settings file in the project.";
    public string DocumentationLabel = "Documentation";
    public string OpenPDFText = "Open PDF";
    public string OpenPDFTooltip = "Opens the local documentation pdf.";
    public string OpenDevNetText = "Open DevNet";
    public string OpenDevNetTooltip = "Online documentation for Photon.";
    public string OpenCloudDashboardText = "Open Cloud Dashboard";
    public string OpenCloudDashboardTooltip = "Review Cloud App information and statistics.";
    public string OpenForumText = "Open Forum";
    public string OpenForumTooltip = "Online support for Photon.";
    public string QuestionsLabel = "Questions? Need help or want to give us feedback? You are most welcome!";
    public string SeeForumButton = "See the Photon Forum";
    public string OpenDashboardButton = "Open Dashboard (web)";
    public string AppIdLabel = "Your AppId";
    public string AppIdInfoLabel = "The AppId a Guid that identifies your game in the Photon Cloud. Find it on your dashboard page.";
    public string CloudRegionLabel = "Cloud Region";
    public string RegionalServersInfo = "Photon Cloud has regional servers. Picking one near your customers improves ping times. You could use more than one but this setup does not support it.";
    public string SaveButton = "Save";
    public string SettingsSavedTitle = "Success";
    public string SettingsSavedMessage = "Saved your settings.\nConnectUsingSettings() will use the settings file.";
    public string OkButton = "Ok";
    public string SeeMyAccountPageButton = "Mh, see my account page";
    public string SetupOwnServerLabel = "Running my app in the cloud was fun but...\nLet me setup my own Photon server.";
    public string OwnHostCloudCompareLabel = "I am not quite sure how 'my own host' compares to 'cloud'.";
    public string ComparisonPageButton = "See comparison page";
    public string YourPhotonServerLabel = "Your Photon Server";
    public string AddressIPLabel = "Address/ip:";
    public string PortLabel = "Port:";
    public string LicensesLabel = "Licenses";
    public string LicenseDownloadText = "Free License Download";
    public string LicenseDownloadTooltip = "Get your free license for up to 100 concurrent players.";
    public string TryPhotonAppLabel = "Running my own server is too much hassle..\nI want to give Photon's free app a try.";
    public string GetCloudAppButton = "Get the free cloud app";
    public string ConnectionTitle = "Connecting";
    public string ConnectionInfo = "Connecting to the account service..";
    public string ErrorTextTitle = "Error";
    public string ServerSettingsMissingLabel = "Photon Unity Networking (PUN) is missing the 'ServerSettings' script. Re-import PUN to fix this.";
    public string MoreThanOneLabel = "There are more than one ";
    public string FilesInResourceFolderLabel = " files in 'Resources' folder. Check your project to keep only one. Using: ";
    public string IncorrectRPCListTitle = "Warning: RPC-list becoming incompatible!";
    public string IncorrectRPCListLabel = "Your project's RPC-list is full, so we can't add some RPCs just compiled.\n\nBy removing outdated RPCs, the list will be long enough but incompatible with older client builds!\n\nMake sure you change the game version where you use PhotonNetwork.ConnectUsingSettings().";
    public string RemoveOutdatedRPCsLabel = "Remove outdated RPCs";
    public string FullRPCListTitle = "Warning: RPC-list is full!";
    public string FullRPCListLabel = "Your project's RPC-list is too long for PUN.\n\nYou can change PUN's source to use short-typed RPC index. Look for comments 'LIMITS RPC COUNT'\n\nAlternatively, remove some RPC methods (use more parameters per RPC maybe).\n\nAfter a RPC-list refresh, make sure you change the game version where you use PhotonNetwork.ConnectUsingSettings().";
    public string SkipRPCListUpdateLabel = "Skip RPC-list update";
    public string PUNNameReplaceTitle = "Warning: RPC-list Compatibility";
    public string PUNNameReplaceLabel = "PUN replaces RPC names with numbers by using the RPC-list. All clients must use the same list for that.\n\nClearing it most likely makes your client incompatible with previous versions! Change your game version or make sure the RPC-list matches other clients.";
    public string RPCListCleared = "Clear RPC-list";
    public string ServerSettingsCleanedWarning = "Cleared the PhotonServerSettings.RpcList! This makes new builds incompatible with older ones. Better change game version in PhotonNetwork.ConnectUsingSettings().";
    public string BestRegionLabel = "best";
}


[InitializeOnLoad]
public class PhotonEditor : EditorWindow
{
    protected static Type WindowType = typeof(PhotonEditor);

    protected Vector2 scrollPos = Vector2.zero;

    private readonly Vector2 preferredSize = new Vector2(350, 400);

    private static Texture2D WizardIcon;

    public static PunWizardText CurrentLang = new PunWizardText();


    protected static AccountService.Origin RegisterOrigin = AccountService.Origin.Pun;

    protected static string DocumentationLocation = "Assets/Photon Unity Networking/PhotonNetwork-Documentation.pdf";

    protected static string UrlFreeLicense = "https://www.exitgames.com/en/OnPremise/Dashboard";

    protected static string UrlDevNet = "http://doc.exitgames.com/en/pun/current";

    protected static string UrlForum = "http://forum.exitgames.com";

    protected static string UrlCompare = "http://doc.exitgames.com/en/realtime/current/getting-started/onpremise-or-saas";

    protected static string UrlHowToSetup = "http://doc.exitgames.com/en/onpremise/current/getting-started/photon-server-in-5min";

    protected static string UrlAppIDExplained = "http://doc.exitgames.com/en/realtime/current/getting-started/obtain-your-app-id";

    protected static string UrlAccountPage = "https://www.exitgames.com/Account/SignIn?email="; // opened in browser

    protected static string UrlCloudDashboard = "https://www.exitgames.com/Dashboard?email=";


    private enum GUIState
    {
        Uninitialized,

        Main,

        Setup
    }

    private enum PhotonSetupStates
    {
        RegisterForPhotonCloud,

        EmailAlreadyRegistered,

        GoEditPhotonServerSettings
    }

    private GUIState guiState = GUIState.Uninitialized;

    private bool isSetupWizard = false;

    private PhotonSetupStates photonSetupState = PhotonSetupStates.RegisterForPhotonCloud;


    private static double lastWarning = 0;

    private static bool postCompileActionsDone;

    private string mailOrAppId = string.Empty;


    private static bool isPunPlus;
    private static bool androidLibExists;
    private static bool iphoneLibExists;

    static PhotonEditor()
    {
        EditorApplication.projectWindowChanged += EditorUpdate;
        EditorApplication.hierarchyWindowChanged += EditorUpdate;
        EditorApplication.playmodeStateChanged += PlaymodeStateChanged;
        EditorApplication.update += OnUpdate;

        WizardIcon = AssetDatabase.LoadAssetAtPath("Assets/Photon Unity Networking/photoncloud-icon.png", typeof(Texture2D)) as Texture2D;

        // detect optional packages
        PhotonEditor.CheckPunPlus();
    }

    internal protected static bool CheckPunPlus()
    {
        androidLibExists = File.Exists("Assets/Plugins/Android/libPhotonSocketPlugin.so");
        iphoneLibExists = File.Exists("Assets/Plugins/IPhone/libPhotonSocketPlugin.a");

        isPunPlus = androidLibExists || iphoneLibExists;
        return isPunPlus;
    }

    private static void ImportWin8Support()
    {
        if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return; // don't import while compiling
        }

        #if UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5 || UNITY_5_0
        const string win8Package = "Assets/Plugins/Photon3Unity3D-Win8.unitypackage";

        bool win8LibsExist = File.Exists("Assets/Plugins/WP8/Photon3Unity3D.dll") && File.Exists("Assets/Plugins/Metro/Photon3Unity3D.dll");
        if (!win8LibsExist && File.Exists(win8Package))
        {
            AssetDatabase.ImportPackage(win8Package, false);
        }
        #endif
    }

    [MenuItem("Window/Photon Unity Networking/Locate Settings Asset %#&p")]
    protected static void Inspect()
    {
        EditorGUIUtility.PingObject(PhotonNetwork.PhotonServerSettings);
        Selection.activeObject = PhotonNetwork.PhotonServerSettings;
    }

    [MenuItem("Window/Photon Unity Networking/PUN Wizard &p")]
    protected static void Init()
    {
        PhotonEditor win = GetWindow(WindowType, false, CurrentLang.WindowTitle, true) as PhotonEditor;
        win.InitPhotonSetupWindow();

        win.isSetupWizard = false;
        win.SwitchMenuState(GUIState.Main);
    }

    /// <summary>Creates an Editor window, showing the cloud-registration wizard for Photon (entry point to setup PUN).</summary>
    protected static void ShowRegistrationWizard()
    {
        PhotonEditor win = GetWindow(WindowType, false, CurrentLang.WindowTitle, true) as PhotonEditor;
        win.isSetupWizard = true;
        win.InitPhotonSetupWindow();
    }

    /// <summary>Re-initializes the Photon Setup window and shows one of three states: register cloud, setup cloud, setup self-hosted.</summary>
    protected void InitPhotonSetupWindow()
    {
        this.minSize = this.preferredSize;

        this.SwitchMenuState(GUIState.Setup);

        switch (PhotonNetwork.PhotonServerSettings.HostType)
        {
            case ServerSettings.HostingOption.PhotonCloud:
            case ServerSettings.HostingOption.BestRegion:
            default:
                this.photonSetupState = PhotonSetupStates.RegisterForPhotonCloud;
                break;
        }
    }

    // called 100 times / sec
    private static void OnUpdate()
    {
        // after a compile, check RPCs to create a cache-list
        if (!postCompileActionsDone && !EditorApplication.isCompiling && !EditorApplication.isPlayingOrWillChangePlaymode && PhotonNetwork.PhotonServerSettings != null)
        {
            #if UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5 || UNITY_5_0
            if (EditorApplication.isUpdating) return;
            #endif

            PhotonEditor.UpdateRpcList();
            postCompileActionsDone = true;  // on compile, this falls back to false (without actively doing anything)

            #if UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5 || UNITY_5_0
            PhotonEditor.ImportWin8Support();
            #endif
        }
    }

    // called in editor, opens wizard for initial setup, keeps scene PhotonViews up to date and closes connections when compiling (to avoid issues)
    private static void EditorUpdate()
    {
        if (PhotonNetwork.PhotonServerSettings == null)
        {
            PhotonNetwork.CreateSettings();
        }
        if (PhotonNetwork.PhotonServerSettings == null)
        {
            return;
        }

        // serverSetting is null when the file gets deleted. otherwise, the wizard should only run once and only if hosting option is not (yet) set
        if (!PhotonNetwork.PhotonServerSettings.DisableAutoOpenWizard && PhotonNetwork.PhotonServerSettings.HostType == ServerSettings.HostingOption.NotSet)
        {
            ShowRegistrationWizard();
            PhotonNetwork.PhotonServerSettings.DisableAutoOpenWizard = true;
            Save();
        }

        // Workaround for TCP crash. Plus this surpresses any other recompile errors.
        if (EditorApplication.isCompiling)
        {
            if (PhotonNetwork.connected)
            {
                if (lastWarning > EditorApplication.timeSinceStartup - 3)
                {
                    // Prevent error spam
                    Debug.LogWarning(CurrentLang.WarningPhotonDisconnect);
                    lastWarning = EditorApplication.timeSinceStartup;
                }

                PhotonNetwork.Disconnect();
            }
        }
    }

    // called in editor on change of play-mode (used to show a message popup that connection settings are incomplete)
    private static void PlaymodeStateChanged()
    {
        if (EditorApplication.isPlaying || !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        if (PhotonNetwork.PhotonServerSettings.HostType == ServerSettings.HostingOption.NotSet)
        {
            EditorUtility.DisplayDialog(CurrentLang.SetupWizardWarningTitle, CurrentLang.SetupWizardWarningMessage, CurrentLang.OkButton);
        }
    }

    private void SwitchMenuState(GUIState newState)
    {
        this.guiState = newState;
        if (this.isSetupWizard && newState != GUIState.Setup)
        {
            this.Close();
        }
    }

    protected virtual void OnGUI()
    {
        PhotonSetupStates oldGuiState = this.photonSetupState;  // used to fix an annoying Editor input field issue: wont refresh until focus is changed.

        GUI.SetNextControlName("");
        this.scrollPos = GUILayout.BeginScrollView(this.scrollPos);

        if (this.guiState == GUIState.Uninitialized)
        {
            this.guiState = (PhotonNetwork.PhotonServerSettings.HostType == ServerSettings.HostingOption.NotSet) ? GUIState.Setup : GUIState.Main;
        }

        if (this.guiState == GUIState.Main)
        {
            this.OnGuiMainWizard();
        }
        else
        {
            this.OnGuiRegisterCloudApp();
        }

        GUILayout.EndScrollView();

        if (oldGuiState != this.photonSetupState)
        {
            GUI.FocusControl("");
        }
    }


    private bool minimumInput = false;
    private bool useMail = false;
    private bool useAppId = false;
    private bool useSkip = false;
    private bool highlightedSettings = false;
    private bool close = false;


    public void Update()
    {
        if (this.close)
        {
            this.Close();
        }
    }

    protected virtual void OnGuiRegisterCloudApp()
    {
        GUI.skin.label.wordWrap = true;
        if (!this.isSetupWizard)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(CurrentLang.MainMenuButton, GUILayout.ExpandWidth(false)))
            {
                this.SwitchMenuState(GUIState.Main);
            }

            GUILayout.EndHorizontal();
        }

        GUILayout.Space(15);

        GUI.skin.label.fontStyle = FontStyle.Bold;
        GUILayout.Label(CurrentLang.SetupWizardTitle);
        EditorGUILayout.Separator();
        GUI.skin.label.fontStyle = FontStyle.Normal;

        GUI.skin.label.richText = true;

        GUILayout.Label(CurrentLang.SetupWizardInfo);



        EditorGUILayout.Separator();
        GUILayout.Label(CurrentLang.EmailOrAppIdLabel);
        this.mailOrAppId = EditorGUILayout.TextField(this.mailOrAppId).Trim();  // note: we trim all input

        if (mailOrAppId.Contains("@"))
        {
            // this should be a mail address
            this.minimumInput = (mailOrAppId.Length >= 5 && mailOrAppId.Contains("."));
            this.useMail = minimumInput;
            this.useAppId = false;
        }
        else
        {
            // this should be an appId
            this.minimumInput = ServerSettingsInspector.IsAppId(mailOrAppId);
            this.useMail = false;
            this.useAppId = minimumInput;
        }

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(CurrentLang.SkipButton, GUILayout.Width(100)))
        {
            this.photonSetupState = PhotonSetupStates.GoEditPhotonServerSettings;
            this.useSkip = true;
            this.useMail = false;
            this.useAppId = false;
        }
        // SETUP button
        EditorGUI.BeginDisabledGroup(!minimumInput);
        if (GUILayout.Button(CurrentLang.SetupButton, GUILayout.Width(100)))
        {
            this.useSkip = false;
            GUIUtility.keyboardControl = 0;
            if (useMail)
            {
                this.RegisterWithEmail(this.mailOrAppId);   // sets state
            }
            if (useAppId)
            {
                this.photonSetupState = PhotonSetupStates.GoEditPhotonServerSettings;
                PhotonNetwork.PhotonServerSettings.UseCloud(this.mailOrAppId);
                PhotonEditor.Save();
            }
        }
        EditorGUI.EndDisabledGroup();
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();


        // existing account needs to fetch AppId online
        if (this.photonSetupState == PhotonSetupStates.EmailAlreadyRegistered)
        {
            // button to open dashboard and get the AppId
            GUILayout.Space(15);
            GUILayout.Label("The email is registered so we can't fetch your AppId (without password).\n\nPlease login online to get your AppId.");


            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(new GUIContent(CurrentLang.OpenCloudDashboardText, CurrentLang.OpenCloudDashboardTooltip), GUILayout.Width(205)))
            {
                EditorUtility.OpenWithDefaultApp(UrlCloudDashboard + Uri.EscapeUriString(this.mailOrAppId));
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }



        if (this.photonSetupState == PhotonSetupStates.GoEditPhotonServerSettings)
        {
            if (!highlightedSettings)
            {
                highlightedSettings = true;
                HighlightSettings();
            }

            GUILayout.Space(15);
            if (useSkip)
            {
                GUILayout.Label("Skipping? No problem:\nEdit your server settings in the PhotonServerSettings file.");
            }
            else if (useMail)
            {
                GUILayout.Label("We created a (free) account and fetched you an AppId.\nWelcome. Your PUN project is setup.");
            }
            else if (useAppId)
            {
                GUILayout.Label("Your AppId is now applied to this project.");
            }


            GUILayout.Space(15);
            GUILayout.Label("<b>Done!</b>\nAll connection settings can be edited in the <b>PhotonServerSettings</b> now.\nHave a look.");


            // find / select settings asset
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", GUILayout.Width(205)))
            {
                this.close = true;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

        }
        GUI.skin.label.richText = false;
    }

    private static void HighlightSettings()
    {
        Selection.objects = new UnityEngine.Object[] {PhotonNetwork.PhotonServerSettings};
        EditorGUIUtility.PingObject(PhotonNetwork.PhotonServerSettings);
    }

    protected virtual void OnGuiMainWizard()
    {

        GUILayout.BeginVertical();
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label(WizardIcon);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        EditorGUILayout.Separator();


        GUILayout.Label(CurrentLang.PUNWizardLabel, EditorStyles.boldLabel);
        if (isPunPlus)
        {
            GUILayout.Label(CurrentLang.MobilePunPlusExportNoteLabel);
        }
        else if (!InternalEditorUtility.HasAdvancedLicenseOnBuildTarget(BuildTarget.Android) || !InternalEditorUtility.HasAdvancedLicenseOnBuildTarget(BuildTarget.iOS))
        {
            GUILayout.Label(CurrentLang.MobileExportNoteLabel);
        }
        EditorGUILayout.Separator();


        // settings button
        GUILayout.BeginHorizontal();
        GUILayout.Label(CurrentLang.SettingsButton, EditorStyles.boldLabel, GUILayout.Width(100));
        if (GUILayout.Button(new GUIContent(CurrentLang.SetupButton, CurrentLang.SetupServerCloudLabel)))
        {
            this.InitPhotonSetupWindow();
        }

        GUILayout.EndHorizontal();
        EditorGUILayout.Separator();


        // find / select settings asset
        GUILayout.BeginHorizontal();
        GUILayout.Label(CurrentLang.SettingsFileLabel, EditorStyles.boldLabel, GUILayout.Width(100));
        if (GUILayout.Button(new GUIContent(CurrentLang.LocateSettingsButton, CurrentLang.SettingsHighlightLabel)))
        {
            EditorGUIUtility.PingObject(PhotonNetwork.PhotonServerSettings);
        }
        GUILayout.EndHorizontal();


        GUILayout.FlexibleSpace();

        // converter
        GUILayout.BeginHorizontal();
        GUILayout.Label(CurrentLang.ConverterLabel, EditorStyles.boldLabel, GUILayout.Width(100));
        if (GUILayout.Button(new GUIContent(CurrentLang.StartButton, CurrentLang.UNtoPUNLabel)))
        {
            PhotonConverter.RunConversion();
        }

        GUILayout.EndHorizontal();
        EditorGUILayout.Separator();


        // documentation
        GUILayout.BeginHorizontal();
        GUILayout.Label(CurrentLang.DocumentationLabel, EditorStyles.boldLabel, GUILayout.Width(100));
        GUILayout.BeginVertical();
        if (GUILayout.Button(new GUIContent(CurrentLang.OpenPDFText, CurrentLang.OpenPDFTooltip)))
        {
            EditorUtility.OpenWithDefaultApp(DocumentationLocation);
        }

        if (GUILayout.Button(new GUIContent(CurrentLang.OpenDevNetText, CurrentLang.OpenDevNetTooltip)))
        {
            EditorUtility.OpenWithDefaultApp(UrlDevNet);
        }

        GUI.skin.label.wordWrap = true;
        GUILayout.Label(CurrentLang.OwnHostCloudCompareLabel);
        if (GUILayout.Button(CurrentLang.ComparisonPageButton))
        {
            Application.OpenURL(UrlCompare);
        }

        if (GUILayout.Button(new GUIContent(CurrentLang.OpenCloudDashboardText, CurrentLang.OpenCloudDashboardTooltip)))
        {
            EditorUtility.OpenWithDefaultApp(UrlCloudDashboard + Uri.EscapeUriString(this.mailOrAppId));
        }

        if (GUILayout.Button(new GUIContent(CurrentLang.OpenForumText, CurrentLang.OpenForumTooltip)))
        {
            EditorUtility.OpenWithDefaultApp(UrlForum);
        }

        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
    }


    [Obsolete]
    protected virtual void OnGuiCompareAndHelpOptions()
    {
        GUILayout.FlexibleSpace();

        GUILayout.Label(CurrentLang.QuestionsLabel);
        if (GUILayout.Button(CurrentLang.SeeForumButton))
        {
            Application.OpenURL(UrlForum);
        }

        if (GUILayout.Button(CurrentLang.OpenDashboardButton))
        {
            EditorUtility.OpenWithDefaultApp(UrlCloudDashboard + Uri.EscapeUriString(this.mailOrAppId));
        }
    }


    protected virtual void RegisterWithEmail(string email)
    {
        EditorUtility.DisplayProgressBar(CurrentLang.ConnectionTitle, CurrentLang.ConnectionInfo, 0.5f);
        var client = new AccountService();
        client.RegisterByEmail(email, RegisterOrigin); // this is the synchronous variant using the static RegisterOrigin. "result" is in the client

        EditorUtility.ClearProgressBar();
        if (client.ReturnCode == 0)
        {
            PhotonNetwork.PhotonServerSettings.UseCloud(client.AppId, 0);
            PhotonEditor.Save();
            this.mailOrAppId = client.AppId;
            this.photonSetupState = PhotonSetupStates.GoEditPhotonServerSettings;
        }
        else
        {
            Debug.LogWarning(client.Message);
            if (client.Message.Contains("registered"))
            {
                PhotonNetwork.PhotonServerSettings.UseCloud("");
                PhotonEditor.Save();
                this.photonSetupState = PhotonSetupStates.EmailAlreadyRegistered;
            }
            else
            {
                EditorUtility.DisplayDialog(CurrentLang.ErrorTextTitle, client.Message, CurrentLang.OkButton);
                PhotonNetwork.PhotonServerSettings.UseCloud("");
                PhotonEditor.Save();
                this.photonSetupState = PhotonSetupStates.RegisterForPhotonCloud;
            }
        }
    }

    #region SettingsFileHandling




    public static void Save()
    {
        EditorUtility.SetDirty(PhotonNetwork.PhotonServerSettings);
    }


    public static void UpdateRpcList()
    {
        List<string> additionalRpcs = new List<string>();
        HashSet<string> currentRpcs = new HashSet<string>();

        var types = GetAllSubTypesInScripts(typeof(MonoBehaviour));

        foreach (var mono in types)
        {
            MethodInfo[] methods = mono.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (MethodInfo method in methods)
            {
                if (method.IsDefined(typeof(UnityEngine.RPC), false))
                {
                    currentRpcs.Add(method.Name);

                    if (!additionalRpcs.Contains(method.Name) && !PhotonNetwork.PhotonServerSettings.RpcList.Contains(method.Name))
                    {
                        additionalRpcs.Add(method.Name);
                    }
                }
            }
        }

        if (additionalRpcs.Count > 0)
        {
            // LIMITS RPC COUNT
            if (additionalRpcs.Count + PhotonNetwork.PhotonServerSettings.RpcList.Count >= byte.MaxValue)
            {
                if (currentRpcs.Count <= byte.MaxValue)
                {
                    bool clearList = EditorUtility.DisplayDialog(CurrentLang.IncorrectRPCListTitle, CurrentLang.IncorrectRPCListLabel, CurrentLang.RemoveOutdatedRPCsLabel, CurrentLang.CancelButton);
                    if (clearList)
                    {
                        PhotonNetwork.PhotonServerSettings.RpcList.Clear();
                        PhotonNetwork.PhotonServerSettings.RpcList.AddRange(currentRpcs);
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog(CurrentLang.FullRPCListTitle, CurrentLang.FullRPCListLabel, CurrentLang.SkipRPCListUpdateLabel);
                    return;
                }
            }

            additionalRpcs.Sort();
            PhotonNetwork.PhotonServerSettings.RpcList.AddRange(additionalRpcs);
            EditorUtility.SetDirty(PhotonNetwork.PhotonServerSettings);
        }
    }

    public static void ClearRpcList()
    {
        bool clearList = EditorUtility.DisplayDialog(CurrentLang.PUNNameReplaceTitle, CurrentLang.PUNNameReplaceLabel, CurrentLang.RPCListCleared, CurrentLang.CancelButton);
        if (clearList)
        {
            PhotonNetwork.PhotonServerSettings.RpcList.Clear();
            Debug.LogWarning(CurrentLang.ServerSettingsCleanedWarning);
        }
    }

    public static System.Type[] GetAllSubTypesInScripts(System.Type aBaseClass)
    {
        var result = new System.Collections.Generic.List<System.Type>();
        System.Reflection.Assembly[] AS = System.AppDomain.CurrentDomain.GetAssemblies();
        foreach (var A in AS)
        {
            // this skips all but the Unity-scripted assemblies for RPC-list creation. You could remove this to search all assemblies in project
            if (!A.FullName.StartsWith("Assembly-"))
            {
                // Debug.Log("Skipping Assembly: " + A);
                continue;
            }

            //Debug.Log("Assembly: " + A.FullName);
            System.Type[] types = A.GetTypes();
            foreach (var T in types)
            {
                if (T.IsSubclassOf(aBaseClass))
                    result.Add(T);
            }
        }
        return result.ToArray();
    }

    #endregion
}
