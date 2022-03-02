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
	Material queenPheromoneMaterial;
	[SerializeField]
	Material cementPheromoneMaterial;
	[SerializeField]
	Mesh pheromoneMesh;

	const int resolution = 60;

	ComputeBuffer
		agentValuesBuffer,
		cellValuesBuffer,
		pheromoneValuesBuffer,
		spawnLocationsBuffer,
		pastAgentValuesBuffer,
		pastCellValuesBuffer,
		pastPheromoneValuesBuffer;

	float step = 1f;
	int indexStep = 0;
	int count = 0;
	Bounds bounds;

	public bool simulationRunning = false;

	bool pheromonesEnabled = true;
	bool cellsEnabled = true;
	bool agentsEnabled = true;

	List<Vector3> spawnLocations;

	List<Vector3Int> queenCells;

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
		counterId = Shader.PropertyToID("_Counter"),
		rangeMinId = Shader.PropertyToID("_RangeMin"),
		rangeMaxId = Shader.PropertyToID("_RangeMax");

    private void Start() {
		queenCells = new List<Vector3Int>();
		/*for (int i = 27; i <= 33; i++) {
			queenCells.Add(new Vector3Int(i, 0, i));
			queenCells.Add(new Vector3Int(i, 0, i - 1));
			queenCells.Add(new Vector3Int(i - 1, 0, i));
			if (i != 33) queenCells.Add(new Vector3Int(i, 1, i));
		}*/
		for (int x = 29; x <= 31; x++) {
			for (int y = 0; y <= 2; y++) {
				for (int z = 29; z <= 31; z++) {
					queenCells.Add(new Vector3Int(x, y, z));
                }
            }
		}


		GameObject floor = Instantiate(floorPrefab, transform.position, transform.rotation);
		floor.GetComponent<Floor>().SetScale(resolution, resolution, resolution);

		agentValuesBuffer = new ComputeBuffer(resolution * resolution * resolution, sizeof(float));
		cellValuesBuffer = new ComputeBuffer(resolution * resolution * resolution, sizeof(float));
		pheromoneValuesBuffer = new ComputeBuffer(resolution * resolution * resolution, sizeof(float) * 2);
		pastAgentValuesBuffer = new ComputeBuffer(resolution * resolution * resolution, sizeof(float));
		pastCellValuesBuffer = new ComputeBuffer(resolution * resolution * resolution, sizeof(float));
		pastPheromoneValuesBuffer = new ComputeBuffer(resolution * resolution * resolution, sizeof(float) * 2);
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
		computeShader.SetInt(counterId, 0);

		computeShader.SetFloat(rangeMinId, 0.1f);
		computeShader.SetFloat(rangeMaxId, 0.5f);

		agentMaterial.SetInt(resolutionId, resolution);
		cellMaterial.SetInt(resolutionId, resolution);
		queenPheromoneMaterial.SetInt(resolutionId, resolution);
		cementPheromoneMaterial.SetInt(resolutionId, resolution);

		SetQueenCells();
	}

	void SetQueenCells() {
		float[] values = new float[resolution * resolution * resolution];
		cellValuesBuffer.GetData(values);
		foreach (Vector3Int queenCell in queenCells) {
			values[queenCell.x + queenCell.y * resolution + queenCell.z * resolution * resolution] = 2;
        }
		cellValuesBuffer.SetData(values);
		pastCellValuesBuffer.SetData(values);
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
		if (count == 200) {
			List<float> values = SpawnAgents(300);
			agentValuesBuffer.SetData(values);
			pastAgentValuesBuffer.SetData(values);
		}
	}

	void UpdateFunctionOnGPU() {
		
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

			values = new float[resolution * resolution * resolution * 2];
			pheromoneValuesBuffer.GetData(values);
			pastPheromoneValuesBuffer.SetData(values);
			count++;
		}

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
			queenPheromoneMaterial.SetFloat(stepId, step);
			queenPheromoneMaterial.SetBuffer(materialValuesId, pheromoneValuesBuffer);
			Graphics.DrawMeshInstancedProcedural(pheromoneMesh, 0, queenPheromoneMaterial, bounds, (int)Mathf.Pow(indexStep * 4, 3));

			cementPheromoneMaterial.SetFloat(stepId, step);
			cementPheromoneMaterial.SetBuffer(materialValuesId, pheromoneValuesBuffer);
			Graphics.DrawMeshInstancedProcedural(pheromoneMesh, 0, cementPheromoneMaterial, bounds, (int)Mathf.Pow(indexStep * 4, 3));
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
