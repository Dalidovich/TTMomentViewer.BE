using TTMomentViewer.Domain.Entities;

namespace TTMomentViewer.BLL.Interfaces;

public interface ILibraryScanner
{
    IReadOnlyList<LibraryFolder> Scan(string rootPath);
}
