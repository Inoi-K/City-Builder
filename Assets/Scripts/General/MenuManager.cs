using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour {
    [SerializeField] Text modeText;

    [SerializeField] GameObject infoPanel;
    [SerializeField] Text infoText;

    int selectedBuildingSize;

    void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            ResetModeText();
        }
    }

    public void Save() {
        GameManager.instance.SaveCity();
    }

    public void Load() {
        GameManager.instance.LoadCity();
    }

    public void OpenPanel(GameObject panel) {
        EventSystem.current.SetSelectedGameObject(panel);
        panel.SetActive(true);
    }

    public void Build(int sizeIndex) {
        GameManager.instance.StartBuilding(sizeIndex);
        modeText.text = "Строить";
    }

    public void MakeTerrain() {
        GameManager.instance.StartMakingTerrain();
        modeText.text = "Готовить";
    }

    public void ShowBuildingInfo() {
        infoText.text = $"Размер: {selectedBuildingSize}x{selectedBuildingSize}";
        infoText.gameObject.SetActive(true);
    }

    public void DeleteBuilding() {
        infoPanel.SetActive(false);
        GameManager.instance.DestroySelectedBuilding();
    }

    // Show info panel about the building
    public void SelectBuilding(Transform building) {
        selectedBuildingSize = Mathf.RoundToInt(building.localScale.x);
        infoPanel.transform.position = Camera.main.WorldToScreenPoint(building.position);
        infoText.gameObject.SetActive(false);
        infoPanel.SetActive(true);
    }

    public void ResetModeText() {
        modeText.text = "";
    }
}
