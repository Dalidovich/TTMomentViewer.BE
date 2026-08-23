using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using TTMomentViewer.BLL.DTOs;
using TTMomentViewer.BLL.Interfaces;
using TTMomentViewer.Domain.Entities;

namespace TTMomentViewer.BLL.Services;

public class ThumbnailService : IThumbnailService
{
    private const int MaxCacheSize = 100;

    private static readonly ConcurrentDictionary<string, byte[]> Cache = new();

    private readonly ILibraryIndex _index;
    private readonly IVideoProcessingService _videoProcessing;
    private readonly ILogger<ThumbnailService> _logger;

    public ThumbnailService(
        ILibraryIndex index,
        IVideoProcessingService videoProcessing,
        ILogger<ThumbnailService> logger)
    {
        _index = index;
        _videoProcessing = videoProcessing;
        _logger = logger;
    }

    public Task<ThumbnailResult?> GetMomentThumbnailAsync(string momentId) =>
        GetThumbnailAsync(_index.GetMoment(momentId));

    public Task<ThumbnailResult?> GetFolderThumbnailAsync(string folderId)
    {
        var folder = _index.GetFolder(folderId);
        return GetThumbnailAsync(folder?.Moments.FirstOrDefault());
    }

    private async Task<ThumbnailResult?> GetThumbnailAsync(Moment? moment)
    {
        if (moment is null) return null;

        var videoPath = Path.Combine(_index.RootPath, moment.RelativePath);
        if (!File.Exists(videoPath)) return null;

        var lastModified = File.GetLastWriteTimeUtc(videoPath);

        if (Cache.TryGetValue(moment.Id, out var cached))
            return new ThumbnailResult(cached, lastModified);

        var data = await Task.Run(() => _videoProcessing.ExtractFrame(videoPath));
        if (data is null)
        {
            _logger.LogError("Thumbnail could not be extracted for {RelativePath}", moment.RelativePath);
            return null;
        }

        EvictIfNeeded();
        Cache[moment.Id] = data;

        return new ThumbnailResult(data, lastModified);
    }

    private static void EvictIfNeeded()
    {
        if (Cache.Count < MaxCacheSize) return;

        var key = Cache.Keys.FirstOrDefault();
        if (key is not null) Cache.TryRemove(key, out _);
    }
}
