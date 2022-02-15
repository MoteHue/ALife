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
		agentPositionsBuffer,
		cellPositionsBuffer,
		pheromonePositionsBuffer,
		agentValuesBuffer,
		cellValuesBuffer,
		pheromoneValuesBuffer,
		spawnLocationsBuffer;

	int counter = 0;
	float step = 1f;
	int indexStep = 0;
	Bounds bounds;

	List<Vector3> emptyValuesVector3;
	List<int> emptyValuesInt;
	List<float> emptyValuesFloat;

	List<Vector3> spawnLocations;

	static readonly int
		materialPositionsId = Shader.PropertyToID("_Positions"),
		agentPositionsId = Shader.PropertyToID("_AgentPositions"),
		cellPositionsId = Shader.PropertyToID("_CellPositions"),
		pheromonePositionsId = Shader.PropertyToID("_PheromonePositions"),
		agentValuesId = Shader.PropertyToID("_AgentValues"),
		cellValuesId = Shader.PropertyToID("_CellValues"),
		pheromoneValuesId = Shader.PropertyToID("_PheromoneValues"),
		resolutionId = Shader.PropertyToID("_Resolution"),
		stepId = Shader.PropertyToID("_Step"),
		timeId = Shader.PropertyToID("_Time"),
		indexStepId = Shader.PropertyToID("_IndexStep"),
		spawnLocationsId = Shader.PropertyToID("_SpawnLocations");

    private void Start() {
		GameObject floor = Instantiate(floorPrefab, transform.position, transform.rotation);
		floor.GetComponent<Floor>().SetScale(resolution, resolution, resolution);

		agentPositionsBuffer = new ComputeBuffer(maxResolution * maxResolution * maxResolution, 3 * sizeof(float));
		cellPositionsBuffer = new ComputeBuffer(maxResolution * maxResolution * maxResolution, 3 * sizeof(float));
		pheromonePositionsBuffer = new ComputeBuffer(maxResolution * maxResolution * maxResolution, 3 * sizeof(float));
		agentValuesBuffer = new ComputeBuffer(maxResolution * maxResolution * maxResolution, sizeof(int));
		cellValuesBuffer = new ComputeBuffer(maxResolution * maxResolution * maxResolution, sizeof(int));
		pheromoneValuesBuffer = new ComputeBuffer(maxResolution * maxResolution * maxResolution, sizeof(float));
		spawnLocationsBuffer = new ComputeBuffer(resolution * 2 + (resolution - 2) * 4 + (resolution - 4) * 2, 3 * sizeof(float));

		bounds = new Bounds(Vector3.zero, Vector3.one * resolution);
		indexStep = Mathf.CeilToInt(resolution / 4f);
		computeShader.SetFloat(stepId, step);

		spawnLocations = new List<Vector3>();
		GenerateSpawnLocations();
		emptyValuesVector3 = new List<Vector3>();
		emptyValuesInt = new List<int>();
		emptyValuesFloat = new List<float>();
		for (int i = 0; i < (maxResolution * maxResolution * maxResolution); i++) {
			emptyValuesVector3.Add(new Vector3(-1, -1, -1));
			emptyValuesInt.Add(0);
			emptyValuesFloat.Add(0f);
		}
		agentPositionsBuffer.SetData(emptyValuesVector3);
		cellPositionsBuffer.SetData(emptyValuesVector3);
		pheromonePositionsBuffer.SetData(emptyValuesVector3);
		agentValuesBuffer.SetData(SpawnAgents(300));
		//agentValuesBuffer.SetData(emptyValuesInt);
		cellValuesBuffer.SetData(emptyValuesInt);
		pheromoneValuesBuffer.SetData(emptyValuesFloat);
		spawnLocationsBuffer.SetData(spawnLocations);

		computeShader.SetBuffer(0, agentPositionsId, agentPositionsBuffer);
		computeShader.SetBuffer(0, cellPositionsId, cellPositionsBuffer);
		computeShader.SetBuffer(0, pheromonePositionsId, pheromonePositionsBuffer);
		computeShader.SetBuffer(0, agentValuesId, agentValuesBuffer);
		computeShader.SetBuffer(0, cellValuesId, cellValuesBuffer);
		computeShader.SetBuffer(0, pheromoneValuesId, pheromoneValuesBuffer);
		computeShader.SetBuffer(0, spawnLocationsId, spawnLocationsBuffer);

		computeShader.SetInt(resolutionId, resolution);
		computeShader.SetInt(indexStepId, indexStep);
	}

	void OnDisable() {
		agentPositionsBuffer.Release();
		agentPositionsBuffer = null;
		cellPositionsBuffer.Release();
		cellPositionsBuffer = null;
		pheromonePositionsBuffer.Release();
		pheromonePositionsBuffer = null;
		agentValuesBuffer.Release();
		agentValuesBuffer = null;
		cellValuesBuffer.Release();
		cellValuesBuffer = null;
		pheromoneValuesBuffer.Release();
		pheromoneValuesBuffer = null;
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

		agentMaterial.SetBuffer(materialPositionsId, agentPositionsBuffer);
		agentMaterial.SetFloat(stepId, step);
		Graphics.DrawMeshInstancedProcedural(agentMesh, 0, agentMaterial, bounds, (int)Mathf.Pow(indexStep * 4, 3));

		cellMaterial.SetBuffer(materialPositionsId, cellPositionsBuffer);
		cellMaterial.SetFloat(stepId, step);
		Graphics.DrawMeshInstancedProcedural(cellMesh, 0, cellMaterial, bounds, (int)Mathf.Pow(indexStep * 4, 3));

		pheromoneMaterial.SetBuffer(materialPositionsId, pheromonePositionsBuffer);
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

	List<int> SpawnAgents(int amount) {
		List<int> returnList = new List<int>();

		for (int i = 0; i < (maxResolution * maxResolution * maxResolution); i++) {
			returnList.Add(0);
		}

		int counter = 0;
		while (counter < amount) {
			Vector3 newPos = spawnLocations[Random.Range(0, spawnLocations.Count)];

			returnList[(int)(newPos.x + resolution * newPos.y + resolution * resolution * newPos.z)]++;
			counter++;
		}

		return returnList;
	}

}	
