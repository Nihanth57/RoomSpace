using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecorUIManager : MonoBehaviour
{
    public GameObject paintingsPanel;
    public GameObject lampsPanel;
    public GameObject doorsWindowsPanel;

    public ARWallObjectManager arWallObjectManager; // Reference to the wall placement script

    void Start()
    {
        // Start with all panels hidden
        paintingsPanel.SetActive(false);
        lampsPanel.SetActive(false);
        doorsWindowsPanel.SetActive(false);
    }

    public void ShowPaintingsPanel()
    {
        paintingsPanel.SetActive(true);
        lampsPanel.SetActive(false);
        doorsWindowsPanel.SetActive(false);
    }

    public void ShowLampsPanel()
    {
        paintingsPanel.SetActive(false);
        lampsPanel.SetActive(true);
        doorsWindowsPanel.SetActive(false);
    }

    public void ShowDoorsWindowsPanel()
    {
        paintingsPanel.SetActive(false);
        lampsPanel.SetActive(false);
        doorsWindowsPanel.SetActive(true);
    }

    public void SelectObject(GameObject objectPrefab)
    {
        arWallObjectManager.SwitchDecoration(objectPrefab);
    }
}
