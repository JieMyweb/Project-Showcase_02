using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Gis_Api.Models;
using NetTopologySuite.IO.Esri.Shapefiles.Readers;
using NetTopologySuite.IO;
using SharpKml.Dom;
using SharpKml.Engine;
using Swashbuckle.AspNetCore.Annotations;

namespace Gis_Api.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class UploadShpOrKmlController : ControllerBase
    {
        private readonly GISSHP2Context _context;

        public UploadShpOrKmlController(GISSHP2Context context)
        {
            _context = context;
        }

        #region Shp、Kml上傳
        /// <summary>
        /// 上傳 Shp 和 Kml 檔案
        /// </summary>
        /// <param name="上傳需要匯入的Shp與Kml"></param>
        /// <param name="DataType"></param>
        /// <returns></returns>
        [HttpPost]
        [SwaggerOperation(Summary = "上傳 Shp 和 Kml 檔案", Description = "1.上傳 Shp 和 Kml 檔案<br>!注意!最多只能上傳五個檔案<br>如.dbf .prj .shp .shx .kml<br>2.定義資料類型")]
        public async Task<IActionResult> UploadFiles(
            [SwaggerSchema(Description = "上傳 Shp 和 Kml 檔案<br>!注意!最多只能上傳五個檔案<br>如.dbf .prj .shp .shx .kml")] IFormFileCollection 上傳需要匯入的Shp與Kml,
            [SwaggerSchema(Description = "資料類型<br>例如：觀光景點")][FromForm] string DataType)
        {
            // 檢查資料庫中是否有相同名稱的資料
            bool isDataTypeExists = await _context.FileUpload.AnyAsync(e => e.Type == DataType);
            if (isDataTypeExists)
            {
                return BadRequest("資料名稱相同請重新輸入");
            }
            if (上傳需要匯入的Shp與Kml.Count > 5)
            {
                return BadRequest("最多只能上傳五個檔案");
            }

            var shpFiles = new List<IFormFile>();
            IFormFile kmlFile = null;

            foreach (var file in 上傳需要匯入的Shp與Kml)
            {
                var extension = Path.GetExtension(file.FileName).ToLower();
                switch (extension)
                {
                    case ".shp":
                    case ".shx":
                    case ".dbf":
                    case ".prj":
                        shpFiles.Add(file);
                        break;
                    case ".kml":
                        kmlFile = file;
                        break;
                    default:
                        return BadRequest("請上傳正確文件");
                }
            }

            if (shpFiles.Count == 4 && kmlFile != null)
            {
                var formFileCollection = new FormFileCollection();
                foreach (var shpFile in shpFiles)
                {
                    formFileCollection.Add(shpFile);
                }
                var uploadShpResult = await UploadShp(formFileCollection, DataType);
                var uploadKmlResult = await UploadKml(kmlFile, DataType);

                if (uploadShpResult is BadRequestObjectResult || uploadKmlResult is BadRequestObjectResult)
                {
                    return BadRequest("上傳失敗");
                }

                return Ok("上傳成功");
            }
            else if (shpFiles.Count == 4)
            {
                var formFileCollection = new FormFileCollection();
                foreach (var shpFile in shpFiles)
                {
                    formFileCollection.Add(shpFile);
                }
                return await UploadShp(formFileCollection, DataType);
            }
            else if (kmlFile != null)
            {
                return await UploadKml(kmlFile, DataType);
            }
            else
            {
                return BadRequest("請上傳正確文件");
            }
        }

        #endregion

        #region 匯入Shp資料
        private async Task<IActionResult> UploadShp(IFormFileCollection shp四種類型的檔案, [FromForm] string DataType)
        {
            // 指定暫存路徑
            var tempPath = "D:\\";
            if (!Directory.Exists(tempPath))
            {
                Directory.CreateDirectory(tempPath);
            }

            var savedFilePaths = new List<string>();

            try
            {
                foreach (var file in shp四種類型的檔案)
                {
                    var filePath = Path.Combine(tempPath, file.FileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                    savedFilePaths.Add(filePath);
                }

                // 使用NetTopologySuite.IO.Esri.Shapefile套件解析上傳的檔案
                var shpFilePath = savedFilePaths.First(f => Path.GetExtension(f).ToLower() == ".shp");
                var options = new ShapefileReaderOptions();
                var features = NetTopologySuite.IO.Esri.Shapefile.ReadAllFeatures(shpFilePath, options);
                var geoJsonWriter = new GeoJsonWriter();

                foreach (var feature in features)
                {
                    var featureId = feature.Attributes["Id"].ToString();

                    // 檢查資料庫中是否已存在相同ID的資料
                    var IdError = await _context.FileUpload
                        .FirstOrDefaultAsync(f => f.Id == featureId);

                    if (IdError != null)
                    {
                        // 如果有重複的ID，則生成新的GUID
                        featureId = Guid.NewGuid().ToString();
                    }

                    var FileUpload = new FileUpload
                    {
                        Id = featureId,
                        Name = feature.Attributes["Name"].ToString(),
                        Type = DataType, // 使用者輸入的名稱
                        GeoJson = geoJsonWriter.Write(feature.Geometry),
                        Geo = feature.Geometry,
                        Wkt = feature.Geometry.ToString()
                    };

                    _context.FileUpload.Add(FileUpload);
                }

                await _context.SaveChangesAsync();
            }
            finally
            {
                // 刪除暫存檔案
                foreach (var filePath in savedFilePaths)
                {
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
            }

            return Ok("上傳成功");
        }
        #endregion

        #region 匯入KML資料
        private async Task<IActionResult> UploadKml(IFormFile kmlFile, [FromForm] string DataType)
        {
            using (var stream = new MemoryStream())
            {
                await kmlFile.CopyToAsync(stream);
                stream.Position = 0;

                KmlFile kml = KmlFile.Load(stream);
                var placemarks = kml.Root.Flatten().OfType<Placemark>().ToList();

                foreach (var placemark in placemarks)
                {
                    NetTopologySuite.Geometries.Geometry geometry = null;
                    if (placemark.Geometry != null)
                    {
                        geometry = ConvertToGeometry(placemark.Geometry);
                    }

                    if (geometry == null)
                    {
                        continue; // 跳過無法轉換的幾何
                    }

                    FileUpload FileUpload = new FileUpload
                    {
                        Id = string.IsNullOrEmpty(placemark.Id) ? Guid.NewGuid().ToString() : placemark.Id,
                        Name = placemark.Name,
                        Type = DataType,
                        GeoJson = ConvertToGeoJson(geometry),
                        Geo = geometry,
                        Wkt = ConvertToWkt(geometry)
                    };

                    _context.FileUpload.Add(FileUpload);
                }

                await _context.SaveChangesAsync();
            }

            return Ok("上傳成功");
        }
        private string ConvertToGeoJson(NetTopologySuite.Geometries.Geometry geometry)
        {
            var geoJsonWriter = new GeoJsonWriter();
            return geoJsonWriter.Write(geometry);
        }

        private string ConvertToWkt(NetTopologySuite.Geometries.Geometry geometry)
        {
            var wktWriter = new WKTWriter();
            return wktWriter.Write(geometry);
        }
        private NetTopologySuite.Geometries.Geometry ConvertToGeometry(SharpKml.Dom.Geometry kmlGeometry)
        {
            if (kmlGeometry is SharpKml.Dom.Point point)
            {
                return new NetTopologySuite.Geometries.Point(point.Coordinate.Longitude, point.Coordinate.Latitude);
            }
            // 其他幾何類型的轉換邏輯
            // 例如處理 LineString, Polygon 等
            if (kmlGeometry is SharpKml.Dom.LineString lineString)
            {
                var coordinates = lineString.Coordinates.Select(c => new NetTopologySuite.Geometries.Coordinate(c.Longitude, c.Latitude)).ToArray();
                return new NetTopologySuite.Geometries.LineString(coordinates);
            }

            if (kmlGeometry is SharpKml.Dom.Polygon polygon)
            {
                var exteriorRing = new NetTopologySuite.Geometries.LinearRing(
                    polygon.OuterBoundary.LinearRing.Coordinates.Select(c => new NetTopologySuite.Geometries.Coordinate(c.Longitude, c.Latitude)).ToArray());
                var interiorRings = polygon.InnerBoundary.Select(b => new NetTopologySuite.Geometries.LinearRing(
                    b.LinearRing.Coordinates.Select(c => new NetTopologySuite.Geometries.Coordinate(c.Longitude, c.Latitude)).ToArray())).ToArray();
                return new NetTopologySuite.Geometries.Polygon(exteriorRing, interiorRings);
            }

            // 其他幾何類型的轉換邏輯
            return null;
        }

        #endregion

    }
}
