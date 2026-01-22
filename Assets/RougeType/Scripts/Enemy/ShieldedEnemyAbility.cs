// ShieldedEnemyAbility.cs

using UnityEngine;

public class ShieldedEnemyAbility : EnemyAbility
{
    public int shieldAmount = 5;

    public override void OnTakeDamage(ref int damage)
    {
        if (shieldAmount > 0)
        {
            int absorbed = Mathf.Min(damage, shieldAmount);
            shieldAmount -= absorbed;
            damage -= absorbed;
            Debug.Log($"Shield absorbed {absorbed} damage, remaining: {shieldAmount}");
        }
    }
}
