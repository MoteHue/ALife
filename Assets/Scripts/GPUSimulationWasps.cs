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
	int microruleCount = 50;
	Bounds bounds;

	List<List<float>> templates;

	int popSize = 30;
	int noOfGenerations = 500;

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
		GenerateTemplates();
		//AssignMicrorules4N();

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
		result = new List<(List<float>, float)>();
		results = new List<List<(List<float>, float)>>();

		spawnLocations = new List<Vector3>();
		GenerateSpawnLocations();
		spawnLocationsBuffer.SetData(spawnLocations);

		SetRandomGenomes();

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

	float Gaussian(float mu, float sigma) {
		float u1 = Random.Range(0f, 1f);
		float u2 = Random.Range(0f, 1f);

		float rand_std_normal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2);

		return mu + sigma * rand_std_normal;
	}

	void Update() {
		//DoSimulation(5000);
		DoGeneticAlgorithm();
		ShowMesh();
	}

	bool GenomeAlreadyContainsMicrorule(List<float> genome, List<float> microrule) {
		for (int i = 0; i < genome.Count / 28; i++) {
			// rotation 0°
			bool microruleFound = true;
			for (int j = 0; j < 26; j++) {
				if (microrule[j] == genome[(i * 28) + j]) {
					microruleFound = false;
					break;
                }
            }
			if (microruleFound) return true;

			// rotation 90°
			List<int> indices = new List<int> { 2,5,8,1,4,7,0,3,6, 11,13,16,10,15,9,12,14, 19,22,25,18,21,24,17,20,23 };
			microruleFound = true;
			for (int j = 0; j < 26; j++) {
				if (microrule[j] == genome[(i * 28) + indices[j]]) {
					microruleFound = false;
					break;
				}
			}
			if (microruleFound) return true;

			// rotation 180°
			indices = new List<int> { 8,7,6,5,4,3,2,1,0, 16,15,14,13,12,11,10,9, 25,24,23,22,21,20,19,18,17 };
			microruleFound = true;
			for (int j = 0; j < 26; j++) {
				if (microrule[j] == genome[(i * 28) + indices[j]]) {
					microruleFound = false;
					break;
				}
			}
			if (microruleFound) return true;

			// rotation 270°
			indices = new List<int> { 6,3,0,7,4,1,8,5,2, 14,12,9,15,10,16,13,11, 23,20,17,24,21,18,25,22,19 };
			microruleFound = true;
			for (int j = 0; j < 26; j++) {
				if (microrule[j] == genome[(i * 28) + indices[j]]) {
					microruleFound = false;
					break;
				}
			}
			if (microruleFound) return true;
		}
		return false;
    }

	void SetRandomGenomes() {
		genomes = new List<List<float>>();
		for (int i = 0; i < popSize; i++) {
			List<float> genome = new List<float>();
			for (int j = 0; j < microruleCount; j++) { 
				bool isNewMicrorule = false;
				List<float> microrule = new List<float>();
				while (!isNewMicrorule) {
					microrule = GenerateRandomMicrorule();
					isNewMicrorule = !GenomeAlreadyContainsMicrorule(genome, microrule);
				}
				string output = "";
				for (int k = 0; k < 28; k++) {
					output += $",{microrule[k]} ";
                }
				//Debug.Log(output);
				genome.AddRange(microrule);
            }
			genomes.Add(genome);
        }
    }

	void DoGeneticAlgorithm() {
		if (genCounter < noOfGenerations) {
			if (popCounter < popSize) {
				if (count == 0) {
					microrules = genomes[popCounter];
				}
				if (count % 10 == 0) {
					progressImage.transform.localScale = new Vector3(count / 100f, 1f, 1f);
				}
				bool finished = DoSimulation(100);
				if (finished) {
					float fitness = CalcLineFitness2();
					result.Add((genomes[popCounter], fitness));
					popCounter++;
					ClearBuffers();
					count = 0;
					genPopText.text = $"Gen: {genCounter} Pop: {popCounter}";
				}
			} else {
				results.Add(result);
				popCounter = 0;

				(List<float>, float) bestResult = results[genCounter][0];
				int bestResultIndex = 0;
				for (int i = 1; i < popSize; i++) {
					if (results[genCounter][i].Item2 > bestResult.Item2) {
						bestResult = results[genCounter][i];
						bestResultIndex = i;
					}
				}

				for (int i = 0; i < microruleCount; i++) {
					bestResult.Item1[i * 28 + 27] = 0;
                }

				for (int j = 0; j < popSize; j++) {
					// ------------------ Select new parents ----------------------- //
					float sum = 0;
					for (int i = 0; i < popSize; i++) {
						if (results[genCounter][i].Item2 > 0) {
							sum += results[genCounter][i].Item2;
						}
					}
					List<(int, float)> indicesAndProportions = new List<(int, float)>();
					for (int i = 0; i < popSize; i++) {
						if (results[genCounter][i].Item2 > 0) {
							indicesAndProportions.Add((i, (results[genCounter][i].Item2 / sum) * 100));
						}
					}
					(int, int) parentIndices = (0, 0);
					float choice = Random.Range(0f, 100f);
					float sum2 = 0;
					for (int i = 0; i < indicesAndProportions.Count; i++) {
						if (sum2 + indicesAndProportions[i].Item2 >= choice) {
							parentIndices.Item1 = i;
							sum -= results[genCounter][parentIndices.Item1].Item2;
						} else {
							sum2 += indicesAndProportions[i].Item2;
						}
					}
					indicesAndProportions = new List<(int, float)>();
					for (int i = 0; i < popSize; i++) {
						if (results[genCounter][i].Item2 > 0 && i != parentIndices.Item1) {
							indicesAndProportions.Add((i, (results[genCounter][i].Item2 / sum) * 100));
						}
					}
					choice = Random.Range(0f, 100f);
					sum2 = 0;
					for (int i = 0; i < indicesAndProportions.Count; i++) {
						if (sum2 + indicesAndProportions[i].Item2 >= choice) {
							parentIndices.Item2 = i;
						} else {
							sum2 += indicesAndProportions[i].Item2;
						}
					}
					// ------------------ New parents selected ----------------------- //

					bool doTwoPointCrossover = false;
					if (Random.Range(0f, 1f) <= 0.2f) doTwoPointCrossover = true;
					if (doTwoPointCrossover) {
						int index1 = Random.Range(0, microruleCount);
						int index2 = Random.Range(0, microruleCount);
						while (index1 == index2) {
							index2 = Random.Range(0, microruleCount);
						}
						(int, int) twoPoints = (Mathf.Min(index1, index2), Mathf.Max(index1, index2));
						List<float> newGenome = new List<float>();
						for (int a = 0; a < twoPoints.Item1; a++) {
							List<float> microrule = new List<float>();
							for (int k = 0; k < 28; k++) {
								microrule.Add(results[genCounter][parentIndices.Item1].Item1[a * 28 + k]);
                            }
							newGenome.AddRange(microrule);
                        }
						for (int b = twoPoints.Item1; b < twoPoints.Item2; b++) {
							List<float> microrule = new List<float>();
							for (int k = 0; k < 28; k++) {
								microrule.Add(results[genCounter][parentIndices.Item2].Item1[b * 28 + k]);
							}
							newGenome.AddRange(microrule);
						}
						for (int c = twoPoints.Item2; c < microruleCount; c++) {
							List<float> microrule = new List<float>();
							for (int k = 0; k < 28; k++) {
								microrule.Add(results[genCounter][parentIndices.Item1].Item1[c * 28 + k]);
							}
							newGenome.AddRange(microrule);
						}
						genomes[j] = newGenome;
					} else {
						genomes[j] = results[genCounter][parentIndices.Item1].Item1;
                    }
				}

				// Genome mutation
				for (int i = 0; i < popSize; i++) {
					genomes[i] = MutateGenome(genomes[i]);
				}

				// Elitism
				genomes[bestResultIndex] = bestResult.Item1;

				Debug.Log($"gen {genCounter}, Best genome: {bestResultIndex}, fitness: {bestResult.Item2}");
				string output = $"gen {genCounter} results\n";
				for (int i = 0; i < popSize; i++) {
					output += $"genome {i}: fitness: {result[i].Item2}\n";
				}
				Debug.Log(output);

				result = new List<(List<float>, float)>();
				genCounter++;

				genPopText.text = $"Gen: {genCounter} Pop: {popCounter}";
			}
		}
	}

	List<float> MutateGenome(List<float> genome) {
		List<float> returnList = new List<float>();

		for (int i = 0; i < microruleCount; i++) {
			List<float> microrule = new List<float>();
			for (int j = 0; j < 28; j++) {
				microrule.Add(genome[(i * 28) + j]);
            }

			float microruleUsed = microrule[27];
			bool shouldReplaceMicrorule = false;
			if (microruleUsed == 1) {
				if (Random.Range(0f, 1f) < 0.01f) shouldReplaceMicrorule = true;
			} else {
				if (Random.Range(0f, 1f) < 0.9f) shouldReplaceMicrorule = true;
			}

			if (shouldReplaceMicrorule) {
				bool microruleIsUnique = false;
				while (!microruleIsUnique) {
					microrule = GenerateRandomMicrorule();
					microruleIsUnique = !GenomeAlreadyContainsMicrorule(returnList, microrule);
				}
			}

			microrule[27] = 0;
			
			returnList.AddRange(microrule);
		}

		return returnList;
    }

	List<float> GenerateRandomMicrorule() {
		List<float> returnList = new List<float>();

		List<float> values = new List<float> { 0,0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,0 };
		List<int> initialCardinalDirections = new List<int> { 4, 10, 12, 13, 15, 21 };

		//float oneChance = 0.8f;

		List<float> template = templates[Random.Range(0, templates.Count)];
		for (int i = 0; i < 26; i++) {
			if (Random.Range(0f, 1f) < template[i]) {
				/*if (Random.Range(0f, 1f) < oneChance) {
					values[i] = 1;
				} else {
					values[i] = 2;
                }
				oneChance = Mathf.Max(0.1f, oneChance - 0.15f);*/
				values[i] = Random.Range(1, 3);
			}
		}

		values[initialCardinalDirections[Random.Range(0, 6)]] = Random.Range(1, 3);

		returnList.AddRange(values);

		returnList.Add(Random.Range(1, 3));
		returnList.Add(0f);

		return returnList;
    }

	float CalcLineFitness() {
		float fitness = 0;
		float[] values = new float[resolution * resolution * resolution];
		cellValuesBuffer.GetData(values);

		for (int x = 0; x < resolution; x++) {
			for (int y = 0; y < resolution; y++) {
				for (int z = 0; z < resolution; z++) {
					if (z == 0 && y == 0 && (x == 32 || x == 34 || x == 36 || x == 38 || x == 40 || x == 42 || x == 44 || x == 46 || x == 48 || x == 50 || x == 52 || x == 54 || x == 56 || x == 58)) {
						if (values[x + y * resolution + z * resolution * resolution] == 1) {
							fitness += 1;
						} else if (values[x + y * resolution + z * resolution * resolution] == 2) {
							fitness -= 1f;
						}
					} else if (z == 0 && y == 0 && (x == 31 || x == 33 || x == 35 || x == 37 || x == 39 || x == 41 || x == 43 || x == 45 || x == 47 || x == 49 || x == 51)) {
						if (values[x + y * resolution + z * resolution * resolution] == 1) {
							fitness -= 1;
						}
					}
				}
			}
		}
		return fitness;
	}

	float CalcLineFitness2() {
		float fitness = 0;
		float[] values = new float[resolution * resolution * resolution];
		cellValuesBuffer.GetData(values);

		int counter = 0;
		for (int x = 59; x > 30; x--) {
			if (values[x] == 1) {
				if (counter == 2) fitness++;
				counter = 0;
			} else if (values[x] == 2) {
				counter++;
            }
        }
		return fitness;
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

	void GenerateTemplates() {
		templates = new List<List<float>>();

		templates.Add(new List<float> { 0.01f,0.01f,0.01f,0.01f,0.01f,0.01f,0.01f,0.01f,0.01f, 0.01f,0.01f,0.01f,0.01f,0.01f,0.01f,0.01f,0.01f, 0.1f,0.1f,0.1f,0.1f,0.1f,0.1f,0.1f,0.1f,0.1f });
		templates.Add(new List<float> { 0.01f,0.01f,0.01f,0.01f,0.01f,0.01f,0.01f,0.01f,0.01f, 0.01f,0.01f,0.01f,0.01f,0.01f,0.01f,0.01f,0.01f, 0.01f,0.01f,0.01f,0.01f,0.01f,0.01f,0.01f,0.01f,0.01f });
		templates.Add(new List<float> { 0.1f, 0.01f, 0.01f, 0.1f, 0.01f, 0.01f, 0.1f, 0.01f, 0.01f, 0.1f, 0.1f, 0.01f, 0.1f, 0.01f, 0.1f, 0.1f, 0.01f, 0.1f, 0.01f, 0.01f, 0.1f, 0.01f, 0.01f, 0.1f, 0.01f, 0.01f });
		templates.Add(new List<float> { 0.01f, 0.01f, 0.01f, 0.01f, 0.05f, 0.1f, 0.01f, 0.1f, 0.1f, 0.01f, 0.01f, 0.01f, 0.01f, 0.1f, 0.01f, 0.1f, 0.1f, 0.01f, 0.01f, 0.01f, 0.01f, 0.05f, 0.1f, 0.01f, 0.1f, 0.1f });
		templates.Add(new List<float> { 0.1f, 0.1f, 0.1f, 0.1f, 0.5f, 0.1f, 0.1f, 0.1f, 0.1f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f });
		templates.Add(new List<float> { 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.1f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.1f, 0.01f, 0.1f, 0.5f });
		templates.Add(new List<float> { 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.1f, 0.01f, 0.1f, 0.5f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.1f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f });
		templates.Add(new List<float> { 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.1f, 0.01f, 0.1f, 0.5f, 0.5f, 0.1f, 0.01f, 0.1f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f });
		templates.Add(new List<float> { 0.01f, 0.1f, 0.01f, 0.1f, 0.5f, 0.1f, 0.01f, 0.1f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.1f, 0.01f, 0.1f, 0.5f, 0.1f, 0.01f, 0.1f, 0.01f });
		templates.Add(new List<float> { 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.5f, 0.1f, 0.01f, 0.1f, 0.1f, 0.01f, 0.1f, 0.5f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f });
		templates.Add(new List<float> { 0.01f, 0.01f, 0.01f, 0.5f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f });
		templates.Add(new List<float> { 0.01f, 0.01f, 0.01f, 0.5f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.5f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f });
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

			microrulesBuffer.SetData(microrules);

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
