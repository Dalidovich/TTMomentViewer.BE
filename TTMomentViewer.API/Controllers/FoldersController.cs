using Microsoft.AspNetCore.Mvc;
using TTMomentViewer.API.Extensions;
using TTMomentViewer.BLL.DTOs;
using TTMomentViewer.BLL.Interfaces;

namespace TTMomentViewer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FoldersController : ControllerBase
{
    private const int MaxPageSize = 100;

    private readonly IFolderService _folderService;
    private readonly IMomentService _momentService;
    private readonly IThumbnailService _thumbnailService;

    public FoldersController(
        IFolderService folderService,
        IMomentService momentService,
        IThumbnailService thumbnailService)
    {
        _folderService = folderService;
        _momentService = momentService;
        _thumbnailService = thumbnailService;
    }

    [HttpGet]
    public ActionResult<PagedResult<FolderDto>> GetFolders([FromQuery] int page = 1, [FromQuery] int pageSize = 30)
    {
        if (page < 1 || pageSize < 1 || pageSize > MaxPageSize)
            return BadRequest(new { error = "Invalid pagination parameters.", statusCode = StatusCodes.Status400BadRequest });

        return _folderService.GetFolders(page, pageSize);
    }

    [HttpGet("{folderId}")]
    public ActionResult<FolderDto> GetFolder(string folderId)
    {
        var folder = _folderService.GetFolder(folderId);
        if (folder is null) return NotFound();

        return folder;
    }

    [HttpGet("{folderId}/moments")]
    public ActionResult<PagedResult<MomentDto>> GetFolderMoments(
        string folderId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30)
    {
        if (page < 1 || pageSize < 1 || pageSize > MaxPageSize)
            return BadRequest(new { error = "Invalid pagination parameters.", statusCode = StatusCodes.Status400BadRequest });

        var moments = _momentService.GetFolderMoments(folderId, page, pageSize);
        if (moments is null) return NotFound();

        return moments;
    }

    [HttpGet("{folderId}/thumbnail")]
    public async Task<IActionResult> GetFolderThumbnail(string folderId)
    {
        var result = await _thumbnailService.GetFolderThumbnailAsync(folderId);
        if (result is null) return NotFound();

        return this.ThumbnailFile(result);
    }
}
