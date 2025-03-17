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


            // 2. Compute DEM bounding box and extract DEM triangles
            var demBox = ComputeModelBoundingBox(terrainModel);
            var demTriangles = ExtractDemTriangles(terrainModel);

            var terrainMesh = terrainModel.LogicalMeshes[0].Primitives[0];
            var buildingsMeshes = buildingsModel.LogicalMeshes[0].Primitives[0];

            var terrainVertices = terrainMesh.GetVertexAccessor("POSITION").AsVector3Array().ToArray();
            var terrainIndices = terrainMesh.GetTriangleIndices().SelectMany(t => new[] { t.A, t.B, t.C }).ToArray();

            var mergedData = new MeshData();
            mergedData.Vertices.AddRange(terrainVertices);
            mergedData.Indices.AddRange(terrainIndices);

            // Process only the *first* mesh in the buildings model.  Crucial change.
            if (buildingsModel.LogicalMeshes.Count > 0) // Check if there are ANY meshes
            {
                var buildingMesh = buildingsModel.LogicalMeshes[0]; // Take only the FIRST mesh

                // Iterate through the primitives of the FIRST building mesh.
                foreach (var primitive in buildingMesh.Primitives)
                {
                    var buildingVertices = primitive.GetVertexAccessor("POSITION").AsVector3Array().ToArray();
                    var buildingIndices = primitive.GetTriangleIndices().SelectMany(t => new[] { t.A, t.B, t.C }).ToArray(); // Get indices as int[]


                    // Adjust building vertices based on the terrain.
                    AdjustBuildingVertices(buildingVertices, terrainVertices, terrainIndices);


                    // Add the adjusted building vertices and indices to the merged data.
                    int vertexOffset = mergedData.Vertices.Count;
                    mergedData.Vertices.AddRange(buildingVertices);
                    foreach (var index in buildingIndices)  // Use the int[] indices directly
                    {
                        mergedData.Indices.Add(index + vertexOffset);
                    }
                }
            }


            RotateMesh(mergedData, -MathF.PI / 2, Vector3.UnitX);
            RotateMesh(mergedData, MathF.PI, Vector3.UnitX); // 180 degrees (PI radians) around X
            //AddBasePlate(mergedData, terrainModel);
            //AddPrintableSides(mergedData);
            ExportToStl(mergedData, outputStlPath);
        }
        //private static void AdjustBuildingVertices(Vector3[] buildingVertices, Vector3[] terrainVertices, int[] terrainIndices)
        //{
        //    if (buildingVertices.Length == 0) return;

        //    float lowestBuildingY = buildingVertices.Min(v => v.Y);
        //    float highestTerrainY = float.MinValue;

        //    for (int i = 0; i < buildingVertices.Length; i++)
        //    {
        //        float terrainY = FindTerrainHeight(buildingVertices[i].X, buildingVertices[i].Z, terrainVertices, terrainIndices);
        //        if (terrainY > highestTerrainY)
        //        {
        //            highestTerrainY = terrainY;
        //        }
        //    }

        //    float deltaY = highestTerrainY - lowestBuildingY;

        //    for (int i = 0; i < buildingVertices.Length; i++)
        //    {
        //        buildingVertices[i] = new Vector3(buildingVertices[i].X, buildingVertices[i].Y + deltaY, buildingVertices[i].Z);
        //    }
        //}

        private static void AdjustBuildingVertices(Vector3[] buildingVertices, Vector3[] terrainVertices, int[] terrainIndices)
        {
            if (buildingVertices.Length == 0) return;

            // 1. Find the lowest point of the building
            float lowestBuildingY = buildingVertices.Min(v => v.Y);

            // 2. Raycast and adjust
            for (int i = 0; i < buildingVertices.Length; i++)
            {
                var vertex = buildingVertices[i];
                float terrainY = FindTerrainHeight(vertex.X, vertex.Z, terrainVertices, terrainIndices);

                // 3. Calculate delta
                float deltaY = vertex.Y - lowestBuildingY;

                // 4. Adjust vertex height
                buildingVertices[i] = new Vector3(vertex.X, terrainY + deltaY, vertex.Z);
            }
        }

        private static Vector3 CalculateBuildingCenter(Vector3[] buildingVertices)
        {
            if (buildingVertices.Length == 0) return Vector3.Zero;

            Vector3 min = new Vector3(float.MaxValue);
            Vector3 max = new Vector3(float.MinValue);

            foreach (var vertex in buildingVertices)
            {
                min = Vector3.Min(min, vertex);
                max = Vector3.Max(max, vertex);
            }

            return (min + max) / 2;
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
        private static void AddBasePlate(MeshData meshData, ModelRoot terrainModel, float heightFraction = 0.25f)
        {
            if (meshData.Vertices.Count == 0 || terrainModel == null) return;

            // 1. Find min/max Y of the *entire* mesh.
            float minY = meshData.Vertices.Min(v => v.Y);
            float maxY = meshData.Vertices.Max(v => v.Y);
            float totalHeight = maxY - minY;
            float basePlaneY = minY - (totalHeight * heightFraction);

            // 2. Calculate bounds using the *TERRAIN* model.
            var terrainMesh = terrainModel.LogicalMeshes[0].Primitives[0];
            var terrainVertices = terrainMesh.GetVertexAccessor("POSITION").AsVector3Array().ToArray();
            var bounds = CalculateBounds(terrainVertices);

            // 3. Create base plane vertices (unrotated).
            Vector3 v0 = new Vector3(bounds.Min.X, basePlaneY, bounds.Min.Z);
            Vector3 v1 = new Vector3(bounds.Max.X, basePlaneY, bounds.Min.Z);
            Vector3 v2 = new Vector3(bounds.Max.X, basePlaneY, bounds.Max.Z);
            Vector3 v3 = new Vector3(bounds.Min.X, basePlaneY, bounds.Max.Z);

            // 4. Define the rotations.  IMPORTANT:  Apply them in the *correct order*.
            var rotation1 = Quaternion.CreateFromAxisAngle(Vector3.UnitX, -MathF.PI / 2); // -90 degrees around X
            var rotation2 = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI);     // 180 degrees around X

            // 5. Combine the rotations
            var combinedRotation = rotation2 * rotation1;


            // 6. Apply the *combined* rotation to the base plate vertices.
            v0 = Vector3.Transform(v0, combinedRotation);
            v1 = Vector3.Transform(v1, combinedRotation);
            v2 = Vector3.Transform(v2, combinedRotation);
            v3 = Vector3.Transform(v3, combinedRotation);


            // 7. Add vertices and indices to meshData.
            int baseVertexOffset = meshData.Vertices.Count;
            meshData.Vertices.AddRange(new[] { v0, v1, v2, v3 });

            meshData.Indices.AddRange(new[] {
                baseVertexOffset, baseVertexOffset + 2, baseVertexOffset + 1,
                baseVertexOffset, baseVertexOffset + 3, baseVertexOffset + 2
            });
        }

        private static (Vector3 Min, Vector3 Max) CalculateBoundsPlate(Vector3[] vertices)
        {
            if (vertices.Length == 0) return (Vector3.Zero, Vector3.Zero);

            Vector3 min = vertices[0];
            Vector3 max = vertices[0];

            for (int i = 1; i < vertices.Length; i++)
            {
                min = Vector3.Min(min, vertices[i]);
                max = Vector3.Max(max, vertices[i]);
            }
            return (min, max);
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
        private static List<(Vector3 A, Vector3 B, Vector3 C)> ExtractDemTriangles(ModelRoot demModel)
        {
            var triList = new List<(Vector3, Vector3, Vector3)>();
            foreach (var node in demModel.LogicalNodes)
            {
                if (node.Mesh == null) continue;
                foreach (var prim in node.Mesh.Primitives)
                {
                    var positions = prim.GetVertexAccessor("POSITION").AsVector3Array();
                    var indices = prim.IndexAccessor.AsIndicesArray();
                    for (int i = 0; i < indices.Count; i += 3)
                    {
                        Vector3 p1 = Vector3.Transform(positions[(int)indices[i]], node.GetWorldMatrix(null, 0));
                        Vector3 p2 = Vector3.Transform(positions[(int)indices[i + 1]], node.GetWorldMatrix(null, 0));
                        Vector3 p3 = Vector3.Transform(positions[(int)indices[i + 2]], node.GetWorldMatrix(null, 0));
                        triList.Add((p1, p2, p3));
                    }
                }
            }
            return triList;
        }
        private static (Vector3 Min, Vector3 Max) ComputeModelBoundingBox(ModelRoot model)
        {
            Vector3 min = new Vector3(float.MaxValue);
            Vector3 max = new Vector3(float.MinValue);
            foreach (var node in model.LogicalNodes)
            {
                var (nMin, nMax) = ComputeNodeBoundingBox(node);
                min = Vector3.Min(min, nMin);
                max = Vector3.Max(max, nMax);
            }
            return (min, max);
        }

        private static (Vector3 Min, Vector3 Max) ComputeNodeBoundingBox(Node node)
        {
            Vector3 min = new Vector3(float.MaxValue);
            Vector3 max = new Vector3(float.MinValue);
            if (node.Mesh != null)
            {
                foreach (var prim in node.Mesh.Primitives)
                {
                    var positions = prim.GetVertexAccessor("POSITION").AsVector3Array();
                    for (int i = 0; i < positions.Count; i++)
                    {
                        Vector3 wpos = Vector3.Transform(positions[i], node.GetWorldMatrix(null, 0));
                        min = Vector3.Min(min, wpos);
                        max = Vector3.Max(max, wpos);
                    }
                }
            }
            return (min, max);
        }




    }


}