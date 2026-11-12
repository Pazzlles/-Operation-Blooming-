
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(300)]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class LiquidSurfaceMesh : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private LiquidVolumeController volumeController;

	[Header("Surface")]
	[SerializeField] private float surfaceOffset = 0.0002f;
	[SerializeField] private float pointTolerance = 0.0001f;

	private Mesh surfaceMesh;
	private Mesh sourceMesh;

	private Vector3[] sourceVertices;
	private int[] sourceTriangles;

	private readonly List<Vector3> points =
		new List<Vector3>();

	private readonly List<Vector3> orderedPoints =
		new List<Vector3>();

	private readonly List<Vector3> meshVertices =
		new List<Vector3>();

	private readonly List<int> meshTriangles =
		new List<int>();

	private MeshRenderer surfaceRenderer;

	private MaterialPropertyBlock surfaceProperties;

	private static readonly int SurfaceModeId =
		Shader.PropertyToID("_SurfaceMode");

	private void Awake()
	{
		surfaceMesh = new Mesh();
		surfaceMesh.name = "Generated Liquid Surface";
		surfaceMesh.MarkDynamic();

		GetComponent<MeshFilter>().sharedMesh = surfaceMesh;

		surfaceRenderer = GetComponent<MeshRenderer>();

		surfaceProperties = new MaterialPropertyBlock();

		surfaceRenderer.GetPropertyBlock(surfaceProperties);

		surfaceProperties.SetFloat(SurfaceModeId, 1f);

		surfaceRenderer.SetPropertyBlock(surfaceProperties);
	}

	private void LateUpdate()
	{
		if (volumeController == null ||
			!volumeController.IsReady)
		{
			ClearSurface();
			return;
		}

		if (volumeController.FillFraction <= 0f)
		{
			ClearSurface();
			return;
		}

		Mesh currentMesh = volumeController.LiquidMesh;

		if (currentMesh == null)
		{
			ClearSurface();
			return;
		}

		if (sourceMesh != currentMesh)
		{
			sourceMesh = currentMesh;
			sourceVertices = sourceMesh.vertices;
			sourceTriangles = sourceMesh.triangles;
		}

		GenerateSurface();
	}

	private void GenerateSurface()
	{
		Transform sourceTransform =
			volumeController.LiquidTransform;

		Vector3 origin = sourceTransform.position;

		float fill = volumeController.CurrentFillLevel;
		float wobbleX = volumeController.CurrentWobbleX;
		float wobbleZ = volumeController.CurrentWobbleZ;

		Vector3 planeNormal = new Vector3(
			wobbleX, 1f, wobbleZ
		).normalized;

		Vector3 planePoint =
			origin + Vector3.up * fill;

		points.Clear();

		for (int i = 0; i < sourceTriangles.Length; i += 3)
		{
			Vector3 a = sourceTransform.TransformPoint(
				sourceVertices[sourceTriangles[i]]
			);

			Vector3 b = sourceTransform.TransformPoint(
				sourceVertices[sourceTriangles[i + 1]]
			);

			Vector3 c = sourceTransform.TransformPoint(
				sourceVertices[sourceTriangles[i + 2]]
			);

			AddIntersection(a, b, planePoint, planeNormal);
			AddIntersection(b, c, planePoint, planeNormal);
			AddIntersection(c, a, planePoint, planeNormal);
		}

		if (points.Count < 3)
		{
			ClearSurface();
			return;
		}

		Vector3 center = Vector3.zero;

		foreach (Vector3 p in points)
			center += p;

		center /= points.Count;

		Vector3 axisX =
			Vector3.ProjectOnPlane(Vector3.right, planeNormal)
				.normalized;

		Vector3 axisZ =
			Vector3.Cross(planeNormal, axisX).normalized;

		orderedPoints.Clear();
		orderedPoints.AddRange(points);

		orderedPoints.Sort((a, b) =>
		{
			Vector3 da = a - center;
			Vector3 db = b - center;

			float angleA = Mathf.Atan2(
				Vector3.Dot(da, axisZ),
				Vector3.Dot(da, axisX)
			);

			float angleB = Mathf.Atan2(
				Vector3.Dot(db, axisZ),
				Vector3.Dot(db, axisX)
			);

			return angleA.CompareTo(angleB);
		});

		meshVertices.Clear();
		meshTriangles.Clear();

		Vector3 offset = planeNormal * surfaceOffset;

		meshVertices.Add(
			transform.InverseTransformPoint(center + offset)
		);

		foreach (Vector3 p in orderedPoints)
		{
			meshVertices.Add(
				transform.InverseTransformPoint(p + offset)
			);
		}

		for (int i = 1; i <= orderedPoints.Count; i++)
		{
			int next = i + 1;

			if (next > orderedPoints.Count)
				next = 1;

			meshTriangles.Add(0);
			meshTriangles.Add(i);
			meshTriangles.Add(next);
		}

		surfaceMesh.Clear();
		surfaceMesh.SetVertices(meshVertices);
		surfaceMesh.SetTriangles(meshTriangles, 0);

		surfaceMesh.RecalculateNormals();
		surfaceMesh.RecalculateBounds();

		surfaceRenderer.enabled = true;
	}

	private void AddIntersection(
		Vector3 a,
		Vector3 b,
		Vector3 planePoint,
		Vector3 normal)
	{
		float da = Vector3.Dot(a - planePoint, normal);
		float db = Vector3.Dot(b - planePoint, normal);

		if ((da > 0f && db > 0f) ||
			(da < 0f && db < 0f))
			return;

		float denominator = da - db;

		if (Mathf.Abs(denominator) < 0.0000001f)
			return;

		float t = da / denominator;

		if (t < 0f || t > 1f)
			return;

		Vector3 intersection = Vector3.Lerp(a, b, t);

		float toleranceSquared =
			pointTolerance * pointTolerance;

		foreach (Vector3 p in points)
		{
			if ((p - intersection).sqrMagnitude <
				toleranceSquared)
				return;
		}

		points.Add(intersection);
	}

	private void ClearSurface()
	{
		if (surfaceMesh != null)
			surfaceMesh.Clear();

		if (surfaceRenderer != null)
			surfaceRenderer.enabled = false;
	}

	private void OnDestroy()
	{
		if (surfaceMesh != null)
			Destroy(surfaceMesh);
	}
}
