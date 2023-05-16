# Google Play Games Plugin Integration

| Supported Platforms |
| :-----------------: |
| Android             |

[Download & Install Plugin](https://github.com/playgameservices/play-games-plugin-for-unity/releases/)

[Follow instructions for proper integration](https://github.com/playgameservices/play-games-plugin-for-unity#configure-your-game)

### External Dependency Manager

This Unity example project by default includes Google's **External Dependency Manager** plugin to resolve dependencies on Android and iOS. When importing the Play Games plugin into the project, do not import `ExternalDependencyManager` or any files within that folder as it will conflict with the already included plugin.

## Set-Up

Once the Google Play Games plugin is integrated into this example project, add `GOOGLE_SDK` to your **Scripting Define Symbols** under `Project Settings > Player > Other Settings`.

Configure `Window > Google Play Games > Setup > Android Setup` as instructed above for proper use of the plugin.

You will need to create a project on [Google Cloud](https://console.developers.google.com/) and set up OAuth 2.0 Client Credentials for both Android and Web to obtain your **Client ID** and **Web Client ID**, respectively. You will also need to enable **Google Play Game Services** in your Google Cloud project under [APIs](https://console.developers.google.com/apis), and configure an Android project on the [Google Play developer console](https://play.google.com/console/).

### brainCloud Configuration

1. In the [brainCloud server portal](https://portal.braincloudservers.com/) for your app, navigate to `Design > Core App Info > Application IDs`
2. Click **Configure Google**
3. Fill out the following required fields using the information in your Google Cloud project for your app:
    - Google App ID
    - Google Client ID
    - Google Client Secret

---

#### Read More

- [brainCloud Portal Tutorial – Authentication - Google (PlayGame)](https://getbraincloud.com/apidocs/portal-usage/authentication-google-playgame/)
- [Games Dev Center – Google Play Games plugin for Unity](https://developer.android.com/games/pgs/unity/overview)
- [Google Identity – Authentication](https://developers.google.com/identity/sign-in/)
- [Google Identity – Google Sign-In for Android (legacy)](https://developers.google.com/identity/sign-in/android/start-integrating#configure_a_project)
- [Google Identity – Sign In with Google for Web](https://developers.google.com/identity/gsi/web/guides/get-google-api-clientid)
- [Unity Documentation – Set up a Google Play Games sign-in](https://docs.unity.com/authentication/en/manual/set-up-google-play-games-signin)
