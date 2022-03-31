using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeWeapon : MonoBehaviour
{
    public int DamageAmount;
    
    private void OnTriggerEnter(Collider other)
    {
        var damageable = other.GetComponent<IDamageable<int>>();

        if (damageable != null)
        {
            damageable.Damage(DamageAmount);
            var direction = (transform.position - other.transform.position).normalized;
            damageable.LaunchObject(-direction);
        }

        //Debug.Log($"Hitting: {other.gameObject.name}");
    }
}
