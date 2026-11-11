# Epic Online Service AuthInterface Integration

| Supported Platforms |
| :-----------------: |
| Windows             |

- [Plugin GitHub](https://github.com/EOS-Contrib/eos_plugin_for_unity)

## Set-Up

You can add the EOS Plugin for Unity by adding the following git URL to your Package Manager:
- https://github.com/EOS-Contrib/eos_plugin_for_unity_upm.git

Once the the EOS plugin is integrated into this example project, add `EPICGAMES_SDK` to your **Scripting Define Symbols** under `Project Settings > Player > Other Settings`.

You will need to fill out most of your EOS Configuration file under `EOS Plugin > EOS Configuration` in order to make use of the EOS Auth Interface in this app. Make sure to set up a Product and its Product Settings in order to retrieve the credentials required in the Epic Games Developer Portal.

For testing in the Editor, you will need to download the [EOS Authentication Tool](https://dev.epicgames.com/docs/epic-online-services/accounts-and-social/eos-epic-account-services/developer-authentication-tool) and keep it running in the background. Then, in the **Main** scene in Unity, the **brainCloud** object's **BCManager** component will have the following fields for you to fill out:
- Dev Auth Tool Host Name
- Dev Auth Tool Credential Name

### brainCloud Configuration

1. In the [brainCloud server portal](https://portal.braincloudservers.com/) for your app, navigate to `App > Design > Integrations > Platforms`
2. Click the **Epic Games** tab
3. Fill out the **Epic Games Client ID** field using the Client ID attached to your Product under the Product Settings in the Epic Games Developer Portal.

The Epic Games Client ID will be the same one as the Client ID in your EOS Unity Configuration file (`EOS Plugin > EOS Configuration` and under Deployment Configuration).

---

If all of the credentials are filled out properly in your EOS Configuration file and in your brainCloud app, you will be able to log into the app using an Epic Games account through the Auth Interface. You can test this easily in the Editor as long as the EOS Authentication Tool is running and you have the Host Name and and Credential Name filled out on the BCManager component.

For testing outside of the Unity Editor, you will need to upload the app to the Product's page on the Epic Games Developer Portal as an Artifact using the **BuildPatchTool**. This should be included in the SDK you downloaded with the EOS Authentication Tool. Then you can download the app using the Epic Games Launcher and you will have to test from there.

---

#### Read More

- [Epic Online Services (EOS) Overview](https://dev.epicgames.com/docs/epic-online-services)
- [EOS Unity Integration](https://dev.epicgames.com/docs/epic-online-services/game-engines/eos-unity-integration)
- [EOS How-To Guides](https://dev.epicgames.com/docs/epic-online-services/how-to-guides)
- [Auth Interface Reference](https://dev.epicgames.com/docs/epic-online-services/accounts-and-social/eos-epic-account-services/auth-interface/auth-reference)
