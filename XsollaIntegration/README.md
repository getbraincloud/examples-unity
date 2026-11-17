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

1. Under your **Merchant** account, create a **Project**.
2. Create a **Login project** for it.
    - For testing in the Unity Editor, enable **Passwordless login** and **Login with device ID**. The login widget can't run in the Editor, so the SDK falls back to device ID there.
3. In the Login project, go to **Security > OAuth 2.0** and add two **OAuth 2.0 clients**:
    - **Public** (for your Unity project). Only its Client ID is needed. Add `https://login.xsolla.com/api/blank` as a redirect URI; the SDK uses it in the Editor and desktop builds, and login fails with error `010-017` without it. For mobile builds, also add `app://xlogin.<your bundle ID>`.
    - **Server** (for your brainCloud app). Copy its Client ID and Secret.
4. Under **Project settings > Integration settings**, make sure **Use webhooks** is enabled.
5. Go to **Project settings > Webhooks**:
    1. Add a secret key:
        - Name it `brainCloud` to indicate it's used by your brainCloud app.
        - Copy the secret. It's only shown once; if you lose it, delete the key and create a new one.
        - Click **Set as active**. Only one key per project is active, and webhooks are signed with it.
    2. Under **Advanced settings**, enable:
        - **Send custom parameters** (***required***, or brainCloud can't match purchases to users)
        - **Show info about the refund reason** (recommended)
        - **Send only necessary user parameters without sensitive data** (recommended)
    3. Set the **Webhook server** URL to the one brainCloud gives you (see below).
6. Go to **API keys**, click **Create API key +**, name it (e.g. `braincloud-server-key`) and copy the secret.
    - Again, it's only shown once; if you lose it, generate a new one.
7. Under **Items catalog**, create your products as **Virtual items** with a real-currency price and set them to **Available**. The SDK only fetches virtual items, so anything created as virtual currency won't show up. Each SKU must match the one on its brainCloud Cash Product.

## brainCloud Configuration

1. In the [brainCloud server portal](https://portal.braincloudservers.com/) for your app, go to `App > Design > Integrations > Payments`.
2. Click the **Xsolla** config.
3. Fill out the required fields:
    - Project ID
    - Project Secret Key (Webhooks)
    - Server OAuth 2.0 Client ID
    - Server OAuth 2.0 Client Secret
4. **Login Project ID** is optional, but needed for the `AppStore.GetUserToken` API call.
5. **Project API Key** is optional, but needed for the server to consume purchases instead of relying on your client app.

This config also gives you the **Webhook URL** to set as your Xsolla **Webhook server** URL above. You can also set how the app handles refund webhooks here.

### Bonus Rewards

Cash Products in `App > Design > Marketplace > Cash Products` can have **Bonus Rewards**: extra currency or items granted when the purchase is made on a specific platform.

Any platform can have them, but they're especially useful for Xsolla. Since Xsolla purchases happen outside a platform's ecosystem, a bonus is a handy incentive for players to buy through your Xsolla web store instead of in-app.

## Unity Project Set Up

Once the plugin is installed, go to `Window > Xsolla > SDK > Auto Fill Settings` and log in with your Xsolla Publisher Account. It fills in your Project ID, Login ID, OAuth 2.0 Client ID and redirect URL, and saves them to `Assets/Resources/XsollaSDKSettings.asset`. Make sure the Client ID is your **public** client's.

#### Important Set-up in the Scripting

When building your `XsollaClientConfiguration`, you ***must*** set `userId` to the user's brainCloud profile ID before passing the config to the `XsollaStoreClient` builder. The SDK sends it with each purchase as the `custom_user_id` custom parameter, which is how the brainCloud webhook matches the purchase to the user. It only works with **Send custom parameters** enabled. You can see this in action in [XsollaHandler.cs](./Assets/App/XsollaHandler.cs#L1203), in the inline `Login()` function.

If you also set `trackingId`, it must be exactly 32 alphanumeric characters, so strip the hyphens from the profile ID.

---

If there are any questions or issues feel free to ask them on the [Issues](https://github.com/getbraincloud/examples-unity/issues) page.

For more information on brainCloud and its services, please check out [brainCloud Learn](https://docs.braincloudservers.com/learn/introduction/) and [API Reference](https://docs.braincloudservers.com/api/introduction).
