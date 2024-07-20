using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Gis_Api.Models;

public partial class Spot
{
    public string Id { get; set; }

    public string Name { get; set; }

    public string Tel { get; set; }

    public string Address { get; set; }

    public string County { get; set; }

    public string Town { get; set; }

    public Geometry geom { get; set; }
}
