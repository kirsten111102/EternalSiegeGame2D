
public interface IEnemyCombat
{
    float AttackRange { get; }

    void TryAttack(UnityEngine.Transform target);
}
