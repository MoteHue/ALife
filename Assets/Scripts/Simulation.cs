using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Simulation : MonoBehaviour
{
    public SceneManagement sceneManagement;
    public SimulationUI simulationUI;
    int timestep = 0;
    public int callBackCounter;
    int agentCount;
    public bool runSimulation;


    public void StartSimulation() {
        runSimulation = true;
        simulationUI.nextTimestepButton.interactable = false;
        agentCount = sceneManagement.agents.Count;
        StartCoroutine(nameof(Simulate));
    }

    public void StepThroughTime() {
        timestep++;
        foreach (AgentBehaviour agent in sceneManagement.agents) {
            agent.OnTimestep();
        }
    }

    IEnumerator Simulate() {
        while (runSimulation) {
            callBackCounter = 0;
            StepThroughTime();
            yield return new WaitWhile(() => callBackCounter < agentCount);
            yield return new WaitForSecondsRealtime(0.1f);
        }
        
    }

}
