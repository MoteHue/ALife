using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SceneManagement : MonoBehaviour
{

    public GameObject cellPrefab;
    public GameObject pheromonePrefab;
    GridManagement gridManagement;
    CellManagement cellManagement;
    PheromoneManagement pheromoneManagement;
    DebugManagement debugManagement;
    VisualManagement visualManagement;
    public Transform cellsVisualParent;
    public Transform pherosVisualParent;

    bool cellsVisualised;
    bool pheromonesVisualised;

    public List<List<List<int>>> cells;
    public List<List<List<int>>> pheromones;
    public List<List<List<GameObject>>> visualCells;
    public List<List<List<GameObject>>> visualPheromones;

    private void Start() {
        cells = new List<List<List<int>>>();
        pheromones = new List<List<List<int>>>();
        visualCells = new List<List<List<GameObject>>>();
        visualPheromones = new List<List<List<GameObject>>>();

        gridManagement = FindObjectOfType<GridManagement>();
        cellManagement = FindObjectOfType<CellManagement>();
        pheromoneManagement = FindObjectOfType<PheromoneManagement>();
        debugManagement = FindObjectOfType<DebugManagement>();
        visualManagement = FindObjectOfType<VisualManagement>();

        cellManagement.gameObject.SetActive(false);
        pheromoneManagement.gameObject.SetActive(false);
        debugManagement.gameObject.SetActive(false);
        visualManagement.gameObject.SetActive(false);
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
        visualManagement.gameObject.SetActive(true);
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
        pheromones[x][y][z] = value;
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

    public void ToggleCells() {
        cellsVisualised = !cellsVisualised;
        if (cellsVisualised) {
            for (int x = 0; x < gridManagement.width; x++) {
                for (int y = 0; y < gridManagement.height; y++) {
                    for (int z = 0; z < gridManagement.depth; z++) {
                        if (cells[x][y][z] != 0) visualCells[x][y][z].GetComponent<CellData>().ActivateMesh(true);
                    }
                }
            }
        } else {
            for (int x = 0; x < gridManagement.width; x++) {
                for (int y = 0; y < gridManagement.height; y++) {
                    for (int z = 0; z < gridManagement.depth; z++) {
                        visualCells[x][y][z].GetComponent<CellData>().ActivateMesh(false);
                    }
                }
            }
        }
        Text buttonText = visualManagement.showCells.GetComponentInChildren<Text>();
        if (cellsVisualised) buttonText.text = "Hide Cells";
        else buttonText.text = "Show Cells";
    }

    public void TogglePheros() {
        pheromonesVisualised = !pheromonesVisualised;
        if (pheromonesVisualised) {
            for (int x = 0; x < gridManagement.width; x++) {
                for (int y = 0; y < gridManagement.height; y++) {
                    for (int z = 0; z < gridManagement.depth; z++) {
                        if (pheromones[x][y][z] != 0) visualPheromones[x][y][z].GetComponent<PheromoneData>().ActivateMesh(true);
                    }
                }
            }
        } else {
            for (int x = 0; x < gridManagement.width; x++) {
                for (int y = 0; y < gridManagement.height; y++) {
                    for (int z = 0; z < gridManagement.depth; z++) {
                        visualPheromones[x][y][z].GetComponent<PheromoneData>().ActivateMesh(false);
                    }
                }
            }
        }
        Text buttonText = visualManagement.showPheros.GetComponentInChildren<Text>();
        if (pheromonesVisualised) buttonText.text = "Hide Pheromones";
        else buttonText.text = "Show Pheromones";
    }

}
