using SharpGLTF.Schema2;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace map2stl.ModelTweaks
{
    /// <summary>
    /// A simple container for merged geometry.
    /// </summary>
    public class MeshData
    {
        public List<Vector3> Vertices { get; } = new List<Vector3>();
        public List<int> Indices { get; } = new List<int>();
    }

    public static class ModelProcessor
    {
        /// <summary>
        /// Main entry point:
        /// - Loads the terrain and OSM GLB files.
        /// - For each building node in the OSM model, samples the base (four corners + center) and casts downward rays
        ///    to determine the highest terrain point beneath the building.
        /// - Shifts the building vertically so that its base is flush with the terrain.
        /// - Merges the terrain and adjusted building geometry into a single mesh.
        /// - Applies a rotation (e.g. -90° about X) so that the model is oriented for 3D printing.
        /// - Exports the final mesh as an ASCII STL.
        /// </summary>
        /// <param name="terrainGlbPath">Path to the terrain (DEM) GLB file.</param>
        /// <param name="buildingsGlbPath">Path to the OSM buildings GLB file.</param>
        /// <param name="outputStlPath">Output path for the merged STL file.</param>
            public static void ProcessModels(string terrainGlbPath, string buildingsGlbPath, string outputStlPath)
            {
                var terrainModel = ModelRoot.Load(terrainGlbPath);
                var buildingsModel = ModelRoot.Load(buildingsGlbPath);

                var terrainMesh = terrainModel.LogicalMeshes[0].Primitives[0];
                var buildingsMeshes = buildingsModel.LogicalMeshes;

                var terrainVertices = terrainMesh.GetVertexAccessor("POSITION").AsVector3Array().ToArray();
                var terrainIndices = terrainMesh.GetTriangleIndices().SelectMany(t => new[] { t.A, t.B, t.C }).ToArray();

                var mergedData = new MeshData();
                mergedData.Vertices.AddRange(terrainVertices);
                mergedData.Indices.AddRange(terrainIndices);

                foreach (var buildingMesh in buildingsMeshes)
                {
                    foreach (var primitive in buildingMesh.Primitives)
                    {
                        var buildingVertices = primitive.GetVertexAccessor("POSITION").AsVector3Array().ToArray();
                        var buildingIndices = primitive.GetTriangleIndices();

                        AdjustBuildingVertices(buildingVertices, terrainVertices, terrainIndices);

                        int vertexOffset = mergedData.Vertices.Count;
                        mergedData.Vertices.AddRange(buildingVertices);
                        foreach (var indexTuple in buildingIndices)
                        {
                            mergedData.Indices.Add(indexTuple.A + vertexOffset);
                            mergedData.Indices.Add(indexTuple.B + vertexOffset);
                            mergedData.Indices.Add(indexTuple.C + vertexOffset);
                        }
                    }
                }

                //AddBasePlane(mergedData);
                //AddPrintableSides(mergedData);
                RotateMesh(mergedData, -MathF.PI / 2, Vector3.UnitX);
                ExportToStl(mergedData, outputStlPath);
            }

        //private static void AdjustBuildingVertices(Vector3[] buildingVertices, Vector3[] terrainVertices, int[] terrainIndices)
        //{
        //    for (int i = 0; i < buildingVertices.Length; i++)
        //    {
        //        var vertex = buildingVertices[i];
        //        float terrainY = FindTerrainHeight(vertex.X, vertex.Z, terrainVertices, terrainIndices);
        //        buildingVertices[i] = new Vector3(vertex.X, terrainY + 0.05f, vertex.Z); // Small offset
        //    }
        //}
        private static float FindSmoothedTerrainHeight(float x, float z, Vector3[] terrainVertices, int[] terrainIndices, float smoothingRadius = 1.0f)
        {
            float totalHeight = 0;
            int count = 0;

            for (int i = 0; i < terrainVertices.Length; i++)
            {
                float dx = terrainVertices[i].X - x;
                float dz = terrainVertices[i].Z - z;
                float distanceSquared = dx * dx + dz * dz;

                if (distanceSquared <= smoothingRadius * smoothingRadius)
                {
                    totalHeight += FindTerrainHeight(terrainVertices[i].X, terrainVertices[i].Z, terrainVertices, terrainIndices);
                    count++;
                }
            }

            if (count > 0)
            {
                return totalHeight / count;
            }
            else
            {
                return FindTerrainHeight(x, z, terrainVertices, terrainIndices); // Fallback to original height
            }
        }

        private static void AdjustBuildingVertices(Vector3[] buildingVertices, Vector3[] terrainVertices, int[] terrainIndices)
        {
            for (int i = 0; i < buildingVertices.Length; i++)
            {
                var vertex = buildingVertices[i];
                float terrainY = FindSmoothedTerrainHeight(vertex.X, vertex.Z, terrainVertices, terrainIndices);
                buildingVertices[i] = new Vector3(vertex.X, terrainY + 0.05f, vertex.Z); // Small offset
            }
        }
        private static float FindTerrainHeight(float x, float z, Vector3[] terrainVertices, int[] terrainIndices)
            {
                Vector3 rayOrigin = new Vector3(x, 1000, z);
                Vector3 rayDirection = new Vector3(0, -1, 0);
                Vector3? closestIntersection = null;
                float closestDistance = float.MaxValue;

                for (int i = 0; i < terrainIndices.Length; i += 3)
                {
                    Vector3 v0 = terrainVertices[terrainIndices[i]];
                    Vector3 v1 = terrainVertices[terrainIndices[i + 1]];
                    Vector3 v2 = terrainVertices[terrainIndices[i + 2]];

                    Vector3? intersection = RayTriangleIntersection(rayOrigin, rayDirection, v0, v1, v2);

                    if (intersection.HasValue)
                    {
                        float distance = (rayOrigin - intersection.Value).Length();
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closestIntersection = intersection;
                        }
                    }
                }

                return closestIntersection?.Y ?? 0;
            }
            private static Vector3? RayTriangleIntersection(Vector3 rayOrigin, Vector3 rayDirection, Vector3 v0, Vector3 v1, Vector3 v2)
        {
            float epsilon = 0.000001f;
            Vector3 edge1 = v1 - v0;
            Vector3 edge2 = v2 - v0;
            Vector3 h = Vector3.Cross(rayDirection, edge2);
            float a = Vector3.Dot(edge1, h);

            if (a > -epsilon && a < epsilon)
                return null;

            float f = 1.0f / a;
            Vector3 s = rayOrigin - v0;
            float u = f * Vector3.Dot(s, h);

            if (u < 0.0f || u > 1.0f)
                return null;

            Vector3 q = Vector3.Cross(s, edge1);
            float v = f * Vector3.Dot(rayDirection, q);

            if (v < 0.0f || u + v > 1.0f)
                return null;

            float t = f * Vector3.Dot(edge2, q);

            if (t > epsilon)
                return rayOrigin + rayDirection * t;
            else
                return null;
        }

        private static (Vector3 Min, Vector3 Max) CalculateBounds(Vector3[] vertices)
        {
            if (vertices.Length == 0) return (Vector3.Zero, Vector3.Zero);

            Vector3 min = new Vector3(float.MaxValue);
            Vector3 max = new Vector3(float.MinValue);

            foreach (var vertex in vertices)
            {
                min = Vector3.Min(min, vertex);
                max = Vector3.Max(max, vertex);
            }

            return (min, max);
        }

        private static void RotateMesh(MeshData meshData, float angle, Vector3 axis)
        {
            var rotation = Quaternion.CreateFromAxisAngle(axis, angle);
            for (int i = 0; i < meshData.Vertices.Count; i++)
            {
                meshData.Vertices[i] = Vector3.Transform(meshData.Vertices[i], rotation);
            }
        }
        private static void ExportToStl(MeshData meshData, string filePath)
        {
            using (var writer = new StreamWriter(filePath))
            {
                writer.WriteLine("solid MergedModel");

                for (int i = 0; i < meshData.Indices.Count; i += 3)
                {
                    var v1 = meshData.Vertices[meshData.Indices[i]];
                    var v2 = meshData.Vertices[meshData.Indices[i + 1]];
                    var v3 = meshData.Vertices[meshData.Indices[i + 2]];

                    var normal = Vector3.Normalize(Vector3.Cross(v2 - v1, v3 - v1));

                    writer.WriteLine($"facet normal {normal.X} {normal.Y} {normal.Z}");
                    writer.WriteLine("outer loop");
                    writer.WriteLine($"vertex {v1.X} {v1.Y} {v1.Z}");
                    writer.WriteLine($"vertex {v2.X} {v2.Y} {v2.Z}");
                    writer.WriteLine($"vertex {v3.X} {v3.Y} {v3.Z}");
                    writer.WriteLine("endloop");
                    writer.WriteLine("endfacet");
                }

                writer.WriteLine("endsolid MergedModel");
            }
        }

        private static void AddBasePlane(MeshData meshData)
        {
            if (meshData.Vertices.Count == 0) return;

            float minY = meshData.Vertices.Min(v => v.Y); // Find lowest Y of *all* vertices
            var bounds = CalculateBounds(meshData.Vertices.ToArray());

            Vector3 v0 = new Vector3(bounds.Min.X, minY, bounds.Min.Z);
            Vector3 v1 = new Vector3(bounds.Max.X, minY, bounds.Min.Z);
            Vector3 v2 = new Vector3(bounds.Max.X, minY, bounds.Max.Z);
            Vector3 v3 = new Vector3(bounds.Min.X, minY, bounds.Max.Z);

            int baseVertexOffset = meshData.Vertices.Count;
            meshData.Vertices.AddRange(new[] { v0, v1, v2, v3 });

            meshData.Indices.AddRange(new[] {
            baseVertexOffset, baseVertexOffset + 2, baseVertexOffset + 1,
            baseVertexOffset, baseVertexOffset + 3, baseVertexOffset + 2
                });
        }


        private static void AddPrintableSides(MeshData meshData)
        {
            if (meshData.Vertices.Count == 0 || meshData.Indices.Count == 0) return;

            var boundaryEdges = FindBoundaryEdges(meshData);
            float basePlaneY = meshData.Vertices.Min(v => v.Y);

            var newVertices = new List<Vector3>();
            var newIndices = new List<int>();

            foreach (var edge in boundaryEdges)
            {
                Vector3 v1 = edge.v1;
                Vector3 v2 = edge.v2;

                Vector3 v1Down = new Vector3(v1.X, basePlaneY, v1.Z);
                Vector3 v2Down = new Vector3(v2.X, basePlaneY, v2.Z);

                int v1Index = meshData.Vertices.IndexOf(v1);
                int v2Index = meshData.Vertices.IndexOf(v2);

                int v1DownIndex = meshData.Vertices.Count + newVertices.Count;
                int v2DownIndex = meshData.Vertices.Count + newVertices.Count + 1;

                newVertices.AddRange(new[] { v1Down, v2Down });

                newIndices.AddRange(new[]
                {
                    v1Index, v1DownIndex, v2DownIndex,
                    v1Index, v2DownIndex, v2Index
                });
            }

            meshData.Vertices.AddRange(newVertices);
            meshData.Indices.AddRange(newIndices);
        }

        private static List<(Vector3 v1, Vector3 v2)> FindBoundaryEdges(MeshData meshData)
        {
            var edges = new Dictionary<(int v1, int v2), int>();

            for (int i = 0; i < meshData.Indices.Count; i += 3)
            {
                int i1 = meshData.Indices[i];
                int i2 = meshData.Indices[i + 1];
                int i3 = meshData.Indices[i + 2];

                AddEdge(edges, i1, i2);
                AddEdge(edges, i2, i3);
                AddEdge(edges, i3, i1);
            }

            var boundaryEdges = new List<(Vector3 v1, Vector3 v2)>();
            foreach (var edge in edges)
            {
                if (edge.Value == 1)
                {
                    Vector3 v1 = meshData.Vertices[edge.Key.v1];
                    Vector3 v2 = meshData.Vertices[edge.Key.v2];
                    boundaryEdges.Add((v1, v2));
                }
            }

            return boundaryEdges;
        }

        private static void AddEdge(Dictionary<(int v1, int v2), int> edges, int i1, int i2)
        {
            if (i1 > i2)
            {
                (i1, i2) = (i2, i1);
            }

            var edgeKey = (i1, i2);
            if (edges.ContainsKey(edgeKey))
            {
                edges[edgeKey]++;
            }
            else
            {
                edges[edgeKey] = 1;
            }
        }
    }
}
