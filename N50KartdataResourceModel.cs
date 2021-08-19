using System;
using System.Collections.Generic;
using System.Linq;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Linq.Indexing;
using static Digitalisert.Dataplattform.ResourceModel;
using static Digitalisert.Dataplattform.ResourceModelExtensions;

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
                        SubType = new string[] { },
                        Title = new[] { kommune.navn },
                        Code = new[] { kommune.kommunenummer },
                        Status = new string[] { },
                        Properties =
                            from wkt in new[] { kommune._omrade.wkt }.Where(v => !String.IsNullOrWhiteSpace(v))
                            select new Property { Name = "Område", Tags = new[] { "@wkt" }, Value = new[] { WKTProjectToWGS84(wkt, 0) } },
                        Source = new[] { metadata.Value<string>("@id") },
                        Modified = kommune.oppdateringsdato
                    }
                );

                AddMap<Kommune>(n50kartdata =>
                    from kommune in n50kartdata.WhereEntityIs<Kommune>("N50Kartdata")
                    let metadata = MetadataFor(kommune)
                    where metadata.Value<string>("@id").StartsWith("N50Kartdata/Kommune")

                    from fylke in new[] {
                        new { Code = "03", Title = "Oslo"},
                        new { Code = "11", Title = "Rogaland"},
                        new { Code = "15", Title = "Møre og Romsdal"},
                        new { Code = "18", Title = "Nordland"},
                        new { Code = "30", Title = "Viken"},
                        new { Code = "34", Title = "Innlandet"},
                        new { Code = "38", Title = "Vestfold og Telemark"},
                        new { Code = "42", Title = "Agder" },
                        new { Code = "46", Title = "Vestland"},
                        new { Code = "50", Title = "Trøndelag"},
                        new { Code = "54", Title = "Troms og Finnmark"},
                    }
                    where fylke.Code == kommune.kommunenummer.Substring(0,2)
 
                    select new Resource
                    {
                        ResourceId = "Fylke/" + fylke.Code,
                        Type = new[] { "Fylke" },
                        SubType = new string[] { },
                        Title = new[] { fylke.Title },
                        Code = new[] { fylke.Code },
                        Status = new string[] { },
                        Properties = (
                            new[] {
                                new Property {
                                    Name = "Kommune",
                                    Resources = new[] { new Resource { ResourceId = "Kommune/" + kommune.kommunenummer } }
                                }
                            }
                        ).Union(
                            from wkt in new[] { kommune._omrade.wkt }.Where(v => !String.IsNullOrWhiteSpace(v))
                            select new Property { Name = "Område", Tags = new[] { "@wkt", "@union" }, Value = new[] { WKTProjectToWGS84(wkt, 0) } }
                        ),
                        Source = new[] { metadata.Value<string>("@id") },
                        Modified = kommune.oppdateringsdato
                    }
                );

                AddMap<NaturvernOmrade>(n50kartdata =>
                    from naturvernomrade in n50kartdata.WhereEntityIs<NaturvernOmrade>("N50Kartdata")
                    let metadata = MetadataFor(naturvernomrade)
                    where metadata.Value<string>("@id").StartsWith("N50Kartdata/NaturvernOmrade")
                    select new Resource
                    {
                        ResourceId = "Naturvern/" + naturvernomrade.verneform + "/" + naturvernomrade.navn,
                        Type = new[] { "Naturvernområde" },
                        SubType = new[] { LoadDocument<Verneform>("N50Kartdata/Verneform/" + naturvernomrade.verneform).description }.Where(s => !String.IsNullOrWhiteSpace(s)),
                        Title = new[] { naturvernomrade.navn },
                        Code = new string[] { },
                        Status = new string[] { },
                        Properties =
                            from wkt in new[] { naturvernomrade._omrade.wkt }.Where(v => !String.IsNullOrWhiteSpace(v))
                            select new Property { Name = "Område", Tags = new[] { "@wkt" }, Value = new[] { WKTProjectToWGS84(wkt, 0) } },
                        Source = new[] { metadata.Value<string>("@id") },
                        Modified = naturvernomrade.oppdateringsdato
                    }
                );

                AddMap<Sti>(n50kartdata =>
                    from sti in n50kartdata.WhereEntityIs<Sti>("N50Kartdata")
                    let metadata = MetadataFor(sti)
                    where metadata.Value<string>("@id").StartsWith("N50Kartdata/Sti")
                    select new Resource
                    {
                        ResourceId = "Sti/" + sti.objid,
                        Type = new[] { "Sti" },
                        SubType = new string[] { sti.vedlikeholdsansvarlig }.Where(t => !String.IsNullOrWhiteSpace(t)),
                        Title = new string[] { },
                        Code = new string[] { },
                        Status = new string[] { (sti.merking == "JA") ? "Merket" : "" }.Where(t => !String.IsNullOrWhiteSpace(t)),
                        Properties =
                            from wkt in new[] { sti._senterlinje.wkt }.Where(v => !String.IsNullOrWhiteSpace(v))
                            select new Property { Name = "Senterlinje", Tags = new[] { "@wkt" }, Value = new[] { WKTProjectToWGS84(wkt, 0) } },
                        Source = new[] { metadata.Value<string>("@id") },
                        Modified = sti.oppdateringsdato
                    }
                );

                Reduce = results  =>
                    from result in results
                    group result by result.ResourceId into g
                    select new Resource
                    {
                        ResourceId = g.Key,
                        Type = g.SelectMany(r => r.Type).Distinct(),
                        SubType = g.SelectMany(r => r.SubType).Distinct(),
                        Title = g.SelectMany(r => r.Title).Distinct(),
                        Code = g.SelectMany(r => r.Code).Distinct(),
                        Status = g.SelectMany(r => r.Status).Distinct(),
                        Properties = (IEnumerable<Property>)Properties(g.SelectMany(r => r.Properties)),
                        Source = g.SelectMany(resource => resource.Source).Distinct(),
                        Modified = g.Select(resource => resource.Modified).Max()
                    };

                Index(Raven.Client.Constants.Documents.Indexing.Fields.AllFields, FieldIndexing.No);

                OutputReduceToCollection = "N50KartdataResource";

                AdditionalAssemblies = new HashSet<AdditionalAssembly> {
                    AdditionalAssembly.FromPath("Digitalisert.Dataplattform.ResourceModel.dll", new HashSet<string> { "Digitalisert.Dataplattform" })
                };
            }
        }
    }
}