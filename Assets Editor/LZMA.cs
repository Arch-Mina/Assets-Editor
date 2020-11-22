using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using SevenZip;
using SevenZip.Compression.LZMA;

namespace Assets_Editor
{
    class LZMA
    {
        public static Bitmap DecompressFileLZMA(string file)
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
            spriteBuffer.Position = 0;
            decoder.Code(reader.BaseStream, spriteBuffer, reader.BaseStream.Length - reader.BaseStream.Position, -1, null);
            spriteBuffer.Position = 0;
            decoder = null;
            return new Bitmap(spriteBuffer);
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


            CoderPropID[] propIDs =  {
                CoderPropID.DictionarySize,
                CoderPropID.PosStateBits,
                CoderPropID.LitContextBits,
                CoderPropID.LitPosBits,
                CoderPropID.Algorithm,
                CoderPropID.NumFastBytes,
                CoderPropID.MatchFinder,
                CoderPropID.EndMarker
            };

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
    }
}


