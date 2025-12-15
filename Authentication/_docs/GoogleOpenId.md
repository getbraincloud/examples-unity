# Google Sign-In Plugin Integration

| Supported Platforms |
| :-----------------: |
| Android             |
| iOS                 |

- [Download & Install Plugin](https://github.com/googlesamples/google-signin-unity/releases)
- [Follow instructions for proper integration](https://github.com/Thaina/google-signin-unity?tab=readme-ov-file#forked-to-upgrade-base-library-to-newer-version)

### External Dependency Manager

This Unity example project by default includes Google's **External Dependency Manager** plugin to resolve dependencies on Android and iOS. When importing the Google Sign-In plugin into the project, do not import the `Parse` or the `PlayServicesResolver` folders or any files within those folders as it will conflict with the already included plugin. If you do, you can safely delete them.

## Set-Up

To add the Google Sign-In plugin to your project, we have to use the forked branch of Google Sign-In Unity Plugin that fixes several issues for Android and iOS. Add it to your project via the Package Manager using this Git URL:
- `https://github.com/Thaina/google-signin-unity.git#newmigration`

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

---

#### Read More

- [brainCloud Portal Tutorial – Authentication - Google (OpenID)](https://docs.braincloudservers.com/learn/portal-tutorials/authentication-google-openid/)
- [Google Identity – Authentication](https://developers.google.com/identity/sign-in/)
- [Google Identity – Google Sign-In for Android (legacy)](https://developers.google.com/identity/sign-in/android/start-integrating)
- [Google Identity – Google Sign-In for iOS and macOS](https://developers.google.com/identity/sign-in/ios/start-integrating)
- [Google Identity – Sign In with Google for Web](https://developers.google.com/identity/gsi/web/guides/get-google-api-clientid)
