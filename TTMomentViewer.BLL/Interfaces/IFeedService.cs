using TTMomentViewer.BLL.DTOs;

namespace TTMomentViewer.BLL.Interfaces;

public interface IFeedService
{
    PagedResult<MomentDto> GetFeed(int seed, int page, int pageSize);
}
