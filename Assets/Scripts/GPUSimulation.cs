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

	static readonly int
		positionsId = Shader.PropertyToID("_Positions"),
		resolutionId = Shader.PropertyToID("_Resolution"),
		stepId = Shader.PropertyToID("_Step"),
		timeId = Shader.PropertyToID("_Time"),
		countId = Shader.PropertyToID("_Count");

	void OnEnable() {
		positionsBuffer = new ComputeBuffer(maxResolution * maxResolution * maxResolution, 3 * 4);
		
		computeShader.SetFloat(stepId, step);
		computeShader.SetBuffer(0, positionsId, positionsBuffer);
		computeShader.SetInt(resolutionId, resolution);
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
		computeShader.Dispatch(0, 1, 1, 1);

		material.SetBuffer(positionsId, positionsBuffer);
		material.SetFloat(stepId, step);
		var bounds = new Bounds(Vector3.zero, Vector3.one * (2f + 2f / resolution));
		Graphics.DrawMeshInstancedProcedural(mesh, 0, material, bounds, resolution * resolution * resolution);
	}

}	
