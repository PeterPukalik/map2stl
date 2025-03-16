using System;
using System.IO;
using System.Numerics;
using System.Collections.Generic;
using Xunit;
using Moq;
using SharpGLTF.Schema2;
using map2stl.ModelTweaks;

namespace map2stl.Tests
{
    public class ModelProcessorTests
    {
        [Fact]
        public void ProcessModels_ShouldProcessAndExportStl_WithRealFiles()
        {
            // Arrange
            string osmFilePath = "C:\\Users\\Pukalik.Peter\\source\\repos\\map2stl\\map2stl\\ModelTweaks\\osm.glb";
            string demFilePath = "C:\\Users\\Pukalik.Peter\\source\\repos\\map2stl\\map2stl\\ModelTweaks\\model.glb";
            string outputStlPath = "C:\\Users\\Pukalik.Peter\\source\\repos\\map2stl\\map2stl\\ModelTweaks\\output.stl";
            string outputglbPath = "C:\\Users\\Pukalik.Peter\\source\\repos\\map2stl\\map2stl\\ModelTweaks\\output.glb";

            // Act
            //ModelProcessor.ProcessModels(osmFilePath, demFilePath, outputStlPath);
            //ModelProcessor.ProcessModels(osmFilePath, demFilePath, outputStlPath, outputglbPath);
            ModelProcessor.ProcessModels(demFilePath, osmFilePath, outputStlPath);

            // Assert
            // Verify that the STL file was created
            Assert.True(File.Exists(outputStlPath));

            // Clean up
           // if (File.Exists(outputStlPath))
           // {
              //  File.Delete(outputStlPath);
            //}
        }


    }
}
