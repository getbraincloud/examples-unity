using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public enum TroopTypes{Grunt, GruntSolder, GruntMarader}

public class SpawnController : MonoBehaviour
{
    public SpawnData SpawnData;
    public float OffsetY;
    
    private BaseTroop _objectToSpawn;

    private float _cooldown = 1;
    private float _timer;

    private TroopTypes _troopSelected = TroopTypes.Grunt;

    private const string _targetTag = "Target";
    
    //ToDo: Need to implement a cap for each troop to be spawned in a round. Then replenish that amount after round is finish so spawning can continue
    
    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("Fire1") && 
            _timer < Time.realtimeSinceStartup)
        {
            RaycastHit rayInfo;
            var hit = Physics.Raycast(Camera.main.ScreenPointToRay(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0)), out rayInfo);
            
            if (hit &&
                !EventSystem.current.IsPointerOverGameObject() &&
                rayInfo.collider.tag.Equals(_targetTag))
            {
                _timer = Time.realtimeSinceStartup + _cooldown;
                _objectToSpawn = SpawnData.GetTroop(_troopSelected);
                Vector3 spawnPoint = rayInfo.point;
                spawnPoint.y += OffsetY;
                var troop = Instantiate(_objectToSpawn, spawnPoint, Quaternion.identity);
                troop.AssignToTeam(0);
            }
        }
    }

    public void TroopChange(TroopTypes troopChangedTo) => _troopSelected = troopChangedTo;

}
