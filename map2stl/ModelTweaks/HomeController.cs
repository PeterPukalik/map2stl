//using Microsoft.AspNetCore.Mvc;
//using SharpGLTF.Schema2;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Numerics;

//namespace map2stl.ModelTweaks
//{
//    /// <summary>
//    /// Simple container for merged vertices + indices.
//    /// </summary>
//    public class MeshData
//    {
//        public List<Vector3> Vertices { get; } = new List<Vector3>();
//        public List<int> Indices { get; } = new List<int>();
//    }

//    public static class ModelProcessor
//    {
//        /// <summary>
//        /// Entry point:
//        /// 1) Load DEM & OSM
//        /// 2) Cull OSM nodes outside DEM bounding box
//        /// 3) For each in-bounds OSM node, vertically align it so its base touches the DEM
//        /// 4) Merge DEM + OSM into a single mesh
//        /// 5) Export ASCII STL
//        /// </summary>
//        public static void ProcessModels(string osmFilePath, string demFilePath, string outputStlPath)
//        {
//            // 1. Load models
//            ModelRoot demModel = ModelRoot.Load(demFilePath);
//            ModelRoot osmModel = ModelRoot.Load(osmFilePath);

//            // 2. Compute DEM bounding box and extract DEM triangles
//            var demBox = ComputeModelBoundingBox(demModel);
//            var demTriangles = ExtractDemTriangles(demModel);

//            // 3. Merge models into a single mesh
//            MeshData merged = new MeshData();

//            // Merge DEM as-is
//            ProcessModelForMerge(demModel, merged, skipOutOfBounds: false, demBox, demTriangles, doVerticalAlign: false);

//            // Merge OSM with culling + vertical alignment
//            ProcessModelForMerge(osmModel, merged, skipOutOfBounds: true, demBox, demTriangles, doVerticalAlign: true);

//            // 4. Export merged mesh to STL
//            ExportToStl(merged, outputStlPath);
//            Console.WriteLine("Exported STL to: " + outputStlPath);
//        }

//        private static void ProcessModelForMerge(
//            ModelRoot model,
//            MeshData merged,
//            bool skipOutOfBounds,
//            (Vector3 Min, Vector3 Max) demBox,
//            List<(Vector3 A, Vector3 B, Vector3 C)> demTriangles,
//            bool doVerticalAlign)
//        {
//            foreach (var node in model.LogicalNodes)
//            {
//                if (node.Mesh == null) continue;
//                var nodeBox = ComputeNodeBoundingBox(node);
//                if (skipOutOfBounds && !BoxesIntersect(nodeBox, demBox))
//                {
//                    Console.WriteLine($"Skipping node '{node.Name}' - out of DEM bounds.");
//                    continue;
//                }

//                if (doVerticalAlign)
//                {
//                    if (!AlignNodeToTerrain(node, demTriangles))
//                    {
//                        Console.WriteLine($"Skipping node '{node.Name}' - vertical alignment failed.");
//                        continue;
//                    }
//                }

//                foreach (var prim in node.Mesh.Primitives)
//                {
//                    var positions = prim.GetVertexAccessor("POSITION").AsVector3Array();
//                    var indices = prim.IndexAccessor.AsIndicesArray();

//                    int baseIndex = merged.Vertices.Count;
//                    for (int i = 0; i < positions.Count; i++)
//                    {
//                        Vector3 worldPos = Vector3.Transform(positions[i], node.GetWorldMatrix(null, 0));
//                        merged.Vertices.Add(worldPos);
//                    }
//                    for (int i = 0; i < indices.Count; i++)
//                    {
//                        merged.Indices.Add(baseIndex + (int)indices[i]);
//                    }
//                }
//            }
//        }

//        /// <summary>
//        /// Vertically aligns a node by raycasting downward from the center of its XZ bounding box.
//        /// Shifts the node so its bounding box min.Y equals the DEM height at that point.
//        /// Returns true if successful.
//        /// </summary>
//        private static bool AlignNodeToTerrain(Node node, List<(Vector3 A, Vector3 B, Vector3 C)> demTriangles)
//        {
//            var nodeBox = ComputeNodeBoundingBox(node);
//            float centerX = (nodeBox.Min.X + nodeBox.Max.X) / 2;
//            float centerZ = (nodeBox.Min.Z + nodeBox.Max.Z) / 2;
//            float nodeMinY = nodeBox.Min.Y;

//            Vector3 startPos = new Vector3(centerX, nodeBox.Max.Y + 1, centerZ);
//            Vector3 dir = new Vector3(0, -1, 0);
//            float? terrainY = RaycastDownToDem(startPos, dir, demTriangles);
//            if (!terrainY.HasValue)
//            {
//                Console.WriteLine($"Alignment failed for node '{node.Name}': no DEM intersection at center.");
//                return false;
//            }

//            float offsetY = terrainY.Value - nodeMinY;
//            if (MathF.Abs(offsetY) < 1e-6f) return true;
//            var translation = Matrix4x4.CreateTranslation(0, offsetY, 0);
//            node.LocalTransform = translation * node.LocalTransform;
//            Console.WriteLine($"Node '{node.Name}' shifted by {offsetY} in Y.");
//            return true;
//        }

//        private static (Vector3 Min, Vector3 Max) ComputeModelBoundingBox(ModelRoot model)
//        {
//            Vector3 min = new Vector3(float.MaxValue);
//            Vector3 max = new Vector3(float.MinValue);
//            foreach (var node in model.LogicalNodes)
//            {
//                var (nMin, nMax) = ComputeNodeBoundingBox(node);
//                min = Vector3.Min(min, nMin);
//                max = Vector3.Max(max, nMax);
//            }
//            return (min, max);
//        }

//        private static (Vector3 Min, Vector3 Max) ComputeNodeBoundingBox(Node node)
//        {
//            Vector3 min = new Vector3(float.MaxValue);
//            Vector3 max = new Vector3(float.MinValue);
//            if (node.Mesh != null)
//            {
//                foreach (var prim in node.Mesh.Primitives)
//                {
//                    var positions = prim.GetVertexAccessor("POSITION").AsVector3Array();
//                    for (int i = 0; i < positions.Count; i++)
//                    {
//                        Vector3 wpos = Vector3.Transform(positions[i], node.GetWorldMatrix(null, 0));
//                        min = Vector3.Min(min, wpos);
//                        max = Vector3.Max(max, wpos);
//                    }
//                }
//            }
//            return (min, max);
//        }

//        private static bool BoxesIntersect((Vector3 Min, Vector3 Max) a, (Vector3 Min, Vector3 Max) b)
//        {
//            return (a.Min.X <= b.Max.X && a.Max.X >= b.Min.X) &&
//                   (a.Min.Y <= b.Max.Y && a.Max.Y >= b.Min.Y) &&
//                   (a.Min.Z <= b.Max.Z && a.Max.Z >= b.Min.Z);
//        }

//        private static List<(Vector3 A, Vector3 B, Vector3 C)> ExtractDemTriangles(ModelRoot demModel)
//        {
//            var triList = new List<(Vector3, Vector3, Vector3)>();
//            foreach (var node in demModel.LogicalNodes)
//            {
//                if (node.Mesh == null) continue;
//                foreach (var prim in node.Mesh.Primitives)
//                {
//                    var positions = prim.GetVertexAccessor("POSITION").AsVector3Array();
//                    var indices = prim.IndexAccessor.AsIndicesArray();
//                    for (int i = 0; i < indices.Count; i += 3)
//                    {
//                        Vector3 p1 = Vector3.Transform(positions[(int)indices[i]], node.GetWorldMatrix(null, 0));
//                        Vector3 p2 = Vector3.Transform(positions[(int)indices[i + 1]], node.GetWorldMatrix(null, 0));
//                        Vector3 p3 = Vector3.Transform(positions[(int)indices[i + 2]], node.GetWorldMatrix(null, 0));
//                        triList.Add((p1, p2, p3));
//                    }
//                }
//            }
//            return triList;
//        }

//        private static float? RaycastDownToDem(Vector3 startPos, Vector3 dir, List<(Vector3 A, Vector3 B, Vector3 C)> demTriangles)
//        {
//            float closest = float.MaxValue;
//            bool found = false;
//            foreach (var tri in demTriangles)
//            {
//                if (RayTriangleIntersect(startPos, dir, tri.A, tri.B, tri.C, out float t))
//                {
//                    if (t < closest)
//                    {
//                        closest = t;
//                        found = true;
//                    }
//                }
//            }
//            if (found)
//            {
//                return startPos.Y + closest * dir.Y;
//            }
//            return null;
//        }

//        private static bool RayTriangleIntersect(Vector3 origin, Vector3 dir, Vector3 v0, Vector3 v1, Vector3 v2, out float t)
//        {
//            t = 0;
//            Vector3 edge1 = v1 - v0;
//            Vector3 edge2 = v2 - v0;
//            Vector3 pvec = Vector3.Cross(dir, edge2);
//            float det = Vector3.Dot(edge1, pvec);
//            if (MathF.Abs(det) < 1e-8f) return false;
//            float invDet = 1.0f / det;
//            Vector3 tvec = origin - v0;
//            float u = Vector3.Dot(tvec, pvec) * invDet;
//            if (u < 0 || u > 1) return false;
//            Vector3 qvec = Vector3.Cross(tvec, edge1);
//            float v = Vector3.Dot(dir, qvec) * invDet;
//            if (v < 0 || (u + v) > 1) return false;
//            float tt = Vector3.Dot(edge2, qvec) * invDet;
//            if (tt < 0) return false;
//            t = tt;
//            return true;
//        }

//        /// <summary>
//        /// Computes a rigid transform mapping corners (b1, b2, b3) to (t1, t2, t3) using a simple orthonormal basis approach.
//        /// </summary>
//        private static Matrix4x4 ComputeRigidTransform(
//            Vector3 b1, Vector3 b2, Vector3 b3,
//            Vector3 t1, Vector3 t2, Vector3 t3)
//        {
//            Vector3 b21 = b2 - b1;
//            Vector3 b31 = b3 - b1;
//            Vector3 t21 = t2 - t1;
//            Vector3 t31 = t3 - t1;

//            Vector3 Bx = Vector3.Normalize(b21);
//            Vector3 Bn = Vector3.Normalize(Vector3.Cross(b21, b31));
//            if (Bn.Length() < 1e-6f) return Matrix4x4.Identity;
//            Vector3 By = Vector3.Normalize(Vector3.Cross(Bn, Bx));
//            Matrix4x4 MB = new Matrix4x4(
//                Bx.X, By.X, Bn.X, 0,
//                Bx.Y, By.Y, Bn.Y, 0,
//                Bx.Z, By.Z, Bn.Z, 0,
//                0, 0, 0, 1);

//            Vector3 Tx = Vector3.Normalize(t21);
//            Vector3 Tn = Vector3.Normalize(Vector3.Cross(t21, t31));
//            if (Tn.Length() < 1e-6f) return Matrix4x4.Identity;
//            Vector3 Ty = Vector3.Normalize(Vector3.Cross(Tn, Tx));
//            Matrix4x4 MT = new Matrix4x4(
//                Tx.X, Ty.X, Tn.X, 0,
//                Tx.Y, Ty.Y, Tn.Y, 0,
//                Tx.Z, Ty.Z, Tn.Z, 0,
//                0, 0, 0, 1);

//            Matrix4x4 MBt = Matrix4x4.Transpose(MB);
//            Matrix4x4 R = Matrix4x4.Multiply(MT, MBt);

//            Vector3 b1Transformed = Vector3.Transform(b1, R);
//            Vector3 trans = t1 - b1Transformed;
//            R.Translation += trans;
//            return R;
//        }

//        /// <summary>
//        /// Exports merged mesh data as an ASCII STL file.
//        /// </summary>
//        private static void ExportToStl(MeshData mesh, string outputStlPath)
//        {
//            using (var sw = new StreamWriter(outputStlPath))
//            {
//                sw.WriteLine("solid merged");
//                for (int i = 0; i < mesh.Indices.Count; i += 3)
//                {
//                    Vector3 v1 = mesh.Vertices[mesh.Indices[i]];
//                    Vector3 v2 = mesh.Vertices[mesh.Indices[i + 1]];
//                    Vector3 v3 = mesh.Vertices[mesh.Indices[i + 2]];

//                    Vector3 e1 = v2 - v1;
//                    Vector3 e2 = v3 - v1;
//                    Vector3 normal = Vector3.Normalize(Vector3.Cross(e1, e2));

//                    sw.WriteLine($"facet normal {normal.X} {normal.Y} {normal.Z}");
//                    sw.WriteLine("  outer loop");
//                    sw.WriteLine($"    vertex {v1.X} {v1.Y} {v1.Z}");
//                    sw.WriteLine($"    vertex {v2.X} {v2.Y} {v2.Z}");
//                    sw.WriteLine($"    vertex {v3.X} {v3.Y} {v3.Z}");
//                    sw.WriteLine("  endloop");
//                    sw.WriteLine("endfacet");
//                }
//                sw.WriteLine("endsolid merged");
//            }
//        }
//    }
//}
