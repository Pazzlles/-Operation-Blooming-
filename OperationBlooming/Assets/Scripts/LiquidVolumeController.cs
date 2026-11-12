
using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(200)]
public class LiquidVolumeController : MonoBehaviour
{
	[Header("Liquid Object")]
	[SerializeField] private MeshFilter liquidMesh;
	[SerializeField] private Renderer liquidRenderer;

	[Header("Volume")]
	[Range(0f, 100f)]
	[SerializeField] private float fillPercent = 50f;

	[Header("Calculation")]
	[Range(8, 32)]
	[SerializeField] private int resolution = 16;

	[SerializeField] private bool useWobble = true;

	private Vector3[] samplePoints;
	private float[] heights;
	private MaterialPropertyBlock propertyBlock;

	private static readonly int FillLevelId =
		Shader.PropertyToID("_FillLevel");

	private static readonly int WobbleXId =
		Shader.PropertyToID("_WobbleX");

	private static readonly int WobbleZId =
		Shader.PropertyToID("_WobbleZ");

	public float CurrentFillLevel { get; private set; }

	public float CurrentWobbleX { get; private set; }

	public float CurrentWobbleZ { get; private set; }

	public Transform LiquidTransform =>
		liquidMesh != null ? liquidMesh.transform : null;

	public Mesh LiquidMesh =>
		liquidMesh != null ? liquidMesh.sharedMesh : null;

	public bool IsReady =>
		samplePoints != null && samplePoints.Length > 0;

	public float FillFraction => fillPercent / 100f;


	private void Start()
	{
		propertyBlock = new MaterialPropertyBlock();

		if (liquidMesh == null || liquidRenderer == null)
		{
			Debug.LogError("Liquid objects are not assigned.");
			enabled = false;
			return;
		}

		try
		{
			GenerateSamplePoints();
		}
		catch (Exception e)
		{
			Debug.LogError("Liquid sampling failed: " + e.Message);
			enabled = false;
			return;
		}

		if (samplePoints.Length == 0)
		{
			Debug.LogError("No interior liquid points found.");
			enabled = false;
			return;
		}

		heights = new float[samplePoints.Length];

		Debug.Log(
			"Liquid samples: " + samplePoints.Length
		);
	}

	private void LateUpdate()
	{
		if (samplePoints == null || samplePoints.Length == 0)
			return;

		Transform liquidTransform = liquidMesh.transform;
		Vector3 origin = liquidTransform.position;

		liquidRenderer.GetPropertyBlock(propertyBlock);

		float wobbleX = useWobble
			? propertyBlock.GetFloat(WobbleXId) : 0f;

		float wobbleZ = useWobble
			? propertyBlock.GetFloat(WobbleZId) : 0f;


		for (int i = 0; i < samplePoints.Length; i++)
		{
			Vector3 worldPoint =
				liquidTransform.TransformPoint(samplePoints[i]);

			Vector3 relative = worldPoint - origin;

			heights[i] =
				relative.y +
				relative.x * wobbleX +
				relative.z * wobbleZ;
		}

		Array.Sort(heights);

		float fraction = fillPercent / 100f;

		float fillLevel;

		if (fraction <= 0f)
		{
			fillLevel = heights[0] - 0.01f;
		}
		else if (fraction >= 1f)
		{
			fillLevel =
				heights[heights.Length - 1] + 0.01f;
		}
		else
		{
			int index = Mathf.Clamp(
				Mathf.FloorToInt(fraction * heights.Length),
				0,
				heights.Length - 1
			);

			fillLevel = heights[index];
		}


		CurrentFillLevel = fillLevel;
		CurrentWobbleX = wobbleX;
		CurrentWobbleZ = wobbleZ;

		propertyBlock.SetFloat(FillLevelId, fillLevel);
		liquidRenderer.SetPropertyBlock(propertyBlock);

	}

	private void GenerateSamplePoints()
	{
		Mesh mesh = liquidMesh.sharedMesh;

		if (mesh == null)
			throw new Exception("Liquid Mesh is missing.");

		Vector3[] vertices = mesh.vertices;
		int[] triangles = mesh.triangles;

		Bounds bounds = mesh.bounds;
		List<Vector3> points = new List<Vector3>();

		for (int iy = 0; iy < resolution; iy++)
		{
			float y = bounds.min.y +
				(iy + 0.5f) * bounds.size.y / resolution;

			for (int iz = 0; iz < resolution; iz++)
			{
				float z = bounds.min.z +
					(iz + 0.5f) * bounds.size.z / resolution;

				List<float> intersections = new List<float>();

				for (int t = 0; t < triangles.Length; t += 3)
				{
					Vector3 a = vertices[triangles[t]];
					Vector3 b = vertices[triangles[t + 1]];
					Vector3 c = vertices[triangles[t + 2]];

					if (TryIntersectX(y, z, a, b, c, out float x))
						intersections.Add(x);
				}

				intersections.Sort();

				for (int ix = 0; ix < resolution; ix++)
				{
					float x = bounds.min.x +
						(ix + 0.5f) * bounds.size.x / resolution;

					int crossings = 0;

					foreach (float intersection in intersections)
					{
						if (intersection < x)
							crossings++;
						else
							break;
					}


					if (crossings % 2 == 1)
						points.Add(new Vector3(x, y, z));
				}
			}
		}

		samplePoints = points.ToArray();
	}


	private bool TryIntersectX(
		float y, float z,
		Vector3 a, Vector3 b, Vector3 c,
		out float x)
	{
		x = 0f;

		float v0y = b.y - a.y;
		float v0z = b.z - a.z;
		float v1y = c.y - a.y;
		float v1z = c.z - a.z;

		float det = v0y * v1z - v1y * v0z;

		if (Mathf.Abs(det) < 0.0000001f)
			return false;

		float py = y - a.y;
		float pz = z - a.z;

		float u = (py * v1z - v1y * pz) / det;
		float v = (v0y * pz - py * v0z) / det;

		if (u < -0.00001f ||
			v < -0.00001f ||
			u + v > 1.00001f)
			return false;

		x = a.x + u * (b.x - a.x) +
			v * (c.x - a.x);

		return true;
	}

	public void SetFillPercent(float percent)
	{
		fillPercent = Mathf.Clamp(percent, 0f, 100f);
	}

	public float GetFillPercent()
	{
		return fillPercent;
	}
}
