using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Simulation : MonoBehaviour
{
    public SceneManagement sceneManagement;
    public SimulationUI simulationUI;
    public int agentCallBackCounter;
    public int pheroCallBackCounter;
    public bool runSimulation;

    public List<List<List<int>>> cells;
    public List<List<List<float>>> pheromoneValues;
    public List<List<List<bool>>> agentLocations;
    public List<Agent> agents;
    public List<Pheromone> pheromones;

    int timestep = 0;


    private void Start() {
        cells = new List<List<List<int>>>();
        pheromoneValues = new List<List<List<float>>>();
        agentLocations = new List<List<List<bool>>>();
        agents = new List<Agent>();
    }

    public void StartSimulation() {
        runSimulation = true;
        simulationUI.nextTimestepButton.interactable = false;
        StartCoroutine(nameof(Simulate));
    }

    public void StepThroughTime() {
        foreach (Agent agent in agents) {
            agent.OnTimestep();
        }
        foreach (Pheromone pheromone in pheromones) {
            pheromone.OnTimestep();
        }
        timestep++;
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

            StepThroughTime();

            yield return new WaitWhile(() => agentCallBackCounter < agents.Count);
            Debug.Log("All agents acted");

            yield return new WaitWhile(() => pheroCallBackCounter < pheromones.Count);
            Debug.Log("All pheros acted");
            foreach (Pheromone pheromone in pheromones) {
                pheromoneValues[pheromone.pos.x][pheromone.pos.y][pheromone.pos.z] = pheromone.value;
            }

                yield return new WaitForSecondsRealtime(1f);
        }
        
    }

}
