using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SceneManagement : MonoBehaviour
{

    public GameObject cellPrefab;
    public GameObject pheromonePrefab;
    public GridManagement gridManagement;
    public CellManagement cellManagement;
    public PheromoneManagement pheromoneManagement;
    public DebugManagement debugManagement;
    public Transform cellsVisualParent;
    public Transform pherosVisualParent;

    bool cellsVisualised;
    bool pheromonesVisualised;

    List<List<List<int>>> cells;
    List<List<List<int>>> pheromones;
    List<List<List<GameObject>>> visualCells;
    List<List<List<GameObject>>> visualPheromones;

    private void Start() {
        cells = new List<List<List<int>>>();
        pheromones = new List<List<List<int>>>();
        visualCells = new List<List<List<GameObject>>>();
        visualPheromones = new List<List<List<GameObject>>>();
        cellManagement.gameObject.SetActive(false);
        pheromoneManagement.gameObject.SetActive(false);
        debugManagement.gameObject.SetActive(false);
    }
    
    public void ButtonGenerateGrid(Toggle t) {
        // Setup variables.
        gridManagement.SetWHD();
        cells = gridManagement.GenerateGrid();
        pheromones = gridManagement.GenerateGrid();
        if (t.isOn) {
            visualCells = gridManagement.GenerateVisualGrid(cellPrefab, cellsVisualParent);
            visualPheromones = gridManagement.GenerateVisualGrid(pheromonePrefab, pherosVisualParent);
            cellsVisualised = true;
            pheromonesVisualised = true;
        }

        // Disable UI elements after use.
        gridManagement.generateGrid.interactable = false;
        gridManagement.generateGrid.GetComponentInChildren<Text>().text = "Grid Generated";
        foreach (InputField field in gridManagement.coords) {
            field.interactable = false;
        }
        t.interactable = false;

        // Enable other UI elements.
        cellManagement.gameObject.SetActive(true);
        pheromoneManagement.gameObject.SetActive(true);
        debugManagement.gameObject.SetActive(true);
    }

    public void ChangeCell(int x, int y, int z, int value, bool visible) {
        cells[x][y][z] = value;
        if (cellsVisualised) {
            CellData cell = visualCells[x][y][z].GetComponent<CellData>();
            cell.ActivateMesh(visible);
        }
    }

    public void ButtonChangeCell(bool b) {
        int x = int.Parse(cellManagement.coords[0].text);
        int y = int.Parse(cellManagement.coords[1].text);
        int z = int.Parse(cellManagement.coords[2].text);
        if (x>=0 && x< gridManagement.width && y>=0 && y< gridManagement.height && z>=0 && z< gridManagement.depth) {
            if (b) ChangeCell(x, y, z, 1, b);
            else ChangeCell(x, y, z, 0, b);
        }
    }
    
    public void ChangePheromone(int x, int y, int z, int value, bool visible) {
        cells[x][y][z] = value;
        if (pheromonesVisualised) {
            PheromoneData phero = visualPheromones[x][y][z].GetComponent<PheromoneData>();
            phero.ActivateMesh(visible);
        }
    }

    public void ButtonChangePheromone(bool b) {
        int x = int.Parse(pheromoneManagement.coords[0].text);
        int y = int.Parse(pheromoneManagement.coords[1].text);
        int z = int.Parse(pheromoneManagement.coords[2].text);
        if (x>=0 && x< gridManagement.width && y>=0 && y< gridManagement.height && z>=0 && z< gridManagement.depth) {
            if (b) ChangePheromone(x, y, z, 1, b);
            else ChangePheromone(x, y, z, 0, b);
        }
    }

    public void DebugLogCells() {
        debugManagement.DebugLogBigOlList("Cells" ,cells);
    }

    public void DebugLogPheromones() {
        debugManagement.DebugLogBigOlList("Pheromones", pheromones);
    }

    

}
