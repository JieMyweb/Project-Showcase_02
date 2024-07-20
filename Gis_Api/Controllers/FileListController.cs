using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Gis_Api.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace Gis_Api.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class FileListController : ControllerBase
    {
        private readonly GISSHP2Context _context;

        public FileListController(GISSHP2Context context)
        {
            _context = context;
        }

        #region 列出目前資料庫中所有Type
        /// <summary>
        /// 列出目前資料庫中所有Type
        /// </summary>
        /// <remarks>列出目前資料庫中所有Type</remarks>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<string>>> GetTypeList()
        {
            var TypeList = await _context.FileUpload
                .Select(f => f.Type)
                .Distinct()
                .ToListAsync();

            return Ok(TypeList);
        }
        #endregion
    }
}
