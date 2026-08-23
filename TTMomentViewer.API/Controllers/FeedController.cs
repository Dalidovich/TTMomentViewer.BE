using Microsoft.AspNetCore.Mvc;
using TTMomentViewer.BLL.DTOs;
using TTMomentViewer.BLL.Interfaces;

namespace TTMomentViewer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FeedController : ControllerBase
{
    private const int MaxPageSize = 100;

    private readonly IFeedService _feedService;

    public FeedController(IFeedService feedService)
    {
        _feedService = feedService;
    }

    [HttpGet]
    public ActionResult<PagedResult<MomentDto>> GetFeed(
        [FromQuery] int seed = 0,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1 || pageSize < 1 || pageSize > MaxPageSize)
            return BadRequest(new { error = "Invalid pagination parameters.", statusCode = StatusCodes.Status400BadRequest });

        return _feedService.GetFeed(seed, page, pageSize);
    }
}
