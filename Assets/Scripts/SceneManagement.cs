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
    public GameObject floorPrefab;
    public GameObject agentPrefab;
    GridUI gridUI;
    CellUI cellUI;
    PheromoneUI pheromoneUI;
    DebugUI debugUI;
    VisualUI visualUI;
    AgentUI agentUI;
    public Transform cellsVisualParent;
    public Transform pherosVisualParent;
    public Transform agentsVisualParent;

    bool cellsVisualised;
    bool pheromonesVisualised;
    bool agentsVisualised;

    public List<List<List<int>>> cells;
    public List<List<List<int>>> pheromones;
    public List<List<List<bool>>> agentLocations;
    public List<List<List<GameObject>>> visualCells;
    public List<List<List<GameObject>>> visualPheromones;
    public List<GameObject> visualAgents;

    List<AgentBehaviour> agents;

    private void Start() {
        cells = new List<List<List<int>>>();
        pheromones = new List<List<List<int>>>();
        visualCells = new List<List<List<GameObject>>>();
        visualPheromones = new List<List<List<GameObject>>>();
        agentLocations = new List<List<List<bool>>>();
        agents = new List<AgentBehaviour>();

        gridUI = FindObjectOfType<GridUI>();
        cellUI = FindObjectOfType<CellUI>();
        pheromoneUI = FindObjectOfType<PheromoneUI>();
        debugUI = FindObjectOfType<DebugUI>();
        visualUI = FindObjectOfType<VisualUI>();
        agentUI = FindObjectOfType<AgentUI>();

        cellUI.gameObject.SetActive(false);
        pheromoneUI.gameObject.SetActive(false);
        debugUI.gameObject.SetActive(false);
        visualUI.gameObject.SetActive(false);
        agentUI.gameObject.SetActive(false);
    }
    
    void GenerateEmptyAgentLocations() {
        for (int x = 0; x < gridUI.width; x++) {
            List<List<bool>> yList = new List<List<bool>>();
            for (int y = 0; y < gridUI.height; y++) {
                List<bool> zList = new List<bool>();
                for (int z = 0; z < gridUI.depth; z++) {
                    zList.Add(false);
                }
                yList.Add(zList);
            }
            agentLocations.Add(yList);
        }
    }

    public void ButtonAddAgents(InputField noOfAgentsInputField) {
        int n;
        bool valid = int.TryParse(noOfAgentsInputField.text, out n);
        if (valid) {
            if (n > gridUI.width * gridUI.height * gridUI.depth) {
                Debug.Log("Can't add more agents than spaces in grid.");
                return;
            }
            AddAgents(n);
            agentUI.addAgentsButton.interactable = false;
            noOfAgentsInputField.interactable = false;
        }
    }

    void AddAgents(int amount) {
        int counter = 0;
        while (counter < amount) {
            int x = Random.Range(0, gridUI.width);
            int y = Random.Range(0, gridUI.height);
            int z = Random.Range(0, gridUI.depth);
            if (!agentLocations[x][y][z]) {
                GameObject agent = Instantiate(agentPrefab, transform.position + new Vector3(x - 0.5f, y - 0.5f, z - 0.5f), transform.rotation, agentsVisualParent);
                AgentBehaviour agentBehaviour = agent.GetComponent<AgentBehaviour>();
                agentBehaviour.pos = new Vector3Int(x, y, z);
                visualAgents.Add(agent);
                agents.Add(agentBehaviour);
                agentLocations[x][y][z] = true;
                counter++;
            }
        }
        visualiseAgents();
    }

    void GenerateVisualGrid() {
        GameObject floor = Instantiate(floorPrefab, transform.position, transform.rotation);
        floor.GetComponent<Floor>().SetScale(gridUI.width, gridUI.height, gridUI.depth);

        visualCells = gridUI.GenerateVisualGrid(cellPrefab, cellsVisualParent);
        visualPheromones = gridUI.GenerateVisualGrid(pheromonePrefab, pherosVisualParent);
        cellsVisualised = true;
        pheromonesVisualised = true;
        agentsVisualised = true;

        // Enable other UI elements.
        cellUI.gameObject.SetActive(true);
        pheromoneUI.gameObject.SetActive(true);
        debugUI.gameObject.SetActive(true);
        visualUI.gameObject.SetActive(true);
        agentUI.gameObject.SetActive(true);
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

        GenerateEmptyAgentLocations();

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

    void visualiseAgents() {
        foreach (GameObject agent in visualAgents) {
            agent.GetComponent<MeshRenderer>().enabled = agentsVisualised;
        }
    }

    public void ToggleAgents() {
        agentsVisualised = !agentsVisualised;
        visualiseAgents();
        Text buttonText = visualUI.showAgents.GetComponentInChildren<Text>();
        if (agentsVisualised) buttonText.text = "Hide Agents";
        else buttonText.text = "Show Agents";
    }

}
