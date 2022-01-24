using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Simulation : MonoBehaviour
{
    public SceneManagement sceneManagement;
    public SimulationUI simulationUI;
    public int callBackCounter;
    public bool runSimulation;

    public List<List<List<int>>> cells;
    public List<List<List<int>>> pheromones;
    public List<List<List<bool>>> agentLocations;
    public List<AgentBehaviour> agents;

    int timestep = 0;


    private void Start() {
        cells = new List<List<List<int>>>();
        pheromones = new List<List<List<int>>>();
        agentLocations = new List<List<List<bool>>>();
        agents = new List<AgentBehaviour>();
    }

    public void StartSimulation() {
        runSimulation = true;
        simulationUI.nextTimestepButton.interactable = false;
        StartCoroutine(nameof(Simulate));
    }

    public void StepThroughTime() {
        timestep++;
        foreach (AgentBehaviour agent in agents) {
            agent.OnTimestep();
        }
    }

    IEnumerator Simulate() {
        while (runSimulation) {
            callBackCounter = 0;
            StepThroughTime();
            yield return new WaitWhile(() => callBackCounter < agents.Count);
            //yield return new WaitForSecondsRealtime(0.1f);
        }
        
    }

}
