using TTMomentViewer.BLL.DTOs;
using TTMomentViewer.BLL.Interfaces;

namespace TTMomentViewer.BLL.Services;

public class MomentService : IMomentService
{
    private readonly ILibraryIndex _index;

    public MomentService(ILibraryIndex index)
    {
        _index = index;
    }

    public MomentDto? GetMoment(string momentId)
    {
        var moment = _index.GetMoment(momentId);
        return moment is null ? null : MomentDto.FromEntity(moment);
    }

    public PagedResult<MomentDto>? GetFolderMoments(string folderId, int page, int pageSize)
    {
        var folder = _index.GetFolder(folderId);
        if (folder is null) return null;

        var moments = folder.Moments;

        return new PagedResult<MomentDto>
        {
            Items = moments.Skip((page - 1) * pageSize).Take(pageSize).Select(MomentDto.FromEntity).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = moments.Count
        };
    }

    public string? ResolveFilePath(string momentId)
    {
        var moment = _index.GetMoment(momentId);
        if (moment is null) return null;

        var filePath = Path.Combine(_index.RootPath, moment.RelativePath);
        return File.Exists(filePath) ? filePath : null;
    }
}
