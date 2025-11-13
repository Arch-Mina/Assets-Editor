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

        public static async Task CompileSpritesAsync(string file, SpriteStorage spriteStorage, bool transparency, uint version, IProgress<int> progress)
        {
            await Task.Run(() =>
            {
                int percentageComplete = 0;
                int currentPercentage = 0;
                int FullProgress = spriteStorage.SprLists.Count;
                string tempFile = file + ".tmp";
                using (BinaryWriter writer = new BinaryWriter(new FileStream(tempFile, FileMode.Create)))
                {
                    uint count = (uint)spriteStorage.SprLists.Count - 1; ;

                    writer.Write(version);

                    writer.Write(count);

                    byte headSize = 8;
                    int addressPosition = headSize;
                    int address = (int)((count * 4) + headSize);
                    byte[] bytes = null;

                    for (uint id = 1; id <= count; id++)
                    {

                        writer.Seek(addressPosition, SeekOrigin.Begin);

                        bytes = CompressBitmap(new Bitmap(spriteStorage.getSpriteStream((uint)id)), transparency);

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

                        percentageComplete = (int)(id * 100 / FullProgress);
                        if (percentageComplete > currentPercentage)
                        {
                            progress?.Report(percentageComplete);
                            currentPercentage = percentageComplete;
                        }
                    }

                    writer.Close();
                    File.Move(tempFile, file, overwrite: true);
                    if (File.Exists(tempFile))
                    {
                        try
                        {
                            File.Delete(tempFile);
                        }
                        catch
                        {
                        }
                    }
                }


            });
        }

        /// <summary>
        /// Reads legacy sprite file (.spr) and returns sprites as MemoryStreams
        /// </summary>
        /// <param name="spriteIdsToLoad">Optional: Only load these specific sprite IDs. If null, loads all.</param>
        public static async Task<Dictionary<uint, MemoryStream>> ReadLegacySprites(string sprFile, bool hasAlpha, IProgress<int> progress = null, HashSet<uint> spriteIdsToLoad = null)
        {
            return await Task.Run(() =>
            {
                var sprites = new Dictionary<uint, MemoryStream>();
                
                if (!File.Exists(sprFile))
                {
                    throw new FileNotFoundException($"Sprite file not found: {sprFile}");
                }

                using (FileStream fileStream = new FileStream(sprFile, FileMode.Open, FileAccess.Read))
                using (BinaryReader reader = new BinaryReader(fileStream))
                {
                    // Read signature
                    uint signature = reader.ReadUInt32();
                    
                    // Read sprite count
                    uint spriteCount = reader.ReadUInt32();

                    int lastReportedProgress = -1;

                    // Read all sprite addresses first
                    List<uint> addresses = new List<uint>((int)spriteCount);
                    for (uint i = 0; i < spriteCount; i++)
                    {
                        addresses.Add(reader.ReadUInt32());
                    }

                    // Read each sprite
                    for (uint id = 1; id <= spriteCount; id++)
                    {
                        // Skip if we have a filter list and this ID is not in it
                        if (spriteIdsToLoad != null && !spriteIdsToLoad.Contains(id))
                        {
                            continue;
                        }
                        
                        uint address = addresses[(int)(id - 1)];

                        if (address == 0)
                        {
                            // Empty sprite - create blank
                            sprites[id] = CreateBlankSpriteStream();
                        }
                        else
                        {
                            // Seek to sprite data
                            reader.BaseStream.Seek(address, SeekOrigin.Begin);

                            // Read colorkey (3 bytes - not used in modern format)
                            byte colorKeyRed = reader.ReadByte();
                            byte colorKeyGreen = reader.ReadByte();
                            byte colorKeyBlue = reader.ReadByte();

                            // Read sprite data size
                            ushort dataSize = reader.ReadUInt16();

                            if (dataSize > 0)
                            {
                                // Read compressed sprite data
                                byte[] compressedPixels = reader.ReadBytes(dataSize);

                                // Decompress to BGRA pixels
                                byte[] pixels = UncompressPixelsBGRA(compressedPixels, hasAlpha);

                                // Create bitmap from pixels
                                Bitmap bitmap = new Bitmap(DefaultSize, DefaultSize, PixelFormat.Format32bppArgb);
                                BitmapData bitmapData = bitmap.LockBits(Rect, ImageLockMode.WriteOnly, bitmap.PixelFormat);
                                Marshal.Copy(pixels, 0, bitmapData.Scan0, pixels.Length);
                                bitmap.UnlockBits(bitmapData);

                                // Save to MemoryStream as PNG
                                MemoryStream memoryStream = new MemoryStream();
                                bitmap.Save(memoryStream, ImageFormat.Png);
                                memoryStream.Position = 0;
                                sprites[id] = memoryStream;

                                bitmap.Dispose();
                            }
                            else
                            {
                                sprites[id] = CreateBlankSpriteStream();
                            }
                        }

                        // Report progress
                        if (progress != null)
                        {
                            int currentProgress = (int)(id * 100 / spriteCount);
                            if (currentProgress != lastReportedProgress)
                            {
                                progress.Report(currentProgress);
                                lastReportedProgress = currentProgress;
                            }
                        }
                    }
                }

                return sprites;
            });
        }

        /// <summary>
        /// Creates a blank sprite as MemoryStream
        /// </summary>
        private static MemoryStream CreateBlankSpriteStream()
        {
            Sprite blankSpr = new Sprite
            {
                ID = 0,
                CompressedPixels = Array.Empty<byte>(),
            };
            using Bitmap bmp = blankSpr.GetBitmap();
            MemoryStream memoryStream = new MemoryStream();
            bmp.Save(memoryStream, ImageFormat.Png);
            memoryStream.Position = 0;
            return memoryStream;
        }

    }

    public class SpriteStorage
    {
        public string SprPath { get; set; }
        public uint Signature;
        public bool Transparency;
        public Dictionary<uint, Sprite> Sprites { get; set; }
        public ConcurrentDictionary<int, MemoryStream> SprLists { get; set; }

        public SpriteStorage()
        {
            Sprites = new Dictionary<uint, Sprite>();
            SprLists = new ConcurrentDictionary<int, MemoryStream>();
            Transparency = false;
            Sprite blankSpr = new Sprite
            {
                ID = 0,
                CompressedPixels = Array.Empty<byte>(),
            };
            using Bitmap _bmp = blankSpr.GetBitmap();
            _bmp.Save(blankSpr.MemoryStream, ImageFormat.Png);
            blankSpr.CompressedPixels = null;
            Sprites[0] = blankSpr;
            SprLists[0] = blankSpr.MemoryStream;
        }
        public SpriteStorage(string path, bool transparency, IProgress<int> reportProgress = null)
        {
            SprPath = path;
            Sprites = new Dictionary<uint, Sprite>();
            SprLists = new ConcurrentDictionary<int, MemoryStream>();
            Transparency = transparency;

            Sprite.CreateBlankSprite();
            using FileStream fileStream = new FileStream(path, FileMode.Open);
            using BinaryReader reader = new BinaryReader(fileStream);

            Signature = reader.ReadUInt32();

            uint totalPics = reader.ReadUInt32();

            List<(uint Index, uint Id)> spriteIndexes = new List<(uint Index, uint Id)>(Convert.ToInt32(totalPics));
            Sprite blankSpr = new Sprite
            {
                ID = 0,
                CompressedPixels = Array.Empty<byte>(),
            };
            using Bitmap _bmp = blankSpr.GetBitmap();
            _bmp.Save(blankSpr.MemoryStream, ImageFormat.Png);
            blankSpr.CompressedPixels = null;
            Sprites[0] = blankSpr;
            SprLists[0] = blankSpr.MemoryStream;
            int lastReportedProgress = -1;
            for (uint i = 0; i < totalPics; ++i)
            {
                Sprite sprite = new Sprite
                {
                    ID = i + 1,
                    Transparent = Transparency
                };
                Sprites[sprite.ID] = sprite;
                SprLists[(int)sprite.ID] = sprite.MemoryStream;
                int progress = (int)(i * 100 / totalPics);
                if (progress != lastReportedProgress && reportProgress != null)
                {
                    reportProgress.Report(progress);
                    lastReportedProgress = progress;
                }
            }
        }

        public MemoryStream getSpriteStream(uint id)
        {
            if (SprLists[(int)id] != null && SprLists[(int)id].Length > 0)
            {
                return SprLists[(int)id];
            }

            Sprite sprite = Sprites[id];
            using (FileStream fileStream = new FileStream(SprPath, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader reader = new BinaryReader(fileStream))
                {
                    reader.BaseStream.Seek(8 + (id - 1) * 4, SeekOrigin.Begin);
                    uint index = reader.ReadUInt32() + 3;
                    reader.BaseStream.Seek(index, SeekOrigin.Begin);
                    sprite.Size = reader.ReadUInt16();
                    sprite.CompressedPixels = reader.ReadBytes((ushort)sprite.Size);
                }
            }

            using (Bitmap bmp = sprite.GetBitmap())
            {
                bmp.Save(sprite.MemoryStream, ImageFormat.Png);
            }
            sprite.CompressedPixels = null;
            SprLists[(int)sprite.ID] = sprite.MemoryStream;

            return SprLists[(int)sprite.ID];
        }

    }

}
