# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This repository contains two projects:

1. **ChanSort** - A Windows desktop application for reordering TV channel lists (third-party, reference only)
2. **SamsungChannelCli** - A standalone CLI tool for editing Samsung UE40ES8090 channel lists

## SamsungChannelCli

A simple .NET 6 command-line tool for editing Samsung TV channel lists (.scm files). Specifically designed for the UE40ES8090 (E-series, _1201 format).

### Build & Run

```bash
cd SamsungChannelCli
dotnet build
dotnet run -- <command> [args]
```

### Commands

| Command | Description |
|---------|-------------|
| `list <file.scm>` | List all channels |
| `move <file.scm> <from> <to>` | Move a channel to a new position |
| `compact <file.scm> [start]` | Renumber channels sequentially |
| `export <file.scm> <out.tsv>` | Export channels to TSV for editing |
| `import <file.scm> <in.tsv>` | Import channel order from edited TSV |

### Samsung SCM Format (E-series)

The `.scm` file is a ZIP archive containing binary channel data:
- `map-SateD` - Satellite channels (168 bytes/record)
- `map-CableD` / `map-AirD` - DVB-C/T channels (320 bytes/record)
- `map-CableA` / `map-AirA` - Analog channels (64 bytes/record)

Key offsets for satellite records (168 bytes):
- Offset 0: Program number (2 bytes, little-endian)
- Offset 7: InUse flag (bit 0x01)
- Offset 36: Name (100 bytes, UTF-16 Big Endian)
- Offset 167: Checksum (sum of bytes 0-166)

---

## ChanSort (Reference)

## Build Commands

**Solution file:** `ChanSort/source/ChanSort.sln`

**Build configurations:**
- `NoDevExpress_Debug` - Builds all loader projects without UI components (no DevExpress license needed)
- `Debug` - Builds everything including UI (requires DevExpress WinForms license)

```bash
# Build without DevExpress (for loader development)
msbuild ChanSort/source/ChanSort.sln /p:Configuration=NoDevExpress_Debug

# Build with DevExpress (requires license)
msbuild ChanSort/source/ChanSort.sln /p:Configuration=Debug
```

**Output directory:** `ChanSort/source/Debug/net48/`

## Running Tests

Tests use MSTest framework.

```bash
# Run all tests
dotnet test ChanSort/source/ChanSort.sln

# Run tests for a specific loader
dotnet test ChanSort/source/Test.Loader.Samsung/Test.Loader.Samsung.csproj
dotnet test ChanSort/source/Test.Loader.LG/Test.Loader.LG.csproj
```

Test projects follow the naming pattern `Test.Loader.<Brand>` and test files are located in `TestFiles/` directories relative to the test project.

## Architecture

### Plugin System

ChanSort uses a plugin architecture where each TV brand's file format is handled by a separate loader DLL:

1. **ISerializerPlugin** (`ChanSort.Api/Controller/ISerializerPlugin.cs`) - Interface that loader plugins implement. Defines `PluginName`, `FileFilter`, and `CreateSerializer()`.

2. **SerializerBase** (`ChanSort.Api/Controller/SerializerBase.cs`) - Abstract base class for all loaders. Key members:
   - `Load()` / `Save()` - Abstract methods to implement
   - `DataRoot` - Contains all channel lists and metadata
   - `Features` - Configures what operations the UI allows (favorites, deletion, editing)
   - `DefaultEncoding` - Text encoding for 8-bit character data

3. **DataRoot** (`ChanSort.Api/Model/DataRoot.cs`) - Container for all loaded data:
   - `ChannelLists` - Collection of channel lists
   - `Satellites`, `Transponder`, `LnbConfig` - Satellite/DVB metadata
   - `AddChannel()`, `AddChannelList()` - Methods loaders use to populate data

### Loader Projects

Each `ChanSort.Loader.*` project handles specific file formats. Reference implementations by data type:
- **Binary:** Samsung.Scm, LG.Binary, Philips.BinarySerializer
- **SQLite:** Hisense, Panasonic, Samsung.Zip, Toshiba
- **XML:** Sony, Grundig, LG.GlobalClone.GcXmlSerializer, Philips.XmlSerializer
- **JSON:** LG.GlobalClone.GcJsonSerializer
- **CSV:** Sharp
- **Text:** M3u, Enigma2, VDR

### Running Custom Builds

For development without DevExpress:
1. Download a binary release from GitHub
2. Copy `ChanSort.exe`, `*.UI.dll`, and `DevExpress.*.dll` to `ChanSort/source/Debug/net48/`
3. Build using `NoDevExpress_Debug` configuration
4. Your loader DLLs will be loaded dynamically by the prebuilt ChanSort.exe

## Target Framework

.NET Framework 4.8, targets "Any CPU" (supports x86, x64, and ARM).
