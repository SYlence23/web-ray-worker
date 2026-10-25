using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace RayTracer
{
    public static class PngEncoder
    {
        private static readonly byte[] Signature = [137, 80, 78, 71, 13, 10, 26, 10];
        private static readonly uint[] CrcTable = BuildCrcTable();

        public static byte[] Encode(ImageBuffer image)
        {
            using var output = new MemoryStream();
            output.Write(Signature);

            var header = new byte[13];
            BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(0), image.Width);
            BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), image.Height);
            header[8] = 8; 
            header[9] = 2; 
            WriteChunk(output, "IHDR", header);

            WriteChunk(output, "IDAT", Compress(image));
            WriteChunk(output, "IEND", [])
                ;

            return output.ToArray();
        }

        private static byte[] Compress(ImageBuffer image)
        {
            var rowLength = image.Width * 3;
            using var compressed = new MemoryStream();
            using (var zlib = new ZLibStream(compressed, CompressionLevel.Fastest))
            {
                for (var y = 0; y < image.Height; y++)
                {
                    zlib.WriteByte(0); 
                    zlib.Write(image.Pixels, y * rowLength, rowLength);
                }
            }

            return compressed.ToArray();
        }

        private static void WriteChunk(Stream output, string type, byte[] data)
        {
            Span<byte> buffer = stackalloc byte[4];

            BinaryPrimitives.WriteInt32BigEndian(buffer, data.Length);
            output.Write(buffer);

            var typeBytes = Encoding.ASCII.GetBytes(type);
            output.Write(typeBytes);
            output.Write(data);

            var crc = UpdateCrc(0xFFFFFFFF, typeBytes);
            crc = UpdateCrc(crc, data) ^ 0xFFFFFFFF;
            BinaryPrimitives.WriteUInt32BigEndian(buffer, crc);
            output.Write(buffer);
        }

        private static uint UpdateCrc(uint crc, byte[] data)
        {
            foreach (var b in data)
            {
                crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
            }

            return crc;
        }

        private static uint[] BuildCrcTable()
        {
            var table = new uint[256];
            for (uint n = 0; n < 256; n++)
            {
                var c = n;
                for (var k = 0; k < 8; k++)
                {
                    c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
                }

                table[n] = c;
            }

            return table;
        }
    }
}
