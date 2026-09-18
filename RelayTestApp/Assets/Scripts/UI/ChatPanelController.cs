using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Scrollback + input + send, parameterized by Kind so one component serves both
/// global and lobby chat. Self-polls StateManager's history each frame instead of needing an
/// explicit refresh call.</summary>
public class ChatPanelController : MonoBehaviour
{
    public enum ChatKind { Global, Lobby }

    public ChatKind Kind;
    public TMP_Text ScrollText;
    public TMP_InputField InputField;
    public Button SendButton;

    private int _lastRenderedCount = -1;

    private void OnEnable()
    {
        _lastRenderedCount = -1; // force a render on the next Update, even if history is unchanged
        SendButton.onClick.AddListener(OnSendClicked);
        if (InputField != null)
            InputField.onSubmit.AddListener(OnInputSubmit);
        if (Kind == ChatKind.Global)
            BrainCloudManager.Instance.ConnectGlobalChat();
    }

    private void OnDisable()
    {
        SendButton.onClick.RemoveListener(OnSendClicked);
        if (InputField != null)
            InputField.onSubmit.RemoveListener(OnInputSubmit);
    }

    private void Update()
    {
        var history = Kind == ChatKind.Global ? StateManager.Instance.GlobalChatHistory : StateManager.Instance.LobbyChatHistory;
        if (history.Count == _lastRenderedCount) return;
        _lastRenderedCount = history.Count;
        Render(history);
    }

    private void Render(List<ChatMessage> history)
    {
        if (ScrollText == null) return;
        string myCxId = BrainCloudManager.Instance.Wrapper.Client.RTTConnectionID;

        var sb = new StringBuilder();
        foreach (var m in history)
        {
            bool isMe = !string.IsNullOrEmpty(m.FromCxId) && m.FromCxId == myCxId;
            // "#59ff73" (me) / "#99bfff" (others) match the cpp reference client's chat name colours exactly.
            string colour = isMe ? "#59ff73" : "#99bfff";
            sb.Append("<color=").Append(colour).Append('>').Append(m.FromName).Append(":</color> ").AppendLine(m.Text);
        }
        ScrollText.text = sb.ToString();
    }

    private void OnInputSubmit(string _) => Send();
    private void OnSendClicked() => Send();

    private void Send()
    {
        if (InputField == null) return;
        string text = InputField.text.Trim();
        if (string.IsNullOrEmpty(text)) return;
        InputField.text = "";

        if (Kind == ChatKind.Global)
            BrainCloudManager.Instance.SendGlobalChatMessage(text);
        else
            BrainCloudManager.Instance.SendLobbyChatMessage(text);
    }
}
