using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeWeapon : MonoBehaviour
{
    public float DamageAmount;
    
    private void OnTriggerEnter(Collider other)
    {
        var damageable = other.GetComponent<IDamageable<float>>();

        if (damageable != null)
        {
            damageable.Damage(DamageAmount);
        }

        Debug.Log($"Hitting: {other.gameObject.name}");
    }
}
