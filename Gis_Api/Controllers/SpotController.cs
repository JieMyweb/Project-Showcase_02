using Gis_Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Esri;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

namespace Gis_Api.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class SpotController : ControllerBase
    {
        private readonly GISSHP2Context _db;

        public SpotController(GISSHP2Context db)
        {
            _db = db;
        }
        #region 輸入景點id
        /// <summary>
        /// 輸入景點id
        /// </summary>
        /// <param name="輸入景點id">ex:C1_000240531A_000003</param>
        /// <returns></returns>
        [HttpPost("{輸入景點id}")]
        public IActionResult GetById(string 輸入景點id)
        {
            var scenicSpot = _db.Spot.FirstOrDefault(s => s.Id == 輸入景點id);

            if (scenicSpot == null)
            {
                return NotFound();
            }
            //ScenicSpotInfo
            var scenicSpotInfo = new Spot
            {
                Id = scenicSpot.Id,
                Name = scenicSpot.Name,
                Tel = (scenicSpot.Tel ?? "").Trim(),
                Address = (scenicSpot.Address ?? "").Trim(),
                geom = scenicSpot.geom
            };

            return Ok(scenicSpotInfo);
        }
        #endregion

        #region 查詢特定縣市景點
        /// <summary>
        /// 查詢特定縣市景點
        /// </summary>
        /// <param name="縣市代號">ex:A</param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException"></exception>
        [HttpPost("{縣市代號}")]
        [SwaggerOperation(Description = "輸入縣市代號，並查詢特定縣市景點")]
        public IActionResult ScenicSpotInCounty(string 縣市代號)
        {
            // 建立一個空的 Geometry
            Geometry countyGeometry = null;

            // 讀取 Shapefile
            foreach (var feature in Shapefile.ReadAllFeatures(@"C:\Users\kim123\Desktop\Gis_project\gis_shp_資料\new_COUNTY_MOI_1090820.shp"))
            {
                // 如果 COUNTYID 的欄位等於傳入的縣市代碼則記錄下這筆 Geometry
                if (feature.Attributes["COUNTYID"].ToString() == 縣市代號)
                {
                    countyGeometry = feature.Geometry;
                    break;
                }
            }

            // 處理找不到縣市代碼的情況
            if (countyGeometry == null) throw new KeyNotFoundException($"找不到代碼為 {縣市代號} 的縣市");

            var data = _db.Spot
                .Where(s => s.geom.Within(countyGeometry))
                //ScenicSpotInfo
                .Select(s => new Spot()
                {
                    Id = s.Id,
                    Name = s.Name,
                    Tel = (s.Tel ?? "").Trim(),
                    Address = (s.Address ?? "").Trim(),
                }).ToList();

            return Ok(data);
        }
        #endregion

        #region 取得所有景點資料
        /// <summary>
        /// 取得所有景點資料
        /// </summary>
        /// <remarks>取出所有景點</remarks>
        /// <returns></returns>
        [HttpGet]
        public IActionResult List()
        {
            var scenicSpots = _db.Spot.ToList();
            //ScenicSpotInfo
            var scenicSpotInfos = scenicSpots.Select(s => new Spot()
            {
                Id = s.Id,
                Name = s.Name,
                Tel = (s.Tel ?? "").Trim(),
                Address = (s.Address ?? "").Trim(),
                geom = s.geom,
            }).ToList();
            return Ok(scenicSpotInfos);
        }
        #endregion

        #region 模糊查詢特定景點
        /// <summary>
        /// 模糊查詢特定景點
        /// </summary>
        /// <param name="request">模糊查詢特定景點 ex:景點</param>
        /// <returns></returns>
        [HttpPost]
        [SwaggerOperation(Description = "模糊查詢特定景點")]
        public async Task<IActionResult> SearchSpots([FromBody] SearchBySpot request)
        {
            if (request == null || string.IsNullOrEmpty(request.Name))
            {
                return BadRequest("找不到該筆資料，請重新搜尋");
            }

            var spots = await _db.Spot
                                 .Where(s => s.Name.Contains(request.Name))
                                 .ToListAsync();
            if (spots == null || !spots.Any())
            {
                return NotFound("No spots found");
            }

            return Ok(spots);
        }
        #endregion

        #region 編輯景點
        /// <summary>
        /// 編輯景點
        /// </summary>
        /// <param name="輸入欲編輯景點項目">接收參數參考以下</param>
        /// <remarks>輸入欲編輯景點項目</remarks>
        /// <returns></returns>
        [HttpPost]
        public IActionResult EditById([FromBody] ApiSpot 輸入欲編輯景點項目)
        {
            var spot = _db.Spot.FirstOrDefault(s => s.Id == 輸入欲編輯景點項目.Id);
            if (spot != null)
            {
                spot.Name = 輸入欲編輯景點項目.Name;

                // 將 px 和 py 座標轉換成 Point 物件
                var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
                var point = geometryFactory.CreatePoint(new Coordinate(輸入欲編輯景點項目.px, 輸入欲編輯景點項目.py));

                // 更新 spot 的 geom 屬性
                spot.geom = point;
                _db.SaveChanges();
                return Ok(new { message = "更新成功" });
            }

            return NotFound(new { message = "找不到景點" });
        }
        #endregion

        #region 刪除景點
        /// <summary>
        /// 刪除景點
        /// </summary>
        /// <param name="輸入欲刪除景點id">ex:C1_000240531A_000003</param>
        /// <remarks>輸入欲刪除景點id</remarks>
        /// <returns></returns>
        [HttpPost("{輸入欲刪除景點id}")]
        public async Task<IActionResult> DeleteByid(string 輸入欲刪除景點id)
        {
            var spot = await _db.Spot.FindAsync(輸入欲刪除景點id);
            if (spot == null)
            {
                return NotFound();
            }

            _db.Spot.Remove(spot);
            await _db.SaveChangesAsync();

            return NoContent();
        }
        #endregion

        #region 新增景點
        /// <summary>
        /// 新增景點
        /// </summary>
        /// <param name="輸入欲新增景點json">接收參數參考以下</param>
        /// <remarks>輸入欲新增景點json</remarks>
        /// <returns></returns>
        [HttpPost]
        public IActionResult Add([FromBody] ApiSpot 輸入欲新增景點json)
        {
            try
            {
                // 將 px 和 py 座標轉換成 Point 物件
                var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
                var point = geometryFactory.CreatePoint(new Coordinate(輸入欲新增景點json.px, 輸入欲新增景點json.py));

                // 創建新的 Spot 實體
                var spots = new Spot
                {
                    Id = GenerateId(), // 呼叫生成ID的方法
                    Name = 輸入欲新增景點json.Name,
                    Tel = 輸入欲新增景點json.Tel,
                    Address = 輸入欲新增景點json.Address,
                    geom = point,
            };

                // 將實體添加到資料庫上下文中
                _db.Spot.Add(spots);
                _db.SaveChanges(); // 將變更保存到資料庫

                return Ok(spots); // 返回新增的實體
            }
            catch (Exception ex)
            {
                // 捕捉例外並返回錯誤訊息
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }
        private string GenerateId()
        {
            // 獲取當前日期並格式化
            string datePart = DateTime.Now.ToString("yyMMdd"); // 格式範例：240531

            // 查詢今日的資料，並取得最後一筆數據的ID
            var today = DateTime.Today;
            var maxIdToday = _db.Spot
                .Where(s => s.Id.Contains(datePart))
                .OrderByDescending(s => s.Id)
                .FirstOrDefault()?.Id;

            // 生成新的流水號 (若今日無任何資料則初始化為1)
            int newSerialNumber = 1;

            if (maxIdToday != null)
            {
                // 提取最後一筆數據的流水號部分
                var lastSerialNumber = int.Parse(maxIdToday.Split('_').Last());
                // 新增流水號
                newSerialNumber = lastSerialNumber + 1;
            }

            // 組合生成 ID
            string newId = $"C1_000{datePart}A_{newSerialNumber.ToString("D6")}";
            return newId;
        }

        #endregion
    }
}
