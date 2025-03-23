using OSGeo.GDAL;
using System;

namespace map2stl.Helpers
{
    public class DemProcessor : IDisposable
    {
        private Dataset _demDataset;
        private double[] _geoTransform;

        public DemProcessor(string demFilePath)
        {
            // Initialize GDAL (do this once in your app ideally)
            Gdal.AllRegister();

            _demDataset = Gdal.Open(demFilePath, Access.GA_ReadOnly);
            if (_demDataset == null)
                throw new Exception("Could not open DEM file: " + demFilePath);

            // Get geotransform (6 elements)
            _geoTransform = new double[6];
            _demDataset.GetGeoTransform(_geoTransform);
            // _geoTransform[0]: top left x; _geoTransform[1]: pixel width; _geoTransform[2]: rotation (usually 0)
            // _geoTransform[3]: top left y; _geoTransform[4]: rotation (usually 0); _geoTransform[5]: pixel height (usually negative)
        }

        /// <summary>
        /// Returns the terrain height at a given world coordinate (x,y).
        /// Assumes that the horizontal coordinates are x = easting and y = northing.
        /// </summary>
        public double GetTerrainHeightAt(double x, double y)
        {
            // Convert world coordinate to pixel coordinate
            double pixel = (x - _geoTransform[0]) / _geoTransform[1];
            double line = (y - _geoTransform[3]) / _geoTransform[5]; // note: _geoTransform[5] is negative

            int iPixel = (int)Math.Floor(pixel);
            int iLine = (int)Math.Floor(line);

            Band band = _demDataset.GetRasterBand(1);
            float[] buffer = new float[1];
            band.ReadRaster(iPixel, iLine, 1, 1, buffer, 1, 1, 0, 0);
            return buffer[0];
        }

        public void Dispose()
        {
            _demDataset?.Dispose();
        }
    }
}
