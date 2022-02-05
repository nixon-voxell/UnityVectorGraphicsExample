using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Voxell.GPUVectorGraphics;
using Voxell.GPUVectorGraphics.Font;
using Voxell.Inspector;

public class DelaunayTest : MonoBehaviour
{
  public FontCurve fontCurve;
  public Transform[] transforms;
  public char character;
  [InspectOnly] public int glyphIdx;
  public float2 maxRect;
  public float2 minRect;

  [InspectOnly] public Mesh mesh;
  public MeshFilter meshFilter;
  public int highlightTriangle;
  public int highlightVertex;

  [Button]
  private void TransformTest()
  {
    if (transforms == null || transforms.Length == 0) return;
    float2[] points = new float2[transforms.Length];
    mesh.Clear();

    int pointCount = points.Length;
    for (int p=0; p < pointCount; p++)
    {
      float3 pos = transforms[p].localPosition;
      points[p] = new float2(pos.x, pos.y);
    }

    NativeList<float2> na_points = new NativeList<float2>(pointCount + 4, Allocator.TempJob);
    NativeList<int> na_triangles = new NativeList<int>(pointCount, Allocator.TempJob);
    for (int p=0; p < pointCount; p++) na_points.Add(points[p]);
    DelaunayTriangulation.Triangulate(minRect, maxRect, ref na_points, ref na_triangles);

    int vertexCount = na_points.Length;
    NativeArray<float3> na_vertices = new NativeArray<float3>(vertexCount, Allocator.Temp);
    for (int v=0; v < vertexCount; v++) na_vertices[v] = new float3(na_points[v], 0.0f);
    mesh.SetVertices<float3>(na_vertices);
    mesh.SetIndices<int>(na_triangles, MeshTopology.Triangles, 0);

    meshFilter.sharedMesh = mesh;

    na_points.Dispose();
    na_triangles.Dispose();
    na_vertices.Dispose();
  }

  [Button]
  private void Test()
  {
    if (fontCurve == null) return;
    glyphIdx = fontCurve.TryGetGlyhIndex(character);
    if (glyphIdx != -1)
    {
      Glyph glyph = fontCurve.Glyphs[glyphIdx];
      int contourCount = glyph.contours.Length;
      List<float2> points = new List<float2>();

      maxRect = glyph.maxRect;
      minRect = glyph.minRect;
      for (int c=0; c < contourCount; c++)
      {
        QuadraticContour glyphContour = glyph.contours[c];
        int segmentCount = glyphContour.segments.Length;

        for (int s=0; s < segmentCount; s++)
          points.Add(glyphContour.segments[s].p0);
      }

      if (mesh == null) mesh = new Mesh();
      mesh.Clear();

      int pointCount = points.Count;
      NativeList<float2> na_points = new NativeList<float2>(pointCount + 4, Allocator.TempJob);
      NativeList<int> na_triangles = new NativeList<int>(pointCount, Allocator.TempJob);
      for (int p=0; p < pointCount; p++) na_points.Add(points[p]);
      DelaunayTriangulation.Triangulate(minRect, maxRect, ref na_points, ref na_triangles);

      int vertexCount = na_points.Length;
      NativeArray<float3> na_vertices = new NativeArray<float3>(vertexCount, Allocator.Temp);
      for (int v=0; v < vertexCount; v++) na_vertices[v] = new float3(na_points[v], 0.0f);
      mesh.SetVertices<float3>(na_vertices);
      mesh.SetIndices<int>(na_triangles, MeshTopology.Triangles, 0);

      meshFilter.sharedMesh = mesh;

      na_points.Dispose();
      na_triangles.Dispose();
      na_vertices.Dispose();
    }
  }

  private void OnDrawGizmos()
  {
    if (mesh == null) return;
    int[] triangles = mesh.triangles;
    Vector3[] vertices = mesh.vertices;
    int tIdx = highlightTriangle*3;
    if (tIdx >= triangles.Length || tIdx < 0) return;

    Gizmos.color = Color.red;
    Gizmos.DrawLine(transform.position + vertices[triangles[tIdx]], transform.position + vertices[triangles[tIdx + 1]]);
    Gizmos.DrawLine(transform.position + vertices[triangles[tIdx + 1]], transform.position + vertices[triangles[tIdx + 2]]);
    Gizmos.DrawLine(transform.position + vertices[triangles[tIdx + 2]], transform.position + vertices[triangles[tIdx]]);

    if (highlightVertex >= vertices.Length || highlightVertex < 0) return;
    Gizmos.DrawSphere(transform.position +  vertices[highlightVertex], 0.02f);
  }
}