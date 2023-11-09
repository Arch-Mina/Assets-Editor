using System;
using System.IO;
using static Assets_Editor.OTB;

namespace Assets_Editor
{
    public class BinaryTreeWriter : IDisposable
    {
        private BinaryReader writer;

        public BinaryTreeWriter(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("input");
            }

            this.writer = new BinaryReader(new FileStream(path, FileMode.Create));
            this.Disposed = false;
        }

        public bool Disposed { get; private set; }

        public void CreateNode(byte type)
        {
            this.WriteByte((byte)SpecialChar.NodeStart, false);
            this.WriteByte(type);
        }

        public void WriteByte(byte value)
        {
            this.WriteBytes(new byte[1] { value }, true);
        }

        public void WriteByte(byte value, bool unescape)
        {
            this.WriteBytes(new byte[1] { value }, unescape);
        }

        public void WriteUInt16(ushort value)
        {
            this.WriteBytes(BitConverter.GetBytes(value), true);
        }

        public void WriteUInt16(ushort value, bool unescape)
        {
            this.WriteBytes(BitConverter.GetBytes(value), unescape);
        }

        public void WriteUInt32(uint value)
        {
            this.WriteBytes(BitConverter.GetBytes(value), true);
        }

        public void WriteUInt32(uint value, bool unescape)
        {
            this.WriteBytes(BitConverter.GetBytes(value), unescape);
        }

        public void WriteProp(ServerItemAttribute attribute, BinaryWriter writer)
        {
            writer.BaseStream.Position = 0;
            byte[] bytes = new byte[writer.BaseStream.Length];
            writer.BaseStream.Read(bytes, 0, (int)writer.BaseStream.Length);
            writer.BaseStream.Position = 0;
            writer.BaseStream.SetLength(0);

            this.WriteProp((byte)attribute, bytes);
        }

        public void WriteProp(RootAttribute attribute, BinaryWriter writer)
        {
            writer.BaseStream.Position = 0;
            byte[] bytes = new byte[writer.BaseStream.Length];
            writer.BaseStream.Read(bytes, 0, (int)writer.BaseStream.Length);
            writer.BaseStream.Position = 0;
            writer.BaseStream.SetLength(0);

            this.WriteProp((byte)attribute, bytes);
        }

        public void WriteBytes(byte[] bytes, bool unescape)
        {
            foreach (byte b in bytes)
            {
                if (unescape && (b == (byte)SpecialChar.NodeStart || b == (byte)SpecialChar.NodeEnd || b == (byte)SpecialChar.EscapeChar))
                {
                    this.writer.BaseStream.WriteByte((byte)SpecialChar.EscapeChar);
                }

                this.writer.BaseStream.WriteByte(b);
            }
        }

        public void CloseNode()
        {
            this.WriteByte((byte)SpecialChar.NodeEnd, false);
        }

        public void Dispose()
        {
            if (this.writer != null)
            {
                this.writer.Dispose();
                this.writer = null;
                this.Disposed = true;
            }
        }

        private void WriteProp(byte attr, byte[] bytes)
        {
            this.WriteByte((byte)attr);
            this.WriteUInt16((ushort)bytes.Length);
            this.WriteBytes(bytes, true);
        }

    }
}
