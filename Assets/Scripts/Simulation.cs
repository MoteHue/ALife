using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Simulation : MonoBehaviour
{
    public SceneManagement sceneManagement;
    GridUI gridUI;
    public SimulationUI simulationUI;

    public bool runSimulation;

    public Vector3Int gridDims;

    public List<List<List<int>>> cellValues;
    public List<List<List<float>>> pheromoneValues;
    public List<List<List<int>>> agentValues;
    public List<List<List<float>>> pastPheromoneValues;

    int timestep = 0;
    float placementProbability = 0.1f;
    float alpha = 1f / 7f;
    float decay = 0.8f;

    public bool sceneVisualised;

    public void SetWHD() {
        gridDims.x = int.Parse(gridUI.coords[0].text);
        gridDims.y = int.Parse(gridUI.coords[1].text);
        gridDims.z = int.Parse(gridUI.coords[2].text);
    }

    private void Start() {
        cellValues = new List<List<List<int>>>();
        pheromoneValues = new List<List<List<float>>>();
        pastPheromoneValues = new List<List<List<float>>>();
        agentValues = new List<List<List<int>>>();
        gridUI = FindObjectOfType<GridUI>();
    }

    public void StartSimulation() {
        runSimulation = true;
        simulationUI.nextTimestepButton.interactable = false;
        StartCoroutine(nameof(Simulate));
    }

    public List<List<List<int>>> GenerateEmptyGridOfInts() {
        List<List<List<int>>> returnList = new List<List<List<int>>>();
        for (int x = 0; x < gridDims.x; x++) {
            List<List<int>> yList = new List<List<int>>();
            for (int y = 0; y < gridDims.y; y++) {
                List<int> zList = new List<int>();
                for (int z = 0; z < gridDims.z; z++) {
                    zList.Add(0);
                }
                yList.Add(zList);
            }
            returnList.Add(yList);
        }
        return returnList;
    }

    public List<List<List<float>>> GenerateEmptyGridOfFloats() {
        List<List<List<float>>> returnList = new List<List<List<float>>>();
        for (int x = 0; x < gridDims.x; x++) {
            List<List<float>> yList = new List<List<float>>();
            for (int y = 0; y < gridDims.y; y++) {
                List<float> zList = new List<float>();
                for (int z = 0; z < gridDims.z; z++) {
                    zList.Add(0);
                }
                yList.Add(zList);
            }
            returnList.Add(yList);
        }
        return returnList;
    }

    public void AddAgents(int amount) {
        int counter = 0;
        while (counter < amount) {
            // Limit spawning to the outer two rings of the grid.
            List<(int, int)> spawnLocations = new List<(int, int)>();
            for (int i = 0; i < gridDims.x; i++) {
                spawnLocations.Add((i, 0));
                spawnLocations.Add((i, 1));
                spawnLocations.Add((i, gridDims.z - 2));
                spawnLocations.Add((i, gridDims.z - 1));
            }

            if (gridDims.z > 4) {
                for (int j = 2; j < gridDims.z - 2; j++) {
                    spawnLocations.Add((0, j));
                    spawnLocations.Add((1, j));
                    spawnLocations.Add((gridDims.x - 2, j));
                    spawnLocations.Add((gridDims.x - 1, j));
                }
            }

            (int, int) newXZ = spawnLocations[Random.Range(0, spawnLocations.Count)];

            int x = newXZ.Item1;
            int y = 0;
            int z = newXZ.Item2;

            if (agentValues[x][y][z] == 0) {
                agentValues[x][y][z]++;
                counter++;
            }
        }
        if (sceneVisualised) {
            sceneManagement.VisualiseAgents();
        }
        
    }

    // Called when a block is sucessfully placed, when an agent moves outside of the grid, or when an agent is stuck.
    void CompleteLifeCycle(Vector3Int currentPos) {
        List<(int, int)> spawnLocations = new List<(int, int)>();
        for (int i = 0; i < gridDims.x; i++) {
            spawnLocations.Add((i, 0));
            spawnLocations.Add((i, 1));
            spawnLocations.Add((i, gridDims.z - 2));
            spawnLocations.Add((i, gridDims.z - 1));
        }

        if (gridDims.z > 4) {
            for (int j = 2; j < gridDims.z - 2; j++) {
                spawnLocations.Add((0, j));
                spawnLocations.Add((1, j));
                spawnLocations.Add((gridDims.x - 2, j));
                spawnLocations.Add((gridDims.x - 1, j));
            }
        }

        (int, int) newXZ = spawnLocations[Random.Range(0, spawnLocations.Count)];

        MakeMove(currentPos, new Vector3Int(newXZ.Item1, 0, newXZ.Item2));
    }

    bool CheckNoObstructions(Vector3Int currentPos, Vector3Int move) {
        int axisCount = 0;
        if (move.x != 0) axisCount++;
        if (move.y != 0) axisCount++;
        if (move.z != 0) axisCount++;

        if (cellValues[currentPos.x + move.x][currentPos.y + move.y][currentPos.z + move.z] == 0) {
            if (axisCount == 3) { // Check for movement through wall diagonals
                if (cellValues[currentPos.x + move.x][currentPos.y][currentPos.z] == 0 || cellValues[currentPos.x][currentPos.y + move.y][currentPos.z] == 0 || cellValues[currentPos.x][currentPos.y][currentPos.z + move.z] == 0) {
                    return true;
                } else {
                    return false;
                }
            } else if (axisCount == 2) {
                if (move.x != 0 && move.y != 0) {
                    if (cellValues[currentPos.x + move.x][currentPos.y][currentPos.z] == 0 || cellValues[currentPos.x][currentPos.y + move.y][currentPos.z] == 0) {
                        return true;
                    } else {
                        return false;
                    }
                } else if (move.x != 0 && move.z != 0) {
                    if (cellValues[currentPos.x + move.x][currentPos.y][currentPos.z] == 0 || cellValues[currentPos.x][currentPos.y][currentPos.z + move.z] == 0) {
                        return true;
                    } else {
                        return false;
                    }
                } else if (move.y != 0 && move.z != 0) {
                    if (cellValues[currentPos.x][currentPos.y + move.y][currentPos.z] == 0 || cellValues[currentPos.x][currentPos.y][currentPos.z + move.z] == 0) {
                        return true;
                    } else {
                        return false;
                    }
                } else {
                    return false;
                }
            } else {
                return true;
            }
        } else {
            return false;
        }
    }

    bool CheckNeighbouringSurface(int x, int y, int z) {
        if (y == 0) return true;

        if (x != 0) {
            if (cellValues[x - 1][y][z] != 0) {
                return true;
            }
        }
        if (y != 0) {
            if (cellValues[x][y - 1][z] != 0) {
                return true;
            }
        }
        if (z != 0) {
            if (cellValues[x][y][z - 1] != 0) {
                return true;
            }
        }
        if (x != gridDims.x - 1) {
            if (cellValues[x + 1][y][z] != 0) {
                return true;
            }
        }
        if (y != gridDims.y - 1) {
            if (cellValues[x][y + 1][z] != 0) {
                return true;
            }
        }
        if (z != gridDims.z - 1) {
            if (cellValues[x][y][z + 1] != 0) {
                return true;
            }
        }
        return false;
    }

    void MakeMove(Vector3Int currentPos, Vector3Int newPos) {
        agentValues[newPos.x][newPos.y][newPos.z]++;
        agentValues[currentPos.x][currentPos.y][currentPos.z]--;
    }

    bool TryMakeMove(Vector3Int currentPos) {
        List<Vector3Int> possibleMoves = new List<Vector3Int>();

        for (int x = -1; x <= 1; x++) {
            //if (pos.x + x >= 0 && pos.x + x < gridUI.width) {
            for (int y = -1; y <= 1; y++) {
                if (currentPos.y + y >= 0 && currentPos.y + y < gridDims.y) { // Can't move out of grid vertically
                    for (int z = -1; z <= 1; z++) {
                        //if (pos.z + z >= 0 && pos.z + z < gridUI.depth) {
                        if ((x != 0 || y != 0 || z != 0)) {
                            if (currentPos.x + x < 0 || currentPos.x + x >= gridDims.x || currentPos.z + z < 0 || currentPos.z + z >= gridDims.z) { // Moving out of grid
                                possibleMoves.Add(new Vector3Int(x, y, z));
                            } else if (CheckNoObstructions(currentPos, new Vector3Int(x, y, z)) && CheckNeighbouringSurface(currentPos.x + x, currentPos.y + y, currentPos.z + z)) { // Moving within grid
                                possibleMoves.Add(new Vector3Int(x, y, z));
                            }
                        }
                        //}
                    }
                }
            }
            //}
        }

        if (possibleMoves.Count != 0) {
            Vector3Int move = possibleMoves[Random.Range(0, possibleMoves.Count)];
            if (currentPos.x + move.x < 0 || currentPos.x + move.x >= gridDims.x || currentPos.z + move.z < 0 || currentPos.z + move.z >= gridDims.z) { // Moving out of grid
                CompleteLifeCycle(currentPos);
            } else { // Moving within grid
                MakeMove(currentPos, currentPos + move);
            }
            return true;
        } else {
            CompleteLifeCycle(currentPos);
            return false;
        }
    }

    void PlaceBlockAt(int x, int y, int z) {
        sceneManagement.ChangeCell(x, y, z, 1, true);
        CompleteLifeCycle(new Vector3Int(x, y, z));
    }

    // 1. Either the location immediately underneath or immediately above the site must contain material.
    // The floor, represented by y == -1, counts as material.
    bool Rule1(int x, int y, int z) {
        if (y == 0) {
            return true;
        }
        if (y > 0) {
            if (cellValues[x][y - 1][z] != 0) {
                return true;
            }
        }
        if (y < gridDims.y - 2) {
            if (cellValues[x][y + 1][z] != 0) {
                return true;
            }
        }
        return false;
    }


    // 2. The site must share a face with a horizontally adjacent location that contains material and satisfies (1).
    bool Rule2(int x, int y, int z) {
        if (x < gridDims.x - 2) {
            if (cellValues[x + 1][y][z] != 0 && Rule1(x + 1, y, z)) {
                return true;
            }
        }
        if (x > 0) {
            if (cellValues[x - 1][y][z] != 0 && Rule1(x - 1, y, z)) {
                return true;
            }
        }
        if (z < gridDims.z - 2) {
            if (cellValues[x][y][z + 1] != 0 && Rule1(x, y, z + 1)) {
                return true;
            }
        }
        if (z > 0) {
            if (cellValues[x][y][z - 1] != 0 && Rule1(x, y, z - 1)) {
                return true;
            }
        }
        return false;
    }

    // 3. One face of the site must neighbour three horizontally adjacent locations that each contain material.
    bool Rule3(int x, int y, int z) {
        if (x < gridDims.x - 2 && z > 0 && z < gridDims.z - 2) {
            if (cellValues[x + 1][y][z - 1] != 0 && cellValues[x + 1][y][z] != 0 && cellValues[x + 1][y][z + 1] != 0) {
                return true;
            }
        }
        if (x > 0 && z > 0 && z < gridDims.z - 2) {
            if (cellValues[x - 1][y][z - 1] != 0 && cellValues[x - 1][y][z] != 0 && cellValues[x - 1][y][z + 1] != 0) {
                return true;
            }
        }
        if (z < gridDims.z - 2 && x > 0 && x < gridDims.x - 2) {
            if (cellValues[x - 1][y][z + 1] != 0 && cellValues[x][y][z + 1] != 0 && cellValues[x + 1][y][z + 1] != 0) {
                return true;
            }
        }
        if (z > 0 && x > 0 && x < gridDims.x - 2) {
            if (cellValues[x - 1][y][z - 1] != 0 && cellValues[x][y][z - 1] != 0 && cellValues[x + 1][y][z - 1] != 0) {
                return true;
            }
        }
        return false;
    }

    bool TryPlaceBlock(Vector3Int currentPos) {
        if (Random.Range(0f, 1f) <= placementProbability) {
            if (Rule1(currentPos.x, currentPos.y, currentPos.z) || Rule2(currentPos.x, currentPos.y, currentPos.z) || Rule3(currentPos.x, currentPos.y, currentPos.z)) {
                PlaceBlockAt(currentPos.x, currentPos.y, currentPos.z);
                return true;
            } else {
                return false;
            }
        } else {
            return false;
        }
    }

    void AgentsStepThroughTime() {
        List<Vector3Int> positions = new List<Vector3Int>();
        for (int x = 0; x < gridDims.x; x++) {
            for (int y = 0; y < gridDims.y; y++) {
                for (int z = 0; z < gridDims.z; z++) {
                    if (agentValues[x][y][z] != 0) {
                        for (int i = 0; i < agentValues[x][y][z]; i++) {
                            positions.Add(new Vector3Int(x, y, z));
                        }
                    }
                }
            }
        }

        foreach (Vector3Int pos in positions) {
            TryMakeMove(pos);
        }

        if (sceneVisualised) {
            sceneManagement.VisualiseAgents();
        }       

        List<Vector3Int> newPositions = new List<Vector3Int>();
        for (int x = 0; x < gridDims.x; x++) {
            for (int y = 0; y < gridDims.y; y++) {
                for (int z = 0; z < gridDims.z; z++) {
                    if (agentValues[x][y][z] != 0) {
                        for (int i = 0; i < agentValues[x][y][z]; i++) {
                            newPositions.Add(new Vector3Int(x, y, z));
                        }
                    }
                }
            }
        }

        foreach (Vector3Int pos in newPositions) {
            TryPlaceBlock(pos);
        }

        if (sceneVisualised) {
            sceneManagement.VisualiseCells();
        }
    }

    // Diffuses pheromone from neighbouring cells. If there is a block in the neighbouring cell, the diffusion value is instead sent back. Edges of the grid are considered to be empty of pheromone.
    float CalcDiffusionFromNeighbours(Vector3Int pos) {
        float sum = 0f;
        float value = pastPheromoneValues[pos.x][pos.y][pos.z];

        if (pos.x > 0) {
            if (cellValues[pos.x - 1][pos.y][pos.z] == 0) {
                sum += -alpha * (value - pastPheromoneValues[pos.x - 1][pos.y][pos.z]);
            } else {
                pheromoneValues[pos.x][pos.y][pos.z] += alpha * value;
            }
        } else {
            sum += -alpha * value;
        }
        if (pos.x < gridDims.x - 1) {
            if (cellValues[pos.x + 1][pos.y][pos.z] == 0) {
                sum += -alpha * (value - pastPheromoneValues[pos.x + 1][pos.y][pos.z]);
            } else {
                pheromoneValues[pos.x][pos.y][pos.z] += alpha * value;
            }
        } else {
            sum += -alpha * value;
        }
        if (pos.y > 0) {
            if (cellValues[pos.x][pos.y - 1][pos.z] == 0) {
                sum += -alpha * (value - pastPheromoneValues[pos.x][pos.y - 1][pos.z]);
            } else {
                pheromoneValues[pos.x][pos.y][pos.z] += alpha * value;
            }
        } else {
            sum += alpha * value; // Note this differs from the others as the floor is considered to be made of blocks.
        }
        if (pos.y < gridDims.y - 1) {
            if (cellValues[pos.x][pos.y + 1][pos.z] == 0) {
                sum += -alpha * (value - pastPheromoneValues[pos.x][pos.y + 1][pos.z]);
            } else {
                pheromoneValues[pos.x][pos.y][pos.z] += alpha * value;
            }
        } else {
            sum += -alpha * value;
        }
        if (pos.z > 0) {
            if (cellValues[pos.x][pos.y][pos.z - 1] == 0) {
                sum += -alpha * (value - pastPheromoneValues[pos.x][pos.y][pos.z - 1]);
            } else {
                pheromoneValues[pos.x][pos.y][pos.z] += alpha * value;
            }
        } else {
            sum += -alpha * value;
        }
        if (pos.z < gridDims.z - 1) {
            if (cellValues[pos.x][pos.y][pos.z + 1] == 0) {
                sum += -alpha * (value - pastPheromoneValues[pos.x][pos.y][pos.z + 1]);
            } else {
                pheromoneValues[pos.x][pos.y][pos.z] += alpha * value;
            }
        } else {
            sum += -alpha * value;
        }

        //if (pos.x > 0) sum += -alpha * (value - pastPheromoneValues[pos.x - 1][pos.y][pos.z]);
        //if (pos.x < gridDims.x - 1) sum += -alpha * (value - pastPheromoneValues[pos.x + 1][pos.y][pos.z]);
        //if (pos.y > 0) sum += -alpha * (value - pastPheromoneValues[pos.x][pos.y - 1][pos.z]);
        //if (pos.y < gridDims.y - 1) sum += -alpha * (value - pastPheromoneValues[pos.x][pos.y + 1][pos.z]);
        //if (pos.z > 0) sum += -alpha * (value - pastPheromoneValues[pos.x][pos.y][pos.z - 1]);
        //if (pos.z < gridDims.z - 1) sum += -alpha * (value - pastPheromoneValues[pos.x][pos.y][pos.z + 1]);

        return sum;
    }

    void DecayPheromoneValues() {
        for (int x = 0; x < gridDims.x; x++) {
            for (int y = 0; y < gridDims.y; y++) {
                for (int z = 0; z < gridDims.z; z++) {
                    pheromoneValues[x][y][z] *= decay;
                }
            }
        }
    }

    void PheromonesStepThroughTime() {
        DecayPheromoneValues();

        for (int x = 0; x < gridDims.x; x++) {
            for (int y = 0; y < gridDims.y; y++) {
                for (int z = 0; z < gridDims.z; z++) {
                    pastPheromoneValues[x][y][z] = pheromoneValues[x][y][z];
                }
            }
        }

        for (int x = 0; x < gridDims.x; x++) {
            for (int y = 0; y < gridDims.y; y++) {
                for (int z = 0; z < gridDims.z; z++) {
                    pheromoneValues[x][y][z] += CalcDiffusionFromNeighbours(new Vector3Int(x, y, z));
                    if (pheromoneValues[x][y][z] < 0.01f) pheromoneValues[x][y][z] = 0f;
                }
            }
        }

        if (sceneVisualised) {
            sceneManagement.VisualisePheros();
        }
    }

    IEnumerator Simulate() {
        while (true) {
            yield return new WaitWhile(() => !runSimulation);

            AgentsStepThroughTime();
            PheromonesStepThroughTime();

            timestep++;

            yield return new WaitForSecondsRealtime(0.5f);
        }
    }

}
