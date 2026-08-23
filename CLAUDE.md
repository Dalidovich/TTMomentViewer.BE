# TTMomentViewer.BE — Backend for TTMomentViewer

## Overview

.NET 9 ASP.NET Core web API that scans a local `VideoLibrary` folder, keeps the index in memory, and serves moment metadata, JPEG thumbnails, and range-enabled video streams to the Angular frontend. No database, no persisted state — the index is rebuilt on every start.

## Architecture

| Layer | Project | Subfolders | Purpose |
|---|---|---|---|
| Presentation | `TTMomentViewer.API` | `Controllers/`, `Middleware/`, `BackgroundServices/`, `Extensions/` | REST controllers, error middleware, startup scan, config |
| Business Logic | `TTMomentViewer.BLL` | `Interfaces/`, `Services/`, `DTOs/`, `Helpers/` | Scanner, index, services, DTOs, mapping |
| Domain | `TTMomentViewer.Domain` | `Entities/`, `Configuration/` | Entities and settings only |

**Dependency flow:** `API → BLL → Domain` (no reverse dependencies). `API` also references `Domain` directly for `LibrarySettings` binding.

No DAL layer and no test project — see `Docs/TechnicalSpecification.md` §9 for the differences from the LCP reference project.

## Library Layout on Disk

```
VideoLibrary
├─ named folder 1
│  ├─ moment 1.mp4
│  └─ moment N.mp4
└─ named folder X
   └─ moment 1.mp4
```

Rules enforced by `LibraryScanner`:
- Exactly one nesting level: `VideoLibrary/<folder>/<video>`. Deeper directories are ignored and logged as `Warning`.
- Video files in the library root are ignored and logged as `Warning`.
- Files with extensions outside `AllowedExtensions` are ignored and logged as `Warning`.
- Folders with no matching files are dropped from the index.
- Folders and files are ordered with `NaturalComparer` (`moment 2` before `moment 10`).

## Data Model

### `LibraryFolder` (TTMomentViewer.Domain/Entities/)
```csharp
Id      : string        (16 hex chars, SHA1 of the folder name)
Name    : string        (folder name on disk)
Moments : List<Moment>  (natural sort by file name)
```

### `Moment` (TTMomentViewer.Domain/Entities/)
```csharp
Id           : string  (16 hex chars, SHA1 of the normalized relative path)
FolderId     : string
FolderName   : string
Name         : string  (file name without extension)
RelativePath : string  ("folder/file.mp4"; never leaves the backend)
Index        : int     (position inside the folder, zero-based)
```

Ids come from `IdHasher`: `SHA1` → hex → first 16 chars, lowercased. `HashRelativePath` normalizes separators to `/` and lowercases before hashing.

Video duration is deliberately not computed — that would require ffprobe per file on every scan. The frontend reads it from the `<video>` element.

## Configuration (`appsettings.json`)

```json
{
  "LibrarySettings": {
    "LibraryRootPath": "./VideoLibrary",
    "AllowedExtensions": [ ".mp4", ".webm", ".mov", ".m4v" ]
  }
}
```

- `LibraryRootPath` — a relative path is resolved against `ContentRootPath` via `LibrarySettings.ResolveLibraryRootPath()`; an absolute path is used as is.
- Missing root folder → `LogError`, the app starts with an empty index instead of crashing.
- `AllowedExtensions` — matched case-insensitively.

## DTOs (TTMomentViewer.BLL/DTOs/)

| DTO | Fields | Purpose |
|---|---|---|
| `FolderDto` | `Id`, `Name`, `MomentCount`, `CoverMomentId` | Folder listing; `CoverMomentId` is the first moment, `null` for an empty folder |
| `MomentDto` | `Id`, `FolderId`, `FolderName`, `Name`, `Index` | Moment metadata; `Index` drives the frontend start page in a folder feed |
| `PagedResult<T>` | `Items`, `Page`, `PageSize`, `TotalCount`, `TotalPages` (computed) | Generic paginated response |
| `ThumbnailResult` | `record(byte[] Data, DateTime LastModified)` | Thumbnail frame with ETag support |

Both DTOs expose a static `FromEntity` mapper; there is no mapping library.

## API Endpoints

Base prefix `/api`, JSON camelCase. Route templates use `[Route("api/[controller]")]`, so Swagger shows `/api/Folders` — matching is case-insensitive and the frontend calls lowercase paths.

| Method | Route | Description |
|---|---|---|
| GET | `/api/folders?page=1&pageSize=30` | Page of folders, `PagedResult<FolderDto>` |
| GET | `/api/folders/{folderId}` | Single folder, `FolderDto`; 404 if unknown |
| GET | `/api/folders/{folderId}/moments?page=1&pageSize=30` | Page of moments in a folder; 404 if the folder is unknown |
| GET | `/api/folders/{folderId}/thumbnail` | JPEG cover of the folder = frame of the moment with `Index = 0` |
| GET | `/api/moments/{momentId}` | Single moment, `MomentDto`; 404 if unknown |
| GET | `/api/moments/{momentId}/stream` | Video file, `PhysicalFile` with `enableRangeProcessing: true` |
| GET | `/api/moments/{momentId}/thumbnail` | JPEG frame, `image/jpeg` |
| GET | `/api/feed?seed=123456&page=1&pageSize=10` | Page of the global shuffled feed |

**Pagination validation** — `page >= 1` and `1 <= pageSize <= 100`, otherwise `400` with `{ error, statusCode }`. `MaxPageSize = 100` is a constant on each paginated controller (`FoldersController`, `FeedController`).

There is no rescan endpoint — library changes are applied by restarting the app.

## Startup Scan

`LibraryScanService` (`IHostedService`, `API/BackgroundServices/`) runs once before requests are served:

1. Resolve `LibraryRootPath` against `ContentRootPath`.
2. `ILibraryScanner.Scan()` enumerates first-level folders and their allowed files (no recursion), natural-sorts both, assigns `Index`, drops empty folders.
3. The result is published into the singleton `ILibraryIndex` via `Load()`.
4. Folder count, moment count, and root path are logged at `Information`.

`LibraryIndex` holds an immutable `Snapshot` (ordered folder list, flat moment list, two `Dictionary` lookups by id, `OrdinalIgnoreCase`) in a `volatile` field — reads are lock-free and always see a consistent snapshot.

## Feed Shuffle

`FeedService.GetFeed(seed, page, pageSize)` runs Fisher–Yates over the flat moment list with `new Random(seed)`, then pages the result. The same `seed` and `page` always return the same items, so paging produces no duplicates and no gaps. When the frontend exhausts the pages for a seed it increments the seed and restarts at page 1.

## Thumbnails

- Generated on demand via `FFMpegConverter.GetVideoThumbnail(videoPath, stream, 1f)` — frame at second 1, run through `Task.Run` to keep the request thread free.
- Cached in a `static ConcurrentDictionary<string, byte[]>` keyed by `momentId`, `MaxCacheSize = 100`, FIFO eviction of the first key on overflow.
- Folder covers reuse the same cache entry as the moment with `Index = 0`.
- Response carries `ETag` (from `LastModified.Ticks`), `Last-Modified`, and `Cache-Control: public, max-age=86400`; a matching `If-None-Match` returns `304`. See `API/Extensions/ThumbnailResponseExtensions.cs`.
- ffmpeg extraction failure → `LogError` and `404`; the frontend then renders a placeholder.

`VideoProcessingService.GetFfmpegExePath()` extracts the ffmpeg binary bundled with NReco, trying `AppContext.BaseDirectory` first and `%LOCALAPPDATA%\TTMomentViewer\ffmpeg` second, and caches the resolved path in a static field.

## Streaming

`MomentsController.Stream` resolves the file through `IMomentService.ResolveFilePath` (404 when the file is missing on disk) and returns `PhysicalFile(..., enableRangeProcessing: true)`, so seeking works over HTTP Range. MIME by extension: `.mp4`/`.m4v` → `video/mp4`, `.webm` → `video/webm`, `.mov` → `video/quicktime`, otherwise `application/octet-stream`.

## Logging

Serilog, console sink only, configured in `Program.cs` before the host is built.

| Event | Level |
|---|---|
| Scan start, scan result (folder and moment counts) | Information |
| Thumbnail generation for a file | Information |
| Ignored file or folder (deep nesting, file in root, unsupported extension) | Warning |
| Missing `LibraryRootPath` | Error |
| ffmpeg failure, unhandled exception | Error |

## Key Conventions

- **No comments in code** — keep source files clean
- **English only in code** — no localized strings anywhere in the sources
- **Nullable enabled** — follow `?` annotations; services return `null` for "not found" and controllers translate that to `404`
- **DI lifetimes** — `ILibraryIndex`, `IThumbnailService`, `IVideoProcessingService` are singletons; `ILibraryScanner`, `IFolderService`, `IMomentService`, `IFeedService` are scoped
- **Constructor injection** — explicit constructors, `_camelCase` private readonly fields
- **CORS** — default policy allows any origin, header, and method
- **Global error handling** — `ExceptionHandlingMiddleware` logs method and path, returns `{ error, statusCode }` with 500
- **No HTTPS redirection** — the frontend talks plain http through the dev proxy
- **Swagger** — enabled in every environment, UI at `/swagger`
- **No tests** — the solution has no test project
- **Relative paths stay internal** — `RelativePath` is never exposed in a DTO; clients address files by moment id only

## Build & Run

```powershell
dotnet build
dotnet run --project TTMomentViewer.API
```

Profiles: `http` (port 5278, default), `https` (ports 7072 / 5278) — see `TTMomentViewer.API/Properties/launchSettings.json`.

Swagger UI at `/swagger`. Startup prints the scanned folder and moment counts.

## Package Dependencies

- `Microsoft.AspNetCore.OpenApi` 9.0.6 (API)
- `Swashbuckle.AspNetCore` 7.3.1 (API)
- `Serilog.AspNetCore` 9.0.0 (API)
- `NReco.VideoConverter` 1.2.1 (BLL — bundles the ffmpeg binary)
- `Microsoft.Extensions.Logging.Abstractions` 9.0.3 (BLL)
- `Microsoft.Extensions.Options` 9.0.3 (Domain)

## Project References

```
TTMomentViewer.API    → TTMomentViewer.BLL, TTMomentViewer.Domain
TTMomentViewer.BLL    → TTMomentViewer.Domain
TTMomentViewer.Domain → (none)
```

## Service Layer Overview

### BLL Interfaces (`TTMomentViewer.BLL/Interfaces/`)

| Interface | Members |
|---|---|
| `ILibraryIndex` | `RootPath`, `Folders`, `Moments`, `Load(rootPath, folders)`, `GetFolder(folderId)`, `GetMoment(momentId)` |
| `ILibraryScanner` | `Scan(rootPath)` → `IReadOnlyList<LibraryFolder>` |
| `IFolderService` | `GetFolders(page, pageSize)`, `GetFolder(folderId)` |
| `IMomentService` | `GetMoment(momentId)`, `GetFolderMoments(folderId, page, pageSize)`, `ResolveFilePath(momentId)` |
| `IFeedService` | `GetFeed(seed, page, pageSize)` |
| `IThumbnailService` | `GetMomentThumbnailAsync(momentId)`, `GetFolderThumbnailAsync(folderId)` |
| `IVideoProcessingService` | `ExtractFrame(videoPath)` → `byte[]?` |

### Helpers (`TTMomentViewer.BLL/Helpers/`)

| Helper | Purpose |
|---|---|
| `NaturalComparer` | Singleton `IComparer<string>` doing digit-aware comparison, case-insensitive, ordinal tie-break |
| `IdHasher` | `HashFolderName`, `HashRelativePath`, `NormalizeRelativePath` |
