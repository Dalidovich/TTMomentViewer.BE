using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using TTMomentViewer.BLL.DTOs;
using TTMomentViewer.BLL.Interfaces;

namespace TTMomentViewer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LibraryController : ControllerBase
{
    private readonly ILibraryService _libraryService;

    public LibraryController(ILibraryService libraryService)
    {
        _libraryService = libraryService;
    }

    [HttpGet("stats")]
    public ActionResult<LibraryStatsDto> GetStats() => _libraryService.GetStats();

    [HttpGet("export")]
    public async Task<IActionResult> Export(CancellationToken cancellationToken)
    {
        var stats = _libraryService.GetStats();
        if (stats.MomentCount == 0) return NotFound();

        var bodyControl = HttpContext.Features.Get<IHttpBodyControlFeature>();
        if (bodyControl is not null) bodyControl.AllowSynchronousIO = true;

        Response.ContentType = "application/zip";
        Response.Headers.ContentDisposition = $"attachment; filename=\"{_libraryService.BuildArchiveFileName()}\"";

        await _libraryService.WriteArchiveAsync(Response.Body, cancellationToken);

        return new EmptyResult();
    }
}
