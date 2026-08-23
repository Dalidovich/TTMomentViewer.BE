using TTMomentViewer.BLL.DTOs;
using TTMomentViewer.BLL.Interfaces;
using TTMomentViewer.Domain.Entities;

namespace TTMomentViewer.BLL.Services;

public class FeedService : IFeedService
{
    private readonly ILibraryIndex _index;

    public FeedService(ILibraryIndex index)
    {
        _index = index;
    }

    public PagedResult<MomentDto> GetFeed(int seed, int page, int pageSize)
    {
        var shuffled = Shuffle(_index.Moments, seed);

        return new PagedResult<MomentDto>
        {
            Items = shuffled.Skip((page - 1) * pageSize).Take(pageSize).Select(MomentDto.FromEntity).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = shuffled.Length
        };
    }

    private static Moment[] Shuffle(IReadOnlyList<Moment> moments, int seed)
    {
        var result = moments.ToArray();
        var random = new Random(seed);

        for (var i = result.Length - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (result[i], result[j]) = (result[j], result[i]);
        }

        return result;
    }
}
