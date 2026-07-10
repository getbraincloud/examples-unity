# Using brainCloud MCP with Unity and AI assistants

brainCloud hosts a [Model Context Protocol](https://modelcontextprotocol.io/) (MCP) server
that lets an AI assistant manage your brainCloud app for you — cloud code scripts, API hooks,
"My Server" (S2S), and Web Services, and look up the brainCloud API docs — using your own
account. You can connect to it two ways while working on these examples:

- **[From Unity's built-in AI Assistant](#connect-from-unitys-ai-assistant)** — drive
  brainCloud from inside the Editor.
- **[From a coding agent like Claude Code](#connect-from-a-coding-agent)** — drive it from a
  terminal assistant, outside Unity.

> **The assistant acts as you.** The first time you connect you sign in to brainCloud in your
> browser, and the server uses that identity for every request. It never sees your app secret,
> and any write to a **Live** app stays blocked until you confirm an unlock in the brainCloud
> portal (ask the assistant to run `requestUnlock`).

## Connect from Unity's AI Assistant

Manage brainCloud from **Edit ▸ Project Settings ▸ AI ▸ Assistant Extensions**, without a
separate tool. This drives Unity's own Assistant, so it consumes **Unity AI credits**.

**Prerequisites**

- **Unity 6** with the AI Assistant package (`com.unity.ai.assistant`) installed.
- **[Node.js](https://nodejs.org)** installed — it provides the `npx` command used to launch
  the connection.
- A **brainCloud account** on a team that has **Builder API** enabled.

### 1. Enable MCP tools

In Unity, open **Edit ▸ Project Settings ▸ AI ▸ Assistant Extensions** and tick
**Enable MCP Tools**.

### 2. Tell Unity where Node is (required)

Unity doesn't inherit your shell's `PATH`, so it can't find `npx` on its own — this is a
common cause of a failed connection (`Executable not found in $PATH: "npx"`).

Under **Path Configuration ▸ User Path**, add the folder that contains `node` and `npx`:

| Platform | Add to User Path |
|---|---|
| macOS (Homebrew) | `/opt/homebrew/bin` |
| macOS (Node installer) | `/usr/local/bin` |
| Windows | e.g. `C:\Program Files\nodejs` |

Not sure of the folder? Run `dirname "$(which npx)"` in a terminal (macOS/Linux) or
`where npx` (Windows). Separate multiple paths with `:` on macOS/Linux, `;` on Windows.

### 3. Add the brainCloud server

Next to **Config File**, click **Open**, and set the file's contents to the following (also
provided as a ready-to-copy file: [`mcp.example.json`](mcp.example.json)):

```json
{
  "enabled": true,
  "path": "",
  "mcpServers": {
    "braincloud": {
      "type": "stdio",
      "command": "npx",
      "args": ["-y", "mcp-remote", "https://mcp.braincloudservers.com/mcp"]
    }
  }
}
```

Save the file.

### 4. Connect and sign in

Back in the settings page, click **Refresh File and Servers**. On the first connection your
browser opens to sign in to brainCloud and authorize access — approve it. `mcp-remote` caches
the token under `~/.mcp-auth`, so you don't sign in every launch.

### 5. Confirm it works

The **Servers** list should show **braincloud** with a green dot and its tool count. Ask the
Assistant *"list my brainCloud teams"* to confirm, then set the app you want to work on with
`useApp`. From here the Assistant can read and edit that app's cloud code and configuration.

## Connect from a coding agent

Prefer a terminal assistant like [Claude Code](https://claude.com/claude-code)? It runs
entirely outside Unity — no Unity packages, Editor, or Unity AI credits. Point it at the same
server:

```bash
claude mcp add --transport http braincloud https://mcp.braincloudservers.com/mcp
```

Then start the agent and run `/mcp` to complete the brainCloud sign-in in your browser. Any
MCP-capable assistant works — the commands differ. With Claude Code, scope resolves per
**git repository**, so registering from inside `BCClashers` covers every example here; add
`-s user` to make it available everywhere.

## Optional: give your agent access to the Unity Editor

This is **not required** for the above, and is worth doing only if you want your assistant to
manipulate scenes, assets, and the console directly. It needs Unity 6 with
`com.unity.ai.assistant`.

Unity ships its own MCP server, which works in the opposite direction: your agent becomes the
client, and the Unity Editor exposes tools to it. Between them sits a *relay* — a small
executable Unity installs to `~/.unity/relay/`. Your assistant launches the relay, which
bridges to the Editor, so the Editor must be running for these tools to respond.

1. In Unity, open **Edit ▸ Project Settings ▸ AI ▸ Unity MCP Server**.
2. Under **Integrations**, find **Claude Code** and select **Configure**.

Unity writes a `unity-mcp` entry into `~/.claude.json` at **user scope**, so every Claude Code
session on your machine will try to launch the relay. To confine it to this repository, remove
that entry and re-add it locally:

```bash
claude mcp remove unity-mcp -s user
claude mcp add unity-mcp -s local -- <relay-command> --mcp
```

Unity builds a separate relay per platform and architecture, so there's no single path to
paste. The **Example Configuration** box at the bottom of the Unity MCP Server settings page
shows the exact command for your machine — copy `command` from there, or use **Locate Server**
to reveal it on disk. After rescoping, the settings page may report `Not Configured` (it only
reads user scope) even though the server works; pressing **Configure** again restores the
user-scope entry.

## Troubleshooting

- **`Executable not found in $PATH: "npx"` / Server Status `FailedToStart`** — Unity can't
  find Node. Recheck **User Path** (step 2); if it still fails, fully quit and reopen the
  Editor so it re-reads `PATH`, then click **Refresh File and Servers**.
- **Browser sign-in doesn't complete / 401** — make sure you finished the browser
  authorization, and that you can reach `https://mcp.braincloudservers.com`. To start over,
  delete the cached credentials at `~/.mcp-auth` and refresh.
- **"No Builder-API-enabled team"** — the account you signed in as has no team with Builder
  API enabled. Enable it in the brainCloud portal, or sign in as the intended account.
- **Writes to a Live app are refused** — this is expected. Ask the Assistant to run
  `requestUnlock`; it returns a one-time link you confirm in the portal, which grants writes
  for a limited time. `lockApp` ends it.
