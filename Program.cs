﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Raven.Client.Documents;
using Raven.Client.Documents.BulkInsert;
using Raven.Client.Documents.Operations.Indexes;
using Raven.Client.Json;

namespace n50kartdata_etl
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var store = new DocumentStore { Urls = new string[] { "http://localhost:8080" }, Database = "Digitalisert" })
            {
                store.Conventions.FindCollectionName = t => t.Name;
                store.Initialize();

                var stopwatch = Stopwatch.StartNew();

                if (store.Maintenance.Send(new GetIndexOperation("N50KartdataResourceIndex")) == null)
                {
                    new N50KartdataResourceModel.N50KartdataResourceIndex().Execute(store);
                }

                using (BulkInsertOperation bulkInsert = store.BulkInsert())
                {
                    using (var context = new N50KartdataContext())
                    {
                        foreach(var kommune in context.Kommuner.AsNoTracking())
                        {
                            bulkInsert.Store(
                                kommune,
                                "N50Kartdata/Kommune/" + kommune.objid,
                                new MetadataAsDictionary(new Dictionary<string, object> { { "@collection", "N50Kartdata"}})
                            );
                        }
                    }
                }

                stopwatch.Stop();
                Console.WriteLine(stopwatch.Elapsed);
            }
        }
    }
}
