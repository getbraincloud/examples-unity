using UnityEngine;
using TMPro;
public class SummonSelector : MonoBehaviour
{
    public EnemyTypes EnemyTypeSelection;
    private SpawnController _spawnController;
    private int remainingSpawnCount;
    public TMP_Text SpawnLimitText;
    [Tooltip("Radial-filled Image over the troop icon; darkens during deploy cooldown, clears when ready.")]
    public UnityEngine.UI.Image CooldownOverlay;

    private void Awake()
    {
        _spawnController = FindFirstObjectByType<SpawnController>();
    }

    public void UpdateSpawnNumber(int currentLimit)
    {
        remainingSpawnCount = currentLimit;
        SpawnLimitText.text = remainingSpawnCount.ToString();
    }
    
    //Called from unity button
    public void OnTroopSelection() => _spawnController.TroopChange(EnemyTypeSelection);

    /// <summary>remaining01: 1 = just deployed (fully darkened), 0 = ready (icon lit).</summary>
    public void SetCooldown(float remaining01)
    {
        if (CooldownOverlay == null) return;
        CooldownOverlay.fillAmount = remaining01;
        CooldownOverlay.enabled = remaining01 > 0.001f;
    }
}
