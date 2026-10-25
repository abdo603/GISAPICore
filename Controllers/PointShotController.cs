// NuGet required:  dotnet add package SkiaSharp
// (on Linux also: SkiaSharp.NativeAssets.Linux.NoDependencies)
using Microsoft.AspNetCore.Mvc;
using SkiaSharp;

namespace GISAPICore.Controllers
{
    [ApiController]
    [Route("api/QueryData")]
    public class PointShotController : ControllerBase
    {
        private readonly IHttpClientFactory _http;

        // Raster export of the Esri street basemap (the VectorTileServer itself only returns .pbf tiles, not images)
        private const string EsriExport =
            "https://services.arcgisonline.com/ArcGIS/rest/services/World_Street_Map/MapServer/export";

        public PointShotController(IHttpClientFactory http) => _http = http;

        // x = latitude, y = longitude (same convention as GetPointShot2GisUrl)
        // GET api/QueryData/GetPointShot?x=24.7976198&y=46.6842152&z=17&w=1000&h=700
        [HttpGet("GetPointShot")]
        public async Task<IActionResult> GetPointShot(
            double x, double y, int z = 17, int w = 1000, int h = 700, bool marker = true)
        {
            if (x is < -85 or > 85 || y is < -180 or > 180)
                return BadRequest(new { status = false, errorCode = 1, error_Code = "Invalid x/y" });
            z = Math.Clamp(z, 1, 19);
            w = Math.Clamp(w, 100, 2000);
            h = Math.Clamp(h, 100, 2000);

            // lat/lon -> Web Mercator (EPSG:3857)
            const double R = 6378137.0;
            double mx = y * Math.PI / 180.0 * R;
            double my = Math.Log(Math.Tan(Math.PI / 4 + x * Math.PI / 360.0)) * R;

            double res = 156543.03392804097 / Math.Pow(2, z); // meters per pixel
            double hw = w / 2.0 * res, hh = h / 2.0 * res;

            var inv = System.Globalization.CultureInfo.InvariantCulture;
            string bbox = string.Join(",",
                (mx - hw).ToString(inv), (my - hh).ToString(inv),
                (mx + hw).ToString(inv), (my + hh).ToString(inv));

            string url = $"{EsriExport}?bbox={bbox}&bboxSR=3857&imageSR=3857&size={w},{h}&format=png32&transparent=false&f=image";

            var client = _http.CreateClient();
            var resp = await client.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
                return StatusCode(502, new { status = false, errorCode = 2, error_Code = "Basemap request failed" });

            byte[] bytes = await resp.Content.ReadAsByteArrayAsync();
            if (!marker) return File(bytes, "image/png");

            // draw a pin at the center
            using var bmp = SKBitmap.Decode(bytes);
            using var canvas = new SKCanvas(bmp);
            float cx = bmp.Width / 2f, cy = bmp.Height / 2f;
            using var fill = new SKPaint { Color = SKColors.Red, IsAntialias = true, Style = SKPaintStyle.Fill };
            using var stroke = new SKPaint { Color = SKColors.White, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 3 };
            canvas.DrawCircle(cx, cy, 10, fill);
            canvas.DrawCircle(cx, cy, 10, stroke);

            using var img = SKImage.FromBitmap(bmp);
            using var data = img.Encode(SKEncodedImageFormat.Png, 100);
            return File(data.ToArray(), "image/png");
        }


        // Returns JSON metadata similar to the requested GetPointShot2GisUrl response.
        // imageUrl points to this API's PNG endpoint, so it works with the current project.
        // GET api/QueryData/GetPointShot2GisUrl?x=24.7976198&y=46.6842152&z=17&w=1000&h=700
        [HttpGet("GetPointShot2GisUrl")]
        public IActionResult GetPointShot2GisUrl(double x, double y, int z = 17, int w = 1000, int h = 700)
        {
            if (x is < -85 or > 85 || y is < -180 or > 180)
                return BadRequest(new { status = false, errorCode = 1, error_Code = "Invalid x/y" });

            z = Math.Clamp(z, 1, 19);
            w = Math.Clamp(w, 100, 2000);
            h = Math.Clamp(h, 100, 2000);
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            string imageUrl = $"{Request.Scheme}://{Request.Host}/api/QueryData/GetPointShot" +
                              $"?x={x.ToString(inv)}&y={y.ToString(inv)}&z={z}&w={w}&h={h}";

            return Ok(new
            {
                status = true,
                errorCode = 0,
                error_Code = (string?)null,
                gis_code = "تم إنشاء رابط الصورة",
                imageUrl,
                urlEsri = "https://webgishosted.momah.gov.sa/server/rest/services/Hosted/BaseMap_FinalCart/VectorTileServer"
            });
        }

        // Same response shape as your existing endpoint: JSON with an imageUrl pointing at the image above
        // GET api/QueryData/GetPointShotUrl?x=...&y=...
        [HttpGet("GetPointShotUrl")]
        public IActionResult GetPointShotUrl(double x, double y, int z = 17, int w = 1000, int h = 700)
        {
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            string imageUrl = $"{Request.Scheme}://{Request.Host}/api/QueryData/GetPointShot" +
                              $"?x={x.ToString(inv)}&y={y.ToString(inv)}&z={z}&w={w}&h={h}";
            return Ok(new
            {
                status = true,
                errorCode = 0,
                error_Code = (string?)null,
                imageUrl
            });
        }
    }
}

// Program.cs:  builder.Services.AddHttpClient();
