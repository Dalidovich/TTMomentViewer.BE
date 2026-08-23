using Microsoft.AspNetCore.Mvc;
using TTMomentViewer.BLL.DTOs;

namespace TTMomentViewer.API.Extensions;

public static class ThumbnailResponseExtensions
{
    private const string CacheControl = "public, max-age=86400";

    public static IActionResult ThumbnailFile(this ControllerBase controller, ThumbnailResult result)
    {
        var etag = $"\"{result.LastModified.Ticks:x}\"";

        controller.Response.Headers.CacheControl = CacheControl;

        if (controller.Request.Headers.IfNoneMatch.ToString() == etag)
            return controller.StatusCode(StatusCodes.Status304NotModified);

        controller.Response.Headers.ETag = etag;
        controller.Response.Headers.LastModified = result.LastModified.ToString("R");

        return controller.File(result.Data, "image/jpeg");
    }
}
