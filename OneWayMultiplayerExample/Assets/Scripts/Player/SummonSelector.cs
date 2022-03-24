using UnityEngine;

public class SummonSelector : MonoBehaviour
{
    public TroopTypes TroopTypeSelection;
    private SpawnController _spawnController;

    private void Awake()
    {
        _spawnController = FindObjectOfType<SpawnController>();
    }
    
    //Called from unity button
    public void OnTroopSelection() => _spawnController.TroopChange(TroopTypeSelection);
}
