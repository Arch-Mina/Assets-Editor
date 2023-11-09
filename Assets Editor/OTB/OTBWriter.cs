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
    public class OTBWriter
    {
        public OTBWriter(List<ServerItem> items)
        {
            Items = items;
        }

        public List<ServerItem> Items { get; private set; }
        public uint MajorVersion { get; set; }
        public uint MinorVersion { get; set; }
        public uint BuildNumber { get; set; }
        public uint ClientVersion { get; set; }

        public bool Write(string path)
        {
            try
            {
                using (BinaryTreeWriter writer = new BinaryTreeWriter(path))
                {
                    writer.WriteUInt32(0, false); // version, always 0

                    writer.CreateNode(0); // root node
                    writer.WriteUInt32(0, true); // flags, unused for root node

                    OtbVersionInfo vi = new OtbVersionInfo();

                    vi.MajorVersion = MajorVersion;
                    vi.MinorVersion = MinorVersion;
                    vi.BuildNumber = BuildNumber;
                    vi.CSDVersion = string.Format("OTB {0}.{1}.{2}-{3}.{4}", vi.MajorVersion, vi.MinorVersion, vi.BuildNumber, ClientVersion / 100, ClientVersion % 100);

                    MemoryStream ms = new MemoryStream();
                    BinaryWriter property = new BinaryWriter(ms);
                    property.Write(vi.MajorVersion);
                    property.Write(vi.MinorVersion);
                    property.Write(vi.BuildNumber);
                    byte[] CSDVersion = Encoding.ASCII.GetBytes(vi.CSDVersion);
                    Array.Resize(ref CSDVersion, 128);
                    property.Write(CSDVersion);

                    writer.WriteProp(RootAttribute.Version, property);

                    foreach (ServerItem item in Items)
                    {
                        List<ServerItemAttribute> saveAttributeList = new List<ServerItemAttribute>();
                        saveAttributeList.Add(ServerItemAttribute.ServerID);

                        if (item.Type != ServerItemType.Deprecated)
                        {
                            saveAttributeList.Add(ServerItemAttribute.ClientID);
                            saveAttributeList.Add(ServerItemAttribute.SpriteHash);

                            if (item.MinimapColor != 0)
                            {
                                saveAttributeList.Add(ServerItemAttribute.MinimapColor);
                            }

                            if (item.MaxReadWriteChars != 0)
                            {
                                saveAttributeList.Add(ServerItemAttribute.MaxReadWriteChars);
                            }

                            if (item.MaxReadChars != 0)
                            {
                                saveAttributeList.Add(ServerItemAttribute.MaxReadChars);
                            }

                            if (item.LightLevel != 0 || item.LightColor != 0)
                            {
                                saveAttributeList.Add(ServerItemAttribute.Light);
                            }

                            if (item.Type == ServerItemType.Ground)
                            {
                                saveAttributeList.Add(ServerItemAttribute.GroundSpeed);
                            }

                            if (item.StackOrder != TileStackOrder.None)
                            {
                                saveAttributeList.Add(ServerItemAttribute.StackOrder);
                            }

                            if (item.TradeAs != 0)
                            {
                                saveAttributeList.Add(ServerItemAttribute.TradeAs);
                            }

                            if (!string.IsNullOrEmpty(item.Name))
                            {
                                saveAttributeList.Add(ServerItemAttribute.Name);
                            }

                            if (!string.IsNullOrEmpty(item.Article))
                            {
                                saveAttributeList.Add(ServerItemAttribute.Article);
                            }

                            if (!string.IsNullOrEmpty(item.Description))
                            {
                                saveAttributeList.Add(ServerItemAttribute.Description);
                            }
                        }

                        switch (item.Type)
                        {
                            case ServerItemType.Container:
                                writer.CreateNode((byte)ServerItemGroup.Container);
                                break;

                            case ServerItemType.Fluid:
                                writer.CreateNode((byte)ServerItemGroup.Fluid);
                                break;
                            case ServerItemType.Ground:
                                writer.CreateNode((byte)ServerItemGroup.Ground);
                                break;

                            case ServerItemType.Splash:
                                writer.CreateNode((byte)ServerItemGroup.Splash);
                                break;

                            case ServerItemType.Deprecated:
                                writer.CreateNode((byte)ServerItemGroup.Deprecated);
                                break;

                            case ServerItemType.Podium:
                                writer.CreateNode((byte)ServerItemGroup.Podium);
                                break;

                            default:
                                writer.CreateNode((byte)ServerItemGroup.None);
                                break;
                        }

                        uint flags = 0;

                        if (item.Unpassable)
                        {
                            flags |= (uint)ServerItemFlag.Unpassable;
                        }

                        if (item.BlockMissiles)
                        {
                            flags |= (uint)ServerItemFlag.BlockMissiles;
                        }

                        if (item.BlockPathfinder)
                        {
                            flags |= (uint)ServerItemFlag.BlockPathfinder;
                        }

                        if (item.HasElevation)
                        {
                            flags |= (uint)ServerItemFlag.HasElevation;
                        }

                        if (item.ForceUse)
                        {
                            flags |= (uint)ServerItemFlag.ForceUse;
                        }

                        if (item.MultiUse)
                        {
                            flags |= (uint)ServerItemFlag.MultiUse;
                        }

                        if (item.Pickupable)
                        {
                            flags |= (uint)ServerItemFlag.Pickupable;
                        }

                        if (item.Movable)
                        {
                            flags |= (uint)ServerItemFlag.Movable;
                        }

                        if (item.Stackable)
                        {
                            flags |= (uint)ServerItemFlag.Stackable;
                        }

                        if (item.StackOrder != TileStackOrder.None)
                        {
                            flags |= (uint)ServerItemFlag.StackOrder;
                        }

                        if (item.Readable)
                        {
                            flags |= (uint)ServerItemFlag.Readable;
                        }

                        if (item.Rotatable)
                        {
                            flags |= (uint)ServerItemFlag.Rotatable;
                        }

                        if (item.Hangable)
                        {
                            flags |= (uint)ServerItemFlag.Hangable;
                        }

                        if (item.HookSouth)
                        {
                            flags |= (uint)ServerItemFlag.HookSouth;
                        }

                        if (item.HookEast)
                        {
                            flags |= (uint)ServerItemFlag.HookEast;
                        }

                        if (item.HasCharges)
                        {
                            flags |= (uint)ServerItemFlag.ClientCharges;
                        }

                        if (item.IgnoreLook)
                        {
                            flags |= (uint)ServerItemFlag.IgnoreLook;
                        }

                        if (item.AllowDistanceRead)
                        {
                            flags |= (uint)ServerItemFlag.AllowDistanceRead;
                        }

                        if (item.IsAnimation)
                        {
                            flags |= (uint)ServerItemFlag.IsAnimation;
                        }

                        if (item.FullGround)
                        {
                            flags |= (uint)ServerItemFlag.FullGround;
                        }

                        if (item.HasCharges)
                        {
                            flags |= (uint)ServerItemFlag.ClientCharges;
                        }


                        writer.WriteUInt32(flags, true);

                        foreach (ServerItemAttribute attribute in saveAttributeList)
                        {
                            switch (attribute)
                            {
                                case ServerItemAttribute.ServerID:
                                    property.Write((ushort)item.ServerId);
                                    writer.WriteProp(ServerItemAttribute.ServerID, property);
                                    break;

                                case ServerItemAttribute.ClientID:
                                    property.Write((ushort)item.ClientId);
                                    writer.WriteProp(ServerItemAttribute.ClientID, property);
                                    break;

                                case ServerItemAttribute.Description:
                                    property.Write(item.Description.ToCharArray());
                                    writer.WriteProp(ServerItemAttribute.Description, property);
                                    break;

                                case ServerItemAttribute.SpriteHash:
                                    property.Write(item.SpriteHash);
                                    writer.WriteProp(ServerItemAttribute.SpriteHash, property);
                                    break;

                                case ServerItemAttribute.MinimapColor:
                                    property.Write((ushort)item.MinimapColor);
                                    writer.WriteProp(ServerItemAttribute.MinimapColor, property);
                                    break;

                                case ServerItemAttribute.MaxReadWriteChars:
                                    property.Write((ushort)item.MaxReadWriteChars);
                                    writer.WriteProp(ServerItemAttribute.MaxReadWriteChars, property);
                                    break;

                                case ServerItemAttribute.MaxReadChars:
                                    property.Write((ushort)item.MaxReadChars);
                                    writer.WriteProp(ServerItemAttribute.MaxReadChars, property);
                                    break;

                                case ServerItemAttribute.Light:
                                    property.Write((ushort)item.LightLevel);
                                    property.Write((ushort)item.LightColor);
                                    writer.WriteProp(ServerItemAttribute.Light, property);
                                    break;

                                case ServerItemAttribute.GroundSpeed:
                                    property.Write((ushort)item.GroundSpeed);
                                    writer.WriteProp(ServerItemAttribute.GroundSpeed, property);
                                    break;

                                case ServerItemAttribute.StackOrder:
                                    property.Write((byte)item.StackOrder);
                                    writer.WriteProp(ServerItemAttribute.StackOrder, property);
                                    break;

                                case ServerItemAttribute.TradeAs:
                                    property.Write((ushort)item.TradeAs);
                                    writer.WriteProp(ServerItemAttribute.TradeAs, property);
                                    break;

                                case ServerItemAttribute.Name:
                                    property.Write(item.Name.ToCharArray());
                                    writer.WriteProp(ServerItemAttribute.Name, property);
                                    break;

                                case ServerItemAttribute.Article:
                                    property.Write(item.Article.ToCharArray());
                                    writer.WriteProp(ServerItemAttribute.Article, property);
                                    break;
                            }
                        }

                        writer.CloseNode();
                    }

                    writer.CloseNode();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("An exception occurred: {0}", ex.Message);
                return false;
            }

            return true;
        }

    }
}
