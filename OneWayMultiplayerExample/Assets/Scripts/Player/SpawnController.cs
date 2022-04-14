using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

[Serializable]
public class TroopTracker
{
    public EnemyTypes TroopType;
    public int SpawnedTroops;
    public int SpawnLimit;
    public SummonSelector SummonSelector;
}

public class SpawnController : MonoBehaviour
{
    public SpawnData SpawnData;
    public float OffsetY;
    
    private BaseTroop _objectToSpawn;

    private List<SummonSelector> troopSelectorList = new List<SummonSelector>();
    [SerializeField]
    private List<TroopTracker> troopList = new List<TroopTracker>();
    [SerializeField]
    private TroopTracker _troopSelected;

    private float _cooldown = 1;
    private float _timer;

    private const string _targetTag = "Target";

    private void Awake()
    {
        //Get our troop data that we can summon
        troopSelectorList = FindObjectsOfType<SummonSelector>().ToList();
        for (int i = 0; i < SpawnData.SpawnParametersList.Count; i++)
        {
            troopList.Add(new TroopTracker());
            troopList[i].SpawnLimit = SpawnData.SpawnParametersList[i].SpawnLimit;
            troopList[i].TroopType = SpawnData.SpawnParametersList[i].TroopType;
        }
        
        //Applying the troop data to UI elements
        for (int i = 0; i < troopList.Count; i++)
        {
            for (int t = 0; t < troopSelectorList.Count; t++)
            {
                if (troopList[i].TroopType == troopSelectorList[t].EnemyTypeSelection)
                {
                    troopList[i].SummonSelector = troopSelectorList[t];
                    troopList[i].SummonSelector.UpdateSpawnNumber(troopList[i].SpawnLimit);
                }
            }
            if (troopList[i].TroopType == EnemyTypes.Grunt)
            {
                _troopSelected = troopList[i];
            }
        }
    }

    //ToDo: Need to implement a cap for each troop to be spawned in a round. Then replenish that amount after round is finish so spawning can continue
    
    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("Fire1") && 
            _timer < Time.realtimeSinceStartup)
        {
            //Cant spawn more of this troop type
            if (_troopSelected.SpawnedTroops >= _troopSelected.SpawnLimit) return;
            
            RaycastHit rayInfo;
            var hit = Physics.Raycast(Camera.main.ScreenPointToRay(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0)), out rayInfo);
            
            if (hit &&
                !EventSystem.current.IsPointerOverGameObject() &&
                rayInfo.collider.tag.Equals(_targetTag))
            {
                _timer = Time.realtimeSinceStartup + _cooldown;
                _objectToSpawn = SpawnData.GetTroop(_troopSelected.TroopType);
                Vector3 spawnPoint = rayInfo.point;
                spawnPoint.y += OffsetY;
                var troop = Instantiate(_objectToSpawn, spawnPoint, Quaternion.identity);
                troop.AssignToTeam(0);
                _troopSelected.SpawnedTroops++;
                _troopSelected.SummonSelector.UpdateSpawnNumber(_troopSelected.SpawnLimit - _troopSelected.SpawnedTroops);
            }
        }
    }

    public void TroopChange(EnemyTypes troopChangedTo)
    {
        for (int i = 0; i < troopList.Count; i++)
        {
            if (troopList[i].TroopType == troopChangedTo)
            {
                _troopSelected = troopList[i];
                return;
            }
        }
    }

}
