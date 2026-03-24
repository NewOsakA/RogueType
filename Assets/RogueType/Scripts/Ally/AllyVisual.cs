using System.Collections;
using UnityEngine;

public class AllyVisual : MonoBehaviour
{
    [Header("Render")]
    public SpriteRenderer spriteRenderer;
    public string sortingLayerName = "Enemy";
    public int sortingOrderOffset = 25;
    public bool flipX = false;

    [Header("Animation")]
    public Sprite[] idleFrames;
    public float idleFrameRate = 8f;
    public Sprite[] shootFrames;
    public float shootFrameRate = 14f;

    private Coroutine idleCoroutine;
    private Coroutine shootCoroutine;

    void OnEnable()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        ApplyFacing();
        PlayIdle();
    }

    public void SetSorting(Vector3 worldPosition)
    {
        if (spriteRenderer == null)
            return;

        spriteRenderer.sortingLayerName = sortingLayerName;
        spriteRenderer.sortingOrder = 5000 - Mathf.RoundToInt(worldPosition.y * 100) + sortingOrderOffset;
    }

    public void PlayIdle()
    {
        if (!isActiveAndEnabled)
            return;

        if (shootCoroutine != null)
        {
            StopCoroutine(shootCoroutine);
            shootCoroutine = null;
        }

        if (idleCoroutine == null)
            idleCoroutine = StartCoroutine(LoopFrames(idleFrames, Mathf.Max(0.01f, idleFrameRate), true));
    }

    public void PlayShoot()
    {
        if (!isActiveAndEnabled)
            return;

        if (shootCoroutine != null)
            StopCoroutine(shootCoroutine);

        shootCoroutine = StartCoroutine(PlayShootRoutine());
    }

    private IEnumerator PlayShootRoutine()
    {
        if (idleCoroutine != null)
        {
            StopCoroutine(idleCoroutine);
            idleCoroutine = null;
        }

        yield return PlayFramesOnce(shootFrames, Mathf.Max(0.01f, shootFrameRate));

        shootCoroutine = null;
        PlayIdle();
    }

    private IEnumerator LoopFrames(Sprite[] frames, float frameRate, bool loop)
    {
        if (spriteRenderer == null || frames == null || frames.Length == 0)
            yield break;

        float delay = 1f / frameRate;

        do
        {
            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i] != null)
                    spriteRenderer.sprite = frames[i];

                yield return new WaitForSeconds(delay);
            }
        }
        while (loop);
    }

    private IEnumerator PlayFramesOnce(Sprite[] frames, float frameRate)
    {
        if (spriteRenderer == null || frames == null || frames.Length == 0)
            yield break;

        float delay = 1f / frameRate;
        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i] != null)
                spriteRenderer.sprite = frames[i];

            yield return new WaitForSeconds(delay);
        }
    }

    private void ApplyFacing()
    {
        if (spriteRenderer != null)
            spriteRenderer.flipX = flipX;
    }
}
