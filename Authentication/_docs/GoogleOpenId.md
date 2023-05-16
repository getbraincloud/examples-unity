# Google Sign-In Plugin Integration
| Supported Platforms |
| :-----------------: |
| Android \| iOS      |

- [Download & Install Plugin](https://github.com/googlesamples/google-signin-unity/releases)
- [Follow instructions for proper integration](https://github.com/googlesamples/google-signin-unity#configuring-the-application--on-the-api-console)

### External Dependency Manager
This Unity example project by default includes Google's **External Dependency Manager** plugin to resolve dependencies on Android and iOS. When importing the Google Sign-In plugin into the project, do not import the `Parse` or the `PlayServicesResolver` folders or any files within those folders as it will conflict with the already included plugin. If you do, you can safely delete them.

## Set-Up
Once the Google Sign-In plugin is integrated into this example project, add `GOOGLE_OPENID_SDK` to your **Scripting Define Symbols** under `Project Settings > Player > Other Settings`.

You will need to create a project on [Google Cloud](https://console.developers.google.com/) and set up OAuth 2.0 Client Credentials to obtain your **Web Client ID**. Both Android and iOS will need their own OAuth 2.0 Client Credentials to obtain **Client IDs** for both platforms. For Android, you will also need to configure a project on the [Google Play developer console](https://play.google.com/console/).

In `UserHandler.AuthenticateGoogleOpenId()`, you can set up `GoogleSignInConfiguration` as it relates to your app. The fields that are already included are set to the required values and you must set the `WebClientID` to the one you obtained on Google Cloud.

### brainCloud Configuration
1. In the [brainCloud server portal](https://portal.braincloudservers.com/) for your app, navigate to `Design > Core App Info > Application IDs`
2. Click **Configure Google**
3. Fill out the following required fields using the information in your Google Cloud project for your app:
    - Google App ID
    - Google Client ID
    - Google Client Secret

### Building for iOS
The Google Sign-In plugin technically still supports iOS, but it requires some extra overhead:
- Before building, open `Assets > External Dependency Manager > iOS Resolver > Settings` and ensure `Link frameworks statically` is enabled
- In the `Build Settings` menu, `Development Build` will need to be disabled
- Open `GoogleSignInDependencies.xml`; the `iosPods` property for `iosPod name ="GoogleSignIn"` will need to be edited to the below:
```
<iosPod name="GoogleSignIn" version="&#60; 5.0.0" bitcodeEnabled="true"
```
- [GoogleService-Info.plist](./GoogleOpenId.md#googleservice-infoplist-template) is a template to create a `GoogleService-Info.plist` file to add to your Xcode project
    - Fill out the properties with your app's credentials from the Google Cloud project
    - When importing this into Xcode, make sure that `Target Membership` is enabled for `Unity-iPhone`
- You will also need to add the [URL types](./GoogleOpenId.md#url-types-property) property to your `Info.plist`
    - Copy your `REVERSED_CLIENT_ID` from `GoogleService-Info.plist`
- Finally, the `Pods` project in Xcode will need `Build Settings > Build Options > Enable Bitcode` set to `Yes`

#### GoogleService-Info.plist Template
```
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
	<key>PLIST_VERSION</key>
	<string>1</string>
	<key>BUNDLE_ID</key>
	<string>$(PRODUCT_BUNDLE_IDENTIFIER)</string>
    <!-- The following properties can be obtained from your Google Cloud project's OAuth credentials -->
	<key>CLIENT_ID</key>
	<string>YOUR_CLIENT_ID_HERE</string>
	<key>REVERSED_CLIENT_ID</key>
	<string>YOUR_REVERSED_CLIENT_ID_HERE</string>
	<key>GIDServerClientID</key>
	<string>YOUR_WEB_CLIENT_ID_HERE</string>
</dict>
</plist>
```

#### URL types Property
```
<key>CFBundleURLTypes</key>
<array>
    <dict>
        <key>CFBundleTypeRole</key>
        <string>Editor</string>
        <key>CFBundleURLSchemes</key>
        <array>
            <!-- Copy your REVERSED_CLIENT_ID key from GoogleService-Info.plist -->
            <string>YOUR_REVERSED_CLIENT_ID_HERE</string>
        </array>
    </dict>
</array>
```

---

#### Read More
- [brainCloud Portal Tutorial – Authentication - Google (OpenID)](https://getbraincloud.com/apidocs/portal-usage/authentication-google-openid/)
- [Google Identity – Authentication](https://developers.google.com/identity/sign-in/)
- [Google Identity – Google Sign-In for Android (legacy)](https://developers.google.com/identity/sign-in/android/start-integrating)
- [Google Identity – Google Sign-In for iOS and macOS](https://developers.google.com/identity/sign-in/ios/start-integrating)
- [Google Identity – Sign In with Google for Web](https://developers.google.com/identity/gsi/web/guides/get-google-api-clientid)
