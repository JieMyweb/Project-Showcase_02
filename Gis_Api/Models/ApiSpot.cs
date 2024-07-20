using NetTopologySuite.Geometries;

namespace Gis_Api.Models
{
    public class ApiSpot
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; }
        public string Tel { get; set; }
        public string Address { get; set; }
        public double px { get; set; }
        public double py { get; set; }
    }
}
