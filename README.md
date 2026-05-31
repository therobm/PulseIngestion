# PulseIngestion

Command-line ingestion tool for a Pulse music library. Scans a source directory
and transcodes lossless/intermediate audio (flac, aac by default) to 320 kbps
MP3 via ffmpeg, preserving metadata. Both source formats and the output format
are configurable.

Work is split into independently toggled scanners that run each cycle: media
conversion, library organization (folder tree + optional rename to track title),
empty-folder and wildcard-duplicate cleanup, and a read-only library-stats
survey. Each cycle emits an HTML report.

Conversion runs as a throttled pool of concurrent ffmpeg processes (thread count
configurable).

## Configuration

On first run the tool writes a default `config.txt` (JSON) next to the
executable and then runs against those defaults. Edit it to point at your
environment before a real run:

| Field | Meaning |
|---|---|
| `FFMpegLocation` | Path to `ffmpeg.exe`. |
| `MusicSource` | Directory scanned for files to convert. |
| `MusicDestination` | Output directory. |
| `ReportDirectory` | Where HTML reports are written (empty => `<MusicSource>\PulseIngestion`). |
| `ThreadCount` | Max concurrent ffmpeg processes. |
| `ScanningIntervalMinutes` | Minutes between scan cycles (default 1440 = 24h). |
| `SourceFormats` | Formats to convert, by name (e.g. `FLAC`, `AAC`). |
| `DestinationMusicFormat` | Output format, by name (e.g. `MP3`, `FLAC`). |
| `EnableFFMpegDebug` | Echo ffmpeg stdout/stderr. |
| `EnableMediaConversion` | Run the transcode scanner. |
| `DeleteAfterConversion` | Delete the source file once converted (otherwise renamed to `.bak`). |
| `EnableOrganization` | Run the library-organization scanner (folder tree from tags). |
| `RenameFilesToTrackTitle` | When organizing, rename files to the track title. |
| `EnableLibraryStats` | Run the read-only library-stats survey. |
| `RemoveEmptyDirectories` | Remove empty directories under the source. |
| `CleanupDuplicatesByWildcard` | Remove/rename files tagged with the duplicate token. |
| `DuplicateFileWildcardToken` | The duplicate marker (default `" (1)"`). |

## Requirements

- .NET 8 runtime (release builds are self-contained win-x64 single-file).
- ffmpeg available at the configured path.
