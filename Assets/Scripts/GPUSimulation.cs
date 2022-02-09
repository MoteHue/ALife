using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPUSimulation : MonoBehaviour {

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
		pheromonePositionsBuffer;

	int counter = 0;
	float step = 1f;
	int indexStep = 0;
	Bounds bounds;

	static readonly int
		materialPositionsId = Shader.PropertyToID("_Positions"),
		agentPositionsId = Shader.PropertyToID("_AgentPositions"),
		cellPositionsId = Shader.PropertyToID("_CellPositions"),
		pheromonePositionsId = Shader.PropertyToID("_PheromonePositions"),
		resolutionId = Shader.PropertyToID("_Resolution"),
		stepId = Shader.PropertyToID("_Step"),
		timeId = Shader.PropertyToID("_Time"),
		indexStepId = Shader.PropertyToID("_IndexStep"),
		countId = Shader.PropertyToID("_Count");

	void OnEnable() {
		agentPositionsBuffer = new ComputeBuffer(maxResolution * maxResolution * maxResolution, 3 * sizeof(float));
		cellPositionsBuffer = new ComputeBuffer(maxResolution * maxResolution * maxResolution, 3 * sizeof(float));
		pheromonePositionsBuffer = new ComputeBuffer(maxResolution * maxResolution * maxResolution, 3 * sizeof(float));
		bounds = new Bounds(Vector3.zero, Vector3.one * resolution);
		indexStep = Mathf.CeilToInt(resolution / 4f);
		computeShader.SetFloat(stepId, step);
		computeShader.SetBuffer(0, agentPositionsId, agentPositionsBuffer);
		computeShader.SetBuffer(0, cellPositionsId, cellPositionsBuffer);
		computeShader.SetBuffer(0, pheromonePositionsId, pheromonePositionsBuffer);
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
	}

	void Update() {
		UpdateFunctionOnGPU();
		counter++;
	}

	void UpdateFunctionOnGPU() {
		
		computeShader.SetFloat(timeId, Time.time);
		computeShader.SetInt(countId, counter);
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

}	
