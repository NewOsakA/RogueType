using UnityEngine;

public static class SkillEffects
{
    public static void ForcePush(float distance)
    {
        foreach (Enemy enemy in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            Debug.Log("ForcePush activated");
            enemy.PushBack(distance);
            enemy.ApplySlow(0.9f, 0.4f);
        }
    }

    public static void MassSlow(float duration)
    {
        Debug.Log("MassSlow activated");
        foreach (Enemy enemy in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            enemy.ApplySlow(0.6f, duration);
        }
    }

    public static void Shockwave(int damage)
    {
        Enemy[] enemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        Debug.Log("Shockwave activated");
        // Debug.Log($"Shockwave found {enemies.Length} enemies");

        foreach (Enemy enemy in enemies)
        {
            Debug.Log("Hit " + enemy.name);
            enemy.TakeDamage(damage);
        }
    }


    public static void TimeBreak(float duration)
    {
        Debug.Log("TimeBreak activated");
        foreach (Enemy enemy in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            enemy.ApplySlow(1f, duration); 
        }
    }
}
