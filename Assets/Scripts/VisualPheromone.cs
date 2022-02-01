using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisualPheromone : MonoBehaviour
{
    public MeshRenderer meshRenderer;
    public Simulation sim;
    public Material materialPrefab;

    

    public virtual void Start() {
        sim = FindObjectOfType<Simulation>();
        meshRenderer.material = Instantiate(materialPrefab);
    }

    public void ActivateMesh(bool b) {
        meshRenderer.enabled = b;
    }

    public void SetAlpha(float value) {
        if (value >= 0.1f && value <= 0.5f) meshRenderer.material.color = new Color(0f, 1f, 0f, 0.5f);
        //else meshRenderer.material.color = new Color(materialPrefab.color.r, materialPrefab.color.g, materialPrefab.color.b, Mathf.Min(value, 0.8f));
        else ActivateMesh(false);
        //meshRenderer.material.color = new Color(materialPrefab.color.r, materialPrefab.color.g, materialPrefab.color.b, Mathf.Min(value, 0.8f));
    }
}
