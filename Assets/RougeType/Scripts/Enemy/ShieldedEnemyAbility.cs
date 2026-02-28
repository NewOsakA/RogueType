using UnityEngine;
using System.Collections;

public class ShieldedEnemyAbility : EnemyAbility
{
    [Header("Shield Settings")]
    public int shieldHits = 5; 

    [Header("Shield UI")]
    public GameObject shieldObject;

    private SpriteRenderer shieldRenderer;
    private Color originalColor;
    private Coroutine flashRoutine;

    public override void Initialize(Enemy enemy)
    {
        base.Initialize(enemy);

        if (shieldObject != null)
        {
            shieldRenderer = shieldObject.GetComponent<SpriteRenderer>();
            if (shieldRenderer != null)
                originalColor = shieldRenderer.color;
        }
    }

    public override void OnTakeDamage(ref int damage)
    {
        if (shieldHits <= 0)
            return;

        shieldHits--;
        damage = 0;

        if (flashRoutine != null)
            enemy.StopCoroutine(flashRoutine);

        flashRoutine = enemy.StartCoroutine(ShieldFlash());

        if (shieldHits <= 0 && shieldObject != null)
        {
            shieldObject.SetActive(false);
        }
    }

    IEnumerator ShieldFlash()
    {
        if (shieldRenderer == null)
            yield break;

        shieldRenderer.color = Color.white;

        yield return new WaitForSeconds(0.1f);

        if (shieldRenderer != null)
            shieldRenderer.color = originalColor;
    }
}