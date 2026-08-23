using Microsoft.AspNetCore.Mvc;
using TTMomentViewer.API.Extensions;
using TTMomentViewer.BLL.DTOs;
using TTMomentViewer.BLL.Interfaces;

namespace TTMomentViewer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MomentsController : ControllerBase
{
    private readonly IMomentService _momentService;
    private readonly IThumbnailService _thumbnailService;

    public MomentsController(IMomentService momentService, IThumbnailService thumbnailService)
    {
        _momentService = momentService;
        _thumbnailService = thumbnailService;
    }

    [HttpGet("{momentId}")]
    public ActionResult<MomentDto> GetMoment(string momentId)
    {
        var moment = _momentService.GetMoment(momentId);
        if (moment is null) return NotFound();

        return moment;
    }

    [HttpGet("{momentId}/stream")]
    public IActionResult Stream(string momentId)
    {
        var filePath = _momentService.ResolveFilePath(momentId);
        if (filePath is null) return NotFound();

        return PhysicalFile(filePath, GetContentType(Path.GetExtension(filePath)), enableRangeProcessing: true);
    }

    [HttpGet("{momentId}/thumbnail")]
    public async Task<IActionResult> GetThumbnail(string momentId)
    {
        var result = await _thumbnailService.GetMomentThumbnailAsync(momentId);
        if (result is null) return NotFound();

        return this.ThumbnailFile(result);
    }

    private static string GetContentType(string extension) => extension.ToLowerInvariant() switch
    {
        ".mp4" => "video/mp4",
        ".m4v" => "video/mp4",
        ".webm" => "video/webm",
        ".mov" => "video/quicktime",
        _ => "application/octet-stream"
    };
}
