using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public GameObject chairsPanel;
    public GameObject tablesPanel;
    public GameObject sofasPanel;

    public FurnitureManager furnitureManager; // Reference to your existing placement script


    void Start()
    {
        chairsPanel.SetActive(false);
        tablesPanel.SetActive(false);
        sofasPanel.SetActive(false);
    }


    

    public void ShowChairPanel()
    {
        chairsPanel.SetActive(true);
        tablesPanel.SetActive(false);
        sofasPanel.SetActive(false);
    }

    public void ShowTablePanel()
    {
        chairsPanel.SetActive(false);
        tablesPanel.SetActive(true);
        sofasPanel.SetActive(false);
    }

    public void ShowSofaPanel()
    {
        chairsPanel.SetActive(false);
        tablesPanel.SetActive(false);
        sofasPanel.SetActive(true);
    }

    public void SelectObject(GameObject objectPrefab)
    {
        furnitureManager.SetSelectedPrefab(objectPrefab);
    }
}
