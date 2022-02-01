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
    public GameObject queenPrefab;
    GridUI gridUI;
    CellUI cellUI;
    public DebugUI debugUI;
    VisualUI visualUI;
    AgentUI agentUI;
    SimulationUI simulationUI;
    public Transform cellsVisualParent;
    public Transform pherosVisualParent;
    public Transform agentsVisualParent;

    public bool cellsVisualised;
    public bool pheromonesVisualised;
    public bool agentsVisualised;

    
    public List<List<List<VisualCell>>> visualCells;
    public List<List<List<VisualPheromone>>> visualPheromones;
    public List<List<List<VisualAgent>>> visualAgents;
    

    private void Start() {
        visualCells = new List<List<List<VisualCell>>>();
        visualPheromones = new List<List<List<VisualPheromone>>>();
        visualAgents = new List<List<List<VisualAgent>>>();

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
        visualUI.gameObject.SetActive(b);
        agentUI.gameObject.SetActive(b);
        debugUI.gameObject.SetActive(b);
        simulationUI.gameObject.SetActive(b);
    }

    public List<List<List<VisualCell>>> GenerateVisualGridOfCells(GameObject prefab, Transform parent) {
        List<List<List<VisualCell>>> returnList = new List<List<List<VisualCell>>>();
        for (int x = 0; x < sim.gridDims.x; x++) {
            List<List<VisualCell>> ys = new List<List<VisualCell>>();
            for (int y = 0; y < sim.gridDims.y; y++) {
                List<VisualCell> zs = new List<VisualCell>();
                for (int z = 0; z < sim.gridDims.z; z++) {
                    GameObject obj = Instantiate(prefab, parent.position + new Vector3(x, y, z), parent.rotation, parent);
                    VisualCell cell = obj.GetComponent<VisualCell>();
                    zs.Add(cell);
                }
                ys.Add(zs);
            }
            returnList.Add(ys);
        }
        return returnList;
    }

    public List<List<List<VisualAgent>>> GenerateVisualGridOfAgents(GameObject prefab, Transform parent) {
        List<List<List<VisualAgent>>> returnList = new List<List<List<VisualAgent>>>();
        for (int x = 0; x < sim.gridDims.x; x++) {
            List<List<VisualAgent>> ys = new List<List<VisualAgent>>();
            for (int y = 0; y < sim.gridDims.y; y++) {
                List<VisualAgent> zs = new List<VisualAgent>();
                for (int z = 0; z < sim.gridDims.z; z++) {
                    GameObject obj = Instantiate(prefab, parent.position + new Vector3(x, y, z), parent.rotation, parent);
                    VisualAgent agent = obj.GetComponent<VisualAgent>();
                    zs.Add(agent);
                }
                ys.Add(zs);
            }
            returnList.Add(ys);
        }
        return returnList;
    }

    public List<List<List<VisualPheromone>>> GenerateVisualGridOfPheromones(GameObject prefab, Transform parent) {
        List<List<List<VisualPheromone>>> returnList = new List<List<List<VisualPheromone>>>();
        for (int x = 0; x < sim.gridDims.x; x++) {
            List<List<VisualPheromone>> ys = new List<List<VisualPheromone>>();
            for (int y = 0; y < sim.gridDims.y; y++) {
                List<VisualPheromone> zs = new List<VisualPheromone>();
                for (int z = 0; z < sim.gridDims.z; z++) {
                    GameObject obj = Instantiate(prefab, parent.position + new Vector3(x, y, z), parent.rotation, parent);
                    VisualPheromone pheromone = obj.GetComponent<VisualPheromone>();
                    zs.Add(pheromone);
                }
                ys.Add(zs);
            }
            returnList.Add(ys);
        }
        return returnList;
    }

    public void ButtonAddAgents(InputField noOfAgentsInputField) {
        int n;
        bool valid = int.TryParse(noOfAgentsInputField.text, out n);
        if (valid) {
            if (n > sim.gridDims.x * sim.gridDims.y * sim.gridDims.z) {
                Debug.Log("Can't add more agents than spaces in grid.");
                return;
            }
            sim.AddAgents(n);
            agentUI.addAgentsButton.interactable = false;
            noOfAgentsInputField.interactable = false;
        }
    }

    void GenerateVisualGrid() {
        GameObject floor = Instantiate(floorPrefab, transform.position, transform.rotation);
        floor.GetComponent<Floor>().SetScale(sim.gridDims.x, sim.gridDims.y, sim.gridDims.z);

        //visualCells = GenerateVisualGridOfCells(cellPrefab, cellsVisualParent);
        //visualPheromones = GenerateVisualGridOfPheromones(pheromonePrefab, pherosVisualParent);
        //visualAgents = GenerateVisualGridOfAgents(agentPrefab, agentsVisualParent);

        for (int x = 0; x < sim.gridDims.x; x++) {
            List<List<VisualCell>> ys = new List<List<VisualCell>>();
            for (int y = 0; y < sim.gridDims.y; y++) {
                List<VisualCell> zs = new List<VisualCell>();
                for (int z = 0; z < sim.gridDims.z; z++) {
                    zs.Add(null);
                }
                ys.Add(zs);
            }
            visualCells.Add(ys);
        }

        for (int x = 0; x < sim.gridDims.x; x++) {
            List<List<VisualPheromone>> ys = new List<List<VisualPheromone>>();
            for (int y = 0; y < sim.gridDims.y; y++) {
                List<VisualPheromone> zs = new List<VisualPheromone>();
                for (int z = 0; z < sim.gridDims.z; z++) {
                    zs.Add(null);
                }
                ys.Add(zs);
            }
            visualPheromones.Add(ys);
        }

        for (int x = 0; x < sim.gridDims.x; x++) {
            List<List<VisualAgent>> ys = new List<List<VisualAgent>>();
            for (int y = 0; y < sim.gridDims.y; y++) {
                List<VisualAgent> zs = new List<VisualAgent>();
                for (int z = 0; z < sim.gridDims.z; z++) {
                    zs.Add(null);
                }
                ys.Add(zs);
            }
            visualAgents.Add(ys);
        }

        cellsVisualised = true;
        pheromonesVisualised = true;
        agentsVisualised = true;
    }
    
    public void ButtonSaveCurrentGridToFile() {
        WriteCurrentGridToFile($"results/{fileName}.json");
    }
    
    void WriteCurrentGridToFile(string filePath) {
        (List<List<List<int>>> , List<List<List<float>>>, List<List<List<int>>>) data = (sim.cellValues, sim.pheromoneValues, sim.agentValues);
        string json = JsonConvert.SerializeObject(data);
        File.WriteAllText(filePath, json);
    }

    void ReadGridFromFile(string filePath) {
        // Read from file
        string json = File.ReadAllText(filePath);
        (List<List<List<int>>>, List<List<List<float>>>, List<List<List<int>>>) data = JsonConvert.DeserializeObject<(List<List<List<int>>>, List<List<List<float>>>, List<List<List<int>>>)>(json);
        sim.cellValues = data.Item1;
        sim.pheromoneValues = data.Item2;
        sim.agentValues = data.Item3;

        sim.gridDims.x = sim.cellValues.Count;
        sim.gridDims.y = sim.cellValues[0].Count;
        sim.gridDims.z = sim.cellValues[0][0].Count;
    }

    public bool coordsValid() {
        bool allValid = true;
        int n;
        foreach (InputField coord in gridUI.coords) {
            if (allValid) allValid = int.TryParse(coord.text, out n);
        }
        return allValid;
    }

    void GenerateGrid(Toggle visualToggle, bool useFile) {
        // Setup variables.
        if (useFile) {
            ReadGridFromFile($"results/{fileName}.json");
        } else {
            if (!coordsValid()) return;
            sim.SetWHD();
            sim.cellValues = sim.GenerateEmptyGridOfInts();
            sim.pheromoneValues = sim.GenerateEmptyGridOfFloats();
        }
        sim.pastPheromoneValues = sim.GenerateEmptyGridOfFloats();

        ToggleExtraUI(true);
        if (visualToggle.isOn) {
            sim.sceneVisualised = true;
            GenerateVisualGrid();
            VisualiseCells();
            VisualisePheros();
            VisualiseAgents();
        }

        sim.agentValues = sim.GenerateEmptyGridOfInts();

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
        sim.cellValues[x][y][z] = value;
        if (cellsVisualised) {
            VisualiseCells();
        }
    }

    public void ButtonChangeCell(bool b) {
        if (!cellUI.coordsValid()) return; 
        int x = int.Parse(cellUI.coords[0].text);
        int y = int.Parse(cellUI.coords[1].text);
        int z = int.Parse(cellUI.coords[2].text);
        if (x>=0 && x< sim.gridDims.x && y>=0 && y< sim.gridDims.y && z>=0 && z< sim.gridDims.z) {
            if (b) ChangeCell(x, y, z, 1, b);
            else ChangeCell(x, y, z, 0, b);
        }
    }

    public void ChangePheromone(int x, int y, int z, float value) {
        sim.pheromoneValues[x][y][z] = value;
        //TODO:
    }

    public void DebugLogCells() {
        debugUI.DebugLogBigOlListOfInts("Cells", sim.cellValues);
    }

    public void DebugLogAgents() {
        debugUI.DebugLogBigOlListOfInts("Agents", sim.agentValues);
    }

    public void DebugLogPheromones() {
        debugUI.DebugLogBigOlListOfFloats("Pheromones", sim.pheromoneValues);
    }

    public void VisualiseCells() {
        for (int x = 0; x < sim.gridDims.x; x++) {
            for (int y = 0; y < sim.gridDims.y; y++) {
                for (int z = 0; z < sim.gridDims.z; z++) {
                    if (cellsVisualised) {
                        if (sim.cellValues[x][y][z] != 0) {
                            if (visualCells[x][y][z] == null) {
                                GameObject obj = Instantiate(cellPrefab, cellsVisualParent.position + new Vector3(x, y, z), cellsVisualParent.rotation, cellsVisualParent);
                                VisualCell cell = obj.GetComponent<VisualCell>();
                                visualCells[x][y][z] = cell;
                            }
                            visualCells[x][y][z].ActivateMesh(true);
                        } else {
                            if (visualCells[x][y][z] != null) visualCells[x][y][z].ActivateMesh(false);
                        }
                    } else {
                        if (visualCells[x][y][z] != null) visualCells[x][y][z].ActivateMesh(false);
                    }
                }
            }
        }
    }

    public void ToggleCells() {
        cellsVisualised = !cellsVisualised;
        VisualiseCells();
        Text buttonText = visualUI.showCells.GetComponentInChildren<Text>();
        if (cellsVisualised) buttonText.text = "Hide Cells";
        else buttonText.text = "Show Cells";
    }

    public void VisualisePheros() {
        for (int x = 0; x < sim.gridDims.x; x++) {
            for (int y = 0; y < sim.gridDims.y; y++) {
                for (int z = 0; z < sim.gridDims.z; z++) {
                    if (pheromonesVisualised) {
                        if (sim.pheromoneValues[x][y][z] > 0.01f) {
                            if (visualPheromones[x][y][z] == null) {
                                GameObject obj = Instantiate(pheromonePrefab, pherosVisualParent.position + new Vector3(x, y, z), pherosVisualParent.rotation, pherosVisualParent);
                                VisualPheromone phero = obj.GetComponent<VisualPheromone>();
                                visualPheromones[x][y][z] = phero;
                            }
                            visualPheromones[x][y][z].ActivateMesh(true);
                            visualPheromones[x][y][z].SetAlpha(sim.pheromoneValues[x][y][z]);
                        } else {
                            if (visualPheromones[x][y][z] != null) visualPheromones[x][y][z].ActivateMesh(false);
                        }
                    } else {
                        if (visualPheromones[x][y][z] != null) visualPheromones[x][y][z].ActivateMesh(false);
                    }
                }
            }
        }
    }

    public void TogglePheros() {
        pheromonesVisualised = !pheromonesVisualised;
        VisualisePheros();
        Text buttonText = visualUI.showPheros.GetComponentInChildren<Text>();
        if (pheromonesVisualised) buttonText.text = "Hide Pheromones";
        else buttonText.text = "Show Pheromones";
    }

    public void VisualiseAgents() {
        for (int x = 0; x < sim.gridDims.x; x++) {
            for (int y = 0; y < sim.gridDims.y; y++) {
                for (int z = 0; z < sim.gridDims.z; z++) {
                    if (sim.agentValues.Count != 0) {
                        if (agentsVisualised) {
                            if (sim.agentValues[x][y][z] != 0) {
                                if (visualAgents[x][y][z] == null) {
                                    GameObject obj = Instantiate(agentPrefab, agentsVisualParent.position + new Vector3(x, y, z), agentsVisualParent.rotation, agentsVisualParent);
                                    VisualAgent agent = obj.GetComponent<VisualAgent>();
                                    visualAgents[x][y][z] = agent;
                                }
                                visualAgents[x][y][z].ActivateMesh(true);
                            } else {
                                if (visualAgents[x][y][z] != null) visualAgents[x][y][z].ActivateMesh(false);
                            }
                        } else {
                            if (visualAgents[x][y][z] != null) visualAgents[x][y][z].ActivateMesh(false);
                        }
                    } 
                }
            }
        }
    }

    public void ToggleAgents() {
        agentsVisualised = !agentsVisualised;
        VisualiseAgents();
        Text buttonText = visualUI.showAgents.GetComponentInChildren<Text>();
        if (agentsVisualised) buttonText.text = "Hide Agents";
        else buttonText.text = "Show Agents";
    }

}
