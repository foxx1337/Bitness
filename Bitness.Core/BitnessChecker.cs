using System.Reflection.PortableExecutable;

namespace Bitness.Core;

/// <summary>
/// Utility for identifying the platform architecture of Windows PE files (executables and DLLs).
/// </summary>
public static class BitnessChecker
{
    /// <summary>
    /// Gets a human-readable string describing the machine type of the specified PE file.
    /// </summary>
    /// <param name="filePath">Path to the PE file to analyze</param>
    /// <returns>A string describing the architecture (e.g., "32-bit (x86)", "64-bit (x64)", "ARM64", "ARM64EC", "ARM64X (Hybrid)")</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist</exception>
    /// <exception cref="InvalidDataException">Thrown when the file is not a valid PE file</exception>
    /// <exception cref="IOException">Thrown when there's an error reading the file</exception>
    public static string GetMachineTypeString(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        try
        {
            using var fileStream = File.OpenRead(filePath);
            using var peReader = new PEReader(fileStream);
            
            var machine = peReader.PEHeaders.CoffHeader.Machine;
            var machineValue = (ushort)machine;

            var machineTypeString = machineValue switch
            {
                0x014C => "32-bit (x86)",      // IMAGE_FILE_MACHINE_I386
                0x8664 => "64-bit (x64)",      // IMAGE_FILE_MACHINE_AMD64
                0xAA64 => "ARM64",             // IMAGE_FILE_MACHINE_ARM64
                _ => $"Unknown (0x{machineValue:X4})"
            };

            if (machineValue is 0x8664 or 0xAA64)
            {
                var loadConfigDir = peReader.PEHeaders.PEHeader?.LoadConfigTableDirectory;
                if (loadConfigDir is not null && loadConfigDir.Value.RelativeVirtualAddress != 0 &&
                    loadConfigDir.Value.Size != 0)
                {
                    var config = ReadImageLoadConfigDirectory(peReader.PEHeaders, fileStream);
                    bool hasChpeMetadata = config.ChpeMetadata != 0;
                    bool hasDynamicRelocTable = config.DynamicRelocTable != 0;
                    if (hasChpeMetadata || hasDynamicRelocTable)
                    {
                        machineTypeString = machineValue == 0x8664
                            ? "ARM64EC"
                            // 0xAA64
                            : "ARM64X (Hybrid)";
                    }
                }
            }

            return machineTypeString;
        }
        catch (BadImageFormatException ex)
        {
            throw new InvalidDataException($"Not a valid PE file: {filePath}", ex);
        }
        catch (Exception ex) when (ex is not FileNotFoundException and not InvalidDataException)
        {
            throw new IOException($"Error reading file: {filePath}", ex);
        }
    }

    private static (ulong DynamicRelocTable, ulong ChpeMetadata)
        ReadImageLoadConfigDirectory(PEHeaders peHeaders, FileStream fileStream)
    {
        DirectoryEntry? loadConfigDir = peHeaders.PEHeader?.LoadConfigTableDirectory;
        if (loadConfigDir is null || loadConfigDir.Value.RelativeVirtualAddress == 0 ||
            loadConfigDir.Value.Size == 0)
        {
            // No Load Config Directory present
            return (0, 0);
        }

        if (!peHeaders.TryGetDirectoryOffset(loadConfigDir.Value, out int loadConfigOffset))
        {
            // Could not translate RVA (should not happen for a well-formed PE)
            return (0, 0);
        }

        byte[] buffer = new byte[loadConfigDir.Value.Size];
        fileStream.Seek(loadConfigOffset, SeekOrigin.Begin);
        fileStream.ReadExactly(buffer, 0, buffer.Length);

        // Parse the Load Config Directory
        ulong dynamicRelocTable = 0;
        ulong chpeMetadata = 0;

        if (loadConfigDir.Value.Size >= 4)
        {
            uint configSize = BitConverter.ToUInt32(buffer, 0);
            if (loadConfigDir.Value.Size >= 0xC8 &&
                configSize >= 0xC0) // 0xC0 = 192 bytes (IMAGE_LOAD_CONFIG_DIRECTORY64::DynamicValueRelocTable offset)
            {
                dynamicRelocTable = BitConverter.ToUInt64(buffer, 0xC0);
            }

            if (configSize >= 0xD0 && loadConfigDir.Value.Size >= 0xD0) // 0xC8 = 200 bytes (IMAGE_LOAD_CONFIG_DIRECTORY64::ChpeMetadata offset)
            {
                chpeMetadata = BitConverter.ToUInt64(buffer, 0xC8);
            }
        }

        return (dynamicRelocTable, chpeMetadata);
    }

    /// <summary>
    /// Gets the machine type enum value for the specified PE file.
    /// </summary>
    /// <param name="filePath">Path to the PE file to analyze</param>
    /// <returns>The Machine enum value from the PE header</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist</exception>
    /// <exception cref="InvalidDataException">Thrown when the file is not a valid PE file</exception>
    /// <exception cref="IOException">Thrown when there's an error reading the file</exception>
    public static Machine GetMachineType(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        try
        {
            using var fileStream = File.OpenRead(filePath);
            using var peReader = new PEReader(fileStream);
            
            return peReader.PEHeaders.CoffHeader.Machine;
        }
        catch (BadImageFormatException ex)
        {
            throw new InvalidDataException($"Not a valid PE file: {filePath}", ex);
        }
        catch (Exception ex) when (ex is not FileNotFoundException and not InvalidDataException)
        {
            throw new IOException($"Error reading file: {filePath}", ex);
        }
    }
}
