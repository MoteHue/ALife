using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pheromone : MonoBehaviour
{
    public MeshRenderer meshRenderer;
    public GridUI gridUI;
    public Simulation sim;
    public float value;
    public Vector3Int pos;
    public Material materialPrefab;

    float alpha = 1f / 7f;

    public virtual void Start() {
        sim = FindObjectOfType<Simulation>();
        gridUI = FindObjectOfType<GridUI>();
        meshRenderer.material = Instantiate(materialPrefab);
    }

    public void ActivateMesh(bool b) {
        meshRenderer.enabled = b;
    }

    float CalcDiffusionFromNeighbours() {
        float sum = 0f;

        if (pos.x > 0) sum += -alpha * (value - sim.pheromoneValues[pos.x - 1][pos.y][pos.z]);
        if (pos.x < gridUI.width - 1) sum += -alpha * (value - sim.pheromoneValues[pos.x + 1][pos.y][pos.z]);
        if (pos.y > 0) sum += -alpha * (value - sim.pheromoneValues[pos.x][pos.y - 1][pos.z]);
        if (pos.y < gridUI.height - 1) sum += -alpha * (value - sim.pheromoneValues[pos.x][pos.y + 1][pos.z]);
        if (pos.z > 0) sum += -alpha * (value - sim.pheromoneValues[pos.x][pos.y][pos.z - 1]);
        if (pos.z < gridUI.depth - 1) sum += -alpha * (value - sim.pheromoneValues[pos.x][pos.y][pos.z + 1]);
        return sum;
    }

    public virtual void OnTimestep() {
        value = CalcDiffusionFromNeighbours();
        if (value > 0.01f) {
            ActivateMesh(true);
        } else {
            ActivateMesh(false);
            value = 0;
        }
        meshRenderer.material.color = new Color(materialPrefab.color.r, materialPrefab.color.g, materialPrefab.color.b, value);
        sim.pheroCallBackCounter++;
    }
}
