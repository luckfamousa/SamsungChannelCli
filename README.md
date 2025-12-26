# SamsungChannelCli

A command-line tool for editing Samsung TV channel lists (.scm files).

Designed for the **Samsung UE40ES8090** (E-series, _1201 format), but may work with other Samsung TVs using the same format.

## Requirements

- .NET 6.0 SDK or later

## Build

```bash
cd SamsungChannelCli
dotnet build
```

## Usage

```bash
dotnet run -- <command> [arguments]
```

### Commands

#### List channels
```bash
dotnet run -- list channel_list.scm
```

#### Move a channel
```bash
# Move channel 24 to position 1
dotnet run -- move channel_list.scm 24 1
```

#### Compact (remove gaps)
```bash
# Renumber all channels sequentially starting from 1
dotnet run -- compact channel_list.scm

# Keep channels 1-2, renumber the rest starting from 3
dotnet run -- compact channel_list.scm 3
```

#### Export to TSV
```bash
dotnet run -- export channel_list.scm channels.tsv
```

This creates a tab-separated file you can edit in any text editor:
```
Number	Name	Source	RecordIndex
1	Das Erste HD	map-SateD	288
2	ZDF HD	map-SateD	63
3	WDR HD Essen	map-SateD	175
...
```

#### Import from TSV
```bash
dotnet run -- import channel_list.scm channels.tsv
```

Reorder lines in the TSV file to change channel positions. The line number becomes the new channel number.

## Workflow Example

1. Export your channel list to a text file:
   ```bash
   dotnet run -- export channel_list.scm channels.tsv
   ```

2. Edit `channels.tsv` in your favorite text editor - reorder lines as needed

3. Import the edited list back:
   ```bash
   dotnet run -- import channel_list.scm channels.tsv
   ```

4. Copy the modified `.scm` file back to your USB drive and import it on your TV

## Backup

The tool automatically creates a `.backup` file before modifying the original `.scm` file.

## Supported Channel Types

| File | Type | Record Size |
|------|------|-------------|
| map-SateD | Satellite (DVB-S) | 168 bytes |
| map-CableD | Cable (DVB-C) | 320 bytes |
| map-AirD | Antenna (DVB-T) | 320 bytes |
| map-CableA | Analog Cable | 64 bytes |
| map-AirA | Analog Antenna | 64 bytes |

## Technical Notes

- The `.scm` file is a ZIP archive containing binary channel data
- Channel names are stored as UTF-16 Big Endian
- Each record has a checksum byte (sum of all preceding bytes)
- This tool is standalone and has no external dependencies
