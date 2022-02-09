using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPUSimulation : MonoBehaviour {

	[SerializeField]
	ComputeShader computeShader;

	[SerializeField]
	Material material;

	[SerializeField]
	Mesh mesh;

	const int maxResolution = 100;

	[SerializeField, Range(10, maxResolution)]
	int resolution = 20;

	ComputeBuffer positionsBuffer;

	int counter = 0;
	float step = 1f;
	int indexStep = 0;

	static readonly int
		positionsId = Shader.PropertyToID("_Positions"),
		resolutionId = Shader.PropertyToID("_Resolution"),
		stepId = Shader.PropertyToID("_Step"),
		timeId = Shader.PropertyToID("_Time"),
		indexStepId = Shader.PropertyToID("_IndexStep"),
		countId = Shader.PropertyToID("_Count");

	void OnEnable() {
		positionsBuffer = new ComputeBuffer(maxResolution * maxResolution * maxResolution, 3 * sizeof(float));

		indexStep = Mathf.CeilToInt(resolution / 4f);
		computeShader.SetFloat(stepId, step);
		computeShader.SetBuffer(0, positionsId, positionsBuffer);
		computeShader.SetInt(resolutionId, resolution);
		computeShader.SetInt(indexStepId, indexStep);
	}

	void OnDisable() {
		positionsBuffer.Release();
		positionsBuffer = null;
	}

	void Update() {
		UpdateFunctionOnGPU();
		counter++;
	}

	void UpdateFunctionOnGPU() {
		
		computeShader.SetFloat(timeId, Time.time);
		computeShader.SetInt(countId, counter);
		computeShader.Dispatch(0, 2, 2, 2);

		material.SetBuffer(positionsId, positionsBuffer);
		material.SetFloat(stepId, step);
		var bounds = new Bounds(Vector3.zero, Vector3.one * resolution);
		Graphics.DrawMeshInstancedProcedural(mesh, 0, material, bounds, (int)Mathf.Pow(indexStep * 4, 3));
	}

}	
