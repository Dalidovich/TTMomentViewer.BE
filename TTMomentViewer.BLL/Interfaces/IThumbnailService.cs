using TTMomentViewer.BLL.DTOs;

namespace TTMomentViewer.BLL.Interfaces;

public interface IThumbnailService
{
    Task<ThumbnailResult?> GetMomentThumbnailAsync(string momentId);

    Task<ThumbnailResult?> GetFolderThumbnailAsync(string folderId);
}
