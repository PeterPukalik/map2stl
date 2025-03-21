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
    public class MeshData
    {
        public List<Vector3> Vertices { get; } = new List<Vector3>();
        public List<int> Indices { get; } = new List<int>();
    }

    public static class ModelProcessor
    {
        /// <summary>
        /// Main entry point:
        /// 1) Load terrain and extrude it downward into a solid "block."
        /// 2) Load buildings, sample the original terrain surface for their base height, place them on top.
        /// 3) Rotate final mesh and export to STL.
        /// </summary>
        /// <param name="terrainGlbPath">Path to terrain (DEM) GLB.</param>
        /// <param name="buildingsGlbPath">Path to buildings (OSM) GLB.</param>
        /// <param name="outputStlPath">Output STL path.</param>
        /// <param name="basePlateHeightFraction">Fraction of total model height to extrude below min Y.</param>
        /// <param name="fillBottom">
        /// If true, attempt to triangulate the bottom face (assuming a convex boundary).
        /// For non-convex boundaries, you need a more robust triangulator.
        /// </param>
        public static void ProcessModels(
            string terrainGlbPath,
            string buildingsGlbPath,
            string outputStlPath,
            float basePlateHeightFraction = 0.0025f,
            bool fillBottom = true)
        {
            // --------------------------------------------------------
            // 1. LOAD & EXTRACT TERRAIN GEOMETRY
            // --------------------------------------------------------
            var terrainModel = ModelRoot.Load(terrainGlbPath);

            var terrainMesh = terrainModel.LogicalMeshes[0].Primitives[0];
            var origTerrainVertices = terrainMesh.GetVertexAccessor("POSITION").AsVector3Array().ToArray();
            var origTerrainIndices = terrainMesh.GetTriangleIndices().SelectMany(t => new[] { t.A, t.B, t.C }).ToArray();

            // Create a MeshData for the top surface of the terrain
            var terrainData = new MeshData();
            terrainData.Vertices.AddRange(origTerrainVertices);
            terrainData.Indices.AddRange(origTerrainIndices);

            // --------------------------------------------------------
            // 2. EXTRUDE TERRAIN TO MAKE IT A WATERTIGHT BLOCK
            // --------------------------------------------------------
            FillUnderTerrain(terrainData, basePlateHeightFraction, fillBottom);
            // terrainData now represents a solid chunk of terrain, with
            // boundary edges extruded down and an optional bottom face.

            // --------------------------------------------------------
            // 3. LOAD BUILDINGS & PLACE THEM ON TOP
            // --------------------------------------------------------
            var buildingsModel = ModelRoot.Load(buildingsGlbPath);

            if (buildingsModel.LogicalMeshes.Count > 0)
            {
                var buildingMesh = buildingsModel.LogicalMeshes[0];
                foreach (var primitive in buildingMesh.Primitives)
                {
                    var buildingVertices = primitive.GetVertexAccessor("POSITION").AsVector3Array().ToArray();
                    var buildingIndices = primitive.GetTriangleIndices()
                                                   .SelectMany(t => new[] { t.A, t.B, t.C })
                                                   .ToArray();

                    // -- SHIFT building so it sits on top of the *original* terrain surface
                    AdjustBuildingVertices(buildingVertices, origTerrainVertices, origTerrainIndices);

                    // -- Merge building geometry into the extruded terrain
                    int offset = terrainData.Vertices.Count;
                    terrainData.Vertices.AddRange(buildingVertices);
                    foreach (var idx in buildingIndices)
                    {
                        terrainData.Indices.Add(idx + offset);
                    }
                }
            }

            // --------------------------------------------------------
            // 4. ROTATE FINAL MESH
            // --------------------------------------------------------
            // If you need to reorient for printing:
            RotateMesh(terrainData, -MathF.PI / 2, Vector3.UnitX); // -90° about X
            RotateMesh(terrainData, MathF.PI, Vector3.UnitX);      // 180° about X

            // --------------------------------------------------------
            // 5. EXPORT
            // --------------------------------------------------------
            ExportToStl(terrainData, outputStlPath);
        }

        #region Building -> Terrain Height Adjustment

        private static void AdjustBuildingVertices(Vector3[] buildingVertices, Vector3[] terrainVertices, int[] terrainIndices)
        {
            if (buildingVertices.Length == 0) return;

            float lowestBuildingY = buildingVertices.Min(v => v.Y);

            for (int i = 0; i < buildingVertices.Length; i++)
            {
                var vertex = buildingVertices[i];
                float terrainY = FindTerrainHeight(vertex.X, vertex.Z, terrainVertices, terrainIndices);
                float deltaY = vertex.Y - lowestBuildingY;
                buildingVertices[i] = new Vector3(vertex.X, terrainY + deltaY, vertex.Z);
            }
        }

        private static float FindTerrainHeight(float x, float z, Vector3[] terrainVertices, int[] terrainIndices)
        {
            Vector3 rayOrigin = new Vector3(x, 10000, z);  // something high
            Vector3 rayDirection = new Vector3(0, -1, 0);

            float closestDistance = float.MaxValue;
            Vector3? closestIntersection = null;

            for (int i = 0; i < terrainIndices.Length; i += 3)
            {
                Vector3 v0 = terrainVertices[terrainIndices[i]];
                Vector3 v1 = terrainVertices[terrainIndices[i + 1]];
                Vector3 v2 = terrainVertices[terrainIndices[i + 2]];

                Vector3? intersection = RayTriangleIntersection(rayOrigin, rayDirection, v0, v1, v2);
                if (intersection.HasValue)
                {
                    float dist = (rayOrigin - intersection.Value).Length();
                    if (dist < closestDistance)
                    {
                        closestDistance = dist;
                        closestIntersection = intersection;
                    }
                }
            }

            return closestIntersection?.Y ?? 0;
        }

        private static Vector3? RayTriangleIntersection(
            Vector3 rayOrigin,
            Vector3 rayDirection,
            Vector3 v0,
            Vector3 v1,
            Vector3 v2)
        {
            float epsilon = 1e-6f;
            Vector3 edge1 = v1 - v0;
            Vector3 edge2 = v2 - v0;
            Vector3 h = Vector3.Cross(rayDirection, edge2);
            float a = Vector3.Dot(edge1, h);

            if (a > -epsilon && a < epsilon) return null;

            float f = 1.0f / a;
            Vector3 s = rayOrigin - v0;
            float u = f * Vector3.Dot(s, h);
            if (u < 0.0f || u > 1.0f) return null;

            Vector3 q = Vector3.Cross(s, edge1);
            float v = f * Vector3.Dot(rayDirection, q);
            if (v < 0.0f || u + v > 1.0f) return null;

            float t = f * Vector3.Dot(edge2, q);
            if (t > epsilon)
            {
                return rayOrigin + rayDirection * t;
            }
            return null;
        }

        #endregion

        #region Extrude Terrain -> Solid Block

        /// <summary>
        /// Finds the outer boundary of the terrain mesh and extrudes it
        /// straight downward by "basePlateHeightFraction" of the total height.
        /// Optionally caps the bottom (naive fan triangulation, works if boundary is convex).
        /// </summary>
        private static void FillUnderTerrain(MeshData meshData, float basePlateHeightFraction, bool fillBottom)
        {
            if (meshData.Vertices.Count == 0) return;

            float minY = meshData.Vertices.Min(v => v.Y);
            float maxY = meshData.Vertices.Max(v => v.Y);
            float totalHeight = maxY - minY;

            float basePlaneY = minY - (totalHeight * basePlateHeightFraction);

            // 1) Identify boundary edges
            var boundaryEdges = FindBoundaryEdgesWithIndices(meshData);

            // 2) Create "down" copies of boundary vertices
            var newVertices = new List<Vector3>();
            var newIndices = new List<int>();

            // Map from original top vertex -> new "down" vertex index
            var downMap = new Dictionary<int, int>();

            foreach (var (i1, i2) in boundaryEdges)
            {
                if (!downMap.TryGetValue(i1, out int i1Down))
                {
                    Vector3 v1 = meshData.Vertices[i1];
                    i1Down = meshData.Vertices.Count + newVertices.Count;
                    newVertices.Add(new Vector3(v1.X, basePlaneY, v1.Z));
                    downMap[i1] = i1Down;
                }
                if (!downMap.TryGetValue(i2, out int i2Down))
                {
                    Vector3 v2 = meshData.Vertices[i2];
                    i2Down = meshData.Vertices.Count + newVertices.Count;
                    newVertices.Add(new Vector3(v2.X, basePlaneY, v2.Z));
                    downMap[i2] = i2Down;
                }

                // 2 triangles for the side wall
                newIndices.AddRange(new[] { i1, i1Down, i2Down });
                newIndices.AddRange(new[] { i1, i2Down, i2 });
            }

            // Add the new "down" vertices
            int baseVertexOffset = meshData.Vertices.Count;
            meshData.Vertices.AddRange(newVertices);

            // Add the side wall faces
            meshData.Indices.AddRange(newIndices);

            // 3) Optionally fill the bottom polygon
            if (fillBottom)
            {
                var bottomVerts = downMap.Values.Distinct().ToList();
                FillBottomPolygon(meshData, bottomVerts);
            }
        }

        /// <summary>
        /// Finds edges that appear in exactly 1 triangle => boundary.
        /// Returns them as (i1, i2) in ascending order.
        /// </summary>
        private static List<(int, int)> FindBoundaryEdgesWithIndices(MeshData meshData)
        {
            var edgeDict = new Dictionary<(int, int), int>();

            for (int i = 0; i < meshData.Indices.Count; i += 3)
            {
                int iA = meshData.Indices[i];
                int iB = meshData.Indices[i + 1];
                int iC = meshData.Indices[i + 2];

                AddEdge(edgeDict, iA, iB);
                AddEdge(edgeDict, iB, iC);
                AddEdge(edgeDict, iC, iA);
            }

            var boundary = new List<(int, int)>();
            foreach (var kvp in edgeDict)
            {
                if (kvp.Value == 1)
                {
                    boundary.Add(kvp.Key); // (min, max)
                }
            }
            return boundary;
        }

        private static void AddEdge(Dictionary<(int, int), int> dict, int a, int b)
        {
            if (b < a) (a, b) = (b, a);
            if (!dict.ContainsKey((a, b))) dict[(a, b)] = 0;
            dict[(a, b)]++;
        }

        /// <summary>
        /// Naive "fan" triangulation for the bottom ring (assuming it's convex).
        /// For non-convex shapes, you need a robust 2D polygon triangulator.
        /// </summary>
        private static void FillBottomPolygon(MeshData meshData, List<int> bottomIndices)
        {
            if (bottomIndices.Count < 3) return;

            // All these vertices share the same Y, so we can do 2D triangulation in XZ.
            var bottomVerts = bottomIndices.Select(i => meshData.Vertices[i]).ToList();
            float cx = bottomVerts.Average(v => v.X);
            float cz = bottomVerts.Average(v => v.Z);
            float yVal = bottomVerts[0].Y; // they all share the same Y
            Vector3 centroid = new Vector3(cx, yVal, cz);

            // Sort boundary vertices by angle around centroid
            bottomIndices.Sort((iA, iB) =>
            {
                var va = meshData.Vertices[iA] - centroid;
                var vb = meshData.Vertices[iB] - centroid;
                float angleA = MathF.Atan2(va.Z, va.X);
                float angleB = MathF.Atan2(vb.Z, vb.X);
                return angleA.CompareTo(angleB);
            });

            // Fan triangulation from [0]
            for (int i = 1; i < bottomIndices.Count - 1; i++)
            {
                meshData.Indices.Add(bottomIndices[0]);
                meshData.Indices.Add(bottomIndices[i]);
                meshData.Indices.Add(bottomIndices[i + 1]);
            }
        }

        #endregion

        #region Rotate & Export

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
            using var writer = new StreamWriter(filePath);
            writer.WriteLine("solid MergedModel");

            for (int i = 0; i < meshData.Indices.Count; i += 3)
            {
                var v1 = meshData.Vertices[meshData.Indices[i]];
                var v2 = meshData.Vertices[meshData.Indices[i + 1]];
                var v3 = meshData.Vertices[meshData.Indices[i + 2]];

                Vector3 normal = Vector3.Normalize(Vector3.Cross(v2 - v1, v3 - v1));
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

        #endregion
    }
}
