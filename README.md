# TTMomentViewer.BE

ASP.NET Core 9 Web API that indexes a local video library and serves moment metadata, JPEG thumbnails, and range-enabled video streams to [TTMomentViewer.FE](../TTMomentViewer.FE).

There is no database. The library is scanned once at startup and kept in memory — restart the app to pick up changes on disk.

## Requirements

- .NET 9 SDK
- A `VideoLibrary` folder with videos (see [Library layout](#library-layout))

ffmpeg is not a prerequisite: the binary ships with `NReco.VideoConverter` and is extracted on first use.

## Quick start

```powershell
dotnet build
dotnet run --project TTMomentViewer.API
```

The API listens on `http://localhost:5278` (profile `http`); the `https` profile adds `https://localhost:7072`. Swagger UI is available at `/swagger` in every environment.

On startup the console prints the resolved library root and the number of folders and moments found.

## Library layout

```
VideoLibrary
├─ named folder 1
│  ├─ moment 1.mp4
│  └─ moment 2.mp4
└─ named folder 2
   └─ moment 1.mp4
```

Exactly one nesting level is supported. Files in the library root, deeper subdirectories, and unsupported extensions are ignored and logged as warnings; empty folders are dropped from the index. Folders and files are ordered naturally, so `moment 2` comes before `moment 10`.

## Configuration

`TTMomentViewer.API/appsettings.json`:

```json
{
  "LibrarySettings": {
    "LibraryRootPath": "VideoLibrary",
    "AllowedExtensions": [ ".mp4", ".webm", ".mov", ".m4v" ]
  }
}
```

| Setting | Description |
| --- | --- |
| `LibraryRootPath` | Absolute path, or a relative one resolved against the content root. A missing folder is logged as an error and the app starts with an empty index. |
| `AllowedExtensions` | Video extensions to index, matched case-insensitively. |

## API

Base prefix `/api`, JSON in camelCase, matching is case-insensitive.

| Method | Route | Description |
| --- | --- | --- |
| GET | `/api/folders?page=1&pageSize=30` | Page of folders |
| GET | `/api/folders/{folderId}` | Single folder |
| GET | `/api/folders/{folderId}/moments?page=1&pageSize=30` | Page of moments in a folder |
| GET | `/api/folders/{folderId}/thumbnail` | JPEG cover of the folder |
| GET | `/api/moments/{momentId}` | Single moment |
| GET | `/api/moments/{momentId}/stream` | Video stream with HTTP Range support |
| GET | `/api/moments/{momentId}/thumbnail` | JPEG frame of the moment |
| GET | `/api/feed?seed=123456&page=1&pageSize=10` | Page of the globally shuffled feed |

`page` must be at least 1 and `pageSize` must be between 1 and 100; anything else returns `400` with `{ error, statusCode }`. Unknown ids return `404`. Unhandled exceptions return `500` in the same shape.

The feed is shuffled with a seeded Fisher–Yates pass, so the same `seed` always yields the same order — paging through it produces neither duplicates nor gaps.

Thumbnails are extracted at second 1 of the video, cached in memory (up to 100 entries, FIFO eviction), and served with `ETag`, `Last-Modified`, and `Cache-Control: public, max-age=86400`, so repeat requests get a `304`.

Relative paths never leave the backend — clients address every file by moment id.

## Solution layout

| Project | Role |
| --- | --- |
| `TTMomentViewer.API` | Controllers, exception middleware, startup scan, DI and configuration |
| `TTMomentViewer.BLL` | Library scanner, in-memory index, folder/moment/feed/thumbnail services, DTOs |
| `TTMomentViewer.Domain` | Entities and settings |

Dependencies flow one way: `API → BLL → Domain`.

## Notes

- CORS is open to any origin, header, and method; HTTPS redirection is disabled because the frontend talks plain HTTP through its dev proxy.
- Logging goes to the console via Serilog.
- The solution has no test project.
