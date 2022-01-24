using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentBehaviour : MonoBehaviour
{
    SceneManagement sceneManagement;
    GridUI gridUI;
    Simulation sim;
    public Vector3Int pos;

    private void Start() {
        sceneManagement = FindObjectOfType<SceneManagement>();
        gridUI = FindObjectOfType<GridUI>();
        sim = FindObjectOfType<Simulation>();
    }

    public void OnTimestep() {
        TryMakeMove();
        TryPlaceBlock();
        sim.callBackCounter++;
    }

    void MoveTo(int x, int y, int z) {
        sim.agentLocations[x][y][z] = true;
        sim.agentLocations[pos.x][pos.y][pos.z] = false;
        pos = new Vector3Int(x, y, z);
        transform.position = new Vector3(x, y, z);
    }

    bool TryMakeMove() {
        int x = Random.Range(-1, 2);
        int y = Random.Range(-1, 2);
        int z = Random.Range(-1, 2);
        while (pos.x + x < 0 || pos.x + x > gridUI.width - 1) x = Random.Range(-1, 2);
        while (pos.y + y < 0 || pos.y + y > gridUI.height - 1) y = Random.Range(-1, 2);
        while (pos.z + z < 0 || pos.z + z > gridUI.depth - 1) z = Random.Range(-1, 2);

        int axisCount = 0;
        if (x != 0) axisCount++;
        if (y != 0) axisCount++;
        if (z != 0) axisCount++;

        if (/*!sim.agentLocations[pos.x + x][pos.y + y][pos.z + z] &&*/ sim.cells[pos.x + x][pos.y + y][pos.z + z] == 0) {
            if (axisCount == 3) { // Check for movement through wall diagonals
                if (sim.cells[pos.x + x][pos.y][pos.z] == 0 && sim.cells[pos.x][pos.y + y][pos.z] == 0 && sim.cells[pos.x][pos.y][pos.z + z] == 0) {
                    MoveTo(pos.x + x, pos.y + y, pos.z + z);
                    return true;
                } else {
                    return false;
                }
            } else if (axisCount == 2) {
                if (x != 0 && y != 0) {
                    if (sim.cells[pos.x + x][pos.y][pos.z] == 0 && sim.cells[pos.x][pos.y + y][pos.z] == 0) {
                        MoveTo(pos.x + x, pos.y + y, pos.z + z);
                        return true;
                    } else {
                        return false;
                    }
                } else if (x != 0 && z != 0) {
                    if (sim.cells[pos.x + x][pos.y][pos.z] == 0 && sim.cells[pos.x][pos.y][pos.z + z] == 0) {
                        MoveTo(pos.x + x, pos.y + y, pos.z + z);
                        return true;
                    } else {
                        return false;
                    }
                } else if (y != 0 && z != 0) {
                    if (sim.cells[pos.x][pos.y + y][pos.z] == 0 && sim.cells[pos.x][pos.y][pos.z + z] == 0) {
                        MoveTo(pos.x + x, pos.y + y, pos.z + z);
                        return true;
                    } else {
                        return false;
                    }
                } else {
                    return false;
                }
            } else { 
                MoveTo(pos.x + x, pos.y + y, pos.z + z);
                return true;
            }
        } else {
            return false;
        }
    }

    void PlaceBlockAt(int x, int y, int z) {
        sceneManagement.ChangeCell(x, y, z, 1, true);
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
        if (y < gridUI.height - 2) {
            if (sim.cells[x][y + 1][z] != 0) {
                return true;
            }
        }
        return false;
    }


    // 2. The site must share a face with a horizontally adjacent location that contains material and satisfies (1).
    bool Rule2(int x, int y, int z) {
        if (x < gridUI.width - 2) {
            if (sim.cells[x + 1][y][z] != 0 && Rule1(x + 1, y, z)) {
                return true;
            }
        }
        if (x > 0) {
            if (sim.cells[x - 1][y][z] != 0 && Rule1(x - 1, y, z)) {
                return true;
            }
        }
        if (z < gridUI.depth - 2) {
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
        if (x < gridUI.width - 2 && z > 0 && z < gridUI.depth - 2) {
            if (sim.cells[x + 1][y][z - 1] != 0 && sim.cells[x + 1][y][z] != 0 && sim.cells[x + 1][y][z + 1] != 0) {
                return true;
            }
        }
        if (x > 0 && z > 0 && z < gridUI.depth - 2) {
            if (sim.cells[x - 1][y][z - 1] != 0 && sim.cells[x - 1][y][z] != 0 && sim.cells[x - 1][y][z + 1] != 0) {
                return true;
            }
        }
        if (z < gridUI.depth - 2 && x > 0 && x < gridUI.width - 2) {
            if (sim.cells[x - 1][y][z + 1] != 0 && sim.cells[x][y][z + 1] != 0 && sim.cells[x + 1][y][z + 1] != 0) {
                return true;
            }
        }
        if (z > 0 && x > 0 && x < gridUI.width - 2) {
            if (sim.cells[x - 1][y][z - 1] != 0 && sim.cells[x][y][z - 1] != 0 && sim.cells[x + 1][y][z - 1] != 0) {
                return true;
            }
        }
        return false;
    }

    bool TryPlaceBlock() {
        if (Rule1(pos.x, pos.y, pos.z) || Rule2(pos.x, pos.y, pos.z) || Rule3(pos.x, pos.y, pos.z)) {
            PlaceBlockAt(pos.x, pos.y, pos.z);
            return true;
        } else {
            return false;
        }
    }
}
