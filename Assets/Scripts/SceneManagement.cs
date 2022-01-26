using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using Newtonsoft.Json;

public class SceneManagement : MonoBehaviour
{

    public string fileName = "test";
    public Simulation sim;
    public GameObject cellPrefab;
    public GameObject pheromonePrefab;
    public GameObject floorPrefab;
    public GameObject agentPrefab;
    GridUI gridUI;
    CellUI cellUI;
    DebugUI debugUI;
    VisualUI visualUI;
    AgentUI agentUI;
    SimulationUI simulationUI;
    public Transform cellsVisualParent;
    public Transform pherosVisualParent;
    public Transform agentsVisualParent;

    bool cellsVisualised;
    bool pheromonesVisualised;
    bool agentsVisualised;

    
    public List<List<List<GameObject>>> visualCells;
    public List<List<List<GameObject>>> visualPheromones;
    public List<GameObject> visualAgents;
    

    private void Start() {
        visualCells = new List<List<List<GameObject>>>();
        visualPheromones = new List<List<List<GameObject>>>();

        gridUI = FindObjectOfType<GridUI>();
        cellUI = FindObjectOfType<CellUI>();
        debugUI = FindObjectOfType<DebugUI>();
        visualUI = FindObjectOfType<VisualUI>();
        agentUI = FindObjectOfType<AgentUI>();
        simulationUI = FindObjectOfType<SimulationUI>();

        ToggleExtraUI(false);
    }
    
    void ToggleExtraUI(bool b) {
        cellUI.gameObject.SetActive(b);
        debugUI.gameObject.SetActive(b);
        visualUI.gameObject.SetActive(b);
        agentUI.gameObject.SetActive(b);
        simulationUI.gameObject.SetActive(b);
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
            sim.agentLocations.Add(yList);
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
            // Limit spawning to the outer two rings of the grid.
            List<(int, int)> spawnLocations = new List<(int, int)>();
            for (int i = 0; i < gridUI.width; i++) {
                spawnLocations.Add((i, 0));
                spawnLocations.Add((i, 1));
                spawnLocations.Add((i, gridUI.depth - 2));
                spawnLocations.Add((i, gridUI.depth - 1));
            }

            if (gridUI.depth > 4) {
                for (int j = 2; j < gridUI.depth - 2; j++) {
                    spawnLocations.Add((0, j));
                    spawnLocations.Add((1, j));
                    spawnLocations.Add((gridUI.width - 2, j));
                    spawnLocations.Add((gridUI.width - 1, j));
                }
            }

            (int, int) newXZ = spawnLocations[Random.Range(0, spawnLocations.Count)];

            int x = newXZ.Item1;
            int y = 0;
            int z = newXZ.Item2;

            if (!sim.agentLocations[x][y][z]) {
                GameObject agentPrefab = Instantiate(this.agentPrefab, transform.position + new Vector3(x - 0.5f, y - 0.5f, z - 0.5f), transform.rotation, agentsVisualParent);
                BuilderAgent agent = agentPrefab.GetComponent<BuilderAgent>();
                agent.pos = new Vector3Int(x, y, z);
                visualAgents.Add(agentPrefab);
                sim.agents.Add(agent);
                sim.agentLocations[x][y][z] = true;
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
        ToggleExtraUI(true);
    }
    
    public void ButtonSaveCurrentGridToFile() {
        WriteCurrentGridToFile($"results/{fileName}.json");
    }
    
    void WriteCurrentGridToFile(string filePath) {
        (List<List<List<int>>> , List<List<List<float>>>) data = (sim.cells, sim.pheromoneValues);
        string json = JsonConvert.SerializeObject(data);
        File.WriteAllText(filePath, json);
    }

    void ReadGridFromFile(string filePath) {
        // Read from file
        string json = File.ReadAllText(filePath);
        (List<List<List<int>>>, List<List<List<float>>>) data = JsonConvert.DeserializeObject<(List<List<List<int>>>, List<List<List<float>>>)>(json);
        sim.cells = data.Item1;
        sim.pheromoneValues = data.Item2;

        gridUI.width = sim.cells.Count;
        gridUI.height = sim.cells[0].Count;
        gridUI.depth = sim.cells[0][0].Count;
    }

    void GenerateGrid(Toggle visualToggle, bool useFile) {
        // Setup variables.
        if (useFile) {
            ReadGridFromFile($"results/{fileName}.json");
        } else {
            if (!gridUI.coordsValid()) return;
            gridUI.SetWHD();
            sim.cells = gridUI.GenerateEmptyGridOfInts();
            sim.pheromoneValues = gridUI.GenerateEmptyGridOfFloats();
        }
        GenerateVisualGrid();
        sim.UpdateLocalPheroValuesFromSimList();
        if (visualToggle.isOn) {
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
        sim.cells[x][y][z] = value;
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

    public void ChangePheromone(int x, int y, int z, float value) {
        sim.pheromoneValues[x][y][z] = value;
        //TODO: 
    }

    public void DebugLogCells() {
        debugUI.DebugLogBigOlListOfInts("Cells", sim.cells);
    }

    public void DebugLogPheromones() {
        debugUI.DebugLogBigOlListOfFloats("Pheromones", sim.pheromoneValues);
    }

    void visualiseCells() {
        if (cellsVisualised) {
            for (int x = 0; x < gridUI.width; x++) {
                for (int y = 0; y < gridUI.height; y++) {
                    for (int z = 0; z < gridUI.depth; z++) {
                        if (sim.cells[x][y][z] != 0) visualCells[x][y][z].GetComponent<CellData>().ActivateMesh(true);
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
                        if (sim.pheromoneValues[x][y][z] != 0) visualPheromones[x][y][z].GetComponent<Pheromone>().ActivateMesh(true);
                    }
                }
            }
        } else {
            for (int x = 0; x < gridUI.width; x++) {
                for (int y = 0; y < gridUI.height; y++) {
                    for (int z = 0; z < gridUI.depth; z++) {
                        visualPheromones[x][y][z].GetComponent<Pheromone>().ActivateMesh(false);
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
