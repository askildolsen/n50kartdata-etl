using System;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using Npgsql;
using NetTopologySuite.Geometries;

namespace n50kartdata_etl
{
    public class N50KartdataContext : DbContext
    {
        public DbSet<Kommune> Kommuner { get; set; }
        public DbSet<NaturvernOmrade> NaturvernOmrader { get; set; }
        public DbSet<Verneform> Verneformer { get; set; }
        public DbSet<Sti> Stier { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(@"Host=localhost;Port=5433;Database=n50kartdata;Username=n50kartdata;Password=n50kartdata", x => x.UseNetTopologySuite());
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .Entity<Sti>()
                .HasNoKey()
                .ToQuery(() => Stier.FromSqlRaw("SELECT *, ST_CurveToLine(senterlinje) AS senterlinje_Line FROM sti"));
        }
    }

    [Table("kommune")]
    public class Kommune
    {
        [Key]
        public int objid { get; set; }
        public string objtype { get; set; }
        public string navn { get; set; }
        public DateTime oppdateringsdato { get; set; }
        public string kommunenummer { get; set; }
        [JsonIgnore]
        public Geometry omrade { get; set; }
        [NotMapped]
        [JsonProperty(PropertyName = "omrade")]
        public Geometri _omrade =>
            new Geometri {
                srid = omrade.SRID,
                geometritype = omrade.GeometryType,
                wkt = omrade.ToString()
            };
    }

    [Table("naturvernomrade")]
    public class NaturvernOmrade
    {
        [Key]
        public int objid { get; set; }
        public string objtype { get; set; }
        public DateTime vernedato { get; set; }
        public string navn { get; set; }
        public DateTime oppdateringsdato { get; set; }
        public string verneform { get; set; }
        [JsonIgnore]
        public Geometry omrade { get; set; }
        [NotMapped]
        [JsonProperty(PropertyName = "omrade")]
        public Geometri _omrade =>
            new Geometri {
                srid = omrade.SRID,
                geometritype = omrade.GeometryType,
                wkt = omrade.ToString()
            };
    }

    [Table("verneform")]
    public class Verneform
    {
        [Key]
        public string identifier { get; set; }
        public string description { get; set; }
    }

    public class Sti
    {
        public int objid { get; set; }
        public string objtype { get; set; }
        public DateTime datafangstdato { get; set; }
        public DateTime oppdateringsdato { get; set; }
        public string merking { get; set; }
        public string vedlikeholdsansvarlig { get; set; }
        public string medium { get; set; }
        public string malemetode { get; set; }
        public string noyaktighet { get; set; }
        [JsonIgnore]
        public Geometry senterlinje_Line { get; set; }
        [NotMapped]
        [JsonProperty(PropertyName = "senterlinje")]
        public Geometri _senterlinje =>
            new Geometri {
                srid = senterlinje_Line.SRID,
                geometritype = senterlinje_Line.GeometryType,
                wkt = senterlinje_Line.ToString()
            };
    }

    public class Geometri
    {
        public int srid { get; set; }
        public string geometritype { get; set; }
        public string wkt { get; set; }
    }
}
