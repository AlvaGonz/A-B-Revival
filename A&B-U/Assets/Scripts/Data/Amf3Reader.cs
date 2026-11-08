using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace ABRevival.Core.Data
{
    /// <summary>
    /// Minimal AMF3 reader shared by the frozen-data importers (level scripts, craft
    /// definitions). Covers the markers used by those payloads and performs no Unity calls.
    /// Original game IP: Vargames (2011).
    /// </summary>
    internal sealed class Amf3Reader
    {
        private readonly byte[] _data;
        private readonly List<string> _strings = new List<string>();
        private readonly List<object> _objects = new List<object>();
        private readonly List<Amf3Traits> _traits = new List<Amf3Traits>();
        private int _position;

        public Amf3Reader(byte[] data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public object ReadValue()
        {
            int marker = ReadByte();
            switch (marker)
            {
                case 0x00: // undefined
                case 0x01: // null
                    return null;
                case 0x02: return false;
                case 0x03: return true;
                case 0x04: return ReadInt();
                case 0x05: return ReadDouble();
                case 0x06: return ReadString();
                case 0x09: return ReadArray();
                case 0x0A: return ReadObject();
                default:
                    throw new NotSupportedException(
                        $"AMF3 marker 0x{marker:X2} at offset {_position - 1} is not present in frozen data.");
            }
        }

        private object ReadArray()
        {
            uint value = ReadU29();
            if ((value & 1) == 0) return _objects[(int)(value >> 1)];

            int count = (int)(value >> 1);
            var list = new List<object>(count);
            _objects.Add(list);

            // AMF3 arrays put associative entries before the dense entries; frozen data has none.
            string key = ReadString();
            if (key.Length != 0)
                throw new NotSupportedException("Associative AMF3 arrays are not used by frozen data.");

            for (int i = 0; i < count; i++) list.Add(ReadValue());
            return list;
        }

        private object ReadObject()
        {
            uint value = ReadU29();
            if ((value & 1) == 0) return _objects[(int)(value >> 1)];

            value >>= 1;
            Amf3Traits traits;
            if ((value & 1) != 0)
            {
                value >>= 1;
                ReadString(); // class name; anonymous in these payloads
                int encoding = (int)(value & 3);
                int sealedCount = (int)(value >> 2);
                var names = new string[sealedCount];
                for (int i = 0; i < sealedCount; i++) names[i] = ReadString();
                traits = new Amf3Traits(encoding, names);
                _traits.Add(traits);
            }
            else
            {
                traits = _traits[(int)(value >> 1)];
            }

            if (traits.Encoding != 0 && traits.Encoding != 2)
                throw new NotSupportedException("Externalizable AMF3 objects are not used by frozen data.");

            var result = new Dictionary<string, object>(traits.Names.Length + 4);
            _objects.Add(result);

            for (int i = 0; i < traits.Names.Length; i++)
                result[traits.Names[i]] = ReadValue();

            if (traits.Encoding == 2)
            {
                while (true)
                {
                    string key = ReadString();
                    if (key.Length == 0) break;
                    result[key] = ReadValue();
                }
            }

            return result;
        }

        private string ReadString()
        {
            uint value = ReadU29();
            if ((value & 1) == 0) return _strings[(int)(value >> 1)];

            int length = (int)(value >> 1);
            if (length == 0) return string.Empty;

            string result = Encoding.UTF8.GetString(_data, _position, length);
            _position += length;
            _strings.Add(result);
            return result;
        }

        private int ReadInt()
        {
            uint value = ReadU29();
            if ((value & 0x10000000u) != 0) return (int)(value | 0xE0000000u);
            return (int)value;
        }

        private double ReadDouble()
        {
            var bytes = new byte[8];
            Array.Copy(_data, _position, bytes, 0, bytes.Length);
            _position += bytes.Length;
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return BitConverter.ToDouble(bytes, 0);
        }

        private uint ReadU29()
        {
            uint value = 0;
            for (int i = 0; i < 3; i++)
            {
                byte next = ReadByte();
                if ((next & 0x80) != 0)
                {
                    value = (value << 7) | (uint)(next & 0x7F);
                }
                else
                {
                    return (value << 7) | next;
                }
            }
            return (value << 8) | ReadByte();
        }

        private byte ReadByte()
        {
            if (_position >= _data.Length)
                throw new EndOfStreamException("Unexpected end of AMF3 payload.");
            return _data[_position++];
        }

        private readonly struct Amf3Traits
        {
            public readonly int Encoding;
            public readonly string[] Names;

            public Amf3Traits(int encoding, string[] names)
            {
                Encoding = encoding;
                Names = names;
            }
        }
    }

    /// <summary>
    /// Zlib decompression shared by the frozen AMF3 payload readers.
    /// </summary>
    internal static class ZlibPayload
    {
        public static byte[] Inflate(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.Length < 6) throw new InvalidDataException("Compressed payload is too short.");
            int cmf = data[0];
            int flg = data[1];
            if ((cmf & 0x0F) != 8) throw new InvalidDataException("Payload is not zlib-deflated.");
            if ((cmf * 256 + flg) % 31 != 0) throw new InvalidDataException("Payload has an invalid zlib header.");

            // Skip the two-byte zlib header and four-byte Adler-32 trailer; DeflateStream reads the raw stream.
            using (var payload = new MemoryStream(data, 2, data.Length - 6, writable: false))
            using (var deflate = new DeflateStream(payload, CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                deflate.CopyTo(output);
                return output.ToArray();
            }
        }
    }
}
