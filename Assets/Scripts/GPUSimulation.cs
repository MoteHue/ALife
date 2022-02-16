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

	List<Vector4> emptyValuesVector4;

	List<Vector3> spawnLocations;

	static readonly int
		materialPositionsId = Shader.PropertyToID("_Positions"),
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
		spawnLocationsId = Shader.PropertyToID("_SpawnLocations");

	void AddPositionsToBuffers() {
		List<Vector4> values = new List<Vector4>();
		for (int z = 0; z < resolution; z++) {
			for (int y = 0; y < resolution; y++) {
				for (int x = 0; x < resolution; x++) {
					Vector4 value = new Vector4(x, y, z, 0);
					values.Add(value);
				}
			}
		}
		cellValuesBuffer.SetData(values);
		pheromoneValuesBuffer.SetData(values);
		pastCellValuesBuffer.SetData(values);
		pastPheromoneValuesBuffer.SetData(values);
	}

    private void Start() {
		GameObject floor = Instantiate(floorPrefab, transform.position, transform.rotation);
		floor.GetComponent<Floor>().SetScale(resolution, resolution, resolution);

		agentValuesBuffer = new ComputeBuffer(maxResolution * maxResolution * maxResolution, sizeof(float) * 4);
		cellValuesBuffer = new ComputeBuffer(maxResolution * maxResolution * maxResolution, sizeof(float) * 4);
		pheromoneValuesBuffer = new ComputeBuffer(maxResolution * maxResolution * maxResolution, sizeof(float) * 4);
		pastAgentValuesBuffer = new ComputeBuffer(maxResolution * maxResolution * maxResolution, sizeof(float) * 4);
		pastCellValuesBuffer = new ComputeBuffer(maxResolution * maxResolution * maxResolution, sizeof(float) * 4);
		pastPheromoneValuesBuffer = new ComputeBuffer(maxResolution * maxResolution * maxResolution, sizeof(float) * 4);
		spawnLocationsBuffer = new ComputeBuffer(resolution * 2 + (resolution - 2) * 4 + (resolution - 4) * 2, sizeof(float) * 3);

		bounds = new Bounds(Vector3.zero, Vector3.one * resolution);
		indexStep = Mathf.CeilToInt(resolution / 4f);
		computeShader.SetFloat(stepId, step);

		spawnLocations = new List<Vector3>();
		GenerateSpawnLocations();
		AddPositionsToBuffers();
		List<Vector4> values = SpawnAgents(300);
		agentValuesBuffer.SetData(values);
		pastAgentValuesBuffer.SetData(values);
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
	}

	void UpdateFunctionOnGPU() {
		
		computeShader.SetFloat(timeId, Time.time);

		computeShader.Dispatch(0, 2, 2, 2);

		Vector4[] values = new Vector4[maxResolution * maxResolution * maxResolution];
		agentValuesBuffer.GetData(values);
		pastAgentValuesBuffer.SetData(values);

		cellValuesBuffer.GetData(values);
		pastCellValuesBuffer.SetData(values);

		pheromoneValuesBuffer.GetData(values);
		pastPheromoneValuesBuffer.SetData(values);

		agentMaterial.SetBuffer(materialPositionsId, agentValuesBuffer);
		agentMaterial.SetFloat(stepId, step);
		Graphics.DrawMeshInstancedProcedural(agentMesh, 0, agentMaterial, bounds, (int)Mathf.Pow(indexStep * 4, 3));

		cellMaterial.SetBuffer(materialPositionsId, cellValuesBuffer);
		cellMaterial.SetFloat(stepId, step);
		Graphics.DrawMeshInstancedProcedural(cellMesh, 0, cellMaterial, bounds, (int)Mathf.Pow(indexStep * 4, 3));

		pheromoneMaterial.SetBuffer(materialPositionsId, pheromoneValuesBuffer);
		pheromoneMaterial.SetFloat(stepId, step);
		Graphics.DrawMeshInstancedProcedural(pheromoneMesh, 0, pheromoneMaterial, bounds, (int)Mathf.Pow(indexStep * 4, 3));
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

	List<int> GenerateRandomChoices(int minInclusive, int maxExclusive, int amount) {
		List<int> returnList = new List<int>();

		for (int i = 0; i < amount; i++) {
			returnList.Add(Random.Range(minInclusive, maxExclusive));
        }

		return returnList;
    }

	List<Vector4> SpawnAgents(int amount) {
		List<Vector4> returnList = new List<Vector4>();
		for (int z = 0; z < resolution; z++) {
			for (int y = 0; y < resolution; y++) {
				for (int x = 0; x < resolution; x++) {
					Vector4 value = new Vector4(x, y, z, 0);
					returnList.Add(value);
				}
			}
		}

		int counter = 0;
		while (counter < amount) {
			Vector3 newPos = spawnLocations[Random.Range(0, spawnLocations.Count)];

			int index = (int)(newPos.x + resolution * newPos.y + resolution * resolution * newPos.z);

			returnList[index] = new Vector4(newPos.x, newPos.y, newPos.z, returnList[index].w + 1);
			counter++;
		}

		return returnList;
	}

}	
