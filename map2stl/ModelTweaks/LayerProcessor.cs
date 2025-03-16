using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics; // For Vector3 and Vector4
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Schema2;
using SharpGLTF.Scenes;
using NetTopologySuite.Noding.Snapround;


// Alias NetTopologySuite's Polygon so it doesn't conflict with other types.
using NtsPolygon = NetTopologySuite.Geometries.Polygon;
using NetTopologySuite.Precision;

public class MeshData
{
    public List<Vector3> Vertices { get; set; } = new List<Vector3>();
    public List<int> Indices { get; set; } = new List<int>();

    // Helper method to add a triangle.
    public void AddTriangle(Vector3 v0, Vector3 v1, Vector3 v2)
    {
        int baseIndex = Vertices.Count;
        Vertices.Add(v0);
        Vertices.Add(v1);
        Vertices.Add(v2);
        Indices.Add(baseIndex);
        Indices.Add(baseIndex + 1);
        Indices.Add(baseIndex + 2);
    }
}

public class LayerProcessor
{
    // A GeometryFactory for NTS geometries (e.g. footprints)
    private readonly GeometryFactory _geomFactory;
    // A coordinate transformer (e.g. WGS84 to local projection)
    private readonly MathTransform _coordTransform;

    public LayerProcessor()
    {
        _geomFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 0);
        var csWgs84 = GeographicCoordinateSystem.WGS84;
        var csProj = ProjectedCoordinateSystem.WebMercator; // Change to your terrain CRS as needed
        _coordTransform = new CoordinateTransformationFactory()
                            .CreateFromCoordinateSystems(csWgs84, csProj)
                            .MathTransform;
    }

    // Stub: Replace with your actual terrain height lookup logic.
    private double GetTerrainHeight(double x, double y)
    {
        // For demonstration, return a flat terrain at height 0.
        return 0.0;
    }

    // Stub: Replace with your logic to generate a terrain mesh.
    // Here we return a flat square mesh.
    private MeshData GetTerrainMesh(string terrainModelPath)
    {
        MeshData mesh = new MeshData();
        float size = 100f;
        Vector3 v0 = new Vector3(-size, -size, 0);
        Vector3 v1 = new Vector3(size, -size, 0);
        Vector3 v2 = new Vector3(size, size, 0);
        Vector3 v3 = new Vector3(-size, size, 0);
        mesh.AddTriangle(v0, v1, v2);
        mesh.AddTriangle(v2, v3, v0);
        return mesh;
    }

    // Stub: Replace with your actual logic to determine a building's height (e.g. from OSM attributes).
    private double GetBuildingHeight(NtsPolygon footprint)
    {
        return 10.0;
    }

    // Builds a 3D building mesh from a footprint (using an NTS polygon) and a given height.
    public MeshData BuildBuildingMesh(NtsPolygon footprint, double height)
    {
        // Transform footprint coordinates into the terrain coordinate system.
        var coords = footprint.Coordinates.Select(c =>
        {
            var pt = _coordTransform != null ? _coordTransform.Transform(new[] { c.X, c.Y }) : new double[] { c.X, c.Y };
            return new Coordinate(pt[0], pt[1]);
        }).ToList();

        // Ensure the polygon is closed.
        if (!coords.First().Equals2D(coords.Last()))
            coords.Add(coords.First());

        // Build base vertices by sampling terrain height.
        List<Vector3> basePoints = new List<Vector3>();
        foreach (var coord in coords.Take(coords.Count - 1))
        {
            double z = GetTerrainHeight(coord.X, coord.Y);
            basePoints.Add(new Vector3((float)coord.X, (float)coord.Y, (float)z));
        }

        // Build top vertices by adding the building's height.
        List<Vector3> topPoints = basePoints.Select(p => new Vector3(p.X, p.Y, p.Z + (float)height)).ToList();

        MeshData mesh = new MeshData();
        int n = basePoints.Count;
        // Create vertical walls.
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            mesh.AddTriangle(basePoints[i], basePoints[j], topPoints[j]);
            mesh.AddTriangle(topPoints[j], topPoints[i], basePoints[i]);
        }
        // Create the roof using a simple fan triangulation.
        if (n >= 3)
        {
            Vector3 p0 = topPoints[0];
            for (int i = 1; i < n - 1; i++)
            {
                mesh.AddTriangle(p0, topPoints[i], topPoints[i + 1]);
            }
        }
        return mesh;
    }

    // Builds a 3D road mesh from a LineString and a specified road width.
    public MeshData BuildRoadMesh(LineString roadLine, double roadWidth = 4.0)
    {
        var points = roadLine.Coordinates.Select(c =>
        {
            var pt = _coordTransform != null ? _coordTransform.Transform(new[] { c.X, c.Y }) : new double[] { c.X, c.Y };
            double z = GetTerrainHeight(pt[0], pt[1]);
            return new Vector3((float)pt[0], (float)pt[1], (float)z);
        }).ToList();

        if (points.Count < 2)
            return new MeshData(); // Return an empty MeshData instead of null

        double halfW = roadWidth / 2.0;
        MeshData mesh = new MeshData();
        List<Vector3> leftEdge = new List<Vector3>();
        List<Vector3> rightEdge = new List<Vector3>();

        for (int i = 0; i < points.Count; i++)
        {
            Vector3 dir;
            if (i == points.Count - 1)
                dir = points[i] - points[i - 1];
            else if (i == 0)
                dir = points[i + 1] - points[i];
            else
                dir = points[i + 1] - points[i - 1];

            // Zero out Z to compute an in-plane perpendicular.
            dir.Z = 0;
            if (dir != Vector3.Zero)
                dir = Vector3.Normalize(dir);
            Vector3 perp = new Vector3(-dir.Y, dir.X, 0);
            if (perp != Vector3.Zero)
                perp = Vector3.Normalize(perp);

            Vector3 pCenter = points[i];
            Vector3 pLeft = pCenter + perp * (float)halfW;
            Vector3 pRight = pCenter - perp * (float)halfW;
            // Recompute terrain height for left/right points.
            pLeft.Z = (float)GetTerrainHeight(pLeft.X, pLeft.Y);
            pRight.Z = (float)GetTerrainHeight(pRight.X, pRight.Y);
            leftEdge.Add(pLeft);
            rightEdge.Add(pRight);
        }

        // Build road ribbon triangles.
        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector3 left0 = leftEdge[i];
            Vector3 right0 = rightEdge[i];
            Vector3 left1 = leftEdge[i + 1];
            Vector3 right1 = rightEdge[i + 1];
            mesh.AddTriangle(left0, right0, left1);
            mesh.AddTriangle(left1, right0, right1);
        }
        return mesh;
    }

    // Clip building footprints to a given terrain boundary.
    public IEnumerable<NtsPolygon> ClipBuildingFootprints(IEnumerable<NtsPolygon> buildings, NtsPolygon terrainBoundary)
    {
        List<NtsPolygon> inside = new List<NtsPolygon>();
        var precisionModel = new PrecisionModel(1.0); // Adjust precision as needed
        var noder = new GeometryPrecisionReducer(precisionModel);

        foreach (var b in buildings)
        {
            var nodedB = noder.Reduce(b);
            var nodedTerrain = noder.Reduce(terrainBoundary);

            if (nodedB.IsValid && nodedTerrain.IsValid)
            {
                Geometry inter = nodedB.Intersection(nodedTerrain);
                if (!inter.IsEmpty)
                {
                    if (inter is NtsPolygon poly)
                    {
                        inside.Add(poly);
                    }
                    else if (inter is MultiPolygon multi)
                    {
                        inside.AddRange(multi.Geometries.Cast<NtsPolygon>());
                    }
                    else if (inter is GeometryCollection geomCollection)
                    {
                        foreach (var geom in geomCollection.Geometries)
                        {
                            if (geom is NtsPolygon gPoly)
                            {
                                inside.Add(gPoly);
                            }
                            else if (geom is MultiPolygon gMulti)
                            {
                                inside.AddRange(gMulti.Geometries.Cast<NtsPolygon>());
                            }
                        }
                    }
                }
            }
        }
        return inside;
    }

    // Clip road lines to a given terrain boundary.
    public IEnumerable<LineString> ClipRoadLines(IEnumerable<LineString> roads, NtsPolygon terrainBoundary)
    {
        List<LineString> inside = new List<LineString>();
        foreach (var r in roads)
        {
            Geometry inter = r.Intersection(terrainBoundary);
            if (inter.IsEmpty)
                continue;
            if (inter is LineString ls)
                inside.Add(ls);
            else if (inter is MultiLineString mls)
                inside.AddRange(mls.Geometries.Cast<LineString>());
        }
        return inside;
    }

    // Main processing and export method.
    public void ProcessAndExport(
        IEnumerable<NtsPolygon> buildingFootprints,
        IEnumerable<LineString> roadLines,
        NtsPolygon terrainBoundary,
        string terrainModelPath,
        string outputGlbPath,
        string outputStlPath)
    {
        var clippedBuildings = ClipBuildingFootprints(buildingFootprints, terrainBoundary);
        var clippedRoads = ClipRoadLines(roadLines, terrainBoundary);

        MeshData terrainMesh = GetTerrainMesh(terrainModelPath);
        MeshData combinedMesh = new MeshData();

        // Combine terrain, building, and road meshes.
        AppendMesh(combinedMesh, terrainMesh);
        foreach (var footprint in clippedBuildings)
        {
            double height = GetBuildingHeight(footprint);
            MeshData bMesh = BuildBuildingMesh(footprint, height);
            AppendMesh(combinedMesh, bMesh);
        }
        foreach (var road in clippedRoads)
        {
            MeshData rMesh = BuildRoadMesh(road);
            if (rMesh != null)
                AppendMesh(combinedMesh, rMesh);
        }

        // Export to glTF.
        ExportGlb(combinedMesh, outputGlbPath);
        // Export to ASCII STL.
        ExportStl(combinedMesh, outputStlPath);
    }

    // Helper method to merge two meshes.
    private void AppendMesh(MeshData target, MeshData source)
    {
        int offset = target.Vertices.Count;
        target.Vertices.AddRange(source.Vertices);
        foreach (var index in source.Indices)
        {
            target.Indices.Add(index + offset);
        }
    }

    // Exports the mesh to a glTF Binary (.glb) file using SharpGLTF.
    private void ExportGlb(MeshData mesh, string outputPath)
    {
        // Create a basic material.
        var material = new MaterialBuilder("default")
                        .WithMetallicRoughnessShader()
                        .WithChannelParam(KnownChannel.BaseColor, KnownProperty.RGBA, new Vector4(0.8f, 0.8f, 0.8f, 1f));
        var gltfMeshBuilder = new MeshBuilder<VertexPositionNormal>("mesh");

        // Build triangles.
        for (int i = 0; i < mesh.Indices.Count; i += 3)
        {
            Vector3 v0 = mesh.Vertices[mesh.Indices[i]];
            Vector3 v1 = mesh.Vertices[mesh.Indices[i + 1]];
            Vector3 v2 = mesh.Vertices[mesh.Indices[i + 2]];

            // For simplicity, we use a default normal (you may compute per-triangle normals).
            var normal = Vector3.UnitZ;
            gltfMeshBuilder.UsePrimitive(material).AddTriangle(
                new VertexPositionNormal(v0, normal),
                new VertexPositionNormal(v1, normal),
                new VertexPositionNormal(v2, normal));
        }
        var scene = new SceneBuilder();
        scene.AddRigidMesh(gltfMeshBuilder, System.Numerics.Matrix4x4.Identity);
        scene.ToGltf2().Save(outputPath);
    }

    // Exports the mesh to a simple ASCII STL file.
    private void ExportStl(MeshData mesh, string outputPath)
    {
        using (var writer = new StreamWriter(outputPath))
        {
            writer.WriteLine("solid mesh");
            for (int i = 0; i < mesh.Indices.Count; i += 3)
            {
                Vector3 v0 = mesh.Vertices[mesh.Indices[i]];
                Vector3 v1 = mesh.Vertices[mesh.Indices[i + 1]];
                Vector3 v2 = mesh.Vertices[mesh.Indices[i + 2]];
                Vector3 normal = Vector3.Normalize(Vector3.Cross(v1 - v0, v2 - v0));
                writer.WriteLine($" facet normal {normal.X} {normal.Y} {normal.Z}");
                writer.WriteLine("  outer loop");
                writer.WriteLine($"   vertex {v0.X} {v0.Y} {v0.Z}");
                writer.WriteLine($"   vertex {v1.X} {v1.Y} {v1.Z}");
                writer.WriteLine($"   vertex {v2.X} {v2.Y} {v2.Z}");
                writer.WriteLine("  endloop");
                writer.WriteLine(" endfacet");
            }
            writer.WriteLine("endsolid mesh");
        }
    }

    public List<NtsPolygon> LoadBuildingFootprintsFromGlb(string glbFilePath)
    {
        var model = ModelRoot.Load(glbFilePath);
        var buildingFootprints = new List<NtsPolygon>();

        foreach (var node in model.LogicalNodes)
        {
            if (node.Mesh == null) continue;

            foreach (var prim in node.Mesh.Primitives)
            {
                var positions = prim.GetVertexAccessor("POSITION").AsVector3Array();
                var coords = positions.Select(p => new Coordinate(p.X, p.Y)).ToList();

                // Ensure the polygon is closed
                if (!coords.First().Equals2D(coords.Last()))
                {
                    coords.Add(coords.First());
                }

                var ring = new LinearRing(coords.ToArray());
                var polygon = new NtsPolygon(ring);
                buildingFootprints.Add(polygon);
            }
        }

        return buildingFootprints;
    }

    public List<LineString> LoadRoadLinesFromGlb(string glbFilePath)
    {
        var model = ModelRoot.Load(glbFilePath);
        var roadLines = new List<LineString>();

        foreach (var node in model.LogicalNodes)
        {
            if (node.Mesh == null) continue;

            foreach (var prim in node.Mesh.Primitives)
            {
                var positions = prim.GetVertexAccessor("POSITION").AsVector3Array();
                var coords = positions.Select(p => new Coordinate(p.X, p.Y)).ToList();

                // Ensure the linestring is closed
                if (!coords.First().Equals2D(coords.Last()))
                {
                    coords.Add(coords.First());
                }

                var lineString = new LineString(coords.ToArray());
                roadLines.Add(lineString);
            }
        }

        return roadLines;
    }

    public NtsPolygon GetTerrainBoundaryFromGlb(string glbFilePath)
    {
        var model = ModelRoot.Load(glbFilePath);
        var boundaryCoords = new List<Coordinate>();

        foreach (var node in model.LogicalNodes)
        {
            if (node.Mesh == null) continue;

            foreach (var prim in node.Mesh.Primitives)
            {
                var positions = prim.GetVertexAccessor("POSITION").AsVector3Array();
                boundaryCoords.AddRange(positions.Select(p => new Coordinate(p.X, p.Y)));
            }
        }

        // Ensure the boundary is closed
        if (!boundaryCoords.First().Equals2D(boundaryCoords.Last()))
        {
            boundaryCoords.Add(boundaryCoords.First());
        }

        // Create a convex hull to approximate the terrain boundary
        var boundaryPolygon = new NtsPolygon(new LinearRing(boundaryCoords.ToArray()));
        var convexHull = (NtsPolygon)boundaryPolygon.ConvexHull();

        return convexHull;
    }
}
