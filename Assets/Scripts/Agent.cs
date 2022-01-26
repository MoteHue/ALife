using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Agent : MonoBehaviour
{
    public SceneManagement sceneManagement;
    public Simulation sim;
    public Vector3Int pos;

    public virtual void Start() {
        sceneManagement = FindObjectOfType<SceneManagement>();
        sim = FindObjectOfType<Simulation>();
    }

    public virtual void OnTimestep() {
        sim.agentCallBackCounter++;
    }    

}
