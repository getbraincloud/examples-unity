using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoostPowerup : SpawnedPowerup {
    public override void Init(KartEntity spawner) {
        base.Init(spawner);
        
        spawner.Controller.GiveBoost(false, 2);
        
        // Runner.Despawn(Object, true);
        // Destroy(gameObject);
    }

    public override void Spawned() {
        base.Spawned();

        Runner.Despawn(Object);
    }
}
