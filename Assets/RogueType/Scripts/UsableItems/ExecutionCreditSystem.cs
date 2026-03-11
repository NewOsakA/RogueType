using UnityEngine;
using System.Collections;

public class ExecutionCreditSystem : MonoBehaviour
{
    public static ExecutionCreditSystem Instance;

    private bool isActive = false;
    private Coroutine activeCoroutine;

    private float currentDuration;
    private float currentBonusPercent;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Activate(float duration, float bonusPercent)
    {
        currentDuration = duration;
        currentBonusPercent = bonusPercent;

        if (activeCoroutine != null)
            StopCoroutine(activeCoroutine);

        activeCoroutine = StartCoroutine(ExecutionWindow());

        Debug.Log(
            $"[ExecutionCredit] Activated {duration}s | +" +
            $"{bonusPercent * 100}% reward"
        );
    }

    IEnumerator ExecutionWindow()
    {
        isActive = true;
        yield return new WaitForSeconds(currentDuration);
        isActive = false;

        Debug.Log("[ExecutionCredit] Ended");
    }

    public int ApplyBonus(int baseReward)
    {
        if (!isActive)
            return baseReward;

        int bonus = Mathf.RoundToInt(baseReward * currentBonusPercent);
        return baseReward + bonus;
    }

    public bool IsActive()
    {
        return isActive;
    }
}
