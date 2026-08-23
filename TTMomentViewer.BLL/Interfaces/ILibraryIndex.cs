using TTMomentViewer.Domain.Entities;

namespace TTMomentViewer.BLL.Interfaces;

public interface ILibraryIndex
{
    string RootPath { get; }

    IReadOnlyList<LibraryFolder> Folders { get; }

    IReadOnlyList<Moment> Moments { get; }

    void Load(string rootPath, IReadOnlyList<LibraryFolder> folders);

    LibraryFolder? GetFolder(string folderId);

    Moment? GetMoment(string momentId);
}
