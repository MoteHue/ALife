using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class GPUSimulationWaspmites : MonoBehaviour {

	[SerializeField]
	GameObject floorPrefab;

	[SerializeField]
	ComputeShader computeShader;

	[SerializeField]
	Material agentMaterial;
	[SerializeField]
	Material trailAgentMaterial;
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
	Material trailPheromoneMaterial;
	[SerializeField]
	Mesh pheromoneMesh;

	[SerializeField]
	Text genPopText;

	[SerializeField]
	Image progressImage;

	const int resolution = 60;

	ComputeBuffer
		agentValuesBuffer,
		cellValuesBuffer,
		pheromoneValuesBuffer,
		spawnLocationsBuffer,
		pastAgentValuesBuffer,
		pastCellValuesBuffer,
		pastPheromoneValuesBuffer,
		microrulesBuffer;

	float step = 1f;
	int indexStep = 0;
	int count = 0;
	int microruleCount = 20;
	Bounds bounds;

	List<List<float>> templates;

	int popSize = 30;
	int noOfGenerations = 100;

	List<List<float>> genomes;

	List<List<(List<float>, float)>> results;

	int popCounter = 0;
	int genCounter = 0;
	List<(List<float>, float)> result;

	public bool simulationRunning = true;

	bool pheromonesEnabled = true;
	bool cellsEnabled = true;
	bool agentsEnabled = true;

	List<Vector3> spawnLocations;
	List<float> microrules;
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
		microruleCountId = Shader.PropertyToID("_MicroruleCount"),
		microrulesId = Shader.PropertyToID("_Microrules");

    private void Start() {
		GenerateTemplates();
		genomes = new List<List<float>>();
		results = new List<List<(List<float>, float)>>();
		result = new List<(List<float>, float)>();

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

		agentValuesBuffer = new ComputeBuffer(resolution * resolution * resolution, sizeof(float) * 2);
		cellValuesBuffer = new ComputeBuffer(resolution * resolution * resolution, sizeof(float));
		pheromoneValuesBuffer = new ComputeBuffer(resolution * resolution * resolution, sizeof(float) * 3);
		pastAgentValuesBuffer = new ComputeBuffer(resolution * resolution * resolution, sizeof(float) * 2);
		pastCellValuesBuffer = new ComputeBuffer(resolution * resolution * resolution, sizeof(float));
		pastPheromoneValuesBuffer = new ComputeBuffer(resolution * resolution * resolution, sizeof(float) * 3);
		spawnLocationsBuffer = new ComputeBuffer(resolution * 2 + (resolution - 2) * 4 + (resolution - 4) * 2, sizeof(float) * 3);
		microrulesBuffer = new ComputeBuffer(32 * microruleCount, sizeof(float));

		bounds = new Bounds(Vector3.zero, Vector3.one * resolution);
		indexStep = Mathf.CeilToInt(resolution / 4f);
		computeShader.SetFloat(stepId, step);
		result = new List<(List<float>, float)>();
		results = new List<List<(List<float>, float)>>();


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
		computeShader.SetBuffer(0, microrulesId, microrulesBuffer);

		computeShader.SetInt(resolutionId, resolution);
		computeShader.SetInt(indexStepId, indexStep);
		computeShader.SetBool(meshEnabledId, pheromonesEnabled);
		computeShader.SetInt(counterId, 0);
		computeShader.SetInt(microruleCountId, microruleCount);

		agentMaterial.SetInt(resolutionId, resolution);
		cellMaterial.SetInt(resolutionId, resolution);
		queenPheromoneMaterial.SetInt(resolutionId, resolution);
		cementPheromoneMaterial.SetInt(resolutionId, resolution);
		trailPheromoneMaterial.SetInt(resolutionId, resolution);
		trailAgentMaterial.SetInt(resolutionId, resolution);

		SetQueenCells();

		SetRandomGenomes();
	}

	void SetQueenCells() {
		float[] values = new float[resolution * resolution * resolution];
		cellValuesBuffer.GetData(values);
		foreach (Vector3Int queenCell in queenCells) {
			values[queenCell.x + queenCell.y * resolution + queenCell.z * resolution * resolution] = 10;
        }
		cellValuesBuffer.SetData(values);
		pastCellValuesBuffer.SetData(values);
	}

	bool GenomeAlreadyContainsMicrorule(List<float> genome, List<float> microrule) {
		for (int i = 0; i < genome.Count / 32; i++) {
			// rotation 0°
			bool microruleFound = true;
			for (int j = 0; j < 26; j++) {
				if (microrule[j] == genome[(i * 32) + j]) {
					microruleFound = false;
					break;
				}
			}
			if (microruleFound) return true;

			// rotation 90°
			List<int> indices = new List<int> { 2, 5, 8, 1, 4, 7, 0, 3, 6, 11, 13, 16, 10, 15, 9, 12, 14, 19, 22, 25, 18, 21, 24, 17, 20, 23 };
			microruleFound = true;
			for (int j = 0; j < 26; j++) {
				if (microrule[j] == genome[(i * 32) + indices[j]]) {
					microruleFound = false;
					break;
				}
			}
			if (microruleFound) return true;

			// rotation 180°
			indices = new List<int> { 8, 7, 6, 5, 4, 3, 2, 1, 0, 16, 15, 14, 13, 12, 11, 10, 9, 25, 24, 23, 22, 21, 20, 19, 18, 17 };
			microruleFound = true;
			for (int j = 0; j < 26; j++) {
				if (microrule[j] == genome[(i * 32) + indices[j]]) {
					microruleFound = false;
					break;
				}
			}
			if (microruleFound) return true;

			// rotation 270°
			indices = new List<int> { 6, 3, 0, 7, 4, 1, 8, 5, 2, 14, 12, 9, 15, 10, 16, 13, 11, 23, 20, 17, 24, 21, 18, 25, 22, 19 };
			microruleFound = true;
			for (int j = 0; j < 26; j++) {
				if (microrule[j] == genome[(i * 32) + indices[j]]) {
					microruleFound = false;
					break;
				}
			}
			if (microruleFound) return true;
		}
		return false;
	}

	List<float> GenerateRandomMicrorule() {
		List<float> returnList = new List<float>();

		List<float> values = new List<float> { 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100 };
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

				genome.AddRange(microrule);

				float minRange = Mathf.Max(0.001f, Random.Range(0f, 1f) * 0.5f);
				float maxRange = 0.5f + Random.Range(0f, 1f);

				genome.Add(minRange);
				genome.Add(maxRange);

				minRange = Mathf.Max(0.001f, Random.Range(0f, 1f) * 0.5f);
				maxRange = 0.5f + Random.Range(0f, 1f);

				genome.Add(minRange);
				genome.Add(maxRange);

			}
			genomes.Add(genome);
		}

		string output = $"gen {0} setup\n";
		for (int i = 0; i < popSize; i++) {
			output += $"genome {i}: [";
			for (int j = 0; j < microruleCount; j++) {
				output += "[";
				for (int k = 0; k < 32; k++) {
					if (genomes[i][j * 32 + k] == 100) output += ",_";
					else output += $",{genomes[i][j * 32 + k]}";
					if (k == 25) output += "  ";
				}
				output += "]\n";
			}
			output += "]\n";
		}
		Debug.Log(output);
	}

	float Gaussian(float mu, float sigma) {
		float u1 = Random.Range(0f, 1f);
		float u2 = Random.Range(0f, 1f);

		float rand_std_normal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) *Mathf.Sin(2.0f * Mathf.PI * u2);

		return mu + sigma * rand_std_normal;
	}

	List<float> MutateGenome(List<float> genome) {
		List<float> returnList = new List<float>();

		for (int i = 0; i < microruleCount; i++) {
			List<float> microrule = new List<float>();
			for (int j = 0; j < 28; j++) {
				microrule.Add(genome[(i * 32) + j]);
			}

			float microruleUsed = microrule[27];
			bool shouldReplaceMicrorule = false;
			if (microruleUsed == 1) {
				string output = "Microrule used: [";
				for (int k = 0; k < 32; k++) {
					if (genome[(i * 32) + k] == 100) output += ",_";
					else output += $",{genome[(i * 32) + k]}";
				}
				output += "]\n";
				Debug.Log(output);
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

			//float minRange = Mathf.Max(0.001f, Random.Range(0f, 1f) * 0.5f);
			//float maxRange = 0.5f + Random.Range(0f, 1f);

			float minRange = Mathf.Max(0.001f, Gaussian(genome[28] * 2, 0.05f) * 0.5f);
			float maxRange = 0.5f + Gaussian(genome[29] - 0.5f, 0.05f);

			microrule.Add(minRange);
			microrule.Add(maxRange);

			minRange = Mathf.Max(0.001f, Gaussian(genome[28] * 2, 0.05f) * 0.5f);
			maxRange = 0.5f + Gaussian(genome[29] - 0.5f, 0.05f);

			microrule.Add(minRange);
			microrule.Add(maxRange);

			returnList.AddRange(microrule);
		}

		return returnList;
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
		microrulesBuffer.Release();
		microrulesBuffer = null;
	}

	void ClearBuffers() {
		float[] values = new float[resolution * resolution * resolution * 2];
		agentValuesBuffer.SetData(values);
		pastAgentValuesBuffer.SetData(values);

		values = new float[resolution * resolution * resolution];
		cellValuesBuffer.SetData(values);
		pastCellValuesBuffer.SetData(values);

		values = new float[resolution * resolution * resolution * 3];
		pheromoneValuesBuffer.SetData(values);
		pastPheromoneValuesBuffer.SetData(values);

		SetQueenCells();
	}

	void Update() {

		DoGeneticAlgorithm();
		
		ShowMesh();
	}

	List<float> pastValues;
	int emptySceneCount = 0;

	void DoGeneticAlgorithm() {
		if (genCounter < noOfGenerations) {
			if (popCounter < popSize) {
				if (count == 0) {
					float[] values = new float[resolution * resolution * resolution];
					cellValuesBuffer.GetData(values);
					pastValues = new List<float>(values);
					microrules = genomes[popCounter];
				}
				if (count % 10 == 0) {
					progressImage.transform.localScale = new Vector3(count / 3000f, 1f, 1f);
				}

				bool finished = DoSimulation(3000);

				if (count % 500 == 0 && count != 0) {
					float[] values = new float[resolution * resolution * resolution];
					cellValuesBuffer.GetData(values);

					bool differenceFound = false;

					for (int i = 0; i < resolution * resolution * resolution; i++) {
						if (values[i] != pastValues[i]) {
							differenceFound = true;
							if (count == 500) {
								emptySceneCount++;
                            }
							break;
						}
					}

					if (!differenceFound) finished = true; // End early if no new cells placed.
					pastValues = new List<float> (values);
				}

				if (finished) {
					float fitness = CalcFitness(new Vector3(30, 30), 10f);
					result.Add((genomes[popCounter], fitness));
					popCounter++;
					ClearBuffers();
					count = 0;
					genPopText.text = $"Gen: {genCounter} Pop: {popCounter}";
				}
			} else {
				results.Add(result);
				popCounter = 0;

				using (StreamWriter file = File.AppendText("results/emptyCounts.txt")) {
					file.WriteLine(emptySceneCount);
                }
				Debug.Log($"gen: {genCounter} emptySceneCount: {emptySceneCount}");
				emptySceneCount = 0;

				(List<float>, float) bestResult = results[genCounter][0];
				int bestResultIndex = 0;
				for (int i = 1; i < popSize; i++) {
					if (results[genCounter][i].Item2 > bestResult.Item2) {
						bestResult = results[genCounter][i];
						bestResultIndex = i;
					}
				}

				for (int i = 0; i < microruleCount; i++) {
					bestResult.Item1[i * 32 + 27] = 0;
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
							for (int k = 0; k < 32; k++) {
								microrule.Add(results[genCounter][parentIndices.Item1].Item1[a * 32 + k]);
							}
							newGenome.AddRange(microrule);
						}
						for (int b = twoPoints.Item1; b < twoPoints.Item2; b++) {
							List<float> microrule = new List<float>();
							for (int k = 0; k < 32; k++) {
								microrule.Add(results[genCounter][parentIndices.Item2].Item1[b * 32 + k]);
							}
							newGenome.AddRange(microrule);
						}
						for (int c = twoPoints.Item2; c < microruleCount; c++) {
							List<float> microrule = new List<float>();
							for (int k = 0; k < 32; k++) {
								microrule.Add(results[genCounter][parentIndices.Item1].Item1[c * 32 + k]);
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

				output = $"gen {genCounter + 1} setup\n";
				for (int i = 0; i < popSize; i++) {
					output += $"genome {i}: [";
					for (int j = 0; j < microruleCount; j++) {
						output += "[";
						for (int k = 0; k < 32; k++) {
							if (genomes[i][j * 32 + k] == 100) output += ",_";
							else output += $",{genomes[i][j * 32 + k]}";
							if (k == 25) output += "  ";
						}
						output += "]\n";
					}
					output += "]\n";
				}
				Debug.Log(output);

				result = new List<(List<float>, float)>();
				genCounter++;

				genPopText.text = $"Gen: {genCounter} Pop: {popCounter}";
			}
		}
	}

    bool DoSimulation(int frameCount) {
		if (count == 200) {
			List<Vector2> values = SpawnAgents(300);
			agentValuesBuffer.SetData(values);
			pastAgentValuesBuffer.SetData(values);
		}

		if (count < frameCount) {
			/*if (Random.Range(0f, 1f) <= 0.5f) {
				List<Vector3> trailSpawnLocations = new List<Vector3> { /*new Vector3(27,0,0), new Vector3(28,0,0), new Vector3(29,0,0), new Vector3(30,0,0), new Vector3(31,0,0), new Vector3(32,0,0), new Vector3(27,0,59), new Vector3(28,0,59), new Vector3(29,0,59), new Vector3(30,0,59), new Vector3(31,0,59), new Vector3(32,0,59),* new Vector3(0,0,27), new Vector3(0,0,28), new Vector3(0,0,29), new Vector3(0,0,30), new Vector3(0,0,31), new Vector3(0,0,32), new Vector3(59,0,27), new Vector3(59,0,28), new Vector3(59,0,29), new Vector3(59,0,30), new Vector3(59,0,31), new Vector3(59,0,32) };

				Vector3 spawnLoc = trailSpawnLocations[Random.Range(0, 12)];

				float[] values = new float[resolution * resolution * resolution * 2];
				agentValuesBuffer.GetData(values);
				values[(int)(spawnLoc.x * 2 + spawnLoc.y * resolution * 2 + spawnLoc.z * resolution * resolution * 2 + 1)]++;
				agentValuesBuffer.SetData(values);
			}*/

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

			trailAgentMaterial.SetFloat(stepId, step);
			trailAgentMaterial.SetBuffer(materialValuesId, agentValuesBuffer);
			Graphics.DrawMeshInstancedProcedural(agentMesh, 0, trailAgentMaterial, bounds, (int)Mathf.Pow(indexStep * 4, 3));
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

			trailPheromoneMaterial.SetFloat(stepId, step);
			trailPheromoneMaterial.SetBuffer(materialValuesId, pheromoneValuesBuffer);
			Graphics.DrawMeshInstancedProcedural(pheromoneMesh, 0, trailPheromoneMaterial, bounds, (int)Mathf.Pow(indexStep * 4, 3));
		}
	}

    void DispatchCycleOfSimulation() {
		
		if (simulationRunning) {
			computeShader.SetFloat(timeId, Time.time);
			computeShader.SetInt(counterId, count);

			microrulesBuffer.SetData(microrules);

			computeShader.Dispatch(0, 2, 2, 2);

			float[] values = new float[resolution * resolution * resolution * 2];
			agentValuesBuffer.GetData(values);
			pastAgentValuesBuffer.SetData(values);

			values = new float[resolution * resolution * resolution];
			cellValuesBuffer.GetData(values);
			pastCellValuesBuffer.SetData(values);

			values = new float[resolution * resolution * resolution * 3];
			pheromoneValuesBuffer.GetData(values);
			pastPheromoneValuesBuffer.SetData(values);

			count++;
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

	List<Vector2> SpawnAgents(int amount) {
		List<Vector2> returnList = new List<Vector2>();
		for (int i = 0; i < resolution * resolution * resolution; i++) {
			returnList.Add(Vector2.zero);
        }

		int counter = 0;
		while (counter < amount) {
			Vector3 newPos = spawnLocations[Random.Range(0, spawnLocations.Count)];

			int index = (int)(newPos.x + resolution * newPos.y + resolution * resolution * newPos.z);

			returnList[index] = new Vector2(returnList[index].x + 1, 0);
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

	float CalcFitness(Vector2 wallCentre, float targetRadius) {
		float[] values = new float[resolution * resolution * resolution];
		cellValuesBuffer.GetData(values);

		float sum = 0;
		int count = 0;
		float heightAdvantage = 0;

		for (int i = 0; i < values.Length; i++) {
			if (values[i] == 1 || values[i] == 2) {
				Vector3 coords = new Vector3(i % resolution, (i / resolution) % resolution, (i / (resolution * resolution)) % resolution);
				float distance = Vector3.Distance(coords, new Vector3(wallCentre.x, coords.y, wallCentre.y));
				float difference = Mathf.Abs(distance - targetRadius);

				if (difference <= 0.5f) {
					sum += 1f;
					if (coords.y * 4f > heightAdvantage) {
						heightAdvantage = coords.y * 4f;
					}
				} else if (difference <= 1.5f) {
					sum += 0.75f;
				} else if (difference <= 2.5f) {
					sum += 0.25f;
				} else if (difference <= 3.5f) {
					sum -= 0.01f;
				} else if (difference <= 4.5f) {
					sum += -0.25f;
				} else if (difference <= 5.5f) {
					sum += -0.75f;
				} else {
					sum += -1f;
				}

				count++;
			}
		}

		if (count == 0) return 0f;

		return (sum / count) * 100f + heightAdvantage;
	}

	void GenerateTemplates() {
		templates = new List<List<float>>();

		//templates.Add(new List<float> { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f });
		templates.Add(new List<float> { 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f });
		templates.Add(new List<float> { 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f });
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

}	
