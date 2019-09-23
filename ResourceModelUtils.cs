using System;
using System.Collections.Generic;
using System.Linq;

namespace n50kartdata_etl
{
    public class ResourceModelUtils
    {

        public static IEnumerable<dynamic> Properties(IEnumerable<dynamic> properties)
        {
            foreach(var propertyG in ((IEnumerable<dynamic>)properties).GroupBy(p => p.Name))
            {
                if (propertyG.Any(p => p.Tags.Contains("@union")))
                {
                    yield
                        return new {
                            Name = propertyG.Key,
                            Value = propertyG.SelectMany(p => (IEnumerable<dynamic>)p.Value).Distinct(),
                            Tags = propertyG.SelectMany(p => (IEnumerable<dynamic>)p.Tags).Distinct(),
                            Resources = propertyG.SelectMany(p => (IEnumerable<dynamic>)p.Resources).Distinct(),
                        };
                }
                else
                {
                    yield return propertyG.First();
                }
            }
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

        public static string ReadResourceFile(string filename)
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream(filename))
            {
                using (var reader = new System.IO.StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
