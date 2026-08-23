using Microsoft.Extensions.Logging;
using NReco.VideoConverter;
using TTMomentViewer.BLL.Interfaces;

namespace TTMomentViewer.BLL.Services;

public class VideoProcessingService : IVideoProcessingService
{
    private const float ThumbnailFrameTime = 1f;

    private static string? _ffmpegExePath;

    private readonly ILogger<VideoProcessingService> _logger;

    public VideoProcessingService(ILogger<VideoProcessingService> logger)
    {
        _logger = logger;
    }

    public byte[]? ExtractFrame(string videoPath)
    {
        try
        {
            var ffmpeg = new FFMpegConverter
            {
                FFMpegToolPath = Path.GetDirectoryName(GetFfmpegExePath())!
            };

            using var stream = new MemoryStream();

            _logger.LogInformation("Generating thumbnail for {VideoPath} at {Seek}s", videoPath, ThumbnailFrameTime);

            ffmpeg.GetVideoThumbnail(videoPath, stream, ThumbnailFrameTime);

            return stream.Length > 0 ? stream.ToArray() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate thumbnail for {VideoPath}", videoPath);
            return null;
        }
    }

    private static string GetFfmpegExePath()
    {
        if (_ffmpegExePath is not null) return _ffmpegExePath;

        var candidates = new[]
        {
            AppContext.BaseDirectory,
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TTMomentViewer", "ffmpeg")
        };

        foreach (var toolDir in candidates)
        {
            try
            {
                Directory.CreateDirectory(toolDir);

                var converter = new FFMpegConverter { FFMpegToolPath = toolDir };
                converter.ExtractFFmpeg();

                var exePath = Path.Combine(toolDir, converter.FFMpegExeName);
                if (!File.Exists(exePath)) continue;

                _ffmpegExePath = exePath;
                return exePath;
            }
            catch (Exception)
            {
            }
        }

        throw new InvalidOperationException(
            @"ffmpeg could not be extracted (tried exe directory and %LOCALAPPDATA%\TTMomentViewer\ffmpeg)");
    }
}
