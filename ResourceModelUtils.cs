using System.Collections.Generic;

namespace n50kartdata_etl
{
    public class ResourceModelUtils
    {
        public static IEnumerable<dynamic> Properties(IEnumerable<dynamic> properties)
        {
            return Digitalisert.Raven.ResourceModelExtensions.Properties(properties);
        }

        public static string WKTProjectToWGS84(string wkt, int fromsrid)
        {
            return Digitalisert.Raven.ResourceModelExtensions.WKTProjectToWGS84(wkt, fromsrid);
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
