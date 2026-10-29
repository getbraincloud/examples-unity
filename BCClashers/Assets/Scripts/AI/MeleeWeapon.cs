using UnityEngine;

// Damage is dealt in code by TroopAI while attacking (reliable). This component now only
// applies knockback when the weapon collider strikes an enemy troop. DamageAmount is still
// read by TroopAI to set its per-hit melee damage.
public class MeleeWeapon : MonoBehaviour
{
    public int DamageAmount;
    private readonly string _troopTag = "Troop";
    private TroopAI _myTroop;

    private void Awake()
    {
        _myTroop = GetComponentInParent<TroopAI>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!gameObject.activeSelf || _myTroop == null) return;

        var troop = other.GetComponent<TroopAI>();
        if (troop == null || troop.TeamID == _myTroop.TeamID) return;

        if (other.tag.Equals(_troopTag))
        {
            troop.IncomingAttacker(_myTroop);
            var direction = (transform.position - other.transform.position).normalized;
            troop.LaunchObject(-direction);
        }
    }
}
