using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Simulation : MonoBehaviour
{
    public SceneManagement sceneManagement;
    GridUI gridUI;
    public SimulationUI simulationUI;
    public int agentCallBackCounter;
    public int pheroCallBackCounter;
    public bool runSimulation;

    public Vector3Int gridDims;

    public List<List<List<int>>> cells;
    public List<List<List<float>>> pheromoneValues;
    public List<List<List<bool>>> agentLocations;
    public List<Agent> agents;
    public List<Pheromone> pheromones;

    int timestep = 0;

    public void SetWHD() {
        gridDims.x = int.Parse(gridUI.coords[0].text);
        gridDims.y = int.Parse(gridUI.coords[1].text);
        gridDims.z = int.Parse(gridUI.coords[2].text);
    }

    private void Start() {
        cells = new List<List<List<int>>>();
        pheromoneValues = new List<List<List<float>>>();
        agentLocations = new List<List<List<bool>>>();
        agents = new List<Agent>();
        gridUI = FindObjectOfType<GridUI>();
    }

    public void StartSimulation() {
        runSimulation = true;
        simulationUI.nextTimestepButton.interactable = false;
        StartCoroutine(nameof(Simulate));
    }

    public void StepThroughTime() {
        
    }

    public void UpdateLocalPheroValuesFromSimList() {
        foreach (Pheromone pheromone in pheromones) {
            pheromone.value = pheromoneValues[pheromone.pos.x][pheromone.pos.y][pheromone.pos.z];
        }
    }

    IEnumerator Simulate() {
        while (runSimulation) {
            agentCallBackCounter = 0;
            pheroCallBackCounter = 0;

            //if (timestep == 1) {
            //    sceneManagement.AddQueen(4, 0, 4);
            //    sceneManagement.AddQueen(4, 0, 5);
            //    sceneManagement.AddQueen(5, 0, 4);
            //    sceneManagement.AddQueen(5, 0, 5);
            //}

            foreach (Agent agent in agents) {
                agent.OnTimestep();
            }

            yield return new WaitWhile(() => agentCallBackCounter < agents.Count);
            Debug.Log("All agents acted");
            UpdateLocalPheroValuesFromSimList();

            foreach (Pheromone pheromone in pheromones) {
                pheromone.OnTimestep();
            }

            yield return new WaitWhile(() => pheroCallBackCounter < pheromones.Count);
            Debug.Log("All pheros acted");
            foreach (Pheromone pheromone in pheromones) {
                pheromoneValues[pheromone.pos.x][pheromone.pos.y][pheromone.pos.z] = pheromone.value;
            }

            timestep++;

            //yield return new WaitForSecondsRealtime(1f);
        }
        
    }

}
