using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CellData : MonoBehaviour
{
    public MeshRenderer meshRenderer;

    public void ActivateMesh(bool b) {
        meshRenderer.enabled = b;
    }
}
