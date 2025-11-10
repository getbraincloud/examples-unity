# Meta Quest / Oculus Authentication

<p align="center">
    <img  src="../_screenshots/x_AuthOculus.png?raw=true">
</p>

### Navigation
- [Unity Project Set Up](#unity-project-set-up)
- [Meta Horizon Developer Dashboard Configuration](#meta-horizon-developer-dashboard-configuration)
- [brainCloud Configuration](#brainCloud-configuration)
- [Meta Horizon Store Add-Ons](#meta-horizon-store-add-Ons)
- [Purchasing Notes](#purchasing-notes)

## Unity Project Set Up

This example app is only able to authenticate on real Meta Quest devices on these platforms:
- Android (Unity 6000.0 or older) / Meta Quest (Unity 6000.1 and newer) via Meta Horizon Store app
- Windows (Meta Quest Link) via Rift app

For your own projects you will need to add several Meta XR plugins from the Unity Asset Store:
- [Meta XR Core SDK](https://assetstore.unity.com/packages/tools/integration/meta-xr-core-sdk-269169)
- [Meta XR Interaction ​SDK](https://assetstore.unity.com/packages/tools/integration/meta-xr-interaction-sdk-265014)
- [Meta XR Platform SDK](https://assetstore.unity.com/packages/tools/integration/meta-xr-platform-sdk-262366)
- [Meta XR Simulator](https://assetstore.unity.com/packages/tools/integration/meta-xr-simulator-266732)

<p align="center">
    <img  src="../_screenshots/_authOculus/UnityPackage.png?raw=true">
</p>

This example already incorporates these plugins.

In your Unity Project you will also need to make sure your project has the **XR Plugin Management** installed under `Project Settings > XR Plugin Management`. Then, after it is installed, you will need to make sure that **OpenXR** is enabled for either your **Windows** platform or **Android / Meta Quest** platform (or both if your app is meant to run on both platforms).

<p align="center">
    <img  src="../_screenshots/_authOculus/XRPluginManagement.png?raw=true">
</p>

For more in-depth information to expand on your Meta Quest app in Unity, you should check out the [Meta Horizon Develop Unity documentation](https://developers.meta.com/horizon/develop/unity).

If you are downloading this example app to test out this process yourself you will need to follow the instructions below in order to get everything set up with the **Meta Horizon Developer Dashboard** in order to get the User ID required for authentication.

## Meta Horizon Developer Dashboard Configuration

It is required that you have apps ***set up AND uploaded*** on the **Meta Horizon Developer Dashboard** in order to successfully retreive the User ID for the app. Additionally, both Meta Horizon Store apps and Rift apps will require you to go through this process. If you are making the same app for both platforms you **WILL** need to create a seperate app for both on the Meta Horizon Developer Dashboard.

Make sure you have downloaded the [Oculus Platform Utility](https://developers.meta.com/horizon/resources/publish-reference-platform-command-line-utility/). It is a requirement for uploading builds.

In order to be able to retrieve the the User ID with the Meta XR Platform SDK for your app there are several steps that need to be done:

- Before you build your app make sure you have created an app already on the **Meta Horizon Developer Dashboard** so that you can retrieve the Application ID for your **OculusPlatformSettings** file in Unity
    - Again, if this app is for both Meta Horizon Store and Rift you will need to create separate apps for them to retrieve separate App IDs
    - For Android / Meta Quest build platforms:
        - Under `Project Settings > Player > Android Settings > Other Settings` make sure you have set a unique **Package Name** for your app; remember you CANNOT change this once you've uploaded a build!
        - Under `Project Settings > Player > Android Settings > Publishing Settings` you will also need to create/use an existing **Custom Keystore**; this CANNOT be changed as well and MUST be used for every Android / Meta Quest build made!

<p align="center">
    <img  src="../_screenshots/_authOculus/OculusPlatformSettings.png?raw=true">
</p>

Once the **OculusPlatformSettings** has the correct Application IDs set and you have ensured you're using a unique **Package Name** and **Custom Keystore** for Android / Meta Quest builds then you can proceed with the following with your app selected in the Meta Horizon Developer Dashboard:

1. Under `Distribution > App Metadata History`, ensure that you have filled out the **Privacy Policy** of the latest version of your app's metadata history under its **Details** page
    - You should also fill out as many details as you can here for your app including under Name, Categorization, etc

<p align="center">
    <img  src="../_screenshots/_authOculus/MetaSetUp1.png?raw=true">
</p>

2. Under `Requirements > Data Use Checkup`, you will need to submit a **Request to Access Platform Features** certification
    - You will need to submit one for both **User ID** and **User Profile**
    - If you intend on making use of [Monetization](#meta-horizon-store-add-ons) you might also want to submit certifications for **In-App Purchases** and/or for **Subscriptions**
    - Follow the steps and answer every question truthfully; the more details the better
    - It will take time for the certification to be reviewed and approved
    - Once approved, you will see `Active` next to the Platform Feature

<p align="center">
    <img  src="../_screenshots/_authOculus/MetaSetUp2.png?raw=true">
</p>

3. Under `Distribution > Release Channels`, you will have to upload a build that includes the **Meta XR Platform SDK** in the build in one of the Release Channels
    - If you haven't uploaded any builds yet we suggest releasing one in the **ALPHA** channel
    - This is where the Oculus Platform Utility (`ovr-platform-util`) will be required in order to upload your builds
    - If you click **Upload New Build** you will see the command required to upload your build using
    - Note: This process is different between Meta Horizon Store apps and Rift apps; pay attention to the command arguments and make sure you set them properly

<p align="center">
    <img  src="../_screenshots/_authOculus/MetaSetUp3.png?raw=true">
</p>

4. Make sure you have added Users to the release channel you are testing on
    - These are the users who are logged in on their Meta Quest device
    - This will include yourself even if you're the admin of the Meta Horizon Developer Dashboard!

Finally, you may need to fill out any **Tasks** that are required under `Requirements > Tasks`, such as **Organization Verification**.

## brainCloud Configuration

1. In the [brainCloud server portal](https://portal.braincloudservers.com/) for your app, navigate to `Design > Core App Info > Application IDs`
2. Click **Configure Oculus**
3. Fill out the following required fields:
    - Oculus App ID
    - Oculus App Secret 

<p align="center">
    <img  src="../_screenshots/_authOculus/BCConfig.png?raw=true">
</p>

You can find the App ID and App Secret for your app on the Meta Horizon Developer Dashboard under `Development > API`.

<p align="center">
    <img  src="../_screenshots/_authOculus/MetaAPI.png?raw=true">
</p>

Make sure to fill out the proper fields. If your app on the Meta Horizon Developer Dashboard is intended for the **Meta Horizon Store** for standalone Quest devices then you will want to fill out the first two fields:
- Meta Horizon (Quest) App ID
- Meta Horizon (Quest) App Secret

If your app is instead a **Rift app** (for the Meta Quest Link app on Windows) then you'll want to fill out the second two fields:
- Rift App ID
- Rift App Secret

All four fields can be filled out if you have both sets of apps for your Unity project.

---

If everything is set up properly you should hopefully see yourself log in once you tap the **LOG IN** button! If not or if there are any questions, feel free to ask them on the [Issues](https://github.com/getbraincloud/examples-unity/issues) page.

## Meta Horizon Store Add-Ons

brainCloud now supports [Meta Horizon Store's Add-ons](https://developers.meta.com/horizon/resources/add-ons/)! If you're familiar with [brainCloud's Marketplace feature](https://help.getbraincloud.com/en/articles/9120848-design-marketplace-products) for managing your in-app purchasing (especially for other third-party stores), this will be pretty simple.

1. First things first, under `Requirements > Data Use Checkup` you will need to activate the **In-App Purchases and/or Downloadable Content** and/or **Subscriptions** platform features for your app in order to make use of Add-ons for your app.

<p align="center">
    <img  src="../_screenshots/_authOculus/MetaSetUp4.png?raw=true">
</p>

2. Next you will need to set up your Add-ons on the Meta Horizon Developer Dashboard by navigating to `Monetization > Add-ons` and then click `Create Add-on`
    - You will need to fill out the **Details**, **Pricing**, and **Metadata** pages in order to publish
    - Under Details, the **SKU** and **Add-on Type** are important to note down
    - Note: **Show in Store** isn't required for this but if it is enabled it may take some time before it will be published

Note: You can also create **Subscriptions** under `Monetization > Subscriptions`. The process is similiar to Add-ons and it is important to note down the **SKU**.

<p align="center">
    <img  src="../_screenshots/_authOculus/MetaAddOns.png?raw=true">
</p>

3. When your Add-ons are published, you will need to configure the **Product** on brainCloud; navigate to `Design > Marketplace > Products`; from here you can add & configure your products on brainCloud and fill out the various details and awards
    - The idea is that you will want to create **Products** that correspond to the **Add-ons** on the Meta Horizon Store and brainCloud will manage the rewards those purchases will give to your users

4. Once a product has been configured you will need to add the Meta Horizon Store for that product's **Price Points** platform; edit your product and click the **+** across from Price Points then click **Add Platform** and select **Meta Horizon**
    - **Amount** is a reference price that should equal the price that you set for the Add-on in the Meta Horizon Developer Dashboard
    - The **Meta Horizon Product ID** should match the **SKU** that you set on the Meta Horizon Developer Dashboard

<p align="center">
    <img  src="../_screenshots/_authOculus/BCProduct.png?raw=true">
</p>

Once you click **Save & Close** and you select the newly created platform as the active Price Point then you're good to start testing!

---

If everything is configured properly (importantly, the **SKU** from the Meta Horizon Developer Dashboard matches the **Meta Horizon Product ID** under the Meta Horizon Price Points Platform on brainCloud), then your products should appear in the example app once you log in! If not or if there are any questions, feel free to ask them on the [Issues](https://github.com/getbraincloud/examples-unity/issues) page.

### Purchasing Notes

- By default, the brainCloud [VerifyPurchase](https://docs.braincloudservers.com/api/capi/appstore/verifypurchase) call will consume any `metaHorizon` **Consumables**. This can be changed to have the app itself consume after the VerifyPurchase call. You can set the [const bool CONSUME_ON_BRAINCLOUD_VERIFY](https://github.com/getbraincloud/examples-unity/blob/develop/AuthenticationOculus/Assets/App/Scripts/PurchaseHandler.cs#L12) in PurchaseHandler.cs to change this behaviour.

- The Json for the `metaHorizon` store doesn't need the `consumeOnVerify` boolean as it is an optional parameter. It is included in this example just to keep you aware of how consumables function in the brainCloud VerifyPurchase call.

- **Durables** are comparable to **Non-consumables** on brainCloud; unfortunately, due to how Meta Horizon's Add-on purchases work, there isn't a reliable way to track if a user has redeemed a Durable Add-on other than checking for if they own it. For this example, we use a very simple [UserPurchases dictionary](https://github.com/getbraincloud/examples-unity/blob/develop/AuthenticationOculus/Assets/App/Scripts/PurchaseHandler.cs#L34) that is synced to brainCloud via [User Entities](https://docs.braincloudservers.com/api/capi/entity/) but we highly recommend you develop a much more robust inventory system for your user data.

- While brainCloud supports **Subscription** purchases, you will need to track the subscription status yourself. This example uses the same [UserPurchases dictionary](https://github.com/getbraincloud/examples-unity/blob/develop/AuthenticationOculus/Assets/App/Scripts/PurchaseHandler.cs#L34) to track the user's subscriptions but only if they've purchased it; in it's current implementation it wouldn't be able to track if the subscription lapses.

- In order to test purchases we recommend that you create a [Test user](https://developers.meta.com/horizon/resources/test-users/) and add them to your app's current Release Channel as a **Test User**. This way, you can test purchases without having to worry about spending actual money, which will happen if you use a real account!

---

For more in-depth information to expand on your Meta Quest app in Unity, you should check out the [Meta Horizon Develop Unity documentation](https://developers.meta.com/horizon/develop/unity).

For more information on brainCloud and its services, please check out [brainCloud Learn](https://docs.braincloudservers.com/learn/introduction/) and [API Reference](https://docs.braincloudservers.com/api/introduction).
