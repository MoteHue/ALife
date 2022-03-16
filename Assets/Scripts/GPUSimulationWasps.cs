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

	[SerializeField]
	Text genPopText;

	[SerializeField]
	Image progressImage;

	const int resolution = 60;

	ComputeBuffer
		agentValuesBuffer,
		cellValuesBuffer,
		spawnLocationsBuffer,
		pastAgentValuesBuffer,
		pastCellValuesBuffer,
		microrulesBuffer;

	float step = 1f;
	int indexStep = 0;
	int count = 0;
	int microruleCount;
	Bounds bounds;

	int popSize = 30;
	int noOfGenerations = 100;

	List<List<float>> genomes;

	List<List<(List<float>, float)>> results;

	int popCounter = 0;
	int genCounter = 0;
	List<(List<float>, float)> result;

	public bool simulationRunning = true;

	bool cellsEnabled = true;
	bool agentsEnabled = true;

	List<Vector3> spawnLocations;
	List<float> microrules;

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
		microruleCountId = Shader.PropertyToID("_MicroruleCount"),
		microrulesId = Shader.PropertyToID("_Microrules"),
		counterId = Shader.PropertyToID("_Counter");

    private void Start() {
		AssignMicrorules4N();

		GameObject floor = Instantiate(floorPrefab, transform.position, transform.rotation);
		floor.GetComponent<Floor>().SetScale(resolution, resolution, resolution);

		agentValuesBuffer = new ComputeBuffer(resolution * resolution * resolution, sizeof(float));
		cellValuesBuffer = new ComputeBuffer(resolution * resolution * resolution, sizeof(float));
		pastAgentValuesBuffer = new ComputeBuffer(resolution * resolution * resolution, sizeof(float));
		pastCellValuesBuffer = new ComputeBuffer(resolution * resolution * resolution, sizeof(float));
		spawnLocationsBuffer = new ComputeBuffer(resolution * resolution * resolution, sizeof(float) * 3);
		microrulesBuffer = new ComputeBuffer(28 * microruleCount, sizeof(float));

		bounds = new Bounds(Vector3.zero, Vector3.one * resolution);
		indexStep = Mathf.CeilToInt(resolution / 4f);
		computeShader.SetFloat(stepId, step);

		spawnLocations = new List<Vector3>();
		GenerateSpawnLocations();
		spawnLocationsBuffer.SetData(spawnLocations);

		microrulesBuffer.SetData(microrules);

		computeShader.SetBuffer(0, agentValuesId, agentValuesBuffer);
		computeShader.SetBuffer(0, cellValuesId, cellValuesBuffer);
		computeShader.SetBuffer(0, pastAgentValuesId, pastAgentValuesBuffer);
		computeShader.SetBuffer(0, pastCellValuesId, pastCellValuesBuffer);
		computeShader.SetBuffer(0, spawnLocationsId, spawnLocationsBuffer);
		computeShader.SetBuffer(0, microrulesId, microrulesBuffer);

		computeShader.SetInt(resolutionId, resolution);
		computeShader.SetInt(indexStepId, indexStep);
		computeShader.SetInt(counterId, 0);
		computeShader.SetInt(microruleCountId, microruleCount);

		agentMaterial.SetInt(resolutionId, resolution);
		cellMaterial.SetInt(resolutionId, resolution);

		//SetRandomGenomes();
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
		microrulesBuffer.Release();
		microrulesBuffer = null;
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
		DoSimulation(5000);
		//DoGeneticAlgorithm();
		ShowMesh();
	}

	/*void DoGeneticAlgorithm() {
		if (genCounter < noOfGenerations) {
			if (popCounter < popSize) {
				if (count == 0) {
					rangeMin = Mathf.Max(0.001f, genomes[popCounter][0] * 0.5f);
					rangeMax = 0.5f + genomes[popCounter][1];
				}
				if (count % 10 == 0) {
					progressImage.transform.localScale = new Vector3(count / 1000f, 1f, 1f);
				}
				bool finished = DoSimulation(1000);
				if (finished) {
					float fitness = CalcDomeFitness();
					result.Add((genomes[popCounter], fitness));
					popCounter++;
					ClearBuffers();
					count = 0;
					genPopText.text = $"Gen: {genCounter} Pop: {popCounter}";
				}
			} else {
				results.Add(result);
				popCounter = 0;
				(List<float>, float) bestResult = result[0];
				for (int i = 1; i < popSize; i++) {
					if (results[genCounter][i].Item2 > bestResult.Item2) {
						bestResult = result[i];
					}
				}
				genomes[0] = bestResult.Item1;
				for (int i = 1; i < popSize; i++) {
					genomes[i] = MutateGenome(bestResult.Item1);
				}
				Debug.Log($"BEST: gen: {genCounter}, bestmin: {bestResult.Item1[0]}, bestmax: {bestResult.Item1[1]}, fitness: {bestResult.Item2}");
				string output = $"gen {genCounter} results\n";
				for (int i = 0; i < popSize; i++) {
					output += $"genome {i}: [{result[i].Item1[0]}, {result[i].Item1[1]}], fitness: {result[i].Item2}\n";
				}
				Debug.Log(output);
				result = new List<(List<float>, float)>();
				genCounter++;
				output = $"gen {genCounter} setup\n";
				for (int i = 0; i < popSize; i++) {
					output += $"genome {i}: [{genomes[i][0]}, {genomes[i][1]}]\n";
				}
				Debug.Log(output);
				genPopText.text = $"Gen: {genCounter} Pop: {popCounter}";
			}
		}
	}*/

	float CalcLineFitness() {
		return 0;
    }

	bool DoSimulation(int frameCount) {
		if (count == 0) {
			List<float> values = SpawnAgents(0);
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
			for (int y = 0; y < resolution; y++) {
				for (int z = 0; z < resolution; z++) {
					spawnLocations.Add(new Vector3(x, y, z));
				}
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

	void AssignMicrorules4N() {
		microruleCount = 66;

		microrules = new List<float> { 0,0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,1,0,0,0,0, 2, 0 };

		microrules.AddRange(new List<float> { 0,0,0,0,0,0,0,0,0, 0,0,0,2,0,0,0,0, 0,0,0,1,0,0,0,0,0, 2, 0 });
		microrules.AddRange(new List<float> { 0,0,0,0,0,0,0,0,0, 2,2,0,0,0,0,0,0, 1,0,0,0,0,0,0,0,0, 2, 0 });
		microrules.AddRange(new List<float> { 0,0,0,0,0,0,0,0,0, 0,2,2,0,2,0,0,0, 0,1,0,0,0,0,0,0,0, 2, 0 });
		microrules.AddRange(new List<float> { 0,0,0,0,0,0,0,0,0, 0,0,2,0,2,0,0,0, 0,0,1,0,0,0,0,0,0, 2, 0 });
		microrules.AddRange(new List<float> { 0,0,0,0,0,0,0,0,0, 0,0,0,2,0,2,2,0, 0,0,0,0,0,1,0,0,0, 2, 0 });
		microrules.AddRange(new List<float> { 0,0,0,0,0,0,0,0,0, 0,0,0,0,0,0,2,2, 0,0,0,0,0,0,0,0,1, 2, 0 });
		microrules.AddRange(new List<float> { 0,0,0,0,0,0,0,0,0, 0,0,0,2,0,2,2,2, 0,0,0,0,0,0,0,1,0, 2, 0 });
		microrules.AddRange(new List<float> { 0,0,0,0,0,0,0,0,0, 0,0,0,2,0,2,2,0, 0,0,0,0,0,0,1,0,0, 2, 0 });

		microrules.AddRange(new List<float> { 0,0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 2,2,2,2,2,2,2,2,2, 2, 0 });
		microrules.AddRange(new List<float> { 0,0,0,0,0,0,0,0,0, 0,0,0,2,0,0,0,0, 2,2,0,2,2,0,2,2,0, 1, 0 });
		microrules.AddRange(new List<float> { 0,0,0,0,0,0,0,0,0, 1,0,0,2,0,1,0,0, 2,2,0,2,2,0,2,2,0, 1, 0 });
		microrules.AddRange(new List<float> { 0,0,0,0,0,0,0,0,0, 0,0,0,1,0,2,1,0, 0,0,0,2,2,0,2,2,0, 1, 0 });

		microrules.AddRange(new List<float> { 1,0,0,1,0,0,1,0,0, 2,0,0,2,0,2,0,0, 0,0,0,0,0,0,0,0,0, 1, 0 });
		microrules.AddRange(new List<float> { 1,0,0,1,0,0,0,0,0, 2,1,0,2,0,0,0,0, 0,0,0,0,0,0,0,0,0, 1, 0 });
		microrules.AddRange(new List<float> { 1,1,0,0,0,0,0,0,0, 2,2,1,0,1,0,0,0, 0,0,0,0,0,0,0,0,0, 1, 0 });
		microrules.AddRange(new List<float> { 1,1,1,0,0,0,0,0,0, 2,2,2,0,1,0,0,0, 0,0,0,0,0,0,0,0,0, 1, 0 });
		microrules.AddRange(new List<float> { 0,0,1,0,0,0,0,0,0, 0,0,2,0,1,0,0,0, 0,0,0,0,0,0,0,0,0, 1, 0 });
		microrules.AddRange(new List<float> { 0,0,0,1,0,0,1,0,0, 1,1,0,2,0,2,1,0, 0,0,0,0,0,0,0,0,0, 1, 0 });
		microrules.AddRange(new List<float> { 0,0,1,0,0,1,0,0,0, 0,0,2,0,2,0,0,1, 0,0,0,0,0,0,0,0,0, 1, 0 });
		microrules.AddRange(new List<float> { 0,0,1,0,0,0,0,0,0, 0,1,2,0,1,0,0,2, 0,0,0,0,0,0,0,0,0, 1, 0 });
		microrules.AddRange(new List<float> { 0,0,0,0,0,0,0,0,1, 0,0,0,0,1,2,1,2, 0,0,0,0,0,0,0,0,0, 1, 0 });
		microrules.AddRange(new List<float> { 0,0,0,0,0,0,1,1,0, 0,0,0,0,1,2,1,2, 0,0,0,0,0,0,0,0,0, 1, 0 });
		microrules.AddRange(new List<float> { 1,1,1,0,0,0,0,0,0, 2,2,2,0,1,0,0,2, 0,0,0,0,0,0,0,0,0, 1, 0 });
		microrules.AddRange(new List<float> { 0,0,0,1,0,0,1,0,0, 1,0,0,2,0,2,1,0, 0,0,0,0,0,0,0,0,0, 1, 0 });
		microrules.AddRange(new List<float> { 0,0,0,0,0,0,1,0,0, 0,0,0,1,0,2,1,0, 0,0,0,0,0,0,0,0,0, 1, 0 });

		microrules.AddRange(new List<float> { 0,0,0,0,0,0,0,0,0, 1,0,0,1,0,1,0,0, 0,0,0,0,0,0,0,0,0, 2, 0 });
		microrules.AddRange(new List<float> { 0,0,0,0,0,0,0,0,0, 1,2,0,1,0,1,0,0, 0,0,0,0,0,0,0,0,0, 2, 0 });
		microrules.AddRange(new List<float> { 0,0,0,0,0,0,0,0,0, 1,2,0,1,0,0,0,0, 0,0,0,0,0,0,0,0,0, 2, 0 });
		microrules.AddRange(new List<float> { 0,0,0,0,0,0,0,0,0, 1,1,2,0,0,0,0,0, 0,0,0,0,0,0,0,0,0, 2, 0 });
		microrules.AddRange(new List<float> { 0,0,0,0,0,0,0,0,0, 1,2,0,2,0,0,0,0, 0,0,0,0,0,0,0,0,0, 2, 0 });
		microrules.AddRange(new List<float> { 0,0,0,0,0,0,0,0,0, 1,2,0,1,0,1,2,0, 0,0,0,0,0,0,0,0,0, 2, 0 });
		microrules.AddRange(new List<float> { 0,0,0,0,0,0,0,0,0, 2,0,0,1,0,1,2,0, 0,0,0,0,0,0,0,0,0, 2, 0 });

		microrules.AddRange(new List<float> { 0,0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 1,2,0,1,2,0,1,2,0, 2, 0 });
		microrules.AddRange(new List<float> { 0,0,0,0,0,0,0,0,0, 0,2,0,0,0,0,0,0, 1,2,0,1,2,0,1,2,0, 2, 0 });
		microrules.AddRange(new List<float> { 0,0,0,0,0,0,0,0,0, 0,2,0,0,0,0,0,0, 1,2,0,2,2,0,0,0,0, 2, 0 });
		microrules.AddRange(new List<float> { 0,0,0,0,0,0,0,0,0, 0,0,2,0,2,0,0,0, 1,1,2,2,2,2,0,0,0, 2, 0 });
		microrules.AddRange(new List<float> { 0,0,0,0,0,0,0,0,0, 0,0,0,0,2,0,0,0, 2,1,1,2,2,2,0,0,0, 2, 0 });
		microrules.AddRange(new List<float> { 0,0,0,0,0,0,0,0,0, 0,2,0,0,0,0,2,0, 1,2,0,1,2,0,1,2,0, 2, 0 });
		microrules.AddRange(new List<float> { 0,0,2,0,0,0,0,0,0, 0,0,2,0,2,0,0,0, 1,1,2,2,2,2,0,0,0, 2, 0 });
		microrules.AddRange(new List<float> { 0,0,2,0,0,0,0,0,0, 0,0,2,2,2,0,0,0, 1,1,2,2,2,2,0,0,0, 2, 0 });

		microrules.AddRange(new List<float> { 0,0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,2,0,0,2,0,0,2,0, 2, 0 });
		microrules.AddRange(new List<float> { 0,0,0,0,0,0,0,0,0, 0,2,0,0,0,0,0,0, 0,2,0,0,2,0,0,2,0, 2, 0 });
		microrules.AddRange(new List<float> { 0,0,0,0,0,0,0,0,0, 0,2,0,0,0,0,0,0, 0,2,0,0,2,0,2,2,0, 2, 0 });
		microrules.AddRange(new List<float> { 0,0,0,0,0,0,0,0,0, 0,2,0,0,0,0,0,0, 0,2,0,2,2,0,0,0,0, 2, 0 });
		microrules.AddRange(new List<float> { 0,0,0,0,0,0,0,0,0, 0,0,2,0,2,0,0,0, 0,0,2,2,2,2,0,0,0, 2, 0 });
		microrules.AddRange(new List<float> { 0,0,0,0,0,0,0,0,0, 2,2,0,0,0,0,2,0, 2,2,0,0,2,0,0,2,0, 2, 0 });
		microrules.AddRange(new List<float> { 0,0,0,0,0,0,0,0,0, 0,2,0,0,0,0,2,0, 0,2,0,0,2,0,0,2,0, 2, 0 });
		microrules.AddRange(new List<float> { 0,0,0,0,0,0,2,0,0, 0,0,0,2,0,2,0,0, 0,0,0,2,2,2,2,0,0, 2, 0 });
		microrules.AddRange(new List<float> { 2,0,0,0,0,0,0,0,0, 2,2,0,0,0,0,2,0, 2,2,0,0,2,0,0,2,0, 2, 0 });
		microrules.AddRange(new List<float> { 0,0,0,0,0,0,0,0,0, 0,2,0,0,0,0,2,2, 0,2,0,0,2,0,0,2,2, 2, 0 });

		microrules.AddRange(new List<float> { 0,0,2,0,0,2,2,2,2, 0,0,2,0,2,2,2,2, 0,0,2,0,0,2,2,2,2, 1, 0 });
		microrules.AddRange(new List<float> { 0,0,0,0,0,0,2,2,2, 0,0,0,0,1,2,2,2, 0,0,0,0,0,0,2,2,2, 1, 0 });
		microrules.AddRange(new List<float> { 2,0,0,2,0,0,2,2,2, 2,0,0,2,1,2,2,2, 2,0,0,2,0,0,2,2,2, 1, 0 });
		microrules.AddRange(new List<float> { 2,0,0,2,0,0,2,0,0, 2,0,0,2,0,2,1,1, 2,0,0,2,0,0,2,0,0, 1, 0 });
		microrules.AddRange(new List<float> { 0,0,2,0,0,2,0,0,2, 0,1,2,0,2,0,1,2, 0,0,2,0,0,2,0,0,2, 1, 0 });
		microrules.AddRange(new List<float> { 0,0,2,0,0,2,0,0,2, 0,1,2,0,2,1,1,2, 0,0,2,0,0,2,0,0,2, 1, 0 });
		microrules.AddRange(new List<float> { 2,2,2,0,0,0,0,0,0, 2,2,2,1,0,1,2,0, 2,2,2,0,0,0,0,0,0, 1, 0 });
		microrules.AddRange(new List<float> { 2,2,2,0,0,0,0,0,0, 2,2,2,1,0,2,0,0, 2,2,2,0,0,0,0,0,0, 1, 0 });
		microrules.AddRange(new List<float> { 0,0,2,0,0,2,0,0,2, 0,1,2,0,2,2,0,2, 0,0,2,0,0,2,0,0,2, 1, 0 });
		microrules.AddRange(new List<float> { 0,0,2,0,0,2,0,0,2, 0,1,2,2,2,1,1,2, 0,0,2,0,0,2,0,0,2, 1, 0 });

		microrules.AddRange(new List<float> { 0,0,0,0,0,0,0,0,0, 1,1,1,2,0,1,2,0, 0,0,0,0,0,0,0,0,0, 2, 0 });
		microrules.AddRange(new List<float> { 0,0,0,0,0,0,0,0,0, 1,1,1,2,0,2,0,0, 0,0,0,0,0,0,0,0,0, 2, 0 });
		microrules.AddRange(new List<float> { 0,0,0,0,0,0,0,0,0, 2,0,1,0,0,2,0,1, 0,0,0,0,0,0,0,0,0, 2, 0 });
		microrules.AddRange(new List<float> { 0,0,0,0,0,0,0,0,0, 0,2,1,2,1,1,1,1, 0,0,0,0,0,0,0,0,0, 2, 0 });
		microrules.AddRange(new List<float> { 0,0,0,0,0,0,0,0,0, 2,2,1,0,1,2,2,1, 0,0,0,0,0,0,0,0,0, 2, 0 });
	}
}	
