using UnityEngine;
using UnityEngine.UI;

public class FindingRoomUI : MonoBehaviour
{
    // Reference to the text UI displaying elapsed time
    public Text timeSearchingText;

    private float elapsedTime = 0f;
    private int lastDisplayedSeconds = 0;

    public void QuickFindLobby()
    {
        BCManager.LobbyManager.QuickFindLobby();
    }
    public void FindLobby()
    {
        BCManager.LobbyManager.FindLobby();
    }
    public void CancelFind()
    {
        BCManager.LobbyManager.CancelFind();
    }

    void OnEnable()
    {
        ResetTimer();
    }

    void Update()
    {
        // Accumulate elapsed time
        elapsedTime += Time.deltaTime;

        // Convert to integer seconds
        int seconds = Mathf.FloorToInt(elapsedTime);

        // Only update the UI when a new second passes
        if (seconds != lastDisplayedSeconds)
        {
            lastDisplayedSeconds = seconds;
            timeSearchingText.text = seconds.ToString();
        }
    }

    private void ResetTimer()
    {
        lastDisplayedSeconds = 0;
        elapsedTime = 0f;
        if (timeSearchingText != null)
            timeSearchingText.text = "0";
    }
}