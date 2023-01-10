using UnityEngine;

public class MeleeWeapon : MonoBehaviour
{
    public int DamageAmount;
    private readonly string TroopTag = "Troop";
    private TroopAI _myTroop;

    private void Awake()
    {
        _myTroop = transform.parent.GetComponent<TroopAI>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!gameObject.activeSelf) return;
        
        var troop = other.GetComponent<TroopAI>();
        
        if (troop != null && troop.TeamID == _myTroop.TeamID) return;
        
        var damageable = other.GetComponent<BaseHealthBehavior>();
        if (damageable != null)
        {
            damageable.Damage(DamageAmount);
        }

        if (other.tag.Equals(TroopTag))
        {
            troop.IncomingAttacker(_myTroop);
            var direction = (transform.position - other.transform.position).normalized;
            troop.LaunchObject(-direction);
        }
    }
}
