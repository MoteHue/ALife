using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QueenAgent : Agent
{

    // Start is called before the first frame update
    public override void Start() {
        base.Start();
    }

    public override void OnTimestep() {
        if (sim == null) {
            sim = FindObjectOfType<Simulation>();
        }
        sim.pheromoneValues[pos.x][pos.y][pos.z] = 5f;
        sim.agentCallBackCounter++;
    }


  
}
