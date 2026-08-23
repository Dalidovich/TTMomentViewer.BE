using TTMomentViewer.BLL.DTOs;

namespace TTMomentViewer.BLL.Interfaces;

public interface IFolderService
{
    PagedResult<FolderDto> GetFolders(int page, int pageSize);

    FolderDto? GetFolder(string folderId);
}
