using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Scenes;
using SharpGLTF.Schema2;
using SharpGLTF.Materials;
using System;
using System.Collections.Generic;
using System.Numerics;

public static class GltfExporter
{
    /// <summary>
    /// Exports the provided MeshData as a GLB (binary glTF) file.
    /// This sample uses a default material with white BaseColor.
    /// (Texture support requires additional steps.)
    /// </summary>
    /// <param name="meshData">Your mesh data (vertices and indices)</param>
    /// <param name="filePath">Output GLB file path</param>
    /// <param name="textureImagePath">
    /// Optional: Path to a texture image.
    /// (Texture assignment requires extra API calls not shown here.)
    /// </param>
    public static void ExportToGltf(MeshData meshData, string filePath, string textureImagePath = null)
    {
        //// Create a basic material with a metallic-roughness shader and white BaseColor.
        //var material = new MaterialBuilder("DefaultMaterial")
        //    .WithMetallicRoughnessShader()
        //    .WithChannelParam("BaseColor", new Vector4(1, 1, 1, 1));

        //// Create a MeshBuilder that uses MaterialBuilder.
        //// The generic parameters are: Material type, vertex type (for position & normal),
        //// texture coordinate type, and an empty type for additional attributes.
        //var meshBuilder = new MeshBuilder<MaterialBuilder, VertexPositionNormal, VertexTexture1, VertexEmpty>("TerrainMesh");

        //// UsePrimitive requires the material and a target index (here 0).
        //var primBuilder = meshBuilder.UsePrimitive(material, 0);

        //// Loop over each triangle in your MeshData.
        //// We use VertexBuilder with all three type arguments.
        //for (int i = 0; i < meshData.Indices.Count; i += 3)
        //{
        //    int idx0 = meshData.Indices[i];
        //    int idx1 = meshData.Indices[i + 1];
        //    int idx2 = meshData.Indices[i + 2];

        //    Vector3 p0 = meshData.Vertices[idx0];
        //    Vector3 p1 = meshData.Vertices[idx1];
        //    Vector3 p2 = meshData.Vertices[idx2];

        //    // Compute a face normal.
        //    Vector3 normal = Vector3.Normalize(Vector3.Cross(p1 - p0, p2 - p0));

        //    // Generate simple planar UV mapping (using XZ coordinates).
        //    var uv0 = new Vector2(p0.X, p0.Z);
        //    var uv1 = new Vector2(p1.X, p1.Z);
        //    var uv2 = new Vector2(p2.X, p2.Z);

        //    // Create vertex builders for each vertex.
        //    var vb0 = new VertexBuilder<VertexPositionNormal, VertexTexture1, VertexEmpty>(
        //        new VertexPositionNormal(p0, normal),
        //        new VertexTexture1(uv0),
        //        new VertexEmpty());
        //    var vb1 = new VertexBuilder<VertexPositionNormal, VertexTexture1, VertexEmpty>(
        //        new VertexPositionNormal(p1, normal),
        //        new VertexTexture1(uv1),
        //        new VertexEmpty());
        //    var vb2 = new VertexBuilder<VertexPositionNormal, VertexTexture1, VertexEmpty>(
        //        new VertexPositionNormal(p2, normal),
        //        new VertexTexture1(uv2),
        //        new VertexEmpty());

        //    primBuilder.AddTriangle(vb0, vb1, vb2);
        //}

        //// Instead of converting the MeshBuilder via ToGltfMesh(),
        //// we add the meshBuilder directly to a SceneBuilder.
        //var sceneBuilder = new SceneBuilder();
        //sceneBuilder.AddRigidMesh(meshBuilder);

        //// Build the glTF model.
        //ModelRoot model = sceneBuilder.ToGltf2();

        //// Save as a binary GLB file.
        //model.SaveGLB(filePath);
    }
}
