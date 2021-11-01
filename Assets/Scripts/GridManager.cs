using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GridManager : MonoBehaviour
{

    public GameObject cellPrefab;
    
    public List<InputField> gridManagementInputFields;
    public List<InputField> cellManagementInputFields;
    public Button GenerateGridButton;

    List<List<List<GameObject>>> cells;
    int height;
    int width;
    int depth;

    private void Start() {
        cells = new List<List<List<GameObject>>>();
    }

    void SetupCellGrid() {
        for (int x = 0; x < width; x++) {
            List<List<GameObject>> yList = new List<List<GameObject>>();
            for (int y = 0; y < height; y++) {
                List<GameObject> zList = new List<GameObject>();
                for (int z = 0; z < depth; z++) {
                    zList.Add(Instantiate(cellPrefab, transform.position + new Vector3(x, y, z), transform.rotation, transform));
                }
                yList.Add(zList);
            }
            cells.Add(yList);
        }
    }

    public void ButtonSetupCellGrid() {
        width = int.Parse(gridManagementInputFields[0].text);
        height = int.Parse(gridManagementInputFields[1].text);
        depth = int.Parse(gridManagementInputFields[2].text);
        SetupCellGrid();
        GenerateGridButton.interactable = false;
    }

    void ChangeCell(int x, int y, int z, bool visible) {
        CellData cell = cells[x][y][z].GetComponent<CellData>();
        cell.ActivateMesh(visible);
    }

    public void ButtonChangeCell(bool b) {
        int x = int.Parse(cellManagementInputFields[0].text);
        int y = int.Parse(cellManagementInputFields[1].text);
        int z = int.Parse(cellManagementInputFields[2].text);
        if (x>=0 && x<width && y>=0 && y<height && z>=0 && z<depth) {
            ChangeCell(x, y, z, b);
        }
    }

}
