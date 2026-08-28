using TTMomentViewer.BLL.DTOs;

namespace TTMomentViewer.BLL.Interfaces;

public interface ILibraryService
{
    LibraryStatsDto GetStats();

    string BuildArchiveFileName();

    Task WriteArchiveAsync(Stream destination, CancellationToken cancellationToken);
}
