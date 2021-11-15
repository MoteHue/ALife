using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Base : MonoBehaviour
{
    public GameObject plane;

    public void SetScale(int x, int y, int z) {
        plane.transform.localScale = new Vector3(x, y, z) / 10;
        plane.transform.position = new Vector3(x / 2, -0.5f, z / 2);
    }
}
