using UnityEngine;

public class TurretSlot : MonoBehaviour
{
    public GameObject currentTurret;
    public GameObject turretBuyPanelPrefab;    // Assign in Inspector
    public GameObject turretUpgradePanelPrefab; // Replace destroyOptionPrefab with this

    private GameObject currentPanel;

    void OnMouseDown()
    {
        if (currentPanel != null) return; // Panel already opened

        if (currentTurret == null)
        {
            // Show turret build panel
            currentPanel = Instantiate(turretBuyPanelPrefab, transform.position, Quaternion.identity);
            currentPanel.GetComponent<TurretBuyPanel>().Init(this);
        }
        else
        {
            // Show turret upgrade panel
            currentPanel = Instantiate(turretUpgradePanelPrefab, transform.position, Quaternion.identity);
            currentPanel.GetComponent<TurretUpgradePanel>().Init(currentTurret.GetComponent<Turret>(), this);
        }
    }

    public void BuildTurret(GameObject turretPrefab)
    {
        currentTurret = Instantiate(turretPrefab, transform.position, Quaternion.identity);
    }

    public void DestroyTurret()
    {
        if (currentTurret != null)
        {
            Destroy(currentTurret);
            currentTurret = null;
        }
    }

    public void ClosePanel()
    {
        if (currentPanel != null)
        {
            Destroy(currentPanel);
            currentPanel = null;
        }
    }
}
