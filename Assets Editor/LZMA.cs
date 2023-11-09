using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using SevenZip;
using SevenZip.Compression.LZMA;
using System.Security.Cryptography;
namespace Assets_Editor
{
    class LZMA
    {
        private static CoderPropID[] propIDs =  
        {
            CoderPropID.DictionarySize,
            CoderPropID.PosStateBits,
            CoderPropID.LitContextBits,
            CoderPropID.LitPosBits,
            CoderPropID.Algorithm,
            CoderPropID.NumFastBytes,
            CoderPropID.MatchFinder,
            CoderPropID.EndMarker
        };
        public static System.Drawing.Bitmap DecompressFileLZMA(string file)
        {
            using MemoryStream spriteBuffer = new MemoryStream();

            Decoder decoder = new Decoder();
            using BinaryReader reader = new BinaryReader(File.OpenRead(file));
            /*
  				CIP's header, 32 bytes
				
				Padded with a variable number of NULL bytes (depending on LZMA file size) at the start then the constant
				byte sequence of "70 0A FA 80 24" followed by the LZMA file size encoded as a 7-bit integer
			*/
            // Since there may be a variable number of NULL bytes just loop until we get through them all, then skip past
            // the remaining 4 bytes of the constant (really should check their values for file validity but just assume it is)
            while (reader.ReadByte() == 0) { }
            reader.ReadUInt32();
            while ((reader.ReadByte() & 0x80) == 0x80) { } // LZMA size, 7-bit integer where MSB = flag for next byte used
            decoder.SetDecoderProperties(reader.ReadBytes(5));
            reader.ReadUInt64(); // Should be the decompressed size but CIP writes the compressed sized, so just use a large buffer size
            // Disabled arithmetic underflow/overflow check in debug mode so this won't cause an exception
            decoder.Code(reader.BaseStream, spriteBuffer, reader.BaseStream.Length - reader.BaseStream.Position, -1, null);
            spriteBuffer.Position = 0;
            decoder = null;
            return new System.Drawing.Bitmap(spriteBuffer);
        }
        public class MyBinaryWriter : BinaryWriter
        {
            public MyBinaryWriter(Stream stream) : base(stream) { }
            public new void Write7BitEncodedInt(int i)
            {
                base.Write7BitEncodedInt(i);
            }
        }
        public static void CompressFileLZMA(MemoryStream inFile, string outFile)
        {
            int dictionary = 33554432;
            int posStateBits = 2;
            int litContextBits = 3;
            int litPosBits = 0;
            int algorithm = 2;
            int numFastBytes = 32;
            string mf = "bt4";
            bool eos = true;
            bool stdInMode = false;

            object[] properties = {
                dictionary,
                posStateBits,
                litContextBits,
                litPosBits,
                algorithm,
                numFastBytes,
                mf,
                eos
            };
            byte[] TibiaHeader = { 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0 };
            using FileStream outStream = new FileStream(outFile, FileMode.Create);
            outStream.Write(TibiaHeader);
            SevenZip.Compression.LZMA.Encoder encoder = new SevenZip.Compression.LZMA.Encoder();
            encoder.SetCoderProperties(propIDs, properties);
            encoder.WriteCoderProperties(outStream);
            long fileSize;
            if (eos || stdInMode)
                fileSize = -1;
            else
                fileSize = inFile.Length;
            for (int i = 0; i < 8; i++)
                outStream.WriteByte((Byte)(fileSize >> (8 * i)));

            encoder.Code(inFile, outStream, -1, -1, null);

            MemoryStream tmp = new MemoryStream();
            int size = (int)outStream.Length - 32;
            MyBinaryWriter bw = new MyBinaryWriter(tmp);
            bw.Write(new byte[] {0x70, 0x0A, 0xFA, 0x80, 0x24});
            bw.Write7BitEncodedInt(size);

            if (tmp.Length == 8)
                outStream.Position = 0x18;
            else
                outStream.Position = 0x19;

            outStream.Write(tmp.ToArray(), 0, (int)tmp.Length);

            outStream.Position = 0;
            outStream.Close();
        }


        public static byte[] Uncompress(byte[] bytes)
        {
            if (bytes.Length < 5)
            {
                throw new Exception("LZMA data is too short.");
            }

            using (MemoryStream input = new MemoryStream(bytes))
            {
                // read the decoder properties
                byte[] properties = new byte[5];
                if (input.Read(properties, 0, 5) != 5)
                {
                    throw (new Exception("LZMA data is too short."));
                }

                long outSize = 0;

                if (BitConverter.IsLittleEndian)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        int v = input.ReadByte();
                        if (v < 0)
                        {
                            throw (new Exception("Can't Read 1."));
                        }

                        outSize |= ((long)v) << (8 * i);
                    }
                }

                MemoryStream output = new MemoryStream();
                long compressedSize = input.Length - input.Position;
                Decoder decoder = new Decoder();
                decoder.SetDecoderProperties(properties);
                decoder.Code(input, output, compressedSize, outSize, null);
                return output.ToArray();
            }
        }

        public static byte[] Compress(byte[] bytes)
        {
            using (MemoryStream input = new MemoryStream(bytes))
            {
                object[] properties =
                {
                    (int)(1 << 21), // DictionarySize
                    (int)(2),       // PosStateBits
                    (int)(3),       // LitContextBits
                    (int)(0),       // LitPosBits
                    (int)(2),       // Algorithm
                    (int)(128),     // NumFastBytes
                    "bt4",          // MatchFinder
                    false           // EndMarker
                };

                MemoryStream output = new MemoryStream();
                Encoder encoder = new Encoder();
                encoder.SetCoderProperties(propIDs, properties);
                encoder.WriteCoderProperties(output);

                if (BitConverter.IsLittleEndian)
                {
                    byte[] LengthHeader = BitConverter.GetBytes(input.Length);
                    output.Write(LengthHeader, 0, LengthHeader.Length);
                }

                encoder.Code(input, output, input.Length, -1, null);
                return output.ToArray();
            }
        }

        public static void ExportLzmaFile(Bitmap targetImg, ref string filename)
        {
            Bitmap targetBmp = targetImg.Clone(new Rectangle(0, 0, targetImg.Width, targetImg.Height), System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            MemoryStream ms = new MemoryStream();
            targetBmp.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            ms.Position = 0xA;
            int bmpData = ms.ReadByte();
            MemoryStream outFile = new MemoryStream();
            //BITMAPV$HEADER
            outFile.WriteByte(0x42);                                    //Magic value identifying bitmap file
            outFile.WriteByte(0x4D);                                    //Magic value identifying bitmap file
            outFile.Write(BitConverter.GetBytes(0));                    // Bitmap Size
            outFile.Write(BitConverter.GetBytes((ushort)0));            // Reserved
            outFile.Write(BitConverter.GetBytes((ushort)0));            // Reserved
            outFile.Write(BitConverter.GetBytes(122));                  // Offset of pixel array
            outFile.Write(BitConverter.GetBytes(108));                  // bV4Size
            outFile.Write(BitConverter.GetBytes(targetBmp.Width));      // bV4Width 
            outFile.Write(BitConverter.GetBytes(targetBmp.Height));     // bV4Height  
            outFile.Write(BitConverter.GetBytes((ushort)1));            // bV4Planes
            outFile.Write(BitConverter.GetBytes((ushort)32));           // bV4BitCount
            outFile.Write(BitConverter.GetBytes(3));                    // bV4V4Compression
            outFile.Write(BitConverter.GetBytes(0));                    // bV4SizeImage
            outFile.Write(BitConverter.GetBytes(0));                    // bV4XPelsPerMeter
            outFile.Write(BitConverter.GetBytes(0));                    // bV4YPelsPerMeter
            outFile.Write(BitConverter.GetBytes(0));                    // bV4ClrUsed
            outFile.Write(BitConverter.GetBytes(0));                    // bV4ClrImportant
            outFile.Write(new byte[] { 0x0, 0x0, 0xFF, 0x0 }, 0, 4);    // bV4RedMask
            outFile.Write(new byte[] { 0x0, 0xFF, 0x0, 0x0 }, 0, 4);    // bV4GreenMask
            outFile.Write(new byte[] { 0xFF, 0x0, 0x0, 0x0 }, 0, 4);    // bV4BlueMask
            outFile.Write(new byte[] { 0x0, 0x0, 0x0, 0xFF }, 0, 4);    // bV4AlphaMask
            outFile.Write(new byte[] { 0x57, 0x69, 0x6E, 0x20 }, 0, 4); //(0x73524742); // bV4CSType (0x42475273 = "BGRs")
            outFile.Write(BitConverter.GetBytes(0));                    // X coordinate of red endpoint
            outFile.Write(BitConverter.GetBytes(0));                    // Y coordinate of red endpoint
            outFile.Write(BitConverter.GetBytes(0));                    // Z coordinate of red endpoint
            outFile.Write(BitConverter.GetBytes(0));                    // X coordinate of green endpoint
            outFile.Write(BitConverter.GetBytes(0));                    // Y coordinate of green endpoint
            outFile.Write(BitConverter.GetBytes(0));                    // Z coordinate of green endpoint
            outFile.Write(BitConverter.GetBytes(0));                    // X coordinate of blue endpoint
            outFile.Write(BitConverter.GetBytes(0));                    // Y coordinate of blue endpoint
            outFile.Write(BitConverter.GetBytes(0));                    // Z coordinate of blue endpoint
            outFile.Write(BitConverter.GetBytes(0));                    // bV4GammaRed
            outFile.Write(BitConverter.GetBytes(0));                    // bV4GammaGreen
            outFile.Write(BitConverter.GetBytes(0));                    // bV4GammaBlue

            outFile.Write(ms.ToArray(), bmpData, (int)ms.Length - bmpData);
            outFile.Seek(2, SeekOrigin.Begin);
            outFile.Write(BitConverter.GetBytes(outFile.Length));
            outFile.Position = 0;
            using HashAlgorithm crypt = SHA256.Create();
            byte[] hashValue = crypt.ComputeHash(outFile);
            string hash = String.Empty;
            foreach (byte theByte in hashValue)
            {
                hash += theByte.ToString("x2");
            }
            outFile.Position = 0;
            string fullpath = filename + "\\sprites-" + hash + ".bmp.lzma";
            filename = "sprites-" + hash + ".bmp.lzma";
            LZMA.CompressFileLZMA(outFile, fullpath);
        }
    }
}


