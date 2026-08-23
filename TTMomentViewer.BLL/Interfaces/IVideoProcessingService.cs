namespace TTMomentViewer.BLL.Interfaces;

public interface IVideoProcessingService
{
    byte[]? ExtractFrame(string videoPath);
}
