﻿using RstbLibrary.Core;
using System.Text.Json;

namespace RstbLibrary;

public enum Endianness
{
    Big, Little
}

public class RSTB
{
    public SortedDictionary<uint, uint> CrcMap { get; set; } = new();
    public SortedDictionary<string, uint> NameMap { get; set; } = new();

    public static RSTB FromBinary(ReadOnlySpan<byte> data, Endianness endian = Endianness.Little)
    {
        RSTB rstb = new();
        RstbHeader header = new(data, endian);

        for (int i = 0; i < header.CrcMapCount; i++) {
            RstbCrcTableEntry entry = new(data, 22 + (8 * i), endian);
            rstb.CrcMap.Add(entry.hash, entry.size);
        }

        for (int i = 0; i < header.NameMapCount; i++) {
            RstbNameTableEntry entry = new(data, 22 + (header.CrcMapCount * 8) + ((header.StringBlockSize + 4) * i), header.StringBlockSize, endian);
            if (entry.GetManagedName() is string key) {
                rstb.NameMap.Add(key, entry.size);
            }
        }

        return rstb;
    }

    public static RSTB FromText(string text)
    {
        return JsonSerializer.Deserialize<RSTB>(text)
            ?? throw new InvalidDataException("Invalid source json, the deserializer returned null");
    }

    public Span<byte> ToBinary(Endianness endian = Endianness.Little)
    {
        RstbHeader header = new(1, 160, CrcMap.Count, NameMap.Count);

        Span<byte> data = new byte[header.GetBufferSize()];
        header.Write(data, offset: 0, endian);

        int writeOffset = 22;

        foreach ((uint hash, uint size) in CrcMap) {
            RstbCrcTableEntry.Write(hash, size, data, writeOffset, endian);
            writeOffset += 8; // sizeof(RstbCrcTableEntry)
        }

        foreach ((string name, uint size) in NameMap) {
            RstbNameTableEntry.Write(name, size, data, writeOffset, 160, endian);
            writeOffset += 164; // sizeof(RstbNameTableEntry)
        }

        return data;
    }

    public string ToText(bool formatJson = false)
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions() {
            WriteIndented = formatJson
        });
    }
}
