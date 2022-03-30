using UnityEngine;
using UnityEngine.Serialization;

public class SummonSelector : MonoBehaviour
{
    [FormerlySerializedAs("TroopTypeSelection")] public EnemyTypes EnemyTypeSelection;
    private SpawnController _spawnController;

    private void Awake()
    {
        _spawnController = FindObjectOfType<SpawnController>();
    }
    
    //Called from unity button
    public void OnTroopSelection() => _spawnController.TroopChange(EnemyTypeSelection);
}
