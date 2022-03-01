using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugUI : MonoBehaviour
{
    public Button debugLogCells;
    public Button debugLogPheromones;
    Simulation sim;

    private void Start() {
        sim = FindObjectOfType<Simulation>();
    }

    public void DebugLogBigOlListOfInts(string title, List<List<List<int>>> list) {
        string debugString1 = "";
        for (int x = 0; x < sim.gridDims.x; x++) {
            string debugString2 = $"X{x}:\n";
            for (int y = 0; y < sim.gridDims.y; y++) {
                string debugString3 = $"Y{y}: ";
                for (int z = 0; z < sim.gridDims.z; z++) {
                    debugString3 += $"{list[x][y][z]}, ";
                }
                debugString2 += $"{debugString3}\n";
            }
            debugString1 += $"{debugString2}\n";
        }
        Debug.Log($"{title}\n{debugString1}");
    }

    public void DebugLogBigOlListOfFloats(string title, List<List<List<float>>> list) {
        string debugString1 = "";
        for (int x = 0; x < sim.gridDims.x; x++) {
            string debugString2 = $"X{x}:\n";
            for (int y = 0; y < sim.gridDims.y; y++) {
                string debugString3 = $"Y{y}: ";
                for (int z = 0; z < sim.gridDims.z; z++) {
                    debugString3 += $"{list[x][y][z]}, ";
                }
                debugString2 += $"{debugString3}\n";
            }
            debugString1 += $"{debugString2}\n";
        }
        Debug.Log($"{title}\n{debugString1}");
    }
}
