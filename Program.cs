using System.IO.Compression;
using System.Text;

namespace SamsungChannelCli;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        var command = args[0].ToLower();

        if (command == "list" && args.Length >= 2)
        {
            return ListChannels(args[1]);
        }
        else if (command == "move" && args.Length >= 4)
        {
            if (!int.TryParse(args[2], out int fromNr) || !int.TryParse(args[3], out int toNr))
            {
                Console.WriteLine("Error: Invalid channel numbers");
                return 1;
            }
            return MoveChannel(args[1], fromNr, toNr);
        }
        else if (command == "compact" && args.Length >= 2)
        {
            int startFrom = 1;
            if (args.Length >= 3)
                int.TryParse(args[2], out startFrom);
            return CompactChannels(args[1], startFrom);
        }
        else if (command == "export" && args.Length >= 3)
        {
            return ExportChannels(args[1], args[2]);
        }
        else if (command == "import" && args.Length >= 3)
        {
            return ImportChannels(args[1], args[2]);
        }
        else if (command == "help" || command == "--help" || command == "-h")
        {
            PrintUsage();
            return 0;
        }
        else
        {
            Console.WriteLine($"Unknown command: {command}");
            PrintUsage();
            return 1;
        }
    }

    static void PrintUsage()
    {
        Console.WriteLine("Samsung Channel List CLI - UE40ES8090 Edition");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  SamsungChannelCli list <file.scm>              List all channels");
        Console.WriteLine("  SamsungChannelCli move <file.scm> <from> <to>  Move channel to new position");
        Console.WriteLine("  SamsungChannelCli compact <file.scm> [start]   Renumber channels sequentially");
        Console.WriteLine("  SamsungChannelCli export <file.scm> <out.tsv>  Export channels to TSV file");
        Console.WriteLine("  SamsungChannelCli import <file.scm> <in.tsv>   Import channel order from TSV");
        Console.WriteLine("  SamsungChannelCli help                         Show this help");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  SamsungChannelCli list channels.scm");
        Console.WriteLine("  SamsungChannelCli move channels.scm 24 1       Move channel 24 to position 1");
        Console.WriteLine("  SamsungChannelCli compact channels.scm         Renumber all channels from 1");
        Console.WriteLine("  SamsungChannelCli export channels.scm list.tsv Export to edit in text editor");
        Console.WriteLine("  SamsungChannelCli import channels.scm list.tsv Apply edited channel order");
    }

    static int ListChannels(string scmPath)
    {
        if (!File.Exists(scmPath))
        {
            Console.WriteLine($"Error: File not found: {scmPath}");
            return 1;
        }

        string tempDir = Path.Combine(Path.GetTempPath(), "SamsungChannelCli_" + Path.GetRandomFileName());

        try
        {
            Directory.CreateDirectory(tempDir);
            ZipFile.ExtractToDirectory(scmPath, tempDir);

            Console.WriteLine($"Channel List: {Path.GetFileName(scmPath)}");
            Console.WriteLine(new string('=', 60));

            ReadAndDisplayChannels(tempDir, "map-CableD", "Digital Cable", ChannelType.DvbCT);
            ReadAndDisplayChannels(tempDir, "map-AirD", "Digital Antenna", ChannelType.DvbCT);
            ReadAndDisplayChannels(tempDir, "map-SateD", "Satellite", ChannelType.Satellite);
            ReadAndDisplayChannels(tempDir, "map-CableA", "Analog Cable", ChannelType.Analog);
            ReadAndDisplayChannels(tempDir, "map-AirA", "Analog Antenna", ChannelType.Analog);

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    static int MoveChannel(string scmPath, int fromNr, int toNr)
    {
        if (!File.Exists(scmPath))
        {
            Console.WriteLine($"Error: File not found: {scmPath}");
            return 1;
        }

        if (fromNr == toNr)
        {
            Console.WriteLine("Source and target are the same. Nothing to do.");
            return 0;
        }

        string tempDir = Path.Combine(Path.GetTempPath(), "SamsungChannelCli_" + Path.GetRandomFileName());

        try
        {
            Directory.CreateDirectory(tempDir);
            ZipFile.ExtractToDirectory(scmPath, tempDir);

            // Try to move in each channel file
            bool found = false;
            found |= MoveChannelInFile(tempDir, "map-SateD", ChannelType.Satellite, fromNr, toNr);
            found |= MoveChannelInFile(tempDir, "map-CableD", ChannelType.DvbCT, fromNr, toNr);
            found |= MoveChannelInFile(tempDir, "map-AirD", ChannelType.DvbCT, fromNr, toNr);

            if (!found)
            {
                Console.WriteLine($"Error: Channel {fromNr} not found");
                return 1;
            }

            // Create backup
            string backupPath = scmPath + ".backup";
            if (!File.Exists(backupPath))
            {
                File.Copy(scmPath, backupPath);
                Console.WriteLine($"Backup created: {backupPath}");
            }

            // Repack the .scm file
            File.Delete(scmPath);
            ZipFile.CreateFromDirectory(tempDir, scmPath, CompressionLevel.Optimal, false);

            Console.WriteLine($"Successfully moved channel {fromNr} to position {toNr}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    static int CompactChannels(string scmPath, int startFrom)
    {
        if (!File.Exists(scmPath))
        {
            Console.WriteLine($"Error: File not found: {scmPath}");
            return 1;
        }

        string tempDir = Path.Combine(Path.GetTempPath(), "SamsungChannelCli_" + Path.GetRandomFileName());

        try
        {
            Directory.CreateDirectory(tempDir);
            ZipFile.ExtractToDirectory(scmPath, tempDir);

            // Compact each channel file
            int total = 0;
            total += CompactChannelsInFile(tempDir, "map-SateD", ChannelType.Satellite, startFrom);
            total += CompactChannelsInFile(tempDir, "map-CableD", ChannelType.DvbCT, startFrom);
            total += CompactChannelsInFile(tempDir, "map-AirD", ChannelType.DvbCT, startFrom);

            if (total == 0)
            {
                Console.WriteLine("No channels found to compact");
                return 0;
            }

            // Create backup if not exists
            string backupPath = scmPath + ".backup";
            if (!File.Exists(backupPath))
            {
                File.Copy(scmPath, backupPath);
                Console.WriteLine($"Backup created: {backupPath}");
            }

            // Repack the .scm file
            File.Delete(scmPath);
            ZipFile.CreateFromDirectory(tempDir, scmPath, CompressionLevel.Optimal, false);

            Console.WriteLine($"Successfully compacted {total} channels starting from position {startFrom}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    static int CompactChannelsInFile(string tempDir, string fileName, ChannelType type, int startFrom)
    {
        string filePath = Path.Combine(tempDir, fileName);
        if (!File.Exists(filePath))
            return 0;

        byte[] data = File.ReadAllBytes(filePath);
        var config = GetChannelConfig(type);
        int recordCount = data.Length / config.RecordSize;

        // Collect all active channels with their current program numbers
        var channels = new List<(int recordIndex, int progNr, string name)>();

        for (int i = 0; i < recordCount; i++)
        {
            int offset = i * config.RecordSize;
            int progNr = BitConverter.ToUInt16(data, offset + config.ProgNrOffset);

            if (progNr == 0 || !IsChannelActive(data, offset, config))
                continue;

            string name = Encoding.BigEndianUnicode
                .GetString(data, offset + config.NameOffset, config.NameLen)
                .TrimEnd('\0');

            channels.Add((i, progNr, name));
        }

        if (channels.Count == 0)
            return 0;

        // Sort by current program number
        channels.Sort((a, b) => a.progNr.CompareTo(b.progNr));

        // Separate channels to keep (below startFrom) and channels to renumber
        var toKeep = channels.Where(c => c.progNr < startFrom).ToList();
        var toRenumber = channels.Where(c => c.progNr >= startFrom).ToList();

        // Assign new sequential numbers starting from startFrom
        int nextNr = startFrom;
        int changed = 0;

        foreach (var (recordIndex, oldProgNr, name) in toRenumber)
        {
            int offset = recordIndex * config.RecordSize;

            if (oldProgNr != nextNr)
            {
                // Update program number
                data[offset + config.ProgNrOffset] = (byte)(nextNr & 0xFF);
                data[offset + config.ProgNrOffset + 1] = (byte)((nextNr >> 8) & 0xFF);

                // Update checksum
                UpdateChecksum(data, offset, config.ChecksumOffset);
                changed++;
            }

            nextNr++;
        }

        if (changed > 0)
        {
            File.WriteAllBytes(filePath, data);
            Console.WriteLine($"{fileName}: Renumbered {changed} channels");
        }

        return channels.Count;
    }

    static int ExportChannels(string scmPath, string tsvPath)
    {
        if (!File.Exists(scmPath))
        {
            Console.WriteLine($"Error: File not found: {scmPath}");
            return 1;
        }

        string tempDir = Path.Combine(Path.GetTempPath(), "SamsungChannelCli_" + Path.GetRandomFileName());

        try
        {
            Directory.CreateDirectory(tempDir);
            ZipFile.ExtractToDirectory(scmPath, tempDir);

            var allChannels = new List<(int progNr, string name, string source, int recordIndex)>();

            // Collect channels from all files
            CollectChannelsForExport(tempDir, "map-SateD", ChannelType.Satellite, allChannels);
            CollectChannelsForExport(tempDir, "map-CableD", ChannelType.DvbCT, allChannels);
            CollectChannelsForExport(tempDir, "map-AirD", ChannelType.DvbCT, allChannels);

            // Sort by program number
            allChannels.Sort((a, b) => a.progNr.CompareTo(b.progNr));

            // Write TSV file
            using var writer = new StreamWriter(tsvPath, false, Encoding.UTF8);
            writer.WriteLine("Number\tName\tSource\tRecordIndex");
            foreach (var (progNr, name, source, recordIndex) in allChannels)
            {
                writer.WriteLine($"{progNr}\t{name}\t{source}\t{recordIndex}");
            }

            Console.WriteLine($"Exported {allChannels.Count} channels to {tsvPath}");
            Console.WriteLine();
            Console.WriteLine("Edit the file in a text editor:");
            Console.WriteLine("  - Reorder lines to change channel order");
            Console.WriteLine("  - The new channel number will be the line number (starting from 1)");
            Console.WriteLine("  - Do NOT modify the Source or RecordIndex columns");
            Console.WriteLine();
            Console.WriteLine($"Then run: SamsungChannelCli import \"{scmPath}\" \"{tsvPath}\"");

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    static void CollectChannelsForExport(string tempDir, string fileName, ChannelType type,
        List<(int progNr, string name, string source, int recordIndex)> channels)
    {
        string filePath = Path.Combine(tempDir, fileName);
        if (!File.Exists(filePath))
            return;

        byte[] data = File.ReadAllBytes(filePath);
        var config = GetChannelConfig(type);
        int recordCount = data.Length / config.RecordSize;

        for (int i = 0; i < recordCount; i++)
        {
            int offset = i * config.RecordSize;
            int progNr = BitConverter.ToUInt16(data, offset + config.ProgNrOffset);

            if (progNr == 0 || !IsChannelActive(data, offset, config))
                continue;

            string name = Encoding.BigEndianUnicode
                .GetString(data, offset + config.NameOffset, config.NameLen)
                .TrimEnd('\0');

            channels.Add((progNr, name, fileName, i));
        }
    }

    static int ImportChannels(string scmPath, string tsvPath)
    {
        if (!File.Exists(scmPath))
        {
            Console.WriteLine($"Error: SCM file not found: {scmPath}");
            return 1;
        }

        if (!File.Exists(tsvPath))
        {
            Console.WriteLine($"Error: TSV file not found: {tsvPath}");
            return 1;
        }

        string tempDir = Path.Combine(Path.GetTempPath(), "SamsungChannelCli_" + Path.GetRandomFileName());

        try
        {
            Directory.CreateDirectory(tempDir);
            ZipFile.ExtractToDirectory(scmPath, tempDir);

            // Read TSV file and build a mapping: (source, recordIndex) -> newProgNr
            var channelMapping = new Dictionary<(string source, int recordIndex), int>();
            int lineNumber = 0;
            int newProgNr = 1;

            foreach (var line in File.ReadAllLines(tsvPath))
            {
                lineNumber++;
                if (lineNumber == 1) continue; // Skip header

                var parts = line.Split('\t');
                if (parts.Length < 4)
                {
                    Console.WriteLine($"Warning: Skipping invalid line {lineNumber}");
                    continue;
                }

                string source = parts[2];
                if (!int.TryParse(parts[3], out int recordIndex))
                {
                    Console.WriteLine($"Warning: Invalid record index on line {lineNumber}");
                    continue;
                }

                channelMapping[(source, recordIndex)] = newProgNr++;
            }

            // Apply the new program numbers
            int updated = 0;
            updated += ApplyImportToFile(tempDir, "map-SateD", ChannelType.Satellite, channelMapping);
            updated += ApplyImportToFile(tempDir, "map-CableD", ChannelType.DvbCT, channelMapping);
            updated += ApplyImportToFile(tempDir, "map-AirD", ChannelType.DvbCT, channelMapping);

            // Create backup if not exists
            string backupPath = scmPath + ".backup";
            if (!File.Exists(backupPath))
            {
                File.Copy(scmPath, backupPath);
                Console.WriteLine($"Backup created: {backupPath}");
            }

            // Repack the .scm file
            File.Delete(scmPath);
            ZipFile.CreateFromDirectory(tempDir, scmPath, CompressionLevel.Optimal, false);

            Console.WriteLine($"Successfully updated {updated} channels from {tsvPath}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    static int ApplyImportToFile(string tempDir, string fileName, ChannelType type,
        Dictionary<(string source, int recordIndex), int> channelMapping)
    {
        string filePath = Path.Combine(tempDir, fileName);
        if (!File.Exists(filePath))
            return 0;

        byte[] data = File.ReadAllBytes(filePath);
        var config = GetChannelConfig(type);
        int recordCount = data.Length / config.RecordSize;
        int updated = 0;

        for (int i = 0; i < recordCount; i++)
        {
            int offset = i * config.RecordSize;
            int progNr = BitConverter.ToUInt16(data, offset + config.ProgNrOffset);

            if (progNr == 0 || !IsChannelActive(data, offset, config))
                continue;

            if (channelMapping.TryGetValue((fileName, i), out int newProgNr))
            {
                if (newProgNr != progNr)
                {
                    // Update program number
                    data[offset + config.ProgNrOffset] = (byte)(newProgNr & 0xFF);
                    data[offset + config.ProgNrOffset + 1] = (byte)((newProgNr >> 8) & 0xFF);

                    // Update checksum
                    UpdateChecksum(data, offset, config.ChecksumOffset);
                    updated++;
                }
            }
        }

        if (updated > 0)
        {
            File.WriteAllBytes(filePath, data);
        }

        return updated;
    }

    static bool MoveChannelInFile(string tempDir, string fileName, ChannelType type, int fromNr, int toNr)
    {
        string filePath = Path.Combine(tempDir, fileName);
        if (!File.Exists(filePath))
            return false;

        byte[] data = File.ReadAllBytes(filePath);
        var config = GetChannelConfig(type);
        int recordCount = data.Length / config.RecordSize;

        // Find the record with the source program number
        int sourceRecordIndex = -1;
        string? channelName = null;

        for (int i = 0; i < recordCount; i++)
        {
            int offset = i * config.RecordSize;
            int progNr = BitConverter.ToUInt16(data, offset + config.ProgNrOffset);

            if (progNr == fromNr && IsChannelActive(data, offset, config))
            {
                sourceRecordIndex = i;
                channelName = Encoding.BigEndianUnicode
                    .GetString(data, offset + config.NameOffset, config.NameLen)
                    .TrimEnd('\0');
                break;
            }
        }

        if (sourceRecordIndex == -1)
            return false;

        Console.WriteLine($"Found: {fromNr}: {channelName} in {fileName}");

        // Update program numbers
        int minNr = Math.Min(fromNr, toNr);
        int maxNr = Math.Max(fromNr, toNr);

        for (int i = 0; i < recordCount; i++)
        {
            int offset = i * config.RecordSize;
            int progNr = BitConverter.ToUInt16(data, offset + config.ProgNrOffset);

            if (progNr == 0 || !IsChannelActive(data, offset, config))
                continue;

            int newProgNr = progNr;

            if (i == sourceRecordIndex)
            {
                // This is the channel we're moving
                newProgNr = toNr;
            }
            else if (progNr >= minNr && progNr <= maxNr)
            {
                // This channel needs to shift
                if (fromNr > toNr)
                {
                    // Moving up: channels between target and source shift down
                    newProgNr = progNr + 1;
                }
                else
                {
                    // Moving down: channels between source and target shift up
                    newProgNr = progNr - 1;
                }
            }

            if (newProgNr != progNr)
            {
                // Update program number (little-endian 16-bit)
                data[offset + config.ProgNrOffset] = (byte)(newProgNr & 0xFF);
                data[offset + config.ProgNrOffset + 1] = (byte)((newProgNr >> 8) & 0xFF);

                // Update checksum
                UpdateChecksum(data, offset, config.ChecksumOffset);
            }
        }

        File.WriteAllBytes(filePath, data);
        return true;
    }

    static bool IsChannelActive(byte[] data, int offset, ChannelConfig config)
    {
        if (config.DeletedOffset.HasValue)
        {
            if ((data[offset + config.DeletedOffset.Value] & 0x01) != 0)
                return false;
        }
        if (config.InUseOffset.HasValue)
        {
            if ((data[offset + config.InUseOffset.Value] & 0x01) == 0)
                return false;
        }
        return true;
    }

    static void UpdateChecksum(byte[] data, int recordOffset, int checksumOffset)
    {
        byte crc = 0;
        for (int i = recordOffset; i < recordOffset + checksumOffset; i++)
        {
            crc += data[i];
        }
        data[recordOffset + checksumOffset] = crc;
    }

    enum ChannelType { DvbCT, Satellite, Analog }

    record ChannelConfig(
        int RecordSize,
        int ProgNrOffset,
        int NameOffset,
        int NameLen,
        int ChecksumOffset,
        int? DeletedOffset,
        int? InUseOffset
    );

    static ChannelConfig GetChannelConfig(ChannelType type)
    {
        return type switch
        {
            ChannelType.DvbCT => new ChannelConfig(320, 0, 64, 100, 319, 8, null),
            ChannelType.Satellite => new ChannelConfig(168, 0, 36, 100, 167, null, 7),
            ChannelType.Analog => new ChannelConfig(64, 9, 20, 10, 63, null, 1),
            _ => throw new ArgumentException($"Unknown channel type: {type}")
        };
    }

    static void ReadAndDisplayChannels(string tempDir, string fileName, string displayName, ChannelType type)
    {
        string filePath = Path.Combine(tempDir, fileName);
        if (!File.Exists(filePath))
            return;

        byte[] data = File.ReadAllBytes(filePath);
        var config = GetChannelConfig(type);
        int recordCount = data.Length / config.RecordSize;
        var channels = new List<(int progNr, string name, bool encrypted, bool locked, bool hidden)>();

        for (int i = 0; i < recordCount; i++)
        {
            int offset = i * config.RecordSize;
            int progNr = type == ChannelType.Analog
                ? data[offset + config.ProgNrOffset]
                : BitConverter.ToUInt16(data, offset + config.ProgNrOffset);

            if (progNr == 0)
                continue;

            if (!IsChannelActive(data, offset, config))
                continue;

            string name = Encoding.BigEndianUnicode
                .GetString(data, offset + config.NameOffset, config.NameLen)
                .TrimEnd('\0');

            bool encrypted = false, locked = false, hidden = false;
            if (type == ChannelType.DvbCT)
            {
                encrypted = (data[offset + 24] & 0x01) != 0;
                hidden = data[offset + 25] != 0;
                locked = (data[offset + 31] & 0x01) != 0;
            }
            else if (type == ChannelType.Satellite)
            {
                encrypted = (data[offset + 136] & 0x01) != 0;
                locked = (data[offset + 13] & 0x01) != 0;
            }

            channels.Add((progNr, name, encrypted, locked, hidden));
        }

        if (channels.Count == 0)
            return;

        Console.WriteLine();
        Console.WriteLine($"=== {displayName} ({fileName}) - {channels.Count} channels ===");
        Console.WriteLine();

        channels.Sort((a, b) => a.progNr.CompareTo(b.progNr));

        foreach (var (progNr, name, encrypted, locked, hidden) in channels)
        {
            var flags = new List<string>();
            if (encrypted) flags.Add("$");
            if (locked) flags.Add("L");
            if (hidden) flags.Add("H");

            string flagStr = flags.Count > 0 ? $" [{string.Join("", flags)}]" : "";
            Console.WriteLine($"  {progNr,4}: {name}{flagStr}");
        }
    }
}
