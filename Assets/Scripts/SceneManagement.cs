using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Text.Json;
using System.IO;

public class SceneManagement : MonoBehaviour
{

    public string fileName = "test";
    public GameObject cellPrefab;
    public GameObject pheromonePrefab;
    public GameObject basePrefab;
    GridUI gridUI;
    CellUI cellUI;
    PheromoneUI pheromoneUI;
    DebugUI debugUI;
    VisualUI visualUI;
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

        gridUI = FindObjectOfType<GridUI>();
        cellUI = FindObjectOfType<CellUI>();
        pheromoneUI = FindObjectOfType<PheromoneUI>();
        debugUI = FindObjectOfType<DebugUI>();
        visualUI = FindObjectOfType<VisualUI>();

        cellUI.gameObject.SetActive(false);
        pheromoneUI.gameObject.SetActive(false);
        debugUI.gameObject.SetActive(false);
        visualUI.gameObject.SetActive(false);
    }
    
    void GenerateVisualGrid() {
        GameObject floor = Instantiate(basePrefab, transform.position, transform.rotation);
        floor.GetComponent<Base>().SetScale(gridUI.width, gridUI.height, gridUI.depth);

        visualCells = gridUI.GenerateVisualGrid(cellPrefab, cellsVisualParent);
        visualPheromones = gridUI.GenerateVisualGrid(pheromonePrefab, pherosVisualParent);
        cellsVisualised = true;
        pheromonesVisualised = true;

        // Enable other UI elements.
        cellUI.gameObject.SetActive(true);
        pheromoneUI.gameObject.SetActive(true);
        debugUI.gameObject.SetActive(true);
        visualUI.gameObject.SetActive(true);
    }
    
    public void ButtonSaveCurrentGridToFile() {
        WriteCurrentGridToFile($"results/{fileName}.json");
    }
    
    void WriteCurrentGridToFile(string filePath) {
        List<List<List<List<int>>>> data = new List<List<List<List<int>>>> { cells, pheromones };
        string json = JsonSerializer.Serialize(data);
        File.WriteAllText(filePath, json);
    }

    void ReadGridFromFile(string filePath) {
        // Read from file
        string json = File.ReadAllText(filePath);
        List<List<List<List<int>>>> data = JsonSerializer.Deserialize<List<List<List<List<int>>>>>(json);
        cells = data[0];
        pheromones = data[1];


        gridUI.width = cells.Count;
        gridUI.height = cells[0].Count;
        gridUI.depth = cells[0][0].Count;
    }

    void GenerateGrid(Toggle visualToggle, bool useFile) {
        // Setup variables.
        if (useFile) {
            ReadGridFromFile($"results/{fileName}.json");
        } else {
            if (!gridUI.coordsValid()) return;
            gridUI.SetWHD();
            cells = gridUI.GenerateEmptyGrid();
            pheromones = gridUI.GenerateEmptyGrid();
        }
        if (visualToggle.isOn) {
            GenerateVisualGrid();
            visualiseCells();
            visualisePheros();
        }

        // Disable UI elements after use.
        gridUI.generateGrid.interactable = false;
        gridUI.generateFromFile.interactable = false;
        gridUI.generateGrid.GetComponentInChildren<Text>().text = "Grid Generated";
        foreach (InputField field in gridUI.coords) {
            field.interactable = false;
        }
        visualToggle.interactable = false;
    }

    public void ButtonGenerateFromFile(Toggle visualToggle) {
        GenerateGrid(visualToggle, true);
    } 

    public void ButtonGenerateGrid(Toggle visualToggle) {
        GenerateGrid(visualToggle, false);
    }

    public void ChangeCell(int x, int y, int z, int value, bool visible) {
        cells[x][y][z] = value;
        if (cellsVisualised) {
            CellData cell = visualCells[x][y][z].GetComponent<CellData>();
            cell.ActivateMesh(visible);
        }
    }

    public void ButtonChangeCell(bool b) {
        if (!cellUI.coordsValid()) return; 
        int x = int.Parse(cellUI.coords[0].text);
        int y = int.Parse(cellUI.coords[1].text);
        int z = int.Parse(cellUI.coords[2].text);
        if (x>=0 && x< gridUI.width && y>=0 && y< gridUI.height && z>=0 && z< gridUI.depth) {
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
        if (!pheromoneUI.coordsValid()) return;
        int x = int.Parse(pheromoneUI.coords[0].text);
        int y = int.Parse(pheromoneUI.coords[1].text);
        int z = int.Parse(pheromoneUI.coords[2].text);
        if (x>=0 && x< gridUI.width && y>=0 && y< gridUI.height && z>=0 && z< gridUI.depth) {
            if (b) ChangePheromone(x, y, z, 1, b);
            else ChangePheromone(x, y, z, 0, b);
        }
    }

    public void DebugLogCells() {
        debugUI.DebugLogBigOlList("Cells" ,cells);
    }

    public void DebugLogPheromones() {
        debugUI.DebugLogBigOlList("Pheromones", pheromones);
    }

    void visualiseCells() {
        if (cellsVisualised) {
            for (int x = 0; x < gridUI.width; x++) {
                for (int y = 0; y < gridUI.height; y++) {
                    for (int z = 0; z < gridUI.depth; z++) {
                        if (cells[x][y][z] != 0) visualCells[x][y][z].GetComponent<CellData>().ActivateMesh(true);
                    }
                }
            }
        } else {
            for (int x = 0; x < gridUI.width; x++) {
                for (int y = 0; y < gridUI.height; y++) {
                    for (int z = 0; z < gridUI.depth; z++) {
                        visualCells[x][y][z].GetComponent<CellData>().ActivateMesh(false);
                    }
                }
            }
        }
    }

    public void ToggleCells() {
        cellsVisualised = !cellsVisualised;
        visualiseCells();
        Text buttonText = visualUI.showCells.GetComponentInChildren<Text>();
        if (cellsVisualised) buttonText.text = "Hide Cells";
        else buttonText.text = "Show Cells";
    }

    void visualisePheros() {
        if (pheromonesVisualised) {
            for (int x = 0; x < gridUI.width; x++) {
                for (int y = 0; y < gridUI.height; y++) {
                    for (int z = 0; z < gridUI.depth; z++) {
                        if (pheromones[x][y][z] != 0) visualPheromones[x][y][z].GetComponent<PheromoneData>().ActivateMesh(true);
                    }
                }
            }
        } else {
            for (int x = 0; x < gridUI.width; x++) {
                for (int y = 0; y < gridUI.height; y++) {
                    for (int z = 0; z < gridUI.depth; z++) {
                        visualPheromones[x][y][z].GetComponent<PheromoneData>().ActivateMesh(false);
                    }
                }
            }
        }
    }

    public void TogglePheros() {
        pheromonesVisualised = !pheromonesVisualised;
        visualisePheros();
        Text buttonText = visualUI.showPheros.GetComponentInChildren<Text>();
        if (pheromonesVisualised) buttonText.text = "Hide Pheromones";
        else buttonText.text = "Show Pheromones";
    }

}
