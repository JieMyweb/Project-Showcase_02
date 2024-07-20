using NetTopologySuite.Geometries;

namespace Gis_Api.Models
{
    public class ScenicSpotInfo
    {
        /// <summary> 代碼 </summary>
        public string Id { get; set; } = null!;

        /// <summary> 名稱 </summary>
        public string Name { get; set; } = null!;

        /// <summary> 電話 </summary>
        public string Tel { get; set; } = null!;

        /// <summary> 地址 </summary>
        public string Address { get; set; } = null!;

        /// <summary>
        /// 空間欄位
        /// </summary>
        public Geometry geom { get; set; } = null!;
    }
}
