using TTMomentViewer.Domain.Entities;

namespace TTMomentViewer.BLL.DTOs;

public class MomentDto
{
    public string Id { get; set; } = string.Empty;

    public string FolderId { get; set; } = string.Empty;

    public string FolderName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int Index { get; set; }

    public static MomentDto FromEntity(Moment moment) => new()
    {
        Id = moment.Id,
        FolderId = moment.FolderId,
        FolderName = moment.FolderName,
        Name = moment.Name,
        Index = moment.Index
    };
}
