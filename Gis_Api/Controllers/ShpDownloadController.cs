using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Gis_Api.Models;
using NetTopologySuite.Features;
using NetTopologySuite.IO.Esri.Shapefiles.Readers;
using System.Text;
using NetTopologySuite.IO;
using NetTopologySuite.IO.KML;
using NetTopologySuite.Geometries;
using System.Xml;
using SharpKml.Dom;
using SharpKml.Engine;
using System.IO.Compression;
using Swashbuckle.AspNetCore.Annotations;

namespace Gis_Api.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ShpDownloadController : ControllerBase
    {
        private readonly GISSHP2Context _context;

        public ShpDownloadController(GISSHP2Context context)
        {
            _context = context;
        }

        #region 下載Shp
        /// <summary>
        /// 下載Shp
        /// </summary>
        /// <param name="匯出的資料Type">欲匯出的資料Type</param>
        /// <returns></returns>
        [HttpPost]
        [SwaggerOperation(Description = "輸入資料類型，並下載Shp檔案")]
        public IActionResult ExportSHP([FromBody] string 匯出的資料Type)
        {
            try
            {
                // 撈取資料庫中的資料並根據 TypeNum 過濾
                var shpData = _context.FileUpload.Where(e => e.Type == 匯出的資料Type).ToList();

                // 如果沒有找到資料，返回 "查無資料!"
                if (!shpData.Any()) return NotFound("查無資料!");

                var features = new List<NetTopologySuite.Features.Feature>();

                foreach (var data in shpData)
                {
                    var geometry = data.Geo;

                    var attributes = new AttributesTable();
                    attributes.Add("Id", data.Id);
                    attributes.Add("Name", data.Name);

                    var feature = new NetTopologySuite.Features.Feature(geometry, attributes);
                    features.Add(feature);
                }

                // 生成 Shapefile 文件並保存在臨時文件夾中
                var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempFolder);

                var shpFilePath = Path.Combine(tempFolder, $"Shp資料_{DateTime.Now:yyyyMMddHHmmss}.shp");
                var shpEncoding = Encoding.UTF8.WebName;
                NetTopologySuite.IO.Esri.Shapefile.WriteAllFeatures(features, shpFilePath, shpEncoding);

                // 壓縮生成的文件到 ZIP
                var zipFilePath = Path.Combine(Path.GetTempPath(), $"Shp資料_{DateTime.Now:yyyyMMddHHmmss}.zip");
                ZipFile.CreateFromDirectory(tempFolder, zipFilePath);

                // 刪除臨時文件夾
                Directory.Delete(tempFolder, true);

                // 返回 ZIP 文件給前端進行下載
                var zipBytes = System.IO.File.ReadAllBytes(zipFilePath);
                System.IO.File.Delete(zipFilePath); // 刪除臨時 ZIP 文件

                return File(zipBytes, "application/zip", $"Shp資料_{DateTime.Now:yyyyMMddHHmmss}.zip");
            }
            catch (Exception)
            {
                return Content("無法解析此檔案為shp");
            }
        }
        #endregion
    }
}

