using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Assets_Editor
{
    public class Sprite
    {

        public Sprite()
        {
            this.ID = 0;
            this.Size = 0;
            this.CompressedPixels = null;
            this.Transparent = false;
            this.MemoryStream = new MemoryStream();
        }

        public const byte DefaultSize = 32;
        public const ushort RGBPixelsDataSize = 3072; // 32*32*3
        public const ushort ARGBPixelsDataSize = 4096; // 32*32*4

        public uint ID { get; set; }
        public uint Size { get; set; }
        public byte[] CompressedPixels { get; set; }
        public bool Transparent { get; set; }
        public MemoryStream MemoryStream { get; set; }

        private static byte[] BlankRGBSprite = new byte[RGBPixelsDataSize];
        private static byte[] BlankARGBSprite = new byte[ARGBPixelsDataSize];
        private static readonly Rectangle Rect = new Rectangle(0, 0, DefaultSize, DefaultSize);

        public byte[] GetPixels()
        {
            if (this.CompressedPixels == null || this.CompressedPixels.Length != this.Size)
            {
                return BlankARGBSprite;
            }

            int read = 0;
            int write = 0;
            int pos = 0;
            int transparentPixels = 0;
            int coloredPixels = 0;
            int length = this.CompressedPixels.Length;
            byte bitPerPixel = (byte)(this.Transparent ? 4 : 3);
            byte[] pixels = new byte[ARGBPixelsDataSize];

            for (read = 0; read < length; read += 4 + (bitPerPixel * coloredPixels))
            {
                transparentPixels = this.CompressedPixels[pos++] | this.CompressedPixels[pos++] << 8;
                coloredPixels = this.CompressedPixels[pos++] | this.CompressedPixels[pos++] << 8;

                if (write + (transparentPixels * 4) <= pixels.Length)
                {
                    for (int i = 0; i < transparentPixels; i++)
                    {
                        pixels[write++] = 0x00; // Blue
                        pixels[write++] = 0x00; // Green
                        pixels[write++] = 0x00; // Red
                        pixels[write++] = 0x00; // Alpha
                    }
                }
                else
                {
                    // Log an error message or handle the issue as appropriate
                }

                if (write + (coloredPixels * 4) <= pixels.Length && pos + (coloredPixels * (this.Transparent ? 4 : 3)) <= this.CompressedPixels.Length)
                {
                    for (int i = 0; i < coloredPixels; i++)
                    {
                        byte red = this.CompressedPixels[pos++];
                        byte green = this.CompressedPixels[pos++];
                        byte blue = this.CompressedPixels[pos++];
                        byte alpha = this.Transparent ? this.CompressedPixels[pos++] : (byte)0xFF;

                        pixels[write++] = blue;
                        pixels[write++] = green;
                        pixels[write++] = red;
                        pixels[write++] = alpha;
                    }
                }
                else
                {
                    // Log an error message or handle the issue as appropriate
                }
            }

            // Fills the remaining pixels
            while (write < ARGBPixelsDataSize)
            {
                pixels[write++] = 0x00; // Blue
                pixels[write++] = 0x00; // Green
                pixels[write++] = 0x00; // Red
                pixels[write++] = 0x00; // Alpha
            }

            return pixels;
        }

        public Bitmap GetBitmap()
        {
            Bitmap bitmap = new Bitmap(DefaultSize, DefaultSize, PixelFormat.Format32bppArgb);
            byte[] pixels = this.GetPixels();

            if (pixels != null)
            {
                BitmapData bitmapData = bitmap.LockBits(Rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);
                Marshal.Copy(pixels, 0, bitmapData.Scan0, pixels.Length);
                bitmap.UnlockBits(bitmapData);
            }

            return bitmap;
        }
        public static void CreateBlankSprite()
        {
            for (short i = 0; i < RGBPixelsDataSize; i++)
            {
                BlankRGBSprite[i] = 0x11;
            }

            for (short i = 0; i < ARGBPixelsDataSize; i++)
            {
                BlankARGBSprite[i] = 0x11;
            }
        }
        
        public static byte[] CompressBitmap(Bitmap bitmap, bool transparent)
        {
            if (bitmap == null)
            {
                throw new ArgumentNullException(nameof(bitmap));
            }

            BitmapData bitmapData = bitmap.LockBits(Rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            byte[] pixels = new byte[ARGBPixelsDataSize];
            Marshal.Copy(bitmapData.Scan0, pixels, 0, ARGBPixelsDataSize);
            bitmap.UnlockBits(bitmapData);

            return CompressPixelsBGRA(pixels, transparent);
        }
        public static byte[] CompressPixelsBGRA(byte[] pixels, bool transparent)
        {
            if (pixels == null)
            {
                throw new ArgumentNullException(nameof(pixels));
            }

            if (pixels.Length != ARGBPixelsDataSize)
            {
                throw new Exception("Invalid pixels data size");
            }

            byte[] compressedPixels;

            using (BinaryWriter writer = new BinaryWriter(new MemoryStream()))
            {
                int read = 0;
                int alphaCount = 0;
                ushort chunkSize = 0;
                long coloredPos = 0;
                long finishOffset = 0;
                int length = pixels.Length / 4;
                int index = 0;

                while (index < length)
                {
                    chunkSize = 0;

                    // Read transparent pixels
                    while (index < length)
                    {
                        read = (index * 4) + 3;

                        // alpha
                        if (pixels[read++] != 0)
                        {
                            break;
                        }

                        alphaCount++;
                        chunkSize++;
                        index++;
                    }

                    // Read colored pixels
                    if (alphaCount < length && index < length)
                    {
                        writer.Write(chunkSize); // Writes the length of the transparent pixels
                        coloredPos = writer.BaseStream.Position; // Save colored position
                        writer.BaseStream.Seek(2, SeekOrigin.Current); // Skip colored position
                        chunkSize = 0;

                        while (index < length)
                        {
                            read = index * 4;

                            byte blue = pixels[read++];
                            byte green = pixels[read++];
                            byte red = pixels[read++];
                            byte alpha = pixels[read++];

                            if (alpha == 0)
                            {
                                break;
                            }

                            writer.Write(red);
                            writer.Write(green);
                            writer.Write(blue);

                            if (transparent)
                            {
                                writer.Write(alpha);
                            }

                            chunkSize++;
                            index++;
                        }

                        finishOffset = writer.BaseStream.Position;
                        writer.BaseStream.Seek(coloredPos, SeekOrigin.Begin); // Go back to chunksize indicator
                        writer.Write(chunkSize); // Writes the length of he colored pixels
                        writer.BaseStream.Seek(finishOffset, SeekOrigin.Begin);
                    }
                }

                compressedPixels = ((MemoryStream)writer.BaseStream).ToArray();
            }

            return compressedPixels;
        }

        public static byte[] CompressPixelsARGB(byte[] pixels, bool transparent)
        {
            if (pixels == null)
            {
                throw new ArgumentNullException(nameof(pixels));
            }

            if (pixels.Length != ARGBPixelsDataSize)
            {
                throw new Exception("Invalid pixels data size");
            }

            byte[] compressedPixels;

            using (BinaryWriter writer = new BinaryWriter(new MemoryStream()))
            {
                int read = 0;
                int alphaCount = 0;
                ushort chunkSize = 0;
                long coloredPos = 0;
                long finishOffset = 0;
                int length = pixels.Length / 4;
                int index = 0;

                while (index < length)
                {
                    chunkSize = 0;

                    // Read transparent pixels
                    while (index < length)
                    {
                        read = index * 4;

                        byte alpha = pixels[read++];
                        read += 3;

                        if (alpha != 0)
                        {
                            break;
                        }

                        alphaCount++;
                        chunkSize++;
                        index++;
                    }

                    // Read colored pixels
                    if (alphaCount < length && index < length)
                    {
                        writer.Write(chunkSize); // Writes the length of the transparent pixels
                        coloredPos = writer.BaseStream.Position; // Save colored position
                        writer.BaseStream.Seek(2, SeekOrigin.Current); // Skip colored position
                        chunkSize = 0;

                        while (index < length)
                        {
                            read = index * 4;

                            byte alpha = pixels[read++];
                            byte red = pixels[read++];
                            byte green = pixels[read++];
                            byte blue = pixels[read++];

                            if (alpha == 0)
                            {
                                break;
                            }

                            writer.Write(red);
                            writer.Write(green);
                            writer.Write(blue);

                            if (transparent)
                            {
                                writer.Write(alpha);
                            }

                            chunkSize++;
                            index++;
                        }

                        finishOffset = writer.BaseStream.Position;
                        writer.BaseStream.Seek(coloredPos, SeekOrigin.Begin); // Go back to chunksize indicator
                        writer.Write(chunkSize); // Writes the length of he colored pixels
                        writer.BaseStream.Seek(finishOffset, SeekOrigin.Begin);
                    }
                }

                compressedPixels = ((MemoryStream)writer.BaseStream).ToArray();
            }

            return compressedPixels;
        }
        public static byte[] UncompressPixelsBGRA(byte[] compressedPixels, bool transparent)
        {
            if (compressedPixels == null)
            {
                throw new ArgumentNullException(nameof(compressedPixels));
            }

            int read = 0;
            int write = 0;
            int pos = 0;
            int transparentPixels = 0;
            int coloredPixels = 0;
            int length = compressedPixels.Length;
            byte bitPerPixel = (byte)(transparent ? 4 : 3);
            byte[] pixels = new byte[ARGBPixelsDataSize];

            for (read = 0; read < length; read += 4 + (bitPerPixel * coloredPixels))
            {
                transparentPixels = compressedPixels[pos++] | compressedPixels[pos++] << 8;
                coloredPixels = compressedPixels[pos++] | compressedPixels[pos++] << 8;

                for (int i = 0; i < transparentPixels; i++)
                {
                    pixels[write++] = 0x00; // Blue
                    pixels[write++] = 0x00; // Green
                    pixels[write++] = 0x00; // Red
                    pixels[write++] = 0x00; // Alpha
                }

                for (int i = 0; i < coloredPixels; i++)
                {
                    byte red = compressedPixels[pos++];
                    byte green = compressedPixels[pos++];
                    byte blue = compressedPixels[pos++];
                    byte alpha = transparent ? compressedPixels[pos++] : (byte)0xFF;

                    pixels[write++] = blue;
                    pixels[write++] = green;
                    pixels[write++] = red;
                    pixels[write++] = alpha;
                }
            }

            // Fills the remaining pixels
            while (write < ARGBPixelsDataSize)
            {
                pixels[write++] = 0x00; // Blue
                pixels[write++] = 0x00; // Green
                pixels[write++] = 0x00; // Red
                pixels[write++] = 0x00; // Alpha
            }

            return pixels;
        }
        public static byte[] UncompressPixelsARGB(byte[] compressedPixels, bool transparent)
        {
            if (compressedPixels == null)
            {
                throw new ArgumentNullException(nameof(compressedPixels));
            }

            int read = 0;
            int write = 0;
            int pos = 0;
            int transparentPixels = 0;
            int coloredPixels = 0;
            int length = compressedPixels.Length;
            byte bitPerPixel = (byte)(transparent ? 4 : 3);
            byte[] pixels = new byte[ARGBPixelsDataSize];

            for (read = 0; read < length; read += 4 + (bitPerPixel * coloredPixels))
            {
                transparentPixels = compressedPixels[pos++] | compressedPixels[pos++] << 8;
                coloredPixels = compressedPixels[pos++] | compressedPixels[pos++] << 8;

                for (int i = 0; i < transparentPixels; i++)
                {
                    pixels[write++] = 0x00; // alpha
                    pixels[write++] = 0x00; // red
                    pixels[write++] = 0x00; // green
                    pixels[write++] = 0x00; // blue
                }

                for (int i = 0; i < coloredPixels; i++)
                {
                    byte red = compressedPixels[pos++];
                    byte green = compressedPixels[pos++];
                    byte blue = compressedPixels[pos++];
                    byte alpha = transparent ? compressedPixels[pos++] : (byte)0xFF;

                    pixels[write++] = alpha;
                    pixels[write++] = red;
                    pixels[write++] = green;
                    pixels[write++] = blue;
                }
            }

            // Fills the remaining pixels
            while (write < ARGBPixelsDataSize)
            {
                pixels[write++] = 0x00; // alpha
                pixels[write++] = 0x00; // red
                pixels[write++] = 0x00; // green
                pixels[write++] = 0x00; // blue
            }

            return pixels;
        }

        public static void CompileSprites(string file, ConcurrentDictionary<int, MemoryStream> sprites, bool transparency, uint version)
        {

            using (BinaryWriter writer = new BinaryWriter(new FileStream(file, FileMode.Create)))
            {
                uint count = (uint)sprites.Count - 1; ;
                bool transparent = transparency;

                writer.Write(version);

                writer.Write(count);

                byte headSize = 8;
                int addressPosition = headSize;
                int address = (int)((count * 4) + headSize);
                byte[] bytes = null;

                for (uint id = 1; id <= count; id++)
                {

                    writer.Seek(addressPosition, SeekOrigin.Begin);

                    bytes = CompressBitmap(new Bitmap(sprites[(int)id]), transparent);

                    if (bytes.Length > 0)
                    {
                        // write address
                        writer.Write((uint)address);
                        writer.Seek(address, SeekOrigin.Begin);

                        // write colorkey
                        writer.Write((byte)0xFF); // red
                        writer.Write((byte)0x00); // blue
                        writer.Write((byte)0xFF); // green

                        // write sprite data size
                        writer.Write((short)bytes.Length);

                        if (bytes.Length > 0)
                        {
                            writer.Write(bytes);
                        }
                    }
                    else
                    {
                        writer.Write((uint)0);
                        writer.Seek(address, SeekOrigin.Begin);
                    }
                    address = (int)writer.BaseStream.Position;
                    addressPosition += 4;
                }
                writer.Close();
            }

        }

    }
    public class SpriteLoader
    {
        public SpriteLoader()
        {
            Signature = 0;
            Transparency = false;
        }

        public uint Signature;
        public bool Transparency;

        public bool ReadSprites(string filename, ref Dictionary<uint, Sprite> sprites, IProgress<int> reportProgress = null)
        {
            Sprite.CreateBlankSprite();
            using FileStream fileStream = new FileStream(filename, FileMode.Open);
            using BinaryReader reader = new BinaryReader(fileStream);

            Signature = reader.ReadUInt32();

            uint totalPics = reader.ReadUInt32();

            List<(uint Index, uint Id)> spriteIndexes = new List<(uint Index, uint Id)>(Convert.ToInt32(totalPics));
            for (uint i = 0; i < totalPics; ++i)
            {
                uint index = reader.ReadUInt32();
                spriteIndexes.Add((index, i + 1));
            }

            Sprite blankSpr = new Sprite
            {
                ID = 0,
                Size = 0,
                CompressedPixels = Array.Empty<byte>(),
                Transparent = Transparency
            };
            using Bitmap _bmp = blankSpr.GetBitmap();
            _bmp.Save(blankSpr.MemoryStream, ImageFormat.Png);
            blankSpr.CompressedPixels = null;
            sprites[0] = blankSpr;

            var options = new ParallelOptions()
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount - 1
            };

            Dictionary<uint, Sprite> localSprites = sprites;
            object lockObject = new object();
            int completedTasks = 0;
            int lastReportedProgress = -1;
            object progressLock = new object();
            Parallel.ForEach(spriteIndexes, options, spriteData =>
            {
                uint index = spriteData.Index + 3;
                ushort size;
                byte[] compressedPixels;

                lock (reader)
                {
                    reader.BaseStream.Seek(index, SeekOrigin.Begin);
                    size = reader.ReadUInt16();
                    compressedPixels = reader.ReadBytes(size);
                }

                Sprite sprite = new Sprite
                {
                    ID = spriteData.Id,
                    Size = size,
                    CompressedPixels = compressedPixels,
                    Transparent = Transparency
                };

                using Bitmap bmp = sprite.GetBitmap();
                bmp.Save(sprite.MemoryStream, ImageFormat.Png);
                sprite.CompressedPixels = null;

                lock (lockObject)
                {
                    localSprites[spriteData.Id] = sprite;
                }
                lock (progressLock)
                {
                    completedTasks++;
                    int progress = (int)(completedTasks * 100 / totalPics);
                    if (progress != lastReportedProgress && reportProgress != null)
                    {
                        reportProgress.Report(progress);
                        lastReportedProgress = progress;
                    }
                }
            });

            return true;
        }

    }
}
