using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeWeapon : MonoBehaviour
{
    public int DamageAmount;
    private readonly string TroopTag = "Troop";
    private BaseTroop _myTroop;

    private void Awake()
    {
        _myTroop = transform.parent.GetComponent<BaseTroop>();
    }

    private void OnTriggerEnter(Collider other)
    {
        var damageable = other.GetComponent<IDamageable<int>>();

        if (damageable != null)
        {
            damageable.Damage(DamageAmount);
            var direction = (transform.position - other.transform.position).normalized;
            damageable.LaunchObject(-direction);
        }

        if (other.tag.Equals(TroopTag))
        {
            var troop = other.GetComponent<BaseTroop>();
            troop.IncomingAttacker(_myTroop);
        }

        //Debug.Log($"Hitting: {other.gameObject.name}");
    }
}
