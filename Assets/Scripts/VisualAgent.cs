using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisualAgent : MonoBehaviour
{
    public SceneManagement sceneManagement;
    public Simulation sim;
    public Vector3Int pos;
    public MeshRenderer meshRenderer;

    public virtual void Start() {
        meshRenderer = GetComponent<MeshRenderer>();
        sceneManagement = FindObjectOfType<SceneManagement>();
        sim = FindObjectOfType<Simulation>();
    }

    public virtual void ActivateMesh(bool b) {
        meshRenderer.enabled = b;
    }

}
