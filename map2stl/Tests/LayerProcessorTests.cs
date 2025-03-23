using System;
using System.IO;
using System.Collections.Generic;
using Xunit;
using NetTopologySuite.Geometries;
using map2stl.ModelTweaks;

namespace map2stl.Tests
{
    public class LayerProcessorTests
    {
        [Fact]
        public void ProcessAndExport_ShouldProcessAndExportStl_WithRealFiles()
        {
            // Arrange
            var processor = new LayerProcessor();
            string osmFilePath = "C:\\Users\\Pukalik.Peter\\source\\repos\\map2stl\\map2stl\\ModelTweaks\\osm.glb";
            var buildingFootprints = processor.LoadBuildingFootprintsFromGlb(osmFilePath);
            var roadLines = processor.LoadRoadLinesFromGlb(osmFilePath);
            var terrainBoundary = processor.GetTerrainBoundaryFromGlb(osmFilePath);
            string terrainModelPath = "C:\\Users\\Pukalik.Peter\\source\\repos\\map2stl\\map2stl\\ModelTweaks\\model.glb";
            string outputGlbPath = "C:\\Users\\Pukalik.Peter\\source\\repos\\map2stl\\map2stl\\ModelTweaks\\output.glb";
            string outputStlPath = "C:\\Users\\Pukalik.Peter\\source\\repos\\map2stl\\map2stl\\ModelTweaks\\output.stl";

            // Act
            processor.ProcessAndExport(buildingFootprints, roadLines, terrainBoundary, terrainModelPath, outputGlbPath, outputStlPath);

            // Assert
            // Verify that the STL file was created
            Assert.True(File.Exists(outputStlPath));
            // Verify that the GLB file was created
            Assert.True(File.Exists(outputGlbPath));

            // Clean up
            if (File.Exists(outputStlPath))
            {
                File.Delete(outputStlPath);
            }
            if (File.Exists(outputGlbPath))
            {
                File.Delete(outputGlbPath);
            }
        }
    }
}

