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

On the [Epic Games Developer Portal](https://dev.epicgames.com/portal), set up a **Product** with the **Epic Games Store** and **Epic Online Services** services enabled.

The IDs needed for Unity and brainCloud are under `Product > Product Settings`, in the **EOS SDK Credentials** section of the **SDK Download & Credentials** tab.

### Clients

On the **Clients** tab, create a client policy and a client for each of:

- **GameClient**: used by the Unity build. Its policy must allow **Ecom**.
- **TrustedServer**: used by brainCloud to call Epic's Ecom APIs. Never ship it in a build.

### Epic Account Services

Create an **Epic Account Services** application and link your **GameClient** client to it. The Auth Interface can't log in without this.

### EGS Offers

Under `Epic Games Store > Offers`, create offers matching your brainCloud `App > Marketplace > Cash Products`.

- `AppStore.VerifyPurchase` only supports **Consumable** items. Buying one grants an Entitlement. Durable items and Ownership checks aren't supported.
- Copy each offer's **Audience Item ID** into the `epicGames` configuration of the matching Cash Product.
- For testing, set your offers to **Free**.

## brainCloud Configuration

1. In the [brainCloud portal](https://portal.braincloudservers.com/), go to `App > Design > Integrations > Platforms` and open the **Epic Games** tab.
2. For the Auth and Connect Interfaces, fill in:
    - **Epic Games Client ID**: your **GameClient** client, not the TrustedServer one
    - **Sandbox ID**
    - **Deployment ID**: must belong to that sandbox

    These match the values in `EOS Plugin > EOS Configuration` in Unity.
3. For `AppStore.VerifyPurchase`, also fill in **Trusted Server Client ID** and **Trusted Server Client Secret** from your **TrustedServer** client.

## Unity Project Setup

After the plugin is installed under `EOS Plugin > EOS Configuration`, enter your Product ID, Sandbox ID, Deployment ID, and **GameClient** Client ID and Secret for your EOS app.

### Testing in the Editor

The Auth and Connect Interfaces can be tested in the Editor using the [Developer Authentication Tool](https://dev.epicgames.com/docs/epic-online-services/accounts-and-social/eos-epic-account-services/developer-authentication-tool), found in the EOS SDK under `Tools/`:

1. Run the tool, pick a port, log in, and save a credential name. Keep it running.
2. In the **Main** scene, fill in the **EOSHandler** object's **EOS Handler** component:
    - **Dev Auth Tool Host Name**: `localhost:<port>`
    - **Dev Auth Tool Credential Name**: the credential you saved

The Ecom Interface can't be tested in the Editor.

### Testing through the Epic Games Launcher

The Ecom Interface needs the EOS overlay, so it only works in a Windows build launched from the Epic Games Launcher.

1. Make a Windows build and check it contains `EOSBootstrapper.exe`.
2. Upload it as an **Artifact** with **BuildPatchTool**, downloaded from the Artifacts page on the Developer Portal. Set the launch executable to `EOSBootstrapper.exe`, and use a new **Build Version** for each upload.
3. Set the uploaded binary to active.
4. Generate an **Access Key** for the product and redeem it on your Epic account.
5. Install and launch the app from the Epic Games Launcher.

To test a new build without re-uploading, copy it over the installed files and launch from the Epic Games Launcher again. Two cautions:

- Don't change the **Product Name** in Unity. It renames the `.exe`, and `EOSBootstrapper.exe` will keep launching the old one. Re-upload if you do.
- Don't run **Verify** in the launcher. It restores the uploaded build.

---

If there are any questions or issues feel free to ask them on the [Issues](https://github.com/getbraincloud/examples-unity/issues) page.

For more information on brainCloud and its services, please check out [brainCloud Learn](https://docs.braincloudservers.com/learn/introduction/) and [API Reference](https://docs.braincloudservers.com/api/introduction).
