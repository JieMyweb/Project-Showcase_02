using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;
using System.Collections.Generic;

public class CustomDocumentFilter : IDocumentFilter
{
    private readonly Dictionary<string, string> _controllerNameMappings = new Dictionary<string, string>
    {
        //定義控制器名稱
        { "Spot", "Opnelayer API" },
        { "UploadShpOrKml", "上傳Shp或Kml" },
        { "ShpDownload", "下載Shp" },
        { "KmlDownload", "下載Kml" },
        { "FileList", "列出目前資料庫中所有Type" },
    };

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        foreach (var path in swaggerDoc.Paths)
        {
            foreach (var operation in path.Value.Operations)
            {
                foreach (var tag in operation.Value.Tags)
                {
                    if (_controllerNameMappings.ContainsKey(tag.Name))
                    {
                        tag.Name = _controllerNameMappings[tag.Name];
                    }
                }
            }
        }
    }
}
