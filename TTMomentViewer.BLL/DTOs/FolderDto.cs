using TTMomentViewer.Domain.Entities;

namespace TTMomentViewer.BLL.DTOs;

public class FolderDto
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int MomentCount { get; set; }

    public string? CoverMomentId { get; set; }

    public static FolderDto FromEntity(LibraryFolder folder) => new()
    {
        Id = folder.Id,
        Name = folder.Name,
        MomentCount = folder.Moments.Count,
        CoverMomentId = folder.Moments.Count > 0 ? folder.Moments[0].Id : null
    };
}
