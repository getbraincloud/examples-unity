# CursorParty

<p align="center">
    <img  src="../_screenshots/x_CursorParty.png?raw=true">
</p>

---

An example that showcases the **Matchmaking**, **Lobby**, and **Relay** services in brainCloud, including region-aware matchmaking with ping data. Works with brainCloud's own regional relay servers as well as **EdgeGap** and **GameLift**.

---

## Setup

To set up lobby types as a **Global Property**, in the [brainCloud server portal](https://portal.braincloudservers.com/), navigate to `Design > Cloud Data > Global Properties`:

1. Press the **+** on the right side to create a new Global Property
2. **Name** and **Category** can be set to what you prefer
3. Ensure **Type** is set to `String`
4. **Value** should look like the following JSON:

```
{
  "0":{
    "lobby":"FreeForAllParty"
  },
  "1":{
    "lobby":"TeamParty"
  }
}
```

CursorParty is set up to look for the word **Team** in the lobby types, so if you want to test Team Mode in the example app, ensure your lobby type has the word **Team** in it. It will otherwise use **Free For All** mode by default.

---

## Ping Region Matchmaking

The app supports region-aware matchmaking using the existing `GetRegionsForLobbies`, `PingRegions`, and `FindOrCreateLobbyWithPingData` APIs — none of this is new API surface, the RTA now just shows how to wire it together.

Enable the **Use Ping Data** toggle in the settings screen before searching. When on, three steps run before any lobby search kicks off:

```csharp
GetRegionsForLobbies(...)   // fetch the available region targets
    -> PingRegions(...)     // measure latency to each one
        -> FindOrCreateLobbyWithPingData(...)  // search with that data attached
```

These must be chained in callbacks — the SDK enforces ordering and will fail fast if `FindOrCreateLobbyWithPingData` is called before ping results are ready.

Once in a lobby, a region quality panel shows your measured ping to each region. The current lobby's region is marked with `◄`, colour-coded green if within 30ms of the best available ping, red if not. During the match, each player's live relay RTT is broadcast to all peers every 2 seconds.

---

## Regional Server Support

brainCloud's own relay servers support regional placement out of the box — configure your regions in the portal and the ping data flow above will handle the rest.

The app also supports **EdgeGap** and **GameLift** relay servers, detected automatically from the lobby response with no extra client configuration needed. For EdgeGap, configure a single beacon per target region to guarantee server placement there; without a single beacon, EdgeGap determines placement using its own geo-location logic. For GameLift, use a region-scoped fleet.

One thing worth noting for EdgeGap: regional placement is only guaranteed when **exactly one beacon** is configured on the server.

---

For more information on brainCloud and its services, please check out [brainCloud Learn](https://docs.braincloudservers.com/learn/introduction/) and [API Reference](https://docs.braincloudservers.com/api/introduction).
