using TTMomentViewer.BLL.DTOs;

namespace TTMomentViewer.BLL.Interfaces;

public interface IMomentService
{
    MomentDto? GetMoment(string momentId);

    PagedResult<MomentDto>? GetFolderMoments(string folderId, int page, int pageSize);

    string? ResolveFilePath(string momentId);
}
