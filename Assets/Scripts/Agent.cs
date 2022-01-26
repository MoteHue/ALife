using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Agent : MonoBehaviour
{
    public SceneManagement sceneManagement;
    public GridUI gridUI;
    public Simulation sim;
    public Vector3Int pos;

    public virtual void Start() {
        sceneManagement = FindObjectOfType<SceneManagement>();
        gridUI = FindObjectOfType<GridUI>();
        sim = FindObjectOfType<Simulation>();
    }

    public virtual void OnTimestep() {
        sim.agentCallBackCounter++;
    }    

}
