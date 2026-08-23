using TTMomentViewer.BLL.Interfaces;
using TTMomentViewer.Domain.Entities;

namespace TTMomentViewer.BLL.Services;

public class LibraryIndex : ILibraryIndex
{
    private volatile Snapshot _snapshot = Snapshot.Empty;

    public string RootPath => _snapshot.RootPath;

    public IReadOnlyList<LibraryFolder> Folders => _snapshot.Folders;

    public IReadOnlyList<Moment> Moments => _snapshot.Moments;

    public void Load(string rootPath, IReadOnlyList<LibraryFolder> folders) =>
        _snapshot = new Snapshot(rootPath, folders);

    public LibraryFolder? GetFolder(string folderId) =>
        _snapshot.FoldersById.GetValueOrDefault(folderId);

    public Moment? GetMoment(string momentId) =>
        _snapshot.MomentsById.GetValueOrDefault(momentId);

    private sealed class Snapshot
    {
        public static readonly Snapshot Empty = new(string.Empty, []);

        public Snapshot(string rootPath, IReadOnlyList<LibraryFolder> folders)
        {
            RootPath = rootPath;
            Folders = folders;

            var moments = new List<Moment>();
            var foldersById = new Dictionary<string, LibraryFolder>(StringComparer.OrdinalIgnoreCase);
            var momentsById = new Dictionary<string, Moment>(StringComparer.OrdinalIgnoreCase);

            foreach (var folder in folders)
            {
                foldersById[folder.Id] = folder;

                foreach (var moment in folder.Moments)
                {
                    moments.Add(moment);
                    momentsById[moment.Id] = moment;
                }
            }

            Moments = moments;
            FoldersById = foldersById;
            MomentsById = momentsById;
        }

        public string RootPath { get; }

        public IReadOnlyList<LibraryFolder> Folders { get; }

        public IReadOnlyList<Moment> Moments { get; }

        public Dictionary<string, LibraryFolder> FoldersById { get; }

        public Dictionary<string, Moment> MomentsById { get; }
    }
}
