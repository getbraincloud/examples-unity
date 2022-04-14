using UnityEngine;
using UnityEngine.Serialization;
using TMPro;
public class SummonSelector : MonoBehaviour
{
    public EnemyTypes EnemyTypeSelection;
    private SpawnController _spawnController;
    private int remainingSpawnCount;
    public TMP_Text SpawnLimitText; 

    private void Awake()
    {
        _spawnController = FindObjectOfType<SpawnController>();
    }

    public void UpdateSpawnNumber(int currentLimit)
    {
        remainingSpawnCount = currentLimit;
        SpawnLimitText.text = remainingSpawnCount.ToString();
    }
    
    //Called from unity button
    public void OnTroopSelection() => _spawnController.TroopChange(EnemyTypeSelection);
}
