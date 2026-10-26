using System;
using UnityEngine;

/// <summary>
/// Live match countdown, shown on both the FFA and Team match screens. Reads
/// StateManager.MatchStartTimeMs (synced from the host via the "match_start" relay op) rather
/// than tracking its own local timer, so every client's countdown agrees regardless of small
/// per-client differences in when relay finished connecting.
/// Place on a Canvas object visible only while GameStates.Match is active.
/// </summary>
public class MatchTimerText : MonoBehaviour
{
    [SerializeField] TMPro.TMP_Text timerText;

    // Matches the cpp reference client's thresholds exactly (game.cpp TIMER_WARN_SEC/TIMER_URGENT_SEC).
    private const int WARN_SEC = 30;
    private const int URGENT_SEC = 10;
    private static readonly Color NormalColor = Color.white;
    private static readonly Color WarnColor = new Color(1.0f, 0.75f, 0.2f, 1.0f);
    private static readonly Color UrgentColor = new Color(1.0f, 0.3f, 0.3f, 1.0f);

    void Update()
    {
        if (timerText == null || StateManager.Instance == null) return;

        long startTimeMs = StateManager.Instance.MatchStartTimeMs;
        if (startTimeMs <= 0)
        {
            timerText.text = "--:--";
            timerText.color = NormalColor;
            return;
        }

        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long remainingMs = Math.Max(0, StateManager.MATCH_DURATION_MS - (now - startTimeMs));
        long remainingSec = (remainingMs + 999) / 1000;
        timerText.text = $"{remainingSec / 60}:{(remainingSec % 60):D2}";
        timerText.color = remainingSec <= URGENT_SEC ? UrgentColor : remainingSec <= WARN_SEC ? WarnColor : NormalColor;
    }
}
