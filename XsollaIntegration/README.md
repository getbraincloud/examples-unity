# Xsolla Web Store Integration

This is a basic marketplace showcase for integrating the Xsolla Mobile SDK into your [brainCloud](https://getbraincloud.com/) app as a Web Store integration.

To download the SDK used in this project for your own Unity project, you can add this to your project's Package Manager as a git URL:
- https://github.com/xsolla/xsolla-sdk-unity.git
- This should additionally add the Unity IAP (4.13.0+) package to your project as well

### Navigation
- [Xsolla Configuration](#xsolla-configuration)
- [brainCloud Configuration](#braincloud-configuration)
- [Bonus Rewards](#bonus-rewards)
- [Unity Project Set Up](#unity-project-set-up)

## Xsolla Configuration

1. Under your **Merchant** account you will need to set up a **Project**
2. You will then need to set-up a **Login project** for your Project
    - For testing within the Unity Editor you can set up a **Passwordless login** and enable **Login with device ID**
3. You will need to set up two different **OAuth 2.0 authentication**; this will give you the Client ID and Secret required for your configs:
    - Public (for your Unity project)
    - Server (for your brainCloud app)
4. Under your **Project Settings** you will also need to go to **Integration settings** and ensure **Use webhooks** is enabled
5. Go to **Webhooks** next and do the following:
    1. Add a Secret key
        - Name it `brainCloud` to to indicate it's being used by your brainCloud app
        - Copy your Secret
            - You can only do this once; if you lose this secret you'll need to delete this and generate a new one
    2. Under **Payments Advanced settings** enable the following (***REQUIRED*** or else brainCloud can't verify purchases):
        - Send only necessary user parameters without sensitive data
        - Send custom parameters
        - Show info about the refund reason
        - Show country WHT and user acquisition fees
    3. You'll receive the **Webhook server** URL when you go to configure Xsolla on your brainCloud app (see below); once you do make sure to fill that out too
6. Finally go to **API keys** and click **Create API key +**; give it an appropriate name (`braincloud-server-key`) and copy the Secret
    - Again this can only be done once here so if you lose the secret you will need to generate a new one

## brainCloud Configuration

1. In the [brainCloud server portal](https://portal.braincloudservers.com/) for your app, navigate to `App > Design > Integrations > Payments`
2. Click the **Xsolla** config
3. Fill out the following required fields:
    - Project ID
    - Project Secret Key (Webhooks)
    - Server OAuth 2.0 Client ID
    - Server OAuth 2.0 Client Secret
4. **Login Project ID** is an optional field that is required for the `AppStore.GetUserToken` API call
5. **Project API Key** is also optional but required for the server to consume the purchase instead of relying on your client app

This config is also where you'll receive the **Webhook URL** to set on your Xsolla **Webhook server** URL mentioned above.

Finally you can also set the app's behavior for the Refund webhook mechanism.

### Bonus Rewards

For `App > Design > Marketplace > Cash Products` configured with your app, you can make use of configured **Bonus Rewards**. This is an optional but useful feature where purchases made under specific platforms can also give the user additional currency or items.

Any platform can be configured to give Bonus Rewards. But since Xsolla purchases are done outside of a platform's ecosystem this is a handy feature to incentivize players to make purchases through your Xsolla web store instead of from within the app.

## Unity Project Set Up

Once the plugin is installed you can configure your Xsolla configuration easily by going to `Window > Xsolla > SDK > Auto Fill Settings`.

You can use your credentials to log into your Xsolla publishing account and fill out your Merchant ID, Project ID, etc. quickly here.

#### Important Set-up in the Scripting

When you go to build your `XsollaClientConfiguration` in your scripting you ***MUST*** pass the user's brainCloud ProfileId to the config's `userId` custom field. This is ***REQUIRED*** during the Xsolla login and when you go to pass the config to the `XsollaStoreClient` builder. If you don't do this then the brainCloud webhook won't be able to match the purchase to the user!

You can see this in action in this project in the script [XsollaHandler.cs](https://github.com/getbraincloud/examples-unity/blob/3PIntegrations/XsollaIntegration/Assets/App/XsollaHandler.cs#L1203) in the inline `Login()` function.

---

If there are any questions or issues feel free to ask them on the [Issues](https://github.com/getbraincloud/examples-unity/issues) page.

For more information on brainCloud and its services, please check out [brainCloud Learn](https://docs.braincloudservers.com/learn/introduction/) and [API Reference](https://docs.braincloudservers.com/api/introduction).
