using UnityEngine;

public class GameManager : MonoBehaviour {
    public static GameManager instance { get; private set; }
    GameData data;
    
    [SerializeField] MapManager mapManager;
    [SerializeField] MenuManager menu;
    
    [SerializeField] LayerMask terrainMask;
    [SerializeField] LayerMask buildingMask;
    float rayDistance = 20;
    
    Transform selectedObject;
    
    MeshRenderer meshRenderer;
    [SerializeField] Material positiveMaterial;
    [SerializeField] Material negativeMaterial;
    float materialSwitchDelay = .3f;
    float baseHeight;
    int selectedSizeIndex;

    bool isBuilding;
    bool isMakingTerrain;

    void Awake() {
        if (instance != null) {
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            ResetModes();
        }
        
        if (isBuilding) {
            BuildingClick();
            BuildingDrag();
        } 
        else if (isMakingTerrain) {
            MakeTerrainClick();
        }
        else {
            Click();
        }
    }

    #region Building
    void BuildingClick() {
        if (Input.GetMouseButtonDown(0)) {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, rayDistance, terrainMask)) {
                if (mapManager.Build(selectedSizeIndex, hit.transform.position)) { // Can build
                    PositionSelectedObject();
                    ResetBuilding();
                }
                else { // Not all tiles are vacant in the relevant area
                    meshRenderer.material = negativeMaterial; // Signal about an unsuccessful build with a contrast color
                    Invoke(nameof(ResetMeshRenderer), materialSwitchDelay); // Reset to the default ghost color after a delay
                }
            }
        }
    }

    void ResetMeshRenderer() {
        if (meshRenderer != null)
            meshRenderer.material = positiveMaterial;
    }
    
    void BuildingDrag() {
        if (selectedObject == null)
            return;
        
        PositionSelectedObject(.25f);
    }

    // Position the selected object while dragging
    void PositionSelectedObject(float verticalOffset = 0f) {
        Vector3 position = new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.WorldToScreenPoint(selectedObject.position).z);
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(position);
        float horizontalOffset = Mathf.Max(1f, selectedSizeIndex) * .5f * mapManager.map.tileSize;
        selectedObject.position = new Vector3(worldPosition.x + horizontalOffset, baseHeight + verticalOffset, worldPosition.z + horizontalOffset);
    }

    public void StartBuilding(int sizeIndex) {
        selectedObject = mapManager.CreateBuilding(sizeIndex, 1f, mapManager.GetDirectionToCenter(sizeIndex, 1f));
        meshRenderer = selectedObject.GetComponent<MeshRenderer>();
        baseHeight = selectedObject.transform.position.y;
        selectedSizeIndex = sizeIndex;
        
        meshRenderer.material = positiveMaterial; // Set transparent "ghost" color
        
        isBuilding = true;
    }
    #endregion

    #region MakeTerrain
    void MakeTerrainClick() {
        if (Input.GetMouseButtonDown(0)) {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, rayDistance, terrainMask)) {
                mapManager.DryTerrain(hit.transform.gameObject);
            }
        }
    }

    public void StartMakingTerrain() {
        isMakingTerrain = true;
    }
    #endregion

    #region ClickInfo
    void Click() {
        if (Input.GetMouseButtonDown(0)) {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, rayDistance, buildingMask)) {
                selectedObject = hit.transform;
                menu.SelectBuilding(selectedObject);
            }
        }
    }

    public void DestroySelectedBuilding() {
        mapManager.DestroyBuilding(selectedObject);
    }
    #endregion

    void ResetModes() {
        ResetBuilding();
        ResetMakingTerrain();
    }

    void ResetBuilding() {
        if (isBuilding)
            Destroy(selectedObject.gameObject);
        selectedObject = null;
        Cursor.visible = true;
        isBuilding = false;
        menu.ResetModeText();
    }

    void ResetMakingTerrain() {
        isMakingTerrain = false;
    }

    public void SaveCity() {
        data = new GameData(mapManager.GetTileMap(), mapManager.map.outlinePercent, mapManager.map.tileSize, mapManager.GetBuildings());
        SaveLoadSystem.SaveData(data);
    }

    public void LoadCity() {
        data = SaveLoadSystem.LoadData();
        mapManager.LoadCity(data);
    }
}
