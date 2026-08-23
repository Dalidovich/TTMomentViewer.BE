using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TTMomentViewer.BLL.Helpers;
using TTMomentViewer.BLL.Interfaces;
using TTMomentViewer.Domain.Configuration;
using TTMomentViewer.Domain.Entities;

namespace TTMomentViewer.BLL.Services;

public class LibraryScanner : ILibraryScanner
{
    private readonly LibrarySettings _settings;
    private readonly ILogger<LibraryScanner> _logger;

    public LibraryScanner(IOptions<LibrarySettings> settings, ILogger<LibraryScanner> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public IReadOnlyList<LibraryFolder> Scan(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
        {
            _logger.LogError("Library root path does not exist: {RootPath}", rootPath);
            return [];
        }

        var allowedExtensions = new HashSet<string>(_settings.AllowedExtensions, StringComparer.OrdinalIgnoreCase);

        LogIgnoredRootFiles(rootPath, allowedExtensions);

        var folders = new List<LibraryFolder>();

        foreach (var directoryPath in Directory.EnumerateDirectories(rootPath))
        {
            var folder = ScanFolder(directoryPath, allowedExtensions);
            if (folder is not null) folders.Add(folder);
        }

        folders.Sort((left, right) => NaturalComparer.Instance.Compare(left.Name, right.Name));

        return folders;
    }

    private LibraryFolder? ScanFolder(string directoryPath, HashSet<string> allowedExtensions)
    {
        var folderName = Path.GetFileName(directoryPath);

        foreach (var nestedPath in Directory.EnumerateDirectories(directoryPath))
        {
            _logger.LogWarning("Nested directory ignored, only one level is supported: {DirectoryPath}", nestedPath);
        }

        var fileNames = new List<string>();

        foreach (var filePath in Directory.EnumerateFiles(directoryPath))
        {
            var fileName = Path.GetFileName(filePath);

            if (!allowedExtensions.Contains(Path.GetExtension(filePath)))
            {
                _logger.LogWarning("File ignored, extension is not allowed: {FilePath}", filePath);
                continue;
            }

            fileNames.Add(fileName);
        }

        if (fileNames.Count == 0) return null;

        fileNames.Sort(NaturalComparer.Instance);

        var folderId = IdHasher.HashFolderName(folderName);
        var folder = new LibraryFolder
        {
            Id = folderId,
            Name = folderName
        };

        for (var index = 0; index < fileNames.Count; index++)
        {
            var relativePath = $"{folderName}/{fileNames[index]}";

            folder.Moments.Add(new Moment
            {
                Id = IdHasher.HashRelativePath(relativePath),
                FolderId = folderId,
                FolderName = folderName,
                Name = Path.GetFileNameWithoutExtension(fileNames[index]),
                RelativePath = relativePath,
                Index = index
            });
        }

        return folder;
    }

    private void LogIgnoredRootFiles(string rootPath, HashSet<string> allowedExtensions)
    {
        foreach (var filePath in Directory.EnumerateFiles(rootPath))
        {
            if (allowedExtensions.Contains(Path.GetExtension(filePath)))
            {
                _logger.LogWarning("Video file in library root ignored: {FilePath}", filePath);
            }
        }
    }
}
