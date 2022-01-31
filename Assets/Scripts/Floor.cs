using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Floor : MonoBehaviour
{
    public GameObject plane;

    public void SetScale(int x, int y, int z) {
        plane.transform.localScale = new Vector3(x, y, z) / 10;
        plane.transform.position = new Vector3((x / 2f), 0f, (z / 2f));
    }
}
