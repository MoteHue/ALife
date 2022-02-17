using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPUSimulation : MonoBehaviour {

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

	[SerializeField]
	Material pheromoneMaterial;
	[SerializeField]
	Mesh pheromoneMesh;

	const int maxResolution = 64;

	[SerializeField, Range(10, maxResolution)]
	int resolution = 20;

	ComputeBuffer
		agentValuesBuffer,
		cellValuesBuffer,
		pheromoneValuesBuffer,
		spawnLocationsBuffer,
		pastAgentValuesBuffer,
		pastCellValuesBuffer,
		pastPheromoneValuesBuffer;

	int counter = 0;
	float step = 1f;
	int indexStep = 0;
	Bounds bounds;

	bool pheromonesEnabled = true;
	bool cellsEnabled = true;
	bool agentsEnabled = true;

	List<Vector3> spawnLocations;

	static readonly int
		materialValuesId = Shader.PropertyToID("_Values"),
		agentValuesId = Shader.PropertyToID("_AgentValues"),
		cellValuesId = Shader.PropertyToID("_CellValues"),
		pheromoneValuesId = Shader.PropertyToID("_PheromoneValues"),
		pastAgentValuesId = Shader.PropertyToID("_PastAgentValues"),
		pastCellValuesId = Shader.PropertyToID("_PastCellValues"),
		pastPheromoneValuesId = Shader.PropertyToID("_PastPheromoneValues"),
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

		agentValuesBuffer = new ComputeBuffer(maxResolution * maxResolution * maxResolution, sizeof(float));
		cellValuesBuffer = new ComputeBuffer(maxResolution * maxResolution * maxResolution, sizeof(float));
		pheromoneValuesBuffer = new ComputeBuffer(maxResolution * maxResolution * maxResolution, sizeof(float));
		pastAgentValuesBuffer = new ComputeBuffer(maxResolution * maxResolution * maxResolution, sizeof(float));
		pastCellValuesBuffer = new ComputeBuffer(maxResolution * maxResolution * maxResolution, sizeof(float));
		pastPheromoneValuesBuffer = new ComputeBuffer(maxResolution * maxResolution * maxResolution, sizeof(float));
		spawnLocationsBuffer = new ComputeBuffer(resolution * 2 + (resolution - 2) * 4 + (resolution - 4) * 2, sizeof(float) * 3);

		bounds = new Bounds(Vector3.zero, Vector3.one * resolution);
		indexStep = Mathf.CeilToInt(resolution / 4f);
		computeShader.SetFloat(stepId, step);

		spawnLocations = new List<Vector3>();
		GenerateSpawnLocations();
		spawnLocationsBuffer.SetData(spawnLocations);

		computeShader.SetBuffer(0, agentValuesId, agentValuesBuffer);
		computeShader.SetBuffer(0, cellValuesId, cellValuesBuffer);
		computeShader.SetBuffer(0, pheromoneValuesId, pheromoneValuesBuffer);
		computeShader.SetBuffer(0, pastAgentValuesId, pastAgentValuesBuffer);
		computeShader.SetBuffer(0, pastCellValuesId, pastCellValuesBuffer);
		computeShader.SetBuffer(0, pastPheromoneValuesId, pastPheromoneValuesBuffer);
		computeShader.SetBuffer(0, spawnLocationsId, spawnLocationsBuffer);

		computeShader.SetInt(resolutionId, resolution);
		computeShader.SetInt(indexStepId, indexStep);
		computeShader.SetBool(meshEnabledId, pheromonesEnabled);
		computeShader.SetInt(counterId, counter);

		agentMaterial.SetInt(resolutionId, resolution);
		cellMaterial.SetInt(resolutionId, resolution);
		pheromoneMaterial.SetInt(resolutionId, resolution);
	}

	void OnDisable() {
		agentValuesBuffer.Release();
		agentValuesBuffer = null;
		cellValuesBuffer.Release();
		cellValuesBuffer = null;
		pheromoneValuesBuffer.Release();
		pheromoneValuesBuffer = null;
		pastAgentValuesBuffer.Release();
		pastAgentValuesBuffer = null;
		pastCellValuesBuffer.Release();
		pastCellValuesBuffer = null;
		pastPheromoneValuesBuffer.Release();
		pastPheromoneValuesBuffer = null;
		spawnLocationsBuffer.Release();
		spawnLocationsBuffer = null;
	}

	void Update() {
		UpdateFunctionOnGPU();
		counter++;
		if (counter == 200) {
			List<float> values = SpawnAgents(300);
			agentValuesBuffer.SetData(values);
			pastAgentValuesBuffer.SetData(values);
		}
	}

	void UpdateFunctionOnGPU() {
		
		computeShader.SetFloat(timeId, Time.time);
		computeShader.SetInt(counterId, counter);

		computeShader.Dispatch(0, 2, 2, 2);

		float[] values = new float[maxResolution * maxResolution * maxResolution];
		agentValuesBuffer.GetData(values);
		pastAgentValuesBuffer.SetData(values);

		cellValuesBuffer.GetData(values);
		pastCellValuesBuffer.SetData(values);

		pheromoneValuesBuffer.GetData(values);
		pastPheromoneValuesBuffer.SetData(values);

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
		
		if (pheromonesEnabled) {
			pheromoneMaterial.SetFloat(stepId, step);
			pheromoneMaterial.SetBuffer(materialValuesId, pheromoneValuesBuffer);
			Graphics.DrawMeshInstancedProcedural(pheromoneMesh, 0, pheromoneMaterial, bounds, (int)Mathf.Pow(indexStep * 4, 3));
		}
		
	}

	void GenerateSpawnLocations() {
		for (int i = 0; i < resolution; i++) {
			spawnLocations.Add(new Vector3(i, 0, 0));
			spawnLocations.Add(new Vector3(i, 0, 1));
			spawnLocations.Add(new Vector3(i, 0, resolution - 2));
			spawnLocations.Add(new Vector3(i, 0, resolution - 1));
		}
		for (int j = 2; j < resolution - 2; j++) {
			spawnLocations.Add(new Vector3(0, 0, j));
			spawnLocations.Add(new Vector3(1, 0, j));
			spawnLocations.Add(new Vector3(resolution - 2, 0, j));
			spawnLocations.Add(new Vector3(resolution - 1, 0, j));
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

	public void TogglePheromones() {
		pheromonesEnabled = !pheromonesEnabled;
    }

	public void ToggleCells() {
		cellsEnabled = !cellsEnabled;
	}

	public void ToggleAgents() {
		agentsEnabled = !agentsEnabled;
	}

}	
