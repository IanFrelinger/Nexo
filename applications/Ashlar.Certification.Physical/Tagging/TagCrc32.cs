namespace Ashlar.Certification.Physical.Tagging;

/// <summary>
/// CRC-32 (IEEE polynomial) for tag payload integrity (not a signature substitute).
/// </summary>
internal static class TagCrc32
{
    private static readonly uint[] Table = CreateTable();

    public static uint Compute(ReadOnlySpan<byte> data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in data)
        {
            crc = Table[(crc ^ b) & 0xFF] ^ (crc >> 8);
        }

        return crc ^ 0xFFFFFFFFu;
    }

    private static uint[] CreateTable()
    {
        const uint polynomial = 0xEDB88320u;
        var table = new uint[256];
        for (var i = 0; i < 256; i++)
        {
            var entry = (uint)i;
            for (var j = 0; j < 8; j++)
            {
                entry = (entry & 1) == 1 ? polynomial ^ (entry >> 1) : entry >> 1;
            }

            table[i] = entry;
        }

        return table;
    }
}
