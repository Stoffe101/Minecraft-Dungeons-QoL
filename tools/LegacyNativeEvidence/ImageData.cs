using System.Buffers.Binary;
using LegacyNativeEvidence;
static class ImageData
{
    public static List<Region> Read(IMemory memory, ulong image, int imageSize) {
        if (imageSize < 4096 || imageSize > 512 * 1024 * 1024) throw new ReadFailure("Invalid image size.");
        var header = memory.Read(image, 4096);
        if (BinaryPrimitives.ReadUInt16LittleEndian(header) != 0x5a4d) throw new ReadFailure("Invalid image header.");
        int pe = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(60));
        if (pe < 64 || pe > 3072 || BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(pe)) != 0x4550 || BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(pe + 4)) != 0x8664) throw new ReadFailure("Expected x64 PE image.");
        int count = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(pe + 6)), optional = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(pe + 20)); int start = pe + 24 + optional;
        if (count < 1 || count > 64 || start + count * 40 > header.Length) throw new ReadFailure("Invalid section table.");
        var regions = new List<Region>(); long total = 0;
        for (int i = 0; i < count; i++) {
            var section = header.AsSpan(start + i * 40, 40); uint flags = BinaryPrimitives.ReadUInt32LittleEndian(section[36..]);
            if ((flags & 0x80000000) == 0 || (flags & 0x20000000) != 0) continue; // writable, non-executable data
            int size = BinaryPrimitives.ReadInt32LittleEndian(section[8..]), rva = BinaryPrimitives.ReadInt32LittleEndian(section[12..]);
            if (size < 0 || rva < 0 || rva > imageSize - size || (total += size) > 64 * 1024 * 1024) throw new ReadFailure("Image data exceeds bounded scan.");
            for (int offset = 0; offset < size; offset += 1024 * 1024 - 4096) {
                int n = Math.Min(1024 * 1024, size - offset); if (n < 32) break;
                regions.Add(new Region(image + (ulong)rva + (ulong)offset, memory.Read(image + (ulong)rva + (ulong)offset, n)));
            }
        }
        if (regions.Count == 0) throw new ReadFailure("No readable image data sections."); return regions;
    }
}
