using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Logging;
using TTMomentViewer.BLL.DTOs;
using TTMomentViewer.BLL.Interfaces;

namespace TTMomentViewer.BLL.Services;

public class LibraryService : ILibraryService
{
    private const int ZipEntryOverheadBytes = 92;
    private const int ZipEndOfDirectoryBytes = 22;

    private readonly ILibraryIndex _index;
    private readonly ILogger<LibraryService> _logger;

    public LibraryService(ILibraryIndex index, ILogger<LibraryService> logger)
    {
        _index = index;
        _logger = logger;
    }

    public LibraryStatsDto GetStats()
    {
        var moments = _index.Moments;

        var totalSizeBytes = 0L;
        var entryOverheadBytes = 0L;

        foreach (var moment in moments)
        {
            totalSizeBytes += moment.SizeBytes;
            entryOverheadBytes += ZipEntryOverheadBytes + Encoding.UTF8.GetByteCount(moment.RelativePath) * 2;
        }

        return new LibraryStatsDto
        {
            FolderCount = _index.Folders.Count,
            MomentCount = moments.Count,
            TotalSizeBytes = totalSizeBytes,
            EstimatedArchiveSizeBytes = moments.Count == 0
                ? 0
                : totalSizeBytes + entryOverheadBytes + ZipEndOfDirectoryBytes
        };
    }

    public string BuildArchiveFileName() =>
        $"ttmomentviewer-library-{DateTime.Now:yyyyMMdd-HHmmss}.zip";

    public async Task WriteArchiveAsync(Stream destination, CancellationToken cancellationToken)
    {
        var rootPath = _index.RootPath;

        using var archive = new ZipArchive(destination, ZipArchiveMode.Create, leaveOpen: true);

        foreach (var moment in _index.Moments)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var filePath = Path.Combine(rootPath, moment.RelativePath);

            if (!File.Exists(filePath))
            {
                _logger.LogWarning("File skipped during export, it no longer exists: {FilePath}", filePath);
                continue;
            }

            var entry = archive.CreateEntry(moment.RelativePath, CompressionLevel.NoCompression);
            entry.LastWriteTime = File.GetLastWriteTime(filePath);

            await using var source = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 81920,
                useAsync: true);

            await using var entryStream = entry.Open();

            await source.CopyToAsync(entryStream, cancellationToken);
        }
    }
}
