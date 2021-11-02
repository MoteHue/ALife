using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugManagement : MonoBehaviour
{
    public Button debugLogCells;
    public Button debugLogPheromones;
    GridManagement gridManagement;

    private void Start() {
        gridManagement = FindObjectOfType<GridManagement>();
    }

    public void DebugLogBigOlList(string title, List<List<List<int>>> list) {
        string debugString1 = "";
        for (int x = 0; x < gridManagement.width; x++) {
            string debugString2 = $"X{x}:\n";
            for (int y = 0; y < gridManagement.height; y++) {
                string debugString3 = $"Y{y}: ";
                for (int z = 0; z < gridManagement.depth; z++) {
                    debugString3 += $"{list[x][y][z]}, ";
                }
                debugString2 += $"{debugString3}\n";
            }
            debugString1 += $"{debugString2}\n";
        }
        Debug.Log($"{title}\n{debugString1}");
    }
}
