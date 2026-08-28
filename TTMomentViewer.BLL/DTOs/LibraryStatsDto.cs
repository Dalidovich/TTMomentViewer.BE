namespace TTMomentViewer.BLL.DTOs;

public class LibraryStatsDto
{
    public int FolderCount { get; set; }

    public int MomentCount { get; set; }

    public long TotalSizeBytes { get; set; }

    public long EstimatedArchiveSizeBytes { get; set; }
}
