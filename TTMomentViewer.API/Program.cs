using Serilog;
using TTMomentViewer.API.BackgroundServices;
using TTMomentViewer.API.Middleware;
using TTMomentViewer.BLL.Interfaces;
using TTMomentViewer.BLL.Services;
using TTMomentViewer.Domain.Configuration;

namespace TTMomentViewer.API;

public class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddSwaggerGen();

            builder.Services.Configure<LibrarySettings>(
                builder.Configuration.GetSection(LibrarySettings.SectionName));

            builder.Services.AddSingleton<ILibraryIndex, LibraryIndex>();
            builder.Services.AddSingleton<IVideoProcessingService, VideoProcessingService>();
            builder.Services.AddSingleton<IThumbnailService, ThumbnailService>();
            builder.Services.AddScoped<ILibraryScanner, LibraryScanner>();
            builder.Services.AddScoped<IFolderService, FolderService>();
            builder.Services.AddScoped<IMomentService, MomentService>();
            builder.Services.AddScoped<IFeedService, FeedService>();
            builder.Services.AddHostedService<LibraryScanService>();

            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            builder.Host.UseSerilog();

            var app = builder.Build();

            app.UseMiddleware<ExceptionHandlingMiddleware>();

            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseCors();

            app.UseAuthorization();

            app.MapControllers();

            Log.Information("Application starting");
            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
