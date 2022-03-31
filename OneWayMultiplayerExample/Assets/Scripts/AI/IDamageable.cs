
using UnityEngine;

public interface IDamageable<T>
{
    void Damage(T damageTaken);
    void Dead();

    void LaunchObject(Vector3 direction);
}
