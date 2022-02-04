using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPUGraph : MonoBehaviour {

	[SerializeField]
	ComputeShader computeShader;

	[SerializeField]
	Material material;

	[SerializeField]
	Mesh mesh;

	const int maxResolution = 100;

	[SerializeField, Range(10, maxResolution)]
	int resolution = 20;

	[SerializeField]
	FunctionLibrary.FunctionName function;

	ComputeBuffer positionsBuffer;

	static readonly int
		positionsId = Shader.PropertyToID("_Positions"),
		resolutionId = Shader.PropertyToID("_Resolution"),
		stepId = Shader.PropertyToID("_Step"),
		timeId = Shader.PropertyToID("_Time");

	void OnEnable() {
		positionsBuffer = new ComputeBuffer(maxResolution * maxResolution * maxResolution, 3 * 4);
	}

	void OnDisable() {
		positionsBuffer.Release();
		positionsBuffer = null;
	}

	void Update() {
		UpdateFunctionOnGPU();
	}

	void UpdateFunctionOnGPU() {
		float step = 1f;
		computeShader.SetInt(resolutionId, resolution);
		computeShader.SetFloat(stepId, step);
		computeShader.SetFloat(timeId, Time.time);
		computeShader.SetBuffer(0, positionsId, positionsBuffer);

		int groups = Mathf.CeilToInt(resolution / 8f);
		computeShader.Dispatch(0, groups, groups, groups);

		material.SetBuffer(positionsId, positionsBuffer);
		material.SetFloat(stepId, step);
		var bounds = new Bounds(Vector3.zero, Vector3.one * (2f + 2f / resolution));
		Graphics.DrawMeshInstancedProcedural(mesh, 0, material, bounds, resolution * resolution * resolution);
	}

}	
