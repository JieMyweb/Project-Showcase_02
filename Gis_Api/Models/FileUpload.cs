using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Gis_Api.Models;

public partial class FileUpload
{
    public string Id { get; set; }

    public string Name { get; set; }

    public string Type { get; set; }

    public string GeoJson { get; set; }

    public Geometry Geo { get; set; }

    public string Wkt { get; set; }
}
