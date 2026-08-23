namespace TTMomentViewer.Domain.Configuration;

public class LibrarySettings
{
    public const string SectionName = "LibrarySettings";

    public string LibraryRootPath { get; set; } = string.Empty;

    public string[] AllowedExtensions { get; set; } = [".mp4", ".webm", ".mov", ".m4v"];

    public string ResolveLibraryRootPath(string contentRootPath)
    {
        if (string.IsNullOrWhiteSpace(LibraryRootPath))
            return string.Empty;

        return Path.IsPathRooted(LibraryRootPath)
            ? Path.GetFullPath(LibraryRootPath)
            : Path.GetFullPath(Path.Combine(contentRootPath, LibraryRootPath));
    }
}
