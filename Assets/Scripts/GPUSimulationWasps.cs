using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GPUSimulationWasps : MonoBehaviour {

	[SerializeField]
	GameObject floorPrefab;

	[SerializeField]
	ComputeShader computeShader;

	[SerializeField]
	Material agentMaterial;
	[SerializeField]
	Mesh agentMesh;

	[SerializeField]
	Material cellMaterial;
	[SerializeField]
	Mesh cellMesh;

	const int resolution = 60;

	ComputeBuffer
		agentValuesBuffer,
		cellValuesBuffer,
		spawnLocationsBuffer,
		pastAgentValuesBuffer,
		pastCellValuesBuffer;

	float step = 1f;
	int indexStep = 0;
	int count = 0;
	Bounds bounds;

	public bool simulationRunning = true;

	bool cellsEnabled = true;
	bool agentsEnabled = true;

	List<Vector3> spawnLocations;

	static readonly int
		materialValuesId = Shader.PropertyToID("_Values"),
		agentValuesId = Shader.PropertyToID("_AgentValues"),
		cellValuesId = Shader.PropertyToID("_CellValues"),
		pastAgentValuesId = Shader.PropertyToID("_PastAgentValues"),
		pastCellValuesId = Shader.PropertyToID("_PastCellValues"),
		resolutionId = Shader.PropertyToID("_Resolution"),
		stepId = Shader.PropertyToID("_Step"),
		timeId = Shader.PropertyToID("_Time"),
		indexStepId = Shader.PropertyToID("_IndexStep"),
		spawnLocationsId = Shader.PropertyToID("_SpawnLocations"),
		meshEnabledId = Shader.PropertyToID("_Enabled"),
		counterId = Shader.PropertyToID("_Counter");

    private void Start() {


		GameObject floor = Instantiate(floorPrefab, transform.position, transform.rotation);
		floor.GetComponent<Floor>().SetScale(resolution, resolution, resolution);

		agentValuesBuffer = new ComputeBuffer(resolution * resolution * resolution, sizeof(float));
		cellValuesBuffer = new ComputeBuffer(resolution * resolution * resolution, sizeof(float));
		pastAgentValuesBuffer = new ComputeBuffer(resolution * resolution * resolution, sizeof(float));
		pastCellValuesBuffer = new ComputeBuffer(resolution * resolution * resolution, sizeof(float));
		spawnLocationsBuffer = new ComputeBuffer(resolution * resolution, sizeof(float) * 3);

		bounds = new Bounds(Vector3.zero, Vector3.one * resolution);
		indexStep = Mathf.CeilToInt(resolution / 4f);
		computeShader.SetFloat(stepId, step);

		spawnLocations = new List<Vector3>();
		GenerateSpawnLocations();
		spawnLocationsBuffer.SetData(spawnLocations);

		computeShader.SetBuffer(0, agentValuesId, agentValuesBuffer);
		computeShader.SetBuffer(0, cellValuesId, cellValuesBuffer);
		computeShader.SetBuffer(0, pastAgentValuesId, pastAgentValuesBuffer);
		computeShader.SetBuffer(0, pastCellValuesId, pastCellValuesBuffer);
		computeShader.SetBuffer(0, spawnLocationsId, spawnLocationsBuffer);

		computeShader.SetInt(resolutionId, resolution);
		computeShader.SetInt(indexStepId, indexStep);
		computeShader.SetInt(counterId, 0);

		agentMaterial.SetInt(resolutionId, resolution);
		cellMaterial.SetInt(resolutionId, resolution);
	}

	void OnDisable() {
		agentValuesBuffer.Release();
		agentValuesBuffer = null;
		cellValuesBuffer.Release();
		cellValuesBuffer = null;
		pastAgentValuesBuffer.Release();
		pastAgentValuesBuffer = null;
		pastCellValuesBuffer.Release();
		pastCellValuesBuffer = null;
		spawnLocationsBuffer.Release();
		spawnLocationsBuffer = null;
	}

	void ClearBuffers() {
		float[] values = new float[resolution * resolution * resolution];
		agentValuesBuffer.SetData(values);
		pastAgentValuesBuffer.SetData(values);

		values = new float[resolution * resolution * resolution];
		cellValuesBuffer.SetData(values);
		pastCellValuesBuffer.SetData(values);
	}

	void Update() {
		DoSimulation(100);
		Debug.Log(count);
		ShowMesh();
	}

    bool DoSimulation(int frameCount) {
		if (count == 0) {
			List<float> values = SpawnAgents(300);
			agentValuesBuffer.SetData(values);
			pastAgentValuesBuffer.SetData(values);
		}

		if (count < frameCount) {
			DispatchCycleOfSimulation();
			return false;
        } else {
			return true;
        }
    }

	void ShowMesh() {
		if (agentsEnabled) {
			agentMaterial.SetFloat(stepId, step);
			agentMaterial.SetBuffer(materialValuesId, agentValuesBuffer);
			Graphics.DrawMeshInstancedProcedural(agentMesh, 0, agentMaterial, bounds, (int)Mathf.Pow(indexStep * 4, 3));
		}

		if (cellsEnabled) {
			cellMaterial.SetFloat(stepId, step);
			cellMaterial.SetBuffer(materialValuesId, cellValuesBuffer);
			Graphics.DrawMeshInstancedProcedural(cellMesh, 0, cellMaterial, bounds, (int)Mathf.Pow(indexStep * 4, 3));
		}
	}

    void DispatchCycleOfSimulation() {
		
		if (simulationRunning) {
			computeShader.SetFloat(timeId, Time.time);
			computeShader.SetInt(counterId, count);

			computeShader.Dispatch(0, 2, 2, 2);

			float[] values = new float[resolution * resolution * resolution];
			agentValuesBuffer.GetData(values);
			pastAgentValuesBuffer.SetData(values);

			values = new float[resolution * resolution * resolution];
			cellValuesBuffer.GetData(values);
			pastCellValuesBuffer.SetData(values);

			count++;
		}
	}

	void GenerateSpawnLocations() {
		for (int x = 0; x < resolution; x++) {
			for (int z = 0; z < resolution; z++) {
				spawnLocations.Add(new Vector3(x, 0, z));

			}
		}
	}

	List<float> SpawnAgents(int amount) {
		List<float> returnList = new List<float>();
		for (int i = 0; i < resolution * resolution * resolution; i++) {
			returnList.Add(0);
        }

		int counter = 0;
		while (counter < amount) {
			Vector3 newPos = spawnLocations[Random.Range(0, spawnLocations.Count)];

			int index = (int)(newPos.x + resolution * newPos.y + resolution * resolution * newPos.z);

			returnList[index]++;
			counter++;
		}

		return returnList;
	}

	public void ToggleCells() {
		cellsEnabled = !cellsEnabled;
	}

	public void ToggleAgents() {
		agentsEnabled = !agentsEnabled;
	}

}	
