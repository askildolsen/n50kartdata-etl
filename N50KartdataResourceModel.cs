using System;
using System.Collections.Generic;
using System.Linq;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Linq.Indexing;
using static n50kartdata_etl.ResourceModel;
using static n50kartdata_etl.ResourceModelUtils;

namespace n50kartdata_etl
{
    public class N50KartdataResourceModel
    {
        public class N50KartdataResourceIndex : AbstractMultiMapIndexCreationTask<Resource>
        {
            public N50KartdataResourceIndex()
            {
                AddMap<Kommune>(n50kartdata =>
                    from kommune in n50kartdata.WhereEntityIs<Kommune>("N50Kartdata")
                    let metadata = MetadataFor(kommune)
                    where metadata.Value<string>("@id").StartsWith("N50Kartdata/Kommune")
                    select new Resource
                    {
                        ResourceId = "Kommune/" + kommune.kommunenummer,
                        Type = new[] { "Kommune" },
                        Title = new[] { kommune.navn },
                        Code = new[] { kommune.kommunenummer },
                        Properties =
                            from wkt in new[] { kommune._omrade.wkt }.Where(v => !String.IsNullOrWhiteSpace(v))
                            select new Property { Name = "Område", Tags = new[] { "@wkt" }, Value = new[] { WKTProjectToWGS84(wkt, 0) } },
                        Source = new[] { metadata.Value<string>("@id") },
                        Modified = kommune.oppdateringsdato
                    }
                );

                Reduce = results  =>
                    from result in results
                    group result by result.ResourceId into g
                    select new Resource
                    {
                        ResourceId = g.Key,
                        Type = g.SelectMany(r => r.Type).Distinct(),
                        Title = g.SelectMany(r => r.Title).Distinct(),
                        Code = g.SelectMany(r => r.Code).Distinct(),
                        Properties = g.SelectMany(resource => resource.Properties),
                        Source = g.SelectMany(resource => resource.Source).Distinct(),
                        Modified = g.Select(resource => resource.Modified).Max()
                    };

                Index(r => r.Properties, FieldIndexing.No);
                Store(r => r.Properties, FieldStorage.Yes);

                OutputReduceToCollection = "N50KartdataResource";

                AdditionalSources = new Dictionary<string, string>
                {
                    {
                        "ResourceModel",
                        ReadResourceFile("n50kartdata_etl.ResourceModelUtils.cs")
                    }
                };
            }

            public override IndexDefinition CreateIndexDefinition()
            {
                var indexDefinition = base.CreateIndexDefinition();
                indexDefinition.Configuration = new IndexConfiguration { { "Indexing.MapTimeoutInSec", "30"} };

                return indexDefinition;
            }
        }

        private static string ReadResourceFile(string filename)
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