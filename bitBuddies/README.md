# bitBuddies — brainCloud Tech Demo

> *Catch 'em all, raise 'em right, love 'em forever.*

bitBuddies is made with Unity Engine and brainCloud to be virtual pet collector game built as a technical demonstration of [brainCloud](https://getbraincloud.com/)'s backend-as-a-service features. Players collect and level up creature companions called bitBuddies, earning currencies and unlocking rewards along the way. The project is designed to showcase how quickly and cleanly a developer can wire up a full game economy, progression system, and social features using brainCloud.

---

## What This Demo Showcases

### 1. Parent–Child Account Architecture
The game uses brainCloud's **parent–child app relationship** as a core design pattern. The player's main profile (the "Parent" account) owns top-level currencies (Coins, Gems, FakeDollars) and progression data. Each individual bitBuddy lives inside a **Child account** with its own stats, currency (BuddyBling), XP (Love), and item inventory.

This demonstrates:
- Cross-app data ownership and isolation
- Running cloud code scripts scoped to child profiles
- Reading child account data (stats, currencies, owned items) from the parent context via the `GetChildAccounts` cloud script
- Updating child profile names from the parent

### 2. Multi-Currency Economy
The game operates with 4 distinct currencies to demonstrate brainCloud's virtual currency system:

| Currency | Type | Scope |
|---|---|---|
| **Coins** | Soft currency | Parent account |
| **Gems** | Premium currency | Parent account |
| **FakeDollars** | Simulated IAP | Parent account |
| **BuddyBling** | Child soft currency | Child account |

Coins can be earned through gameplay, gems can be purchased via IAP (simulated), or awarded by quests and level-ups. BuddyBling is earned per-bitBuddy through toy interactions and parent currency spent in the Mouse Merchant shop. The demo includes currency exchanges (Gems → Coins) and multi-currency transactions.

### 3. In-App Purchasing & Shop Promos
The **Parent Shop** and **Mouse Merchant** demonstrate brainCloud's catalog and storefront features:

- Coin Bags purchasable with Gems (e.g. 10,000 Coins for 10 Gems)
- Gem packages purchasable with simulated money
- **Daily Freebie items** with 24-hour server-enforced cooldowns
- **Promotional pricing** (e.g. discounted Gem bundles, 5x Instant Level-Up pack)
- Child-account cosmetic items (Hats, Sunglasses, Gold Chains) purchasable with BuddyBling
- All shop items configured via brainCloud's item catalog (`GetItemCatalog` cloud script)

### 4. Cloud Code Scripts
All significant game logic runs server-side through brainCloud Cloud Code scripts, organized into root-level scripts, shared utilities, child account scripts, and loot box scripts.

#### Root Scripts (Parent Account)

| Script | Purpose |
|---|---|
| `GetQuestInfo` | Fetches all milestone/quest data and the parent shop item catalog in one call. Returns quest titles, progress stats, rewards, unlock requirements, shop item metadata, cooldowns, and the current freebie cooldown status. |
| `ClaimQuestReward` | Validates and claims a quest reward. Calls `GetQuestInfo` to verify the submitted score meets the threshold, increments the appropriate player stat to advance the quest line, and returns reward info. |
| `GetParentShopCatalog` | Fetches the full parent-level item catalog and returns a simplified list of shop items including item ID, display name, description, metadata, buy price, and cooldown. |
| `ClaimParentShopItem` | Full purchase flow for the parent shop. Verifies affordability, deducts the appropriate currency (Gems or FakeDollars), awards the item or grants currency directly for consumables, updates player stats, and schedules `DropFreebieUserItems` if the item has a cooldown. |
| `AwardCoinsToUser` | Awards a specified amount of Coins to the authenticated parent user. |
| `AwardGemsToUser` | Awards a specified amount of Gems to the authenticated parent user. |
| `AwardMoneyToUser` | Awards a specified amount of FakeDollars (simulated IAP currency) to the authenticated parent user. |
| `ConsumeCoinsForUser` | Consumes a specified amount of Coins from the parent user's virtual currency balance. |
| `IncreaseXPForParent` | Increments XP for the parent user, detects level-ups, and updates `summaryFriendData` with current XP, level, and next level-up requirement for the client UI progress bar. |
| `GetCooldownFreebieUserItem` | Checks the parent user's inventory for the freebie item and returns its `coolDownUntil` timestamp so the client knows when the next freebie is available. |
| `DropFreebieUserItems` | Scheduled script that removes all freebie user items from a player's inventory after their cooldown expires, keeping accounts clean. |
| `AwardBlingToChild` | Switches to a specified child profile and runs `AwardBuddyBling` in the child app, awarding BuddyBling currency to that child's account. |

#### Shared Utility Include

| Script | Purpose |
|---|---|
| `ChildUtils` | Shared include used by most child-related scripts. Provides helpers for switching between parent and child profiles (`switchToChildAccount`, `switchToChildAppSafely`, `addChildAccount`), reading child profiles, flattening currency balances, checking item ownership, updating buddy XP/level with level-up detection and `summaryFriendData` sync, and creating new buddy entities from loot box rolls using weighted rarity tables. |

#### Child Account Scripts

| Script | Purpose |
|---|---|
| `getChildProfiles` | Retrieves all child profiles linked to the parent, switches into each profile to gather currencies, stats, and equipped user items (including Love booster metadata), and returns the list. |
| `getChildItemCatalog` | Switches to a child profile and fetches the child app's item catalog, returning items organized by category — toys (with payout and spawn metadata) and Mouse Merchant items (with pricing, booster, and consumable metadata). |
| `fetchStats` | Fetches player statistics for both the parent profile and a specified child profile, returning them separately. |
| `fetchCurrencies` | Switches to a specified child profile and returns a flattened map of that child's virtual currency balances. |
| `updateChildAccountName` | Switches to a specified child profile, updates the username, and increments the parent's `userChangedName` stat on success. |
| `claimLoveBooster` | Claims the daily Love Booster for a specified child profile. Handles awarding and activating the booster item, checks cooldown status, and schedules `DropDailyLoveBoosterUserItem` to clean up after the 24-hour cooldown. Returns the active window expiry and XP multiplier. |
| `claimMouseMerchantItem` | Full purchase flow for the Mouse Merchant. Supports three pricing types (parent Gems, child BuddyBling, or free), three result types (wearable items, consumable payouts, and level-up items), affordability checks, currency deduction from the correct profile, XP level-up calculation, `summaryFriendData` updates, and stat tracking. |
| `consumeToy` | Switches to a child profile, finds the specified toy in the child's inventory, activates it, and creates a short-lived `toyRewardInfo` custom entity storing the toy's reward payout info (Coins, Love, BuddyBling, cooldown). Entity TTL is set to match the toy's session window. |
| `consumeCurrencyFromToy` | Validates currency pick-ups from a toy session by cross-checking submitted amounts against the `toyRewardInfo` custom entity. Clamps values to valid maximums, awards BuddyBling and Coins to the appropriate profiles, increments `CoinsGainedForParent`, and updates the buddy's XP/level via `updateBuddyLevel`. |
| `obtainToy` | Purchases a toy from the child item catalog. Checks that the child meets the level requirement, verifies the parent can afford the Coin cost, consumes Coins from the parent, awards the toy item to the child, prevents duplicate ownership, and tracks `toysBought` and special stats (e.g. `scienceKitObtained`). |
| `increaseChildBuddyExperience` | Increases the XP of a specific child buddy by calling the `updateBuddyLevel` helper from `ChildUtils`. Handles level-up detection and `summaryFriendData` sync. |
| `updateChildCoinCollected` | Calculates idle coin earnings for a child buddy based on elapsed time since `lastIdleTimestamp`, awards Coins and proportional XP to the parent, updates `summaryFriendData` with a refreshed timestamp, and increments `CoinsGainedForParent` on the child profile. |
| `deleteChildProfile` | Deletes a specified child profile (buddy) using a system user session, provided the parent has more than one child profile. Increments the `trashBuddies` stat on success. |

#### Loot Box Scripts

| Script | Purpose |
|---|---|
| `addBasicChildAccount` | Creates a new child profile by opening a Basic loot box. Uses weighted rarity tables to randomly determine the buddy's rarity and stats, then initializes the buddy entity and `summaryFriendData` in the new child profile. |
| `addStarterChildAccount` | Same flow as `addBasicChildAccount` using the starter rarity pool. |
| `addRareChildAccount` | Same flow using the rare rarity pool. |
| `addSuperRareChildAccount` | Same flow using the superRare rarity pool. |
| `addLegendaryChildAccount` | Same flow using the legendary rarity pool, guaranteeing a Legendary buddy. |

### 5. Global Properties
Game configuration that needs to be tunable without a code deploy is stored in **brainCloud Global Properties**, fetched on login:

- `MysteryBoxInfo` — JSON blob defining box rarities, drop rates, costs, and level unlock requirements
- `RewardPickUpLifetime` — How long reward pickups remain on the floor before auto-collecting
- `ChildAccountMaximum` — Server-enforced cap on how many bitBuddies a player can own
- `AboutApp` — Text content for the in-app About screen (supports link-outs)

### 6. Offline Idle Income
bitBuddies passively generate Coins even while the player is offline. The idle income system is implemented using **timestamps stored in Summary Friend Data**. On login, the client calculates elapsed time and the earned coin amount client-side before presenting it to the player for collection. Rates and capacity vary by bitBuddy rarity.

### 7. Player Statistics & Stat Tracking
The demo uses brainCloud's **player statistics** system to track gameplay milestones used by the quest and achievement system. Stats tracked include:

- `LoginCount`
- `BuddiesOwned`
- `BuddiesLeveledUp`
- `ToysBought`
- `HatsBought`, `SunglassesBought`, `ChainNecklacesBought`
- `ScienceKitsBought`
- `Level5Buddies`
- `BoughtLevelUpPromos`

Stats are incremented locally via a `StatTracker` singleton and synced to brainCloud through cloud scripts, ensuring they are available for quest evaluation server-side.

### 8. Quest Chains (Achievements with Rewards)
The **Quests page** demonstrates brainCloud's milestone/achievement system organized into serial quest chains. Players must claim a completed quest to unlock the next one in the chain. Rewards (Coins or Gems) are dispensed automatically on claim.

Three quest lines are included:
- **bitBuddies** — Progression goals (own 3 buddies, eat 3 level-ups, buy toys, reach level 5)
- **bitBling** — Cosmetic spending goals (buy hats, sunglasses, chains)
- **General** — Meta goals (change name, trash a buddy, log in 3 times, make purchases)

### 9. Summary Friend Data
Each child profile uses **Summary Friend Data** as a lightweight cache for the parent context to read without a full child account fetch. Data stored includes rarity, sprite path, coin multiplier, idle rate, capacity, current level, XP, and next level-up threshold. This avoids expensive cross-app lookups on every parent screen load.

### 10. Experience & Levelling
Both the Parent account and each bitBuddy have independent XP/levelling systems:

- **bitBuddy levelling** uses brainCloud's child-account XP (`IncreaseXP`) via Love earned from toy play. Level-ups are gated behind a consumable token the player must collect in-room, adding a gameplay moment to each level-up.
- **Parent levelling** is triggered by child level-ups and uses brainCloud's parent-account XP system, awarding Coins and Gems at each new level.

---

## Project Structure Notes

| Class | Role |
|---|---|
| `BrainCloudManager` | Singleton wrapper around `BrainCloudWrapper`; owns all API call methods and success/failure callback helpers |
| `ToyManager` | Manages toy bench state, reward batching (7.5s debounce before sending to server), and the Love multiplier countdown |
| `MouseMerchantItem` | Handles shop item purchase flow for the per-buddy Mouse Merchant, covering love boosters, cosmetics, level-up consumables, and gem→bling exchanges |
| `GameManager` | Central game state; holds child account list, selected child info, shop configs, and mystery box definitions |
| `StatTracker` | Local stat increment cache used to feed the quest system |

---

## Platform Target

Windows PC and Mac