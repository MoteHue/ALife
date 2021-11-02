using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GridManager : MonoBehaviour
{

    public GameObject cellPrefab;
    
    public List<InputField> gridManagementInputFields;
    public List<InputField> cellManagementInputFields;
    public Button generateGridButton;
    public GameObject cellManagement;
    public GameObject debugManagement;

    List<List<List<int>>> cells;

    List<List<List<GameObject>>> visualCells;
    int height;
    int width;
    int depth;

    bool cellsVisualised;
    bool pheromonesVisualised;

    private void Start() {
        cells = new List<List<List<int>>>();
        visualCells = new List<List<List<GameObject>>>();
        cellManagement.SetActive(false);
        debugManagement.SetActive(false);
    }

    void SetupCellGrid() {
        for (int x = 0; x < width; x++) {
            List<List<int>> yList = new List<List<int>>();
            for (int y = 0; y < height; y++) {
                List<int> zList = new List<int>();
                for (int z = 0; z < depth; z++) {
                    zList.Add(0);
                }
                yList.Add(zList);
            }
            cells.Add(yList);
        }
    }

    void SetupVisualCellGrid() {
        for (int x = 0; x < width; x++) {
            List<List<GameObject>> yList = new List<List<GameObject>>();
            for (int y = 0; y < height; y++) {
                List<GameObject> zList = new List<GameObject>();
                for (int z = 0; z < depth; z++) {
                    zList.Add(Instantiate(cellPrefab, transform.position + new Vector3(x, y, z), transform.rotation, transform));
                }
                yList.Add(zList);
            }
            visualCells.Add(yList);
        }
    }

    public void ButtonSetupCellGrid(Toggle t) {
        width = int.Parse(gridManagementInputFields[0].text);
        height = int.Parse(gridManagementInputFields[1].text);
        depth = int.Parse(gridManagementInputFields[2].text);
        SetupCellGrid();
        if (t.isOn) {
            SetupVisualCellGrid();
            cellsVisualised = true;
        }
        generateGridButton.interactable = false;
        generateGridButton.GetComponentInChildren<Text>().text = "Grid Generated";
        foreach (InputField field in gridManagementInputFields) {
            field.interactable = false;
        }
        t.interactable = false;
        cellManagement.SetActive(true);
        debugManagement.SetActive(true);
    }

    void ChangeCell(int x, int y, int z, int value, bool visible) {
        cells[x][y][z] = value;
        if (cellsVisualised) {
            CellData cell = visualCells[x][y][z].GetComponent<CellData>();
            cell.ActivateMesh(visible);
        }
    }

    public void ButtonChangeCell(bool b) {
        int x = int.Parse(cellManagementInputFields[0].text);
        int y = int.Parse(cellManagementInputFields[1].text);
        int z = int.Parse(cellManagementInputFields[2].text);
        if (x>=0 && x<width && y>=0 && y<height && z>=0 && z<depth) {
            if (b) ChangeCell(x, y, z, 1, b);
            else ChangeCell(x, y, z, 0, b);
        }
    }

    public void DebugLogBigOlList() {
        string debugString1 = "";
        for (int x = 0; x < width; x++) {
            string debugString2 = $"X{x}:\n";
            for (int y = 0; y < height; y++) {
                string debugString3 = $"Y{y}: ";
                for (int z = 0; z < depth; z++) {
                    debugString3 += $"{cells[x][y][z]}, ";
                }
                debugString2 += $"{debugString3}\n";
            }
            debugString1 += $"{debugString2}\n";
        }
        Debug.Log(debugString1);    
    }

}
