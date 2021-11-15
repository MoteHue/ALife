using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PheromoneUI : MonoBehaviour
{
    public List<InputField> coords;
    public Button addCell;
    public Button removeCell;

    public bool coordsValid() {
        bool allValid = true;
        int n;
        foreach (InputField coord in coords) {
            if (allValid) allValid = int.TryParse(coord.text, out n);
        }
        return allValid;
    }
}
