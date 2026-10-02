using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace YMI_PMT_PayrollManagement_API.Services
{
    /// <summary>
    /// Pure C# PNG parser & decoder that decompresses PNG pixel data (RGB/RGBA)
    /// into raw uncompressed/zlib RGB bytes for PDF Image XObject embedding.
    /// Requires zero external NuGet or OS drawing packages.
    /// </summary>
    public static class PngHelper
    {
        public class DecodedPng
        {
            public int Width { get; set; }
            public int Height { get; set; }
            public byte[] RgbData { get; set; } = Array.Empty<byte>();
            public byte[] CompressedRgb { get; set; } = Array.Empty<byte>();
        }

        public static DecodedPng? TryLoadAndDecode(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return null;
                byte[] bytes = File.ReadAllBytes(filePath);
                return Decode(bytes);
            }
            catch
            {
                return null;
            }
        }

        public static DecodedPng Decode(byte[] pngBytes)
        {
            if (pngBytes == null || pngBytes.Length < 33)
                throw new InvalidDataException("Invalid PNG buffer size.");

            // Verify PNG Signature: 0x89, 'P', 'N', 'G', '\r', '\n', 0x1A, '\n'
            if (pngBytes[0] != 0x89 || pngBytes[1] != 0x50 || pngBytes[2] != 0x4E || pngBytes[3] != 0x47 ||
                pngBytes[4] != 0x0D || pngBytes[5] != 0x0A || pngBytes[6] != 0x1A || pngBytes[7] != 0x0A)
            {
                throw new InvalidDataException("Invalid PNG signature header.");
            }

            int width = 0;
            int height = 0;
            int bitDepth = 0;
            int colorType = 0;

            using var idatStream = new MemoryStream();

            int offset = 8;
            while (offset + 8 <= pngBytes.Length)
            {
                int length = ReadInt32BigEndian(pngBytes, offset);
                string chunkType = Encoding.ASCII.GetString(pngBytes, offset + 4, 4);
                int dataOffset = offset + 8;

                if (chunkType == "IHDR")
                {
                    width = ReadInt32BigEndian(pngBytes, dataOffset);
                    height = ReadInt32BigEndian(pngBytes, dataOffset + 4);
                    bitDepth = pngBytes[dataOffset + 8];
                    colorType = pngBytes[dataOffset + 9];
                }
                else if (chunkType == "IDAT")
                {
                    idatStream.Write(pngBytes, dataOffset, length);
                }
                else if (chunkType == "IEND")
                {
                    break;
                }

                offset += 12 + length; // 4 (length) + 4 (type) + length (data) + 4 (crc)
            }

            if (width <= 0 || height <= 0 || bitDepth != 8)
            {
                throw new NotSupportedException($"Unsupported PNG: {width}x{height}, bitDepth={bitDepth}, colorType={colorType}");
            }

            // Decompress IDAT zlib stream
            idatStream.Position = 0;
            using var zlib = new ZLibStream(idatStream, CompressionMode.Decompress);
            using var decompressedMs = new MemoryStream();
            zlib.CopyTo(decompressedMs);
            byte[] raw = decompressedMs.ToArray();

            int bpp = (colorType == 6) ? 4 : (colorType == 2) ? 3 : 1;
            int stride = width * bpp;
            int expectedLen = height * (1 + stride);

            if (raw.Length < expectedLen)
            {
                throw new InvalidDataException($"Decompressed PNG data truncated: {raw.Length} < {expectedLen}");
            }

            byte[] currScanline = new byte[stride];
            byte[] prevScanline = new byte[stride];
            byte[] rgbData = new byte[width * height * 3];

            int rawIdx = 0;
            int outIdx = 0;

            for (int y = 0; y < height; y++)
            {
                byte filter = raw[rawIdx++];
                Array.Copy(raw, rawIdx, currScanline, 0, stride);
                rawIdx += stride;

                // Unfilter current scanline
                switch (filter)
                {
                    case 0: // None
                        break;
                    case 1: // Sub
                        for (int x = bpp; x < stride; x++)
                        {
                            currScanline[x] = (byte)(currScanline[x] + currScanline[x - bpp]);
                        }
                        break;
                    case 2: // Up
                        for (int x = 0; x < stride; x++)
                        {
                            currScanline[x] = (byte)(currScanline[x] + prevScanline[x]);
                        }
                        break;
                    case 3: // Average
                        for (int x = 0; x < stride; x++)
                        {
                            int left = (x >= bpp) ? currScanline[x - bpp] : 0;
                            int up = prevScanline[x];
                            currScanline[x] = (byte)(currScanline[x] + ((left + up) >> 1));
                        }
                        break;
                    case 4: // Paeth
                        for (int x = 0; x < stride; x++)
                        {
                            int left = (x >= bpp) ? currScanline[x - bpp] : 0;
                            int up = prevScanline[x];
                            int upLeft = (x >= bpp) ? prevScanline[x - bpp] : 0;
                            currScanline[x] = (byte)(currScanline[x] + PaethPredictor(left, up, upLeft));
                        }
                        break;
                }

                // Extract RGB into rgbData (composite alpha over white if RGBA)
                if (colorType == 6) // RGBA
                {
                    for (int x = 0; x < width; x++)
                    {
                        int src = x * 4;
                        byte r = currScanline[src];
                        byte g = currScanline[src + 1];
                        byte b = currScanline[src + 2];
                        byte a = currScanline[src + 3];

                        if (a == 255)
                        {
                            rgbData[outIdx++] = r;
                            rgbData[outIdx++] = g;
                            rgbData[outIdx++] = b;
                        }
                        else if (a == 0)
                        {
                            rgbData[outIdx++] = 255;
                            rgbData[outIdx++] = 255;
                            rgbData[outIdx++] = 255;
                        }
                        else
                        {
                            rgbData[outIdx++] = (byte)((r * a + 255 * (255 - a)) / 255);
                            rgbData[outIdx++] = (byte)((g * a + 255 * (255 - a)) / 255);
                            rgbData[outIdx++] = (byte)((b * a + 255 * (255 - a)) / 255);
                        }
                    }
                }
                else if (colorType == 2) // RGB
                {
                    Array.Copy(currScanline, 0, rgbData, outIdx, stride);
                    outIdx += stride;
                }

                Array.Copy(currScanline, prevScanline, stride);
            }

            // Compress RGB data using ZLibStream for PDF FlateDecode
            using var compMs = new MemoryStream();
            using (var zlibOut = new ZLibStream(compMs, CompressionMode.Compress, true))
            {
                zlibOut.Write(rgbData, 0, rgbData.Length);
            }
            byte[] compressedRgb = compMs.ToArray();

            return new DecodedPng
            {
                Width = width,
                Height = height,
                RgbData = rgbData,
                CompressedRgb = compressedRgb
            };
        }

        private static byte PaethPredictor(int a, int b, int c)
        {
            int p = a + b - c;
            int pa = Math.Abs(p - a);
            int pb = Math.Abs(p - b);
            int pc = Math.Abs(p - c);

            if (pa <= pb && pa <= pc) return (byte)a;
            if (pb <= pc) return (byte)b;
            return (byte)c;
        }

        private static int ReadInt32BigEndian(byte[] data, int offset)
        {
            return (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];
        }
    }
}
