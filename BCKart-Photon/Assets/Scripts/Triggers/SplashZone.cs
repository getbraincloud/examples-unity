using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SplashZone : MonoBehaviour {
    public GameObject splashPrefab;

    private float _cooldown;

    private void OnTriggerStay(Collider other) {
        if ( other.TryGetComponent(out KartEntity kart) ) {
            if ( !kart.Object.HasInputAuthority ) return;
            if ( _cooldown > 0f ) return;

            Instantiate(splashPrefab, other.transform.position, other.transform.rotation);

            _cooldown = 5f;
        }
    }

    private void Update() {
        if ( _cooldown > 0f )
            _cooldown -= Time.deltaTime;
    }
}
