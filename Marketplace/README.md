# Marketplace

<p align="center">
    <img  src="https://apps.braincloudservers.com/Builds/screenshots/Marketplace.png">
</p>

This example is a small virtual-goods economy built on top of [brainCloud](https://getbraincloud.com/): a store, an inventory, a couple of currencies, and enough supporting mechanics (leveling, boosts, freebies, subscriptions) to make it feel like a real game economy rather than a bare-bones "buy one product" demo.

It uses brainCloud's [Script](https://docs.braincloudservers.com/api/capi/scripting/), [App Store](https://docs.braincloudservers.com/api/capi/appstore/), [User Items](https://docs.braincloudservers.com/api/capi/useritems/), [Redemption Code](https://docs.braincloudservers.com/api/capi/redemptioncode/), and [Player State](https://docs.braincloudservers.com/api/capi/playerstate/) services under the hood.

This used to also be a Push Notification example. That's gone now, and this project is focused on the marketplace/economy side only.

---

## What's in here

- **Coins and Gems** as the two virtual currencies
- **Leveling and XP**: level up and you'll get a `LevelUpModal`. XP is awarded server-side and just reflected on the client (`AppManager.OnUserLevelUpdated` / `OnUserXPUpdated`).
- **A store** for browsing and buying items, bundles, currency packs, and real-money products (`StoreWindow`, `StoreItemCard`, `ViewStoreItemModal`).
- **An inventory** (`PlayerInventory`, `UserItemCard`) where owned items can be:
  - equipped into a slot (avatar frame, shirt, etc.)
  - activated, if they're time-limited boosts (there's a coin multiplier and an XP generator that keeps accruing XP in real time, even while you're offline, with a countdown shown above the level star while it's running)
  - opened, if they're bundles containing other stuff
- **Freebies**: items you can claim for free once a cooldown has passed.
- **Subscriptions**: a `no_ads` subscription with renewal/expiry tracking, which works with both a real platform subscription and a mocked one.
- **Redemption Codes**: promo codes entered from the store that grant coins or items through a brainCloud redemption campaign (see [Redemption Codes](#redemption-codes) below).
- **In-App Purchases**, either the real thing through Unity IAP + brainCloud's App Store service, or a mock mode (on by default, see below) that fakes the actual purchase through brainCloud instead of going through Unity IAP or a real store account.

All the actual gameplay logic (buying, selling, equipping, activating, opening bundles, claiming freebies, awarding XP and coins, verifying subscriptions) lives server-side as brainCloud Cloud Code. Those scripts are in the [`CloudCode`](./CloudCode) folder, and you'll need to upload them to your app under `Design > Cloud Code > Scripts` before any of this will work.

If you want to dig into how it's wired up, these are the scripts to start with:
- [`AppManager`](./Assets/App/Scripts/AppManager.cs): session state such as currencies, XP/leveling, modals, profile image
- [`InventoryService`](./Assets/App/Scripts/Util/InventoryService.cs): fetches store/inventory data and drives most of the buy/sell/equip/activate/bundle/freebie logic
- [`BrainCloudMarketplace`](./Assets/App/Scripts/Store/BrainCloudMarketplace.cs) and [`BCProduct`](./Assets/App/Scripts/Store/BCProduct.cs): bridges brainCloud's Marketplace/App Store service with Unity IAP, both real and mock
- [`ImageCacheService`](./Assets/App/Scripts/Util/ImageCacheService.cs): loads and caches item art from URLs (disk-cached with ETag revalidation on native platforms, memory-only on WebGL since disk caching doesn't behave well there). Items whose catalog `image` is a bare filename instead of a URL (e.g. `shirt-golden.png`) are resolved from the **Local Image Overrides** list on the `ImageCacheService` object in the `Splash` scene

---

## Mock purchases (the default)

`AppManager.MockPurchasesEnabled` is `true` by default. In this mode, purchases and subscriptions are faked entirely through brainCloud (`BrainCloudMarketplace.MockPurchaseProduct`, the `VerifyPurchaseMockStore` script, and a fake subscription renewal timer) instead of going through Unity IAP and an actual store. That means you can run the store/economy loop in the Editor, on desktop, or on WebGL without touching the Google Cloud Console, Play Console, OAuth credentials, or a `.p12` certificate. None of the real store account setup is needed.

One thing this *doesn't* skip: brainCloud's own product catalog. Real-money products are still fetched from brainCloud's App Store service using a `storeId` (`InventoryService.GetPlatformStoreId()`), and even in mock mode that defaults to `"googlePlay"` on Editor/Desktop/WebGL (iOS/macOS resolve to `"itunes"`). So for those products to actually show up in the store, they still need to exist on the brainCloud portal under `Design > Marketplace > Products`, linked to a Google Product ID (see the [brainCloud Marketplace](#braincloud-marketplace) steps below for that part). Items, bundles, freebies, and multipliers bought with Coins/Gems aren't affected by any of this, since those come from the item catalog and need no store setup at all, mock or real.

When you're ready to test real purchases end-to-end through an actual store, set `AppManager.MockPurchasesEnabled = false` and follow the rest of the setup below.

---

## Setting up real In-App Purchases

Both Android and iOS use Unity's In-App Purchasing service to kick off a purchase, which then gets verified through brainCloud's App Store service so it's synced to the user's account. Android goes through the [Google Play Store](https://play.google.com/console/about/in-appproductssetup/), iOS through the [App Store](https://developer.apple.com/in-app-purchase/), and since the two platforms don't work the same way, it's worth reading through their developer portals as you set things up.

### Unity In-App Purchasing

You'll need Unity's **In-App Purchasing** service enabled for the project. It's free to enable; it's just the interface layer for getting, initiating, and validating purchases on each platform.

Follow Unity's [setup guide](https://docs.unity3d.com/Manual/UnityIAPSettingUp.html) to turn it on, and grab the Google Play **License Key** from your Unity services dashboard for Android purchases.

### Google Play Store

You'll need a project set up on both the [Google Cloud Console](https://console.cloud.google.com/) and the [Google Play Console](https://play.google.com/console/developers).

#### Google Cloud Console

- Enable **Google Cloud APIs**, **Google Play Android Developer API**, **Google Play Game Services**, **Service Usage API**, and **Token Service API**
- Create an **OAuth 2.0 Client ID**, which gives you the Client ID and Client Secret you'll need on both brainCloud and the Google Play Console
- Create a **Service Account** under **Credentials**, which is what lets brainCloud talk to your app on the Play Store
    - Also generate a **Google Service Account p12 Certificate** under **Keys**, and keep the downloaded `.p12` somewhere safe

#### Google Play Console

- Make sure billing is enabled on your developer account
- Give the service account **API access** to your developer account
- Under **Users and permissions**, add that same service account and grant it:
    - View app information (read-only)
    - View financial data
    - Manage orders and subscriptions
    - Manage store presence
    - Manage policy declarations
- Also add the OAuth client ID account as a user with **View app information (read-only)**
- Go through **Monetization setup** to enable monetization (this is also where you'll find the Base64-encoded RSA public key Unity IAP needs)
- Add your **In-app products** and/or **Subscriptions**

#### brainCloud Marketplace

1. In the [brainCloud portal](https://portal.braincloudservers.com/) for your app, go to `Design > Core App Info > Application IDs`
2. Click **GOOGLE** under **Configure Platforms**
3. Fill in the fields using what you set up in Google Cloud:
    - Google Service Account Email (the service account from above)
    - Google Package Name
    - Google App ID
    - Google Client ID / Client Secret (from the OAuth client)
4. Upload the `.p12` certificate for the service account
5. Head to `Design > Marketplace > Products` and set up your products
    - Each product here represents something a user can buy, and setting it up here is what lets brainCloud grant the right currency/items once a purchase is verified
    - The **Google Product ID** has to match the product ID from the Play Console
    - Prices here are just for reference; they don't need to match what's actually configured in the Play Store
    - It's a good idea to pull the localized title/description/price from the product metadata at runtime rather than hardcoding them, so users see pricing correct for their region
    - [`BrainCloudMarketplace`](./Assets/App/Scripts/Store/BrainCloudMarketplace.cs), [`BCProduct`](./Assets/App/Scripts/Store/BCProduct.cs), and [`StoreWindow`](./Assets/App/Scripts/UI/Elements/StoreWindow.cs) show how this example uses all of that

### App Store

Set up a **Bundle Identifier** on [Apple's developer portal](https://developer.apple.com/account/resources/identifiers/list), then create an app in [App Store Connect](https://appstoreconnect.apple.com/apps) using it.

#### App Store Connect

1. Under `App Information > Bundle ID`, make sure the right Bundle ID is selected
2. Create in-app purchase products under `Features > In-App Purchases` and subscriptions under `Features > Subscriptions`
    - You can only create one of each until you've uploaded a build for the first time; if you need more for testing, a test build will unlock that

#### brainCloud Marketplace

1. In the [brainCloud portal](https://portal.braincloudservers.com/) for your app, go to `Design > Core App Info > Application IDs`
2. Click **APPLE** under **Configure Platforms**
3. Fill in **Bundle Id** with the bundle identifier you created
4. Head to `Design > Marketplace > Products` and set up your products
    - Your **Product ID** needs to match what's in App Store Connect
    - Use the same Product ID across iPhone, iPad, and Apple TV (Apple used to require these be different, but that's no longer the case)
    - See [step 5](#braincloud-marketplace) above for the rest, same idea, just on the Apple side

---

## Redemption Codes

The store's **Redemption Codes** section (last in the store, also reachable from the side nav) has a single card that opens an **Enter Code** view in `ViewStoreItemModal`. The code entered there is sent with `RedemptionCodeService.RedeemCode`, and brainCloud does the rest:

1. The code is matched to a **redemption campaign**. No `codeType` is passed: codes of 20 characters or fewer are shared promo codes, so brainCloud finds the campaign from the code itself.
2. The campaign's **fulfillment script**, [`RedemptionFulfillment`](./CloudCode/RedemptionFulfillment.ccjs), reads the reward from the campaign's **Code Info JSON** (`data.customCodeInfo`), grants it, and returns it as `customRedemptionInfo`.
3. The client shows the reward from that response: coins update the balance, and equippable items (like the Golden Shirt) are added to the inventory and equipped right away.

The reward is pure campaign data, so new promos need no client or script changes. Two `rewardType`s are supported:

```json
{ "rewardType": "currency", "currency": "Coins", "amount": 2500 }
{ "rewardType": "item", "defId": "shirt-golden" }
```

### Setting up a campaign

1. Upload [`RedemptionFulfillment`](./CloudCode/RedemptionFulfillment.ccjs) with the other Cloud Code scripts.
2. In the portal, go to `Design > Promotions > Redemption Codes` (exact menu names can differ between portal versions) and create a campaign:
    - **Code Info JSON**: one of the reward objects above
    - **Fulfillment script**: `RedemptionFulfillment`, run synchronously
    - **Promo code**: the shared code players type in (the client uppercases input, so use uppercase codes)
    - **Redemption limit / retries**: a shared code can only be redeemed once per player
3. For item rewards, the item definition must exist and be **published** in the Item Catalog, or the fulfillment fails with "Item has not been published yet".

The modal shows **Invalid Code** for unknown codes or failed fulfillments, and **Already Redeemed** when brainCloud reports the code as already used. The full error is logged to the console.

---

## Cloud Code

The scripts in [`CloudCode`](./CloudCode) are where all the real logic lives: currency/XP awards, buying/selling/equipping items, opening bundles, claiming freebies, activating boosts, verifying purchases and subscriptions, and fulfilling redemption codes. Upload them to your brainCloud app under `Design > Cloud Code > Scripts`, keeping the names as-is, since the client calls them by name through the Script Service (things like `BuyItem`, `SellItem`, `EquipItem`, `ActivateItem`, `UseFreebie`, `OpenBundle`, `AwardUserCoins`). `RedemptionFulfillment` is the exception: the client never calls it directly, and brainCloud runs it when a campaign's code is redeemed.

---

For more on brainCloud's monetization tools, check out [brainCloud Monetization](https://docs.braincloudservers.com/learn/key-concepts/monetization/). For everything else, there's [brainCloud Learn](https://docs.braincloudservers.com/learn/introduction/) and the [API Reference](https://docs.braincloudservers.com/api/introduction).
