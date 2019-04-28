using System;
using System.Collections.Generic;

namespace n50kartdata_etl
{
    public class ResourceModelUtils
    {
        public static string ResourceTarget(string Context, string ResourceId)
        {
            return Context + "/" + CalculateXXHash64(ResourceId);
        }

        private static string CalculateXXHash64(string key)
        {
            return Sparrow.Hashing.XXHash64.Calculate(key, System.Text.Encoding.UTF8).ToString();
        }

        public static string WKTProjectToWGS84(string wkt, int fromsrid)
        {
            var geometry = new NetTopologySuite.IO.WKTReader().Read(wkt);

            ProjNet.CoordinateSystems.CoordinateSystem utm = ProjNet.CoordinateSystems.ProjectedCoordinateSystem.WGS84_UTM(33, true) as ProjNet.CoordinateSystems.CoordinateSystem;
            ProjNet.CoordinateSystems.CoordinateSystem wgs84 = ProjNet.CoordinateSystems.GeographicCoordinateSystem.WGS84 as ProjNet.CoordinateSystems.CoordinateSystem;

            var transformation = new ProjNet.CoordinateSystems.Transformations.CoordinateTransformationFactory().CreateFromCoordinateSystems(utm, wgs84);

            return NetTopologySuite.CoordinateSystems.Transformations.GeometryTransform.TransformGeometry(
                geometry.Factory,
                geometry,
                transformation.MathTransform).ToString();
        }
    }
}
