using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentBehaviour : MonoBehaviour
{
    SceneManagement sceneManagement;
    public Vector3Int pos;

    private void Start() {
        sceneManagement = FindObjectOfType<SceneManagement>();
    }

    public void OnTimestep() {
        TryMakeMove();
        TryPlaceBlock();
    }

    void MoveTo(int x, int y, int z) {
        sceneManagement.agentLocations[x][y][z] = true;
        sceneManagement.agentLocations[pos.x][pos.y][pos.z] = false;
        pos = new Vector3Int(x, y, z);
    }

    bool TryMakeMove() {
        int x = Random.Range(-1, 2);
        int y = Random.Range(-1, 2);
        int z = Random.Range(-1, 2);
        if (!sceneManagement.agentLocations[pos.x + x][pos.y + y][pos.z + z] && sceneManagement.cells[pos.x + x][pos.y + y][pos.z + z] == 0) {
            MoveTo(pos.x + x, pos.y + y, pos.z + z);
            return true;
        } else {
            return false;
        }
    }

    void PlaceBlockAt(int x, int y, int z) {

    }

    // 1. Either the location immediately underneath or immediately above the site must contain material.
    bool Rule1(int x, int y, int z) {
        if (sceneManagement.cells[x][y + 1][z] != 0 || sceneManagement.cells[x][y - 1][z] != 0) {
            return true;
        } else {
            return false;
        }
    }

    // 2. The site must share a face with a horizontally adjacent location that contains material and satisfies (1).
    bool Rule2(int x, int y, int z) {
        if ((sceneManagement.cells[x + 1][y][z] != 0 && Rule1(x + 1, y, z))
           || (sceneManagement.cells[x - 1][y][z] != 0 && Rule1(x - 1, y, z))
           || (sceneManagement.cells[x][y][z + 1] != 0 && Rule1(x - 1, y, z))
           || (sceneManagement.cells[x][y][z - 1] != 0 && Rule1(x - 1, y, z))) {
            return true;
        } else {
            return false;
        }
    }

    // 3. One face of the site must neighbour three horizontally adjacent locations that each contain material.
    bool Rule3(int x, int y, int z) {
        if ((sceneManagement.cells[x + 1][y][z - 1] != 0 && sceneManagement.cells[x + 1][y][z] != 0 && sceneManagement.cells[x + 1][y][z + 1] != 0)
            || (sceneManagement.cells[x - 1][y][z - 1] != 0 && sceneManagement.cells[x - 1][y][z] != 0 && sceneManagement.cells[x - 1][y][z + 1] != 0)
            || (sceneManagement.cells[x - 1][y][z + 1] != 0 && sceneManagement.cells[x][y][z + 1] != 0 && sceneManagement.cells[x + 1][y][z + 1] != 0)
            || (sceneManagement.cells[x - 1][y][z - 1] != 0 && sceneManagement.cells[x][y][z - 1] != 0 && sceneManagement.cells[x + 1][y][z - 1] != 0)) {
            return true;
        } else {
            return false;
        }
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
