namespace TTMomentViewer.Domain.Entities;

public class Moment
{
    public string Id { get; set; } = string.Empty;

    public string FolderId { get; set; } = string.Empty;

    public string FolderName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string RelativePath { get; set; } = string.Empty;

    public int Index { get; set; }

    public long SizeBytes { get; set; }
}
