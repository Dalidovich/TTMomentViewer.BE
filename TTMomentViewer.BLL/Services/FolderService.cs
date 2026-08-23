using TTMomentViewer.BLL.DTOs;
using TTMomentViewer.BLL.Interfaces;

namespace TTMomentViewer.BLL.Services;

public class FolderService : IFolderService
{
    private readonly ILibraryIndex _index;

    public FolderService(ILibraryIndex index)
    {
        _index = index;
    }

    public PagedResult<FolderDto> GetFolders(int page, int pageSize)
    {
        var folders = _index.Folders;

        return new PagedResult<FolderDto>
        {
            Items = folders.Skip((page - 1) * pageSize).Take(pageSize).Select(FolderDto.FromEntity).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = folders.Count
        };
    }

    public FolderDto? GetFolder(string folderId)
    {
        var folder = _index.GetFolder(folderId);
        return folder is null ? null : FolderDto.FromEntity(folder);
    }
}
