using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Assets_Editor.OTB;

namespace Assets_Editor
{
    public class OTBReader
    {
        public OTBReader()
        {
            Items = new List<ServerItem>();
        }
        public List<ServerItem> Items { get; private set; }
        public uint MajorVersion { get; set; }
        public uint MinorVersion { get; set; }
        public uint BuildNumber { get; set; }
        public uint ClientVersion { get; set; }


        public bool Read(string path)
        {
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                using (BinaryTreeReader reader = new BinaryTreeReader(path))
                {
                    // get root node
                    BinaryReader node = reader.GetRootNode();
                    if (node == null)
                    {
                        return false;
                    }

                    node.ReadByte(); // first byte of otb is 0
                    node.ReadUInt32(); // 4 bytes flags, unused

                    byte attr = node.ReadByte();
                    if ((RootAttribute)attr == RootAttribute.Version)
                    {
                        ushort datalen = node.ReadUInt16();
                        if (datalen != 140) // 4 + 4 + 4 + 1 * 128
                        {
                            Debug.WriteLine(String.Format("Size of version header is invalid, updated .otb version?"));
                            return false;
                        }

                        MajorVersion = node.ReadUInt32(); // major, file version
                        MinorVersion = node.ReadUInt32(); // minor, client version
                        BuildNumber = node.ReadUInt32();  // build number, revision
                        ClientVersion = 1290;
                        node.BaseStream.Seek(128, SeekOrigin.Current);
                    }

                    node = reader.GetChildNode();
                    if (node == null)
                    {
                        return false;
                    }

                    do
                    {
                        ServerItem item = new ServerItem();

                        ServerItemGroup itemGroup = (ServerItemGroup)node.ReadByte();
                        switch (itemGroup)
                        {
                            case ServerItemGroup.None:
                                item.Type = ServerItemType.None;
                                break;

                            case ServerItemGroup.Ground:
                                item.Type = ServerItemType.Ground;
                                break;

                            case ServerItemGroup.Container:
                                item.Type = ServerItemType.Container;
                                break;

                            case ServerItemGroup.Splash:
                                item.Type = ServerItemType.Splash;
                                break;

                            case ServerItemGroup.Fluid:
                                item.Type = ServerItemType.Fluid;
                                break;

                            case ServerItemGroup.Deprecated:
                                item.Type = ServerItemType.Deprecated;
                                break;

                            case ServerItemGroup.Podium:
                                item.Type = ServerItemType.Podium;
                                break;
                        }

                        ServerItemFlag flags = (ServerItemFlag)node.ReadUInt32();

                        item.Unpassable = ((flags & ServerItemFlag.Unpassable) == ServerItemFlag.Unpassable);
                        item.BlockMissiles = ((flags & ServerItemFlag.BlockMissiles) == ServerItemFlag.BlockMissiles);
                        item.BlockPathfinder = ((flags & ServerItemFlag.BlockPathfinder) == ServerItemFlag.BlockPathfinder);
                        item.HasElevation = ((flags & ServerItemFlag.HasElevation) == ServerItemFlag.HasElevation);
                        item.ForceUse = ((flags & ServerItemFlag.ForceUse) == ServerItemFlag.ForceUse);
                        item.MultiUse = ((flags & ServerItemFlag.MultiUse) == ServerItemFlag.MultiUse);
                        item.Pickupable = ((flags & ServerItemFlag.Pickupable) == ServerItemFlag.Pickupable);
                        item.Movable = ((flags & ServerItemFlag.Movable) == ServerItemFlag.Movable);
                        item.Stackable = ((flags & ServerItemFlag.Stackable) == ServerItemFlag.Stackable);
                        item.HasStackOrder = ((flags & ServerItemFlag.StackOrder) == ServerItemFlag.StackOrder);
                        item.Readable = ((flags & ServerItemFlag.Readable) == ServerItemFlag.Readable);
                        item.Rotatable = ((flags & ServerItemFlag.Rotatable) == ServerItemFlag.Rotatable);
                        item.Hangable = ((flags & ServerItemFlag.Hangable) == ServerItemFlag.Hangable);
                        item.HookSouth = ((flags & ServerItemFlag.HookSouth) == ServerItemFlag.HookSouth);
                        item.HookEast = ((flags & ServerItemFlag.HookEast) == ServerItemFlag.HookEast);
                        item.AllowDistanceRead = ((flags & ServerItemFlag.AllowDistanceRead) == ServerItemFlag.AllowDistanceRead);
                        item.HasCharges = ((flags & ServerItemFlag.ClientCharges) == ServerItemFlag.ClientCharges);
                        item.IgnoreLook = ((flags & ServerItemFlag.IgnoreLook) == ServerItemFlag.IgnoreLook);
                        item.FullGround = ((flags & ServerItemFlag.FullGround) == ServerItemFlag.FullGround);
                        item.IsAnimation = ((flags & ServerItemFlag.IsAnimation) == ServerItemFlag.IsAnimation);

                        while (node.PeekChar() != -1)
                        {
                            ServerItemAttribute attribute = (ServerItemAttribute)node.ReadByte();
                            ushort datalen = node.ReadUInt16();

                            switch (attribute)
                            {
                                case ServerItemAttribute.ServerID:
                                    item.ServerId = node.ReadUInt16();
                                    break;

                                case ServerItemAttribute.ClientID:
                                    item.ClientId = node.ReadUInt16();
                                    break;

                                case ServerItemAttribute.GroundSpeed:
                                    item.GroundSpeed = node.ReadUInt16();
                                    break;

                                case ServerItemAttribute.Name:
                                    byte[] buffer = node.ReadBytes(datalen);
                                    item.Name = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
                                    break;

                                case ServerItemAttribute.SpriteHash:
                                    item.SpriteHash = node.ReadBytes(datalen);
                                    break;

                                case ServerItemAttribute.MinimapColor:
                                    item.MinimapColor = node.ReadUInt16();
                                    break;

                                case ServerItemAttribute.MaxReadWriteChars:
                                    item.MaxReadWriteChars = node.ReadUInt16();
                                    break;

                                case ServerItemAttribute.MaxReadChars:
                                    item.MaxReadChars = node.ReadUInt16();
                                    break;

                                case ServerItemAttribute.Light:
                                    item.LightLevel = node.ReadUInt16();
                                    item.LightColor = node.ReadUInt16();
                                    break;

                                case ServerItemAttribute.StackOrder:
                                    item.StackOrder = (TileStackOrder)node.ReadByte();
                                    break;

                                case ServerItemAttribute.TradeAs:
                                    item.TradeAs = node.ReadUInt16();
                                    break;

                                case ServerItemAttribute.Article:
                                    byte[] articleBuffer = node.ReadBytes(datalen);
                                    item.Article = Encoding.UTF8.GetString(articleBuffer, 0, articleBuffer.Length);
                                    break;

                                case ServerItemAttribute.Description:
                                    byte[] descBuffer = node.ReadBytes(datalen);
                                    item.Description = Encoding.UTF8.GetString(descBuffer, 0, descBuffer.Length);
                                    break;

                                default:
                                    node.BaseStream.Seek(datalen, SeekOrigin.Current);
                                    break;
                            }
                        }

                        if (item.SpriteHash == null && item.Type != ServerItemType.Deprecated)
                        {
                            item.SpriteHash = new byte[16];
                        }

                        Items.Add(item);
                        node = reader.GetNextNode();
                    }
                    while (node != null);
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

    }
}
