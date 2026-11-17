# Epic Online Services Auth & Store Integration

This is a basic authentication & marketplace showcase for integrating Epic Online Services plugin into your [brainCloud](https://getbraincloud.com/) app.

To download the EOS plugin used in this project for your own Unity project, you can add this to your project's Package Manager as a git URL:
- https://github.com/EOS-Contrib/eos_plugin_for_unity_upm.git

### Navigation
- [Epic Online Services Configuration](#epic-online-services-configuration)
- [EGS Offers](#egs-offers)
- [brainCloud Configuration](#braincloud-configuration)
- [Unity Project Set Up](#unity-project-set-up)

## Epic Online Services Configuration

On the [Epic Games Developer Portal](https://dev.epicgames.com/portal), you will need to set up a **Product**. Your Product will need to enable the **Epic Games Store** service and the **Epic Online Servives** service.

Once those are set up you can retrieve the various IDs you need for the Unity project and brainCloud app from `Product > Product Settings`.

From the **Product Settings** page, if you navigate to **Clients** tab, you can set up a **GameClient** client policy and **TrustedService** policy and create clients, each with the respective policy, in order to generate the Client IDs and Secrets further required for your Unity project and brainCloud app.

### EGS Offers

Under the **Epic Games Store** service on the **Offers** page you can create the offers that are tied to the `App > Marketplace > Cash Products` on brainCloud.

Do note that brainCloud only supports **Consumable** products for the `AppStore.VerifyPurchase` API call! These are called Entitlements on the EGS side. When you create the Entitlement, you will need to copy the **Audience Item ID** to the `epicGames` configuration for your Cash Product on brainCloud `App > Marketplace > Cash Products`.

For testing purposes, we recommend you setting your Consumable Offers to Free until you need to properly integrate your Product's Offers.

Note: brainCloud currently only supports EGS Entitlements and not the Ownerships.

## brainCloud Configuration

1. In the [brainCloud server portal](https://portal.braincloudservers.com/) for your app, navigate to `App > Design > Integrations > Platforms`
2. Click the **Epic Games** tab
3. At bare minimum, for the Auth AND Connect Interface to work at creating new accounts on brainCloud, you will need to fill out these fields (these values will be the same as the ones in your EOS Unity Configuration file (`EOS Plugin > EOS Configuration` and under Deployment Configuration)):
    - Epic Games Client ID
    - Sandbox ID
    - Deployment ID
4. For the `AppStore.VerifyPurchase` API call to work you will also need to fill out the **Trusted Server Client ID** and **Trusted Server Client Secret** fields as well
    - This will be the **TrustedServer** client you would have set up for your Product on the Epic Games Developer portal

## Unity Project Set Up

After you've set up your Product and various Product Settings on the Epic Games Developer Portal, you will need to fill out most of your EOS Configuration file under `EOS Plugin > EOS Configuration` in order to make use of the EOS Auth, Connect, and Ecom Interfaces in this app.

For testing in the Editor, you will need to download the [EOS Authentication Tool](https://dev.epicgames.com/docs/epic-online-services/accounts-and-social/eos-epic-account-services/developer-authentication-tool) and keep it running in the background. Then, in the **Main** scene in Unity, the **EOSHandler** object's **EOS Handler** component will have the following fields for you to fill out:
- Dev Auth Tool Host Name
- Dev Auth Tool Credential Name

These fields must be filled out in order to test the Auth and Connect interfaces from within the Editor.

For testing outside of the Unity Editor, which is required for the Ecom Interface, you will need to upload the app to the Product's page on the Epic Games Developer Portal as an Artifact using the **BuildPatchTool**. This should be included in the SDK you downloaded with the EOS Authentication Tool. Then you can download the app using the Epic Games Launcher and you will have to test from there. For further testing, you can simply copy over your new Windows build over the files that the Epic Games Launcher installed, replacing all files. Then launching the app through the Epic Games Launcher should work.

---

If there are any questions or issues feel free to ask them on the [Issues](https://github.com/getbraincloud/examples-unity/issues) page.

For more information on brainCloud and its services, please check out [brainCloud Learn](https://docs.braincloudservers.com/learn/introduction/) and [API Reference](https://docs.braincloudservers.com/api/introduction).
