using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GridUI : MonoBehaviour
{
    public List<InputField> coords;
    public Toggle visualise;
    public Button generateGrid;

    public int width;
    public int height;
    public int depth;

    public bool coordsValid() {
        bool allValid = true;
        int n;
        foreach (InputField coord in coords) {
            if (allValid) allValid = int.TryParse(coord.text, out n);
        }
        return allValid;
    }

    public void SetWHD() {
        width = int.Parse(coords[0].text);
        height = int.Parse(coords[1].text);
        depth = int.Parse(coords[2].text);
    }

    public List<List<List<int>>> GenerateGrid() { 
        List<List<List<int>>> returnList = new List<List<List<int>>>();
        for (int x = 0; x < width; x++) {
            List<List<int>> yList = new List<List<int>>();
            for (int y = 0; y < height; y++) {
                List<int> zList = new List<int>();
                for (int z = 0; z < depth; z++) {
                    zList.Add(0);
                }
                yList.Add(zList);
            }
            returnList.Add(yList);
        }
        return returnList;
    }

    public List<List<List<GameObject>>> GenerateVisualGrid(GameObject prefab, Transform parent) {
        List<List<List<GameObject>>> returnList = new List<List<List<GameObject>>>();
        for (int x = 0; x < width; x++) {
            List<List<GameObject>> ys = new List<List<GameObject>>();
            for (int y = 0; y < height; y++) {
                List<GameObject> zs = new List<GameObject>();
                for (int z = 0; z < depth; z++) {
                    zs.Add(Instantiate(prefab, transform.position + new Vector3(x, y, z), transform.rotation, parent));
                }
                ys.Add(zs);
            }
            returnList.Add(ys);
        }
        return returnList;
    }
}
