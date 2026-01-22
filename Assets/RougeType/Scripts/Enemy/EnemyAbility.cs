// EnemyAbility.cs
using UnityEngine;

public abstract class EnemyAbility : MonoBehaviour
{
    protected Enemy enemy;

    public virtual void Initialize(Enemy attachedEnemy)
    {
        enemy = attachedEnemy;
    }

    public virtual void OnUpdate() { }
    public virtual void OnTakeDamage(ref int damage) { }
    public virtual void OnDie() { }
    public virtual void OnHitWall(Wall wall) { }
}
