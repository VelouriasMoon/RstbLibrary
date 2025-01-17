﻿using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace RstbLibrary.Core;

[StructLayout(LayoutKind.Sequential, Size = 12)]
internal readonly ref struct RstbHeader
{
    private readonly ReadOnlySpan<byte> _magic;
    private readonly int _version;
    private readonly int _stringBlockSize;
    private readonly int _crcMapCount;
    private readonly int _nameMapCount;

    public int Version => _version;
    public int StringBlockSize => _stringBlockSize;
    public int CrcMapCount => _crcMapCount;
    public int NameMapCount => _nameMapCount;

    public int GetBufferSize()
    {
        int headerSize = 22; // Marshal.SizeOf<RstbHeader>();
        int crcEntrySize = 8; // Marshal.SizeOf<RstbCrcTableEntry>();
        int nameEntrySize = _stringBlockSize + 4; // Marshal.SizeOf<RstbNameTableEntry>();

        return headerSize + (_crcMapCount * crcEntrySize) + (_nameMapCount * nameEntrySize);
    }

    public unsafe void Write(Span<byte> data, int offset, Endianness endian)
    {
        Span<byte> sub = data[offset..];
        // 0x52535442 == "RSTB"u8
        BinaryPrimitives.WriteUInt64LittleEndian(sub[0..8], 0x4C4254534552);

        if (endian == Endianness.Big) {
            BinaryPrimitives.WriteInt32BigEndian(data[6..10], _version);
            BinaryPrimitives.WriteInt32BigEndian(data[10..14], _stringBlockSize);
            BinaryPrimitives.WriteInt32BigEndian(sub[14..18], _crcMapCount);
            BinaryPrimitives.WriteInt32BigEndian(sub[18..22], _nameMapCount);
        }
        else {
            BinaryPrimitives.WriteInt32LittleEndian(data[6..10], _version);
            BinaryPrimitives.WriteInt32LittleEndian(data[10..14], _stringBlockSize);
            BinaryPrimitives.WriteInt32LittleEndian(sub[14..18], _crcMapCount);
            BinaryPrimitives.WriteInt32LittleEndian(sub[18..22], _nameMapCount);
        }
    }

    public RstbHeader(ReadOnlySpan<byte> data, Endianness endian)
    {
        _magic = data[0..6];

        if (!_magic.SequenceEqual("RESTBL"u8)) {
            throw new InvalidDataException("Invalid RESTBL magic");
        }

        if (endian == Endianness.Big) {
            _version = BinaryPrimitives.ReadInt32BigEndian(data[6..10]);
            _stringBlockSize = BinaryPrimitives.ReadInt32BigEndian(data[10..14]);
            _crcMapCount = BinaryPrimitives.ReadInt32BigEndian(data[14..18]);
            _nameMapCount = BinaryPrimitives.ReadInt32BigEndian(data[18..22]);
        }
        else {
            _version = BinaryPrimitives.ReadInt32LittleEndian(data[6..10]);
            _stringBlockSize = BinaryPrimitives.ReadInt32LittleEndian(data[10..14]);
            _crcMapCount = BinaryPrimitives.ReadInt32LittleEndian(data[14..18]);
            _nameMapCount = BinaryPrimitives.ReadInt32LittleEndian(data[18..22]);
        }
    }

    public RstbHeader(int version, int stringBlockSize, int crcMapCount, int nameMapCount)
    {
        _magic = "RESTBL"u8;
        _version = version;
        _stringBlockSize = stringBlockSize;
        _crcMapCount = crcMapCount;
        _nameMapCount = nameMapCount;
    }
}
