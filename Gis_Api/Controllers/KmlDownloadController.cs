using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Gis_Api.Models;
using NetTopologySuite.IO;
using SharpKml.Dom;
using SharpKml.Engine;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json.Linq;
using System.Text;
using SharpKml.Base;
using Swashbuckle.AspNetCore.Annotations;
using System.Net.Http.Headers;

namespace Gis_Api.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class KmlDownloadController : ControllerBase
    {
        private readonly GISSHP2Context _context;

        public KmlDownloadController(GISSHP2Context context)
        {
            _context = context;
        }

        #region 下載Kml
        /// <summary>
        /// 下載Kml
        /// </summary>
        /// <param name="type">欲匯出的資料Type</param>
        /// <returns></returns>
        [HttpPost]
        [SwaggerOperation(Description = "輸入資料類型，並下載Kml檔案")]
        public IActionResult ExportKml([FromBody] string type)
        {
            try
            {
                // 根據 type 參數從資料庫撈取資料
                var kmlUploads = _context.FileUpload
                    .Where(k => k.Type == type)
                    .ToList();

                if (kmlUploads == null || !kmlUploads.Any())
                {
                    return NotFound();
                }

                var kml = new Kml();
                var document = new Document();

                foreach (var upload in kmlUploads)
                {
                    var placemark = new Placemark
                    {
                        Geometry = ConvertGeoJsonGeometryToKmlGeometry(JObject.Parse(upload.GeoJson)),
                        Name = upload.Name
                    };
                    // 添加自訂資料（Id 和 Name）
                    placemark.ExtendedData = new ExtendedData();
                    placemark.ExtendedData.AddData(new Data
                    {
                        Name = "Id",
                        Value = upload.Id.ToString()
                    });
                    placemark.ExtendedData.AddData(new Data
                    {
                        Name = "Name",
                        Value = upload.Name
                    });

                    document.AddFeature(placemark);
                }

                kml.Feature = document;

                var serializer = new Serializer();
                using (var memoryStream = new MemoryStream())
                {
                    serializer.Serialize(kml, memoryStream);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    using (var streamReader = new StreamReader(memoryStream))
                    {
                        var result = File(Encoding.UTF8.GetBytes(streamReader.ReadToEnd()), "application/vnd.google-earth.kml+xml", "output.kml");

                        // 發送Line Notify通知
                        //await SendLineNotification($"'{type}' 已經成功下載成Kml檔案");

                        return result;
                    }
                }
            }
            catch (Exception)
            {
                // 發Line notify送錯誤通知
                //await SendLineNotification($"Error occurred while exporting KML file for type '{type}': {ex.Message}");
                return Content("無法解析此檔案為kml");
            }
        }

        private Geometry ConvertGeoJsonGeometryToKmlGeometry(JObject geoJsonGeometry)
        {
            var type = geoJsonGeometry["type"].ToString();
            switch (type)
            {
                case "Point":
                    var pointCoordinates = geoJsonGeometry["coordinates"];
                    return new Point
                    {
                        Coordinate = new Vector((double)pointCoordinates[1], (double)pointCoordinates[0])
                    };
                case "LineString":
                    var lineStringCoordinates = geoJsonGeometry["coordinates"]
                        .Select(c => new Vector((double)c[1], (double)c[0]))
                        .ToList();
                    return new LineString
                    {
                        Coordinates = new CoordinateCollection(lineStringCoordinates)
                    };
                // 你可以根據需要添加更多的幾何類型處理邏輯，例如 Polygon
                // case "Polygon":
                //     // 解析 Polygon 資料
                //     break;
                default:
                    throw new NotSupportedException($"不支持的 GeoJSON 幾何類型: {type}");
            }
        }
        #endregion

    }
}
