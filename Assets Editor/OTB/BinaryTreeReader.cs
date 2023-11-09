using System;
using System.IO;
using static Assets_Editor.OTB;

namespace Assets_Editor
{
    public class BinaryTreeReader : IDisposable
    {

        private BinaryReader reader;
        private long currentNodePosition;
        private uint currentNodeSize;

        public BinaryTreeReader(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("input");
            }

            this.reader = new BinaryReader(new FileStream(path, FileMode.Open));
            this.Disposed = false;
        }

        public bool Disposed { get; private set; }

        public BinaryReader GetRootNode()
        {
            return this.GetChildNode();
        }

        public BinaryReader GetChildNode()
        {
            this.Advance();
            return this.GetNodeData();
        }

        public BinaryReader GetNextNode()
        {
            this.reader.BaseStream.Seek(this.currentNodePosition, SeekOrigin.Begin);

            SpecialChar value = (SpecialChar)this.reader.ReadByte();
            if (value != SpecialChar.NodeStart)
            {
                return null;
            }

            value = (SpecialChar)this.reader.ReadByte();

            int level = 1;
            while (true)
            {
                value = (SpecialChar)this.reader.ReadByte();
                if (value == SpecialChar.NodeEnd)
                {
                    --level;
                    if (level == 0)
                    {
                        value = (SpecialChar)this.reader.ReadByte();
                        if (value == SpecialChar.NodeEnd)
                        {
                            return null;
                        }
                        else if (value != SpecialChar.NodeStart)
                        {
                            return null;
                        }
                        else
                        {
                            this.currentNodePosition = this.reader.BaseStream.Position - 1;
                            return this.GetNodeData();
                        }
                    }
                }
                else if (value == SpecialChar.NodeStart)
                {
                    ++level;
                }
                else if (value == SpecialChar.EscapeChar)
                {
                    this.reader.ReadByte();
                }
            }
        }

        public void Dispose()
        {
            if (this.reader != null)
            {
                this.reader.Dispose();
                this.reader = null;
                this.Disposed = true;
            }
        }

        private BinaryReader GetNodeData()
        {
            this.reader.BaseStream.Seek(this.currentNodePosition, SeekOrigin.Begin);

            // read node type
            byte value = this.reader.ReadByte();

            if ((SpecialChar)value != SpecialChar.NodeStart)
            {
                return null;
            }

            MemoryStream ms = new MemoryStream(200);

            this.currentNodeSize = 0;
            while (true)
            {
                value = this.reader.ReadByte();
                if ((SpecialChar)value == SpecialChar.NodeEnd || (SpecialChar)value == SpecialChar.NodeStart)
                {
                    break;
                }
                else if ((SpecialChar)value == SpecialChar.EscapeChar)
                {
                    value = this.reader.ReadByte();
                }

                this.currentNodeSize++;
                ms.WriteByte(value);
            }

            this.reader.BaseStream.Seek(this.currentNodePosition, SeekOrigin.Begin);
            ms.Position = 0;
            return new BinaryReader(ms);
        }

        private bool Advance()
        {
            try
            {
                long seekPos = 0;
                if (this.currentNodePosition == 0)
                {
                    seekPos = 4;
                }
                else
                {
                    seekPos = this.currentNodePosition;
                }

                this.reader.BaseStream.Seek(seekPos, SeekOrigin.Begin);

                SpecialChar value = (SpecialChar)this.reader.ReadByte();
                if (value != SpecialChar.NodeStart)
                {
                    return false;
                }

                if (this.currentNodePosition == 0)
                {
                    this.currentNodePosition = this.reader.BaseStream.Position - 1;
                    return true;
                }
                else
                {
                    value = (SpecialChar)this.reader.ReadByte();

                    while (true)
                    {
                        value = (SpecialChar)this.reader.ReadByte();
                        if (value == SpecialChar.NodeEnd)
                        {
                            return false;
                        }
                        else if (value == SpecialChar.NodeStart)
                        {
                            this.currentNodePosition = this.reader.BaseStream.Position - 1;
                            return true;
                        }
                        else if (value == SpecialChar.EscapeChar)
                        {
                            this.reader.ReadByte();
                        }
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
