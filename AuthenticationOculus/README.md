# Meta Horizon Authentication

<p align="center">
    <img  src="../_screenshots/x_AuthOculus.png?raw=true">
</p>

## Setting Up

This example app is only able to authenticate on real Meta Quest devices on these platforms:
- Android (Unity 6000.0 or older) / Meta Quest (Unity 6000.1 and newer) via Meta Horizon Store app
- Windows (Meta Quest Link) via Rift app

For your own projects you will need to add several Meta XR plugins from the Unity Asset Store:
- [https://assetstore.unity.com/packages/tools/integration/meta-xr-core-sdk-269169](Meta XR Core SDK)
- [https://assetstore.unity.com/packages/tools/integration/meta-xr-interaction-sdk-265014](Meta XR Interaction ​SDK)
- [https://assetstore.unity.com/packages/tools/integration/meta-xr-platform-sdk-262366](Meta XR Platform SDK)
- [https://assetstore.unity.com/packages/tools/integration/meta-xr-simulator-266732](Meta XR Simulator)

<p align="center">
    <img  src="../_screenshots/_authOculus/UnityPackage.png?raw=true">
</p>

This example already incorporates these plugins.

In your Unity Project you will also need to make sure your project has the **XR Plugin Management** installed under `Project Settiongs > XR Plugin Management`. Then, after it is installed, you will need to make sure that **OpenXR** is enabled for either your **Windows** platform or **Android/Meta Quest** plaform (or both if your app is meant to run on both platforms).

<p align="center">
    <img  src="../_screenshots/_authOculus/XRPluginManagement.png?raw=true">
</p>

For more in-depth information to expand on your Meta Quest app in Unity, you should check out the [Meta Horizon Develop Unity documentation](https://developers.meta.com/horizon/develop/unity).

If you are downloading this example app to test out this process yourself you will need to follow the instructions below in order to get everything set up with the Meta Horizon Developer Dashboard in order to get the User ID required for authentication.

### Meta Horizon Developer Dashboard Configuration

It is required that you have apps ***set up AND uploaded*** on the **Meta Horizon Developer Dashboard** in order to successfully retreive the User ID for the app. Additionally, both Meta Horizon Store apps and Rift apps will require you to go through this process. If you are making the same app for both platforms you **WILL** need to create a seperate app for both on the Meta Horizon Developer Dashboard.

Make sure you have downloaded the [Oculus Platform Utility](https://developers.meta.com/horizon/resources/publish-reference-platform-command-line-utility/). It is a requirement for uploading builds.

In order to be able to retrieve the the User ID with the Meta XR Platform SDK for your app there are several steps that need to be done:

Before you build your app make sure you have created an app already on the **Meta Horizon Developer Dashboard** so that you can retrieve the Application ID for your **OculusPlatformSettings** file in Unity
    - Again, if this app is for both Meta Horizon Store and Rift you will need to create seperate apps for them to retrieve seperate App IDs
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
    - Also fill out any others here that is relevant to your app
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

### brainCloud Configuration

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

**Note**: For the time being, if you have both a Meta Horizon Store app and Rift app, you can only use the ID and secret from one app. Otherwise you will need to configure your project to use two different brainCloud apps. We are investigating a method to be able to use the same brainCloud app for both types of Meta Quest apps in the future.

---

If everything is set up properly you should hopefully see yourself log in once you tap the LOG IN button! If not or if there are any questions, feel free to ask them on the [Issues](https://github.com/getbraincloud/examples-unity/issues) page.

---

For more in-depth information to expand on your Meta Quest app in Unity, you should check out the [Meta Horizon Develop Unity documentation](https://developers.meta.com/horizon/develop/unity).

For more information on brainCloud and its services, please check out [brainCloud Learn](https://docs.braincloudservers.com/learn/introduction/) and [API Reference](https://docs.braincloudservers.com/api/introduction).
