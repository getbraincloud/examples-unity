
public interface IPrimaryAction
{
    void PerformAction();
}

public interface IDamageable<T>
{
    void Damage(T damageTaken);
    void Dead();
}
