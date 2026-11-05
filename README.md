# Toridame

A personal fork of the [Jellyfin](https://github.com/jellyfin/jellyfin) media server, based on **Jellyfin 12.4**. It adds a handful of performance and quality-of-life changes. It is not affiliated with or endorsed by the Jellyfin project.

The web client lives in a separate repository: [Toridame-web](https://github.com/jt-ito/Toridame-web). Both are meant to be used together.

## What's different from stock Jellyfin 12.4

**Subtitles and fonts**
- An **Extract Subtitles** scheduled task (Dashboard → Scheduled Tasks → Advanced) that pulls out text subtitle tracks ahead of time, at low priority, skipping files where extraction can't help.
- Optionally runs after every library scan. **Off by default.** Turn it on under Dashboard → Libraries → Display.
- Embedded fonts are extracted in one batch, which removes the long wait for styled subtitles the first time an anime episode plays.

**Scanning**
- Per-stage timing logs for library scans, so slow stages are easy to find.
- An optional scan-time warning for videos whose audio or video stream ends well before the rest of the file (cut-off downloads, bad remuxes). **Off by default.** Log output only. Turn it on under Dashboard → Libraries → Display.

**Watch history**
- A per-user setting to **not save watch history**: playback start, progress and finish are not recorded, so nothing reaches Continue Watching, Next Up or played status.
- `DELETE UserItems/WatchHistory` clears a user's history, either only resume points (`continueWatchingOnly=true`) or everything. Favorites and ratings are kept. Users can clear their own; administrators can clear anyone's.

**Playback reliability**
- Fixes for resuming after a long pause and for seeking, including recovery from a stalled transcode.

**Stability**
- A startup fix for a dependency cycle between the transcode manager and the trickplay and chapter managers.

## Building

Requires the .NET 10 SDK.

```bash
dotnet build
dotnet test
```

Self-contained Linux build:

```bash
dotnet publish Jellyfin.Server/Jellyfin.Server.csproj -c Release -r linux-x64 --self-contained -o publish
```

The server needs the built web client next to it. Build [Toridame-web](https://github.com/jt-ito/Toridame-web) and point the server at its `dist` folder with `--webdir`, or place it in a `jellyfin-web` folder beside the `jellyfin` executable.

## Running

The server listens on its usual port (8096) unless you change it. Transcoding needs ffmpeg; [jellyfin-ffmpeg](https://github.com/jellyfin/jellyfin-ffmpeg) is recommended.

**Do not point this at your real Jellyfin data or config folders.** This fork upgrades the database in ways an older server may not read back. Use a separate data directory while testing.

## Staying close to upstream

The history starts from an unmodified Jellyfin 12.4 snapshot, followed by this fork's changes, so `git diff` between the first two commits shows exactly what changed. The original repository is available as the `upstream` remote. The history is not shared with upstream's, so moving to a newer Jellyfin release means re-applying these changes rather than a plain merge.

## License

GPL-2.0, as for Jellyfin. See [LICENSE](LICENSE). Jellyfin is a trademark of its respective owners.
