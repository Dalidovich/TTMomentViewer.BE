using Microsoft.Extensions.Options;
using TTMomentViewer.BLL.Interfaces;
using TTMomentViewer.Domain.Configuration;

namespace TTMomentViewer.API.BackgroundServices;

public class LibraryScanService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILibraryIndex _index;
    private readonly IHostEnvironment _environment;
    private readonly LibrarySettings _settings;
    private readonly ILogger<LibraryScanService> _logger;

    public LibraryScanService(
        IServiceScopeFactory scopeFactory,
        ILibraryIndex index,
        IHostEnvironment environment,
        IOptions<LibrarySettings> settings,
        ILogger<LibraryScanService> logger)
    {
        _scopeFactory = scopeFactory;
        _index = index;
        _environment = environment;
        _settings = settings.Value;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var rootPath = _settings.ResolveLibraryRootPath(_environment.ContentRootPath);

        _logger.LogInformation("Library scan started: {RootPath}", rootPath);

        using var scope = _scopeFactory.CreateScope();
        var scanner = scope.ServiceProvider.GetRequiredService<ILibraryScanner>();

        _index.Load(rootPath, scanner.Scan(rootPath));

        _logger.LogInformation("Library scan completed: {FolderCount} folders, {MomentCount} moments in {RootPath}",
            _index.Folders.Count, _index.Moments.Count, rootPath);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
