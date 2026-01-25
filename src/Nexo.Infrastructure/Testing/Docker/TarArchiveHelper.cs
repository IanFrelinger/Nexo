using System.IO.Compression;

namespace Nexo.Infrastructure.Testing.Docker;

/// <summary>
/// Helper for creating tar archives for Docker build contexts.
/// 
/// Creates a minimal tar archive containing the build context files.
/// </summary>
internal static class TarArchiveHelper
{
    /// <summary>
    /// Creates a tar.gz archive of the build context directory.
    /// </summary>
    public static Stream CreateBuildContextTar(string contextPath, string dockerfilePath)
    {
        var memoryStream = new MemoryStream();
        
        using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress, leaveOpen: true))
        {
            WriteTarArchive(gzipStream, contextPath, dockerfilePath);
        }
        
        memoryStream.Position = 0;
        return memoryStream;
    }

    private static void WriteTarArchive(Stream output, string contextPath, string dockerfilePath)
    {
        // Write Dockerfile
        var dockerfileRelativePath = Path.GetRelativePath(contextPath, dockerfilePath);
        WriteTarEntry(output, dockerfileRelativePath, File.ReadAllBytes(dockerfilePath));

        // Write essential files from context
        var essentialFiles = new[]
        {
            "*.sln",
            "*.props",
            "*.json",
            "Directory.Build.props",
            "Directory.Packages.props",
            "global.json"
        };

        foreach (var pattern in essentialFiles)
        {
            var files = Directory.GetFiles(contextPath, pattern, SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(contextPath, file);
                WriteTarEntry(output, relativePath, File.ReadAllBytes(file));
            }
        }

        // Write src directory structure (simplified - just key files)
        var srcPath = Path.Combine(contextPath, "src");
        if (Directory.Exists(srcPath))
        {
            WriteDirectoryRecursive(output, srcPath, contextPath);
        }

        // Write two null blocks to indicate end of tar
        var nullBlock = new byte[512];
        output.Write(nullBlock, 0, 512);
        output.Write(nullBlock, 0, 512);
    }

    private static void WriteDirectoryRecursive(Stream output, string directory, string basePath)
    {
        // Write directory entry
        var relativePath = Path.GetRelativePath(basePath, directory).Replace('\\', '/');
        if (!relativePath.EndsWith("/"))
        {
            relativePath += "/";
        }
        WriteTarEntry(output, relativePath, Array.Empty<byte>(), isDirectory: true);

        // Write files
        foreach (var file in Directory.GetFiles(directory))
        {
            var relativeFilePath = Path.GetRelativePath(basePath, file).Replace('\\', '/');
            WriteTarEntry(output, relativeFilePath, File.ReadAllBytes(file));
        }

        // Recurse into subdirectories
        foreach (var subdir in Directory.GetDirectories(directory))
        {
            WriteDirectoryRecursive(output, subdir, basePath);
        }
    }

    private static void WriteTarEntry(Stream output, string path, byte[] content, bool isDirectory = false)
    {
        // Tar header is 512 bytes
        var header = new byte[512];
        var pathBytes = System.Text.Encoding.UTF8.GetBytes(path);
        
        // Copy path (max 100 bytes)
        Array.Copy(pathBytes, 0, header, 0, Math.Min(pathBytes.Length, 100));
        
        // File mode (octal)
        var mode = isDirectory ? "040755" : "0100644";
        var modeBytes = System.Text.Encoding.ASCII.GetBytes(mode);
        Array.Copy(modeBytes, 0, header, 100, modeBytes.Length);
        
        // UID/GID (0)
        WriteOctal(header, 108, 0, 8);
        WriteOctal(header, 116, 0, 8);
        
        // Size
        WriteOctal(header, 124, content.Length, 12);
        
        // Modification time (now)
        WriteOctal(header, 136, (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds()), 12);
        
        // Type flag
        header[156] = (byte)(isDirectory ? '5' : '0');
        
        // Checksum (calculate after header is mostly complete)
        WriteOctal(header, 148, CalculateChecksum(header), 8);
        
        output.Write(header, 0, 512);
        
        // Write file content
        if (content.Length > 0)
        {
            output.Write(content, 0, content.Length);
            
            // Pad to 512-byte boundary
            var padding = (512 - (content.Length % 512)) % 512;
            if (padding > 0)
            {
                var padBytes = new byte[padding];
                output.Write(padBytes, 0, padding);
            }
        }
    }

    private static void WriteOctal(byte[] buffer, int offset, long value, int length)
    {
        var octal = Convert.ToString(value, 8).PadLeft(length - 1, '0') + " ";
        var octalBytes = System.Text.Encoding.ASCII.GetBytes(octal);
        Array.Copy(octalBytes, 0, buffer, offset, Math.Min(octalBytes.Length, length));
    }

    private static int CalculateChecksum(byte[] header)
    {
        var sum = 0;
        for (int i = 0; i < 512; i++)
        {
            if (i >= 148 && i < 156) // Skip checksum field
            {
                sum += 32; // Space
            }
            else
            {
                sum += header[i];
            }
        }
        return sum;
    }
}
