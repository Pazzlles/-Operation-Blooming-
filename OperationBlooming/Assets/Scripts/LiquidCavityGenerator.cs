
using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class LiquidCavityGenerator : MonoBehaviour
{
	[Header("Source Geometry")]
	[SerializeField] private MeshFilter glassMesh;

	[Header("Cavity Limits")]
	[SerializeField] private Transform bottomMarker;
	[SerializeField] private Transform rimMarker;

	[Header("Generation Quality")]
	[Range(8, 128)]
	[SerializeField] private int heightSegments = 48;

	[Range(8, 128)]
	[SerializeField] private int radialSegments = 48;

	[Header("Wall Clearance")]
	[Min(0f)]
	[SerializeField] private float wallInset = 0.001f;

	[Header("Safety")]
	[Min(0f)]
	[SerializeField] private float minRadius = 0.0001f;

	private const float Epsilon = 0.0000001f;

	public Mesh GenerateMesh()
	{
		if (glassMesh == null ||
			glassMesh.sharedMesh == null)
		{
			throw new Exception(
				"Assign the glass MeshFilter."
			);
		}

		if (bottomMarker == null || rimMarker == null)
		{
			throw new Exception(
				"Assign Bottom and Rim markers."
			);
		}

		// Все вычисления выполняем в системе
		// координат объекта Liquid.
		Vector3 bottom = transform.InverseTransformPoint(
			bottomMarker.position
		);

		Vector3 rim = transform.InverseTransformPoint(
			rimMarker.position
		);

		if (rim.y <= bottom.y)
		{
			throw new Exception(
				"Rim must be above Bottom on local Y."
			);
		}

		Vector2 center = new Vector2(
			(bottom.x + rim.x) * 0.5f,
			(bottom.z + rim.z) * 0.5f
		);

		// Получаем геометрию стекла.
		Mesh source = glassMesh.sharedMesh;

		Vector3[] glassVertices = source.vertices;
		int[] glassTriangles = source.triangles;

		// Переводим вершины стекла в локальные
		// координаты Liquid с учётом всех масштабов.
		for (int i = 0; i < glassVertices.Length; i++)
		{
			Vector3 world =
				glassMesh.transform.TransformPoint(
					glassVertices[i]
				);

			glassVertices[i] =
				transform.InverseTransformPoint(world);
		}

		int rings = heightSegments + 1;
		int sideVertexCount = rings * radialSegments;

		Vector3[] vertices =
			new Vector3[sideVertexCount + 2];

		Vector2[] uv = new Vector2[vertices.Length];

		List<int> triangles = new List<int>();

		float height = rim.y - bottom.y;

		// Создаём горизонтальные кольца.
		for (int y = 0; y < rings; y++)
		{
			float fraction =
				(float)y / heightSegments;

			float currentY =
				Mathf.Lerp(bottom.y, rim.y, fraction);

			Vector3 origin = new Vector3(
				center.x, currentY, center.y
			);

			for (int r = 0; r < radialSegments; r++)
			{
				float angle =
					Mathf.PI * 2f * r / radialSegments;

				Vector3 direction = new Vector3(
					Mathf.Cos(angle),
					0f,
					Mathf.Sin(angle)
				);

				float distance = FindInnerWall(
					origin,
					direction,
					glassVertices,
					glassTriangles
				);

				if (float.IsInfinity(distance))
				{
					throw new Exception(
						$"No inner wall at height ring {y}, " +
						$"direction {r}. Check the markers " +
						"and the glass mesh."
					);
				}

				if (distance <= wallInset + minRadius)
				{
					throw new Exception(
						$"Not enough cavity space at ring {y}. " +
						"Reduce Wall Inset or adjust markers."
					);
				}

				float radius = distance - wallInset;

				int index = y * radialSegments + r;

				vertices[index] =
					origin + direction * radius;

				uv[index] = new Vector2(
					(float)r / radialSegments,
					fraction
				);
			}
		}

		// Соединяем соседние кольца.
		for (int y = 0; y < heightSegments; y++)
		{
			for (int r = 0; r < radialSegments; r++)
			{
				int next = (r + 1) % radialSegments;

				int a = y * radialSegments + r;
				int b = y * radialSegments + next;

				int c = (y + 1) * radialSegments + r;
				int d = (y + 1) * radialSegments + next;

				// Внешняя сторона.
				triangles.Add(a);
				triangles.Add(c);
				triangles.Add(b);

				triangles.Add(b);
				triangles.Add(c);
				triangles.Add(d);
			}
		}

		// Закрываем объём для LiquidVolumeController.
		int bottomCenter = sideVertexCount;
		int topCenter = sideVertexCount + 1;

		vertices[bottomCenter] = new Vector3(
			center.x, bottom.y, center.y
		);

		vertices[topCenter] = new Vector3(
			center.x, rim.y, center.y
		);

		uv[bottomCenter] = Vector2.zero;
		uv[topCenter] = Vector2.one;

		int topRing = heightSegments * radialSegments;

		for (int r = 0; r < radialSegments; r++)
		{
			int next = (r + 1) % radialSegments;

			triangles.Add(bottomCenter);
			triangles.Add(r);
			triangles.Add(next);

			triangles.Add(topCenter);
			triangles.Add(topRing + next);
			triangles.Add(topRing + r);
		}

		Mesh result = new Mesh();
		result.name = "Generated Liquid Cavity";

		if (vertices.Length > 65535)
		{
			result.indexFormat =
				UnityEngine.Rendering.IndexFormat.UInt32;
		}

		result.vertices = vertices;
		result.uv = uv;
		result.SetTriangles(triangles, 0);

		result.RecalculateNormals();
		result.RecalculateBounds();

		return result;
	}

	private float FindInnerWall(
		Vector3 origin,
		Vector3 direction,
		Vector3[] vertices,
		int[] triangles)
	{
		float nearest = float.PositiveInfinity;

		for (int i = 0; i < triangles.Length; i += 3)
		{
			Vector3 a = vertices[triangles[i]];
			Vector3 b = vertices[triangles[i + 1]];
			Vector3 c = vertices[triangles[i + 2]];

			if (RayTriangle(
				origin, direction, a, b, c,
				out float distance))
			{
				if (distance < nearest)
					nearest = distance;
			}
		}

		return nearest;
	}

	// Двустороннее пересечение луча с треугольником.
	private bool RayTriangle(
		Vector3 origin,
		Vector3 direction,
		Vector3 a,
		Vector3 b,
		Vector3 c,
		out float distance)
	{
		distance = 0f;

		Vector3 edge1 = b - a;
		Vector3 edge2 = c - a;

		Vector3 p = Vector3.Cross(direction, edge2);
		float det = Vector3.Dot(edge1, p);

		if (Mathf.Abs(det) < Epsilon)
			return false;

		float invDet = 1f / det;

		Vector3 t = origin - a;
		float u = Vector3.Dot(t, p) * invDet;

		if (u < 0f || u > 1f)
			return false;

		Vector3 q = Vector3.Cross(t, edge1);
		float v = Vector3.Dot(direction, q) * invDet;

		if (v < 0f || u + v > 1f)
			return false;

		distance =
			Vector3.Dot(edge2, q) * invDet;

		return distance > Epsilon;
	}
}
