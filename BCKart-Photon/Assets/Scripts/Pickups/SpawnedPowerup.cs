using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// This is the object that physically sits in the world after being 'used' as an item.
/// </summary>
public abstract class SpawnedPowerup : NetworkBehaviour, ICollidable {
    [Networked] public NetworkBool HasInit { get; private set; }
    
    public virtual void Init(KartEntity spawner) { }

    public override void Spawned() {
        base.Spawned();

        HasInit = true;
    }

    public virtual bool Collide(KartEntity kart) {
        return false;
    }
}