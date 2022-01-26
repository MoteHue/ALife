using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuilderAgent : Agent
{
    float placementProbability = 0.1f;

    // Start is called before the first frame update
    public override void Start() {
        base.Start();
    }

    public override void OnTimestep() {
        TryMakeMove();
        TryPlaceBlock();
        sim.agentCallBackCounter++;
    }

    // Called when a block is sucessfully placed, when an agent moves outside of the grid, or when an agent is stuck.
    void CompleteLifeCycle() {
        List<(int, int)> spawnLocations = new List<(int, int)>();
        for (int i = 0; i < sim.gridDims.x; i++) {
            spawnLocations.Add((i, 0));
            spawnLocations.Add((i, 1));
            spawnLocations.Add((i, sim.gridDims.z - 2));
            spawnLocations.Add((i, sim.gridDims.z - 1));
        }

        if (sim.gridDims.z > 4) {
            for (int j = 2; j < sim.gridDims.z - 2; j++) {
                spawnLocations.Add((0, j));
                spawnLocations.Add((1, j));
                spawnLocations.Add((sim.gridDims.x - 2, j));
                spawnLocations.Add((sim.gridDims.x - 1, j));
            }
        }

        (int, int) newXZ = spawnLocations[Random.Range(0, spawnLocations.Count)];

        MoveTo(newXZ.Item1, 0, newXZ.Item2);
    }

    bool CheckNoObstructions(int x, int y, int z) {
        int axisCount = 0;
        if (x != 0) axisCount++;
        if (y != 0) axisCount++;
        if (z != 0) axisCount++;

        if (/*!sim.agentLocations[pos.x + x][pos.y + y][pos.z + z] &&*/ sim.cells[pos.x + x][pos.y + y][pos.z + z] == 0) {
            if (axisCount == 3) { // Check for movement through wall diagonals
                if (sim.cells[pos.x + x][pos.y][pos.z] == 0 || sim.cells[pos.x][pos.y + y][pos.z] == 0 || sim.cells[pos.x][pos.y][pos.z + z] == 0) {
                    return true;
                } else {
                    return false;
                }
            } else if (axisCount == 2) {
                if (x != 0 && y != 0) {
                    if (sim.cells[pos.x + x][pos.y][pos.z] == 0 || sim.cells[pos.x][pos.y + y][pos.z] == 0) {
                        return true;
                    } else {
                        return false;
                    }
                } else if (x != 0 && z != 0) {
                    if (sim.cells[pos.x + x][pos.y][pos.z] == 0 || sim.cells[pos.x][pos.y][pos.z + z] == 0) {
                        return true;
                    } else {
                        return false;
                    }
                } else if (y != 0 && z != 0) {
                    if (sim.cells[pos.x][pos.y + y][pos.z] == 0 || sim.cells[pos.x][pos.y][pos.z + z] == 0) {
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
            if (sim.cells[x - 1][y][z] != 0) {
                return true;
            }
        }
        if (y != 0) {
            if (sim.cells[x][y - 1][z] != 0) {
                return true;
            }
        }
        if (z != 0) {
            if (sim.cells[x][y][z - 1] != 0) {
                return true;
            }
        }
        if (x != sim.gridDims.x - 1) {
            if (sim.cells[x + 1][y][z] != 0) {
                return true;
            }
        }
        if (y != sim.gridDims.y - 1) {
            if (sim.cells[x][y + 1][z] != 0) {
                return true;
            }
        }
        if (z != sim.gridDims.z - 1) {
            if (sim.cells[x][y][z + 1] != 0) {
                return true;
            }
        }
        return false;
    }

    void MoveTo(int x, int y, int z) {
        sim.agentLocations[x][y][z] = true;
        sim.agentLocations[pos.x][pos.y][pos.z] = false;
        pos = new Vector3Int(x, y, z);
        transform.position = new Vector3(x, y, z);
    }

    bool TryMakeMove() {
        List<Vector3Int> possibleMoves = new List<Vector3Int>();

        for (int x = -1; x <= 1; x++) {
            //if (pos.x + x >= 0 && pos.x + x < gridUI.width) {
                for (int y = -1; y <= 1; y++) {
                    if (pos.y + y >= 0 && pos.y + y < sim.gridDims.y) { // Can't move out of grid vertically
                        for (int z = -1; z <= 1; z++) {
                            //if (pos.z + z >= 0 && pos.z + z < gridUI.depth) {
                                if ((x != 0 || y != 0 || z != 0)) {
                                    if (pos.x + x < 0 || pos.x + x >= sim.gridDims.x || pos.z + z < 0 || pos.z + z >= sim.gridDims.z) { // Moving out of grid
                                        possibleMoves.Add(new Vector3Int(x, y, z));
                                    } else if (CheckNoObstructions(x, y, z) && CheckNeighbouringSurface(pos.x + x, pos.y + y, pos.z + z)) { // Moving within grid
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
            if (pos.x + move.x < 0 || pos.x + move.x >= sim.gridDims.x || pos.z + move.z < 0 || pos.z + move.z >= sim.gridDims.z) { // Moving out of grid
                CompleteLifeCycle();
            } else { // Moving within grid
                MoveTo(pos.x + move.x, pos.y + move.y, pos.z + move.z);
            }
            return true;
        } else {
            CompleteLifeCycle();
            return false;
        }
    }

    void PlaceBlockAt(int x, int y, int z) {
        sceneManagement.ChangeCell(x, y, z, 1, true);
        CompleteLifeCycle();
    }

    // 1. Either the location immediately underneath or immediately above the site must contain material.
    // The floor, represented by y == -1, counts as material.
    bool Rule1(int x, int y, int z) {
        if (y == 0) {
            return true;
        }
        if (y > 0) {
            if (sim.cells[x][y - 1][z] != 0) {
                return true;
            }
        }
        if (y < sim.gridDims.y - 2) {
            if (sim.cells[x][y + 1][z] != 0) {
                return true;
            }
        }
        return false;
    }


    // 2. The site must share a face with a horizontally adjacent location that contains material and satisfies (1).
    bool Rule2(int x, int y, int z) {
        if (x < sim.gridDims.x - 2) {
            if (sim.cells[x + 1][y][z] != 0 && Rule1(x + 1, y, z)) {
                return true;
            }
        }
        if (x > 0) {
            if (sim.cells[x - 1][y][z] != 0 && Rule1(x - 1, y, z)) {
                return true;
            }
        }
        if (z < sim.gridDims.z - 2) {
            if (sim.cells[x][y][z + 1] != 0 && Rule1(x, y, z + 1)) {
                return true;
            }
        }
        if (z > 0) {
            if (sim.cells[x][y][z - 1] != 0 && Rule1(x, y, z - 1)) {
                return true;
            }
        }
        return false;
    }

    // 3. One face of the site must neighbour three horizontally adjacent locations that each contain material.
    bool Rule3(int x, int y, int z) {
        if (x < sim.gridDims.x - 2 && z > 0 && z < sim.gridDims.z - 2) {
            if (sim.cells[x + 1][y][z - 1] != 0 && sim.cells[x + 1][y][z] != 0 && sim.cells[x + 1][y][z + 1] != 0) {
                return true;
            }
        }
        if (x > 0 && z > 0 && z < sim.gridDims.z - 2) {
            if (sim.cells[x - 1][y][z - 1] != 0 && sim.cells[x - 1][y][z] != 0 && sim.cells[x - 1][y][z + 1] != 0) {
                return true;
            }
        }
        if (z < sim.gridDims.z - 2 && x > 0 && x < sim.gridDims.x - 2) {
            if (sim.cells[x - 1][y][z + 1] != 0 && sim.cells[x][y][z + 1] != 0 && sim.cells[x + 1][y][z + 1] != 0) {
                return true;
            }
        }
        if (z > 0 && x > 0 && x < sim.gridDims.x - 2) {
            if (sim.cells[x - 1][y][z - 1] != 0 && sim.cells[x][y][z - 1] != 0 && sim.cells[x + 1][y][z - 1] != 0) {
                return true;
            }
        }
        return false;
    }

    bool TryPlaceBlock() {
        if (Random.Range(0f, 1f) <= placementProbability) {
            if (Rule1(pos.x, pos.y, pos.z) || Rule2(pos.x, pos.y, pos.z) || Rule3(pos.x, pos.y, pos.z)) {
                PlaceBlockAt(pos.x, pos.y, pos.z);
                return true;
            } else {
                return false;
            }
        } else {
            return false;
        }
    }

}
