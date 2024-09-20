# CursorParty

<p align="center">
    <img  src="../_screenshots/x_CursorParty.png?raw=true">
</p>

---

An example that showcases the **Matchmaking** and **Relay** services in brainCloud.

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

For more information on brainCloud and its services, please check out [brainCloud Learn](https://docs.braincloudservers.com/learn/introduction/) and [API Reference](https://docs.braincloudservers.com/api/introduction).
