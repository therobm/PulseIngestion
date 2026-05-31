# PulseIngestion

Command-line ingestion tool for a Pulse music library. Scans a source directory
and transcodes lossless/intermediate audio (flac, aac) to 320 kbps MP3 via
ffmpeg, preserving metadata. Optionally cleans up empty folders and
wildcard-tagged duplicate files before conversion.

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
| `ThreadCount` | Max concurrent ffmpeg processes. |
| `SourceExtensions` | Extensions to convert (e.g. `flac`, `aac`). |
| `EnableFFMpegDebug` | Echo ffmpeg stdout/stderr. |
| `DeleteAfterConversion` | Delete the source file once converted (otherwise renamed to `.bak`). |
| `DeleteEmptyFolders` | Remove empty directories under the source before scanning. |
| `CleanupDuplicatesByWildcard` | Remove/rename files tagged with the duplicate token. |
| `DuplicateFileWildcardToken` | The duplicate marker (default `" (1)"`). |

## Requirements

- .NET 8 runtime (release builds are self-contained win-x64 single-file).
- ffmpeg available at the configured path.
