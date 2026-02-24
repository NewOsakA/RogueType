using UnityEngine;

public class SkillShopController : MonoBehaviour
{
    public GameObject skillShop;

    void Start()
    {
        if (skillShop != null)
            skillShop.SetActive(false);
    }

    public void Open()
    {
        if (!GameManager.Instance.IsBasePhase())
            return;

        skillShop.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Close()
    {
        skillShop.SetActive(false);
        Time.timeScale = 1f;
    }
}
