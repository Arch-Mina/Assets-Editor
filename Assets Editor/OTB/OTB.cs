using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Assets_Editor;
using Efundies;
using Tibia.Protobuf.Appearances;

namespace Assets_Editor
{
    public static class OTB
    {
        public enum SpecialChar : byte
        {
            NodeStart = 0xFE,
            NodeEnd = 0xFF,
            EscapeChar = 0xFD,
        }
        public enum RootAttribute
        {
            Version = 0x01
        }
        public enum ServerItemAttribute : byte
        {
            ServerID = 0x10,
            ClientID = 0x11,
            Name = 0x12,
            Description = 0x13,
            GroundSpeed = 0x14,
            SpriteHash = 0x20,
            MinimapColor = 0x21,
            MaxReadWriteChars = 0x22,
            MaxReadChars = 0x23,
            Light = 0x2A,
            StackOrder = 0x2B,
            TradeAs = 0x2D,
            Article = 0x2F,
            
        }
        public enum ServerItemType : byte
        {
            None = 0,
            Ground = 1,
            Container = 2,
            Fluid = 3,
            Splash = 4,
            Deprecated = 5,
            Podium = 6
        }

        public enum ServerItemGroup : byte
        {
            None = 0,
            Ground = 1,
            Container = 2,
            Weapon = 3,
            Ammunition = 4,
            Armor = 5,
            Changes = 6,
            Teleport = 7,
            MagicField = 8,
            Writable = 9,
            Key = 10,
            Splash = 11,
            Fluid = 12,
            Door = 13,
            Deprecated = 14,
            Podium = 15
        }
        public enum ServerItemFlag
        {
            None = 0,
            Unpassable = 1 << 0,
            BlockMissiles = 1 << 1,
            BlockPathfinder = 1 << 2,
            HasElevation = 1 << 3,
            MultiUse = 1 << 4,
            Pickupable = 1 << 5,
            Movable = 1 << 6,
            Stackable = 1 << 7,
            FloorChangeDown = 1 << 8,
            FloorChangeNorth = 1 << 9,
            FloorChangeEast = 1 << 10,
            FloorChangeSouth = 1 << 11,
            FloorChangeWest = 1 << 12,
            StackOrder = 1 << 13,
            Readable = 1 << 14,
            Rotatable = 1 << 15,
            Hangable = 1 << 16,
            HookEast = 1 << 17,
            HookSouth = 1 << 18,
            CanNotDecay = 1 << 19,
            AllowDistanceRead = 1 << 20,
            Unused = 1 << 21,
            ClientCharges = 1 << 22,
            IgnoreLook = 1 << 23,
            IsAnimation = 1 << 24,
            FullGround = 1 << 25,
            ForceUse = 1 << 26
        }
        public enum TileStackOrder : byte
        {
            None = 0,
            Border = 1,
            Bottom = 2,
            Top = 3
        }
        public class OtbVersionInfo
        {
            #region Public Properties

            public uint MajorVersion { get; set; }

            public uint MinorVersion { get; set; }

            public uint BuildNumber { get; set; }

            public string CSDVersion { get; set; }

            #endregion
        }

        public class ServerItem
        {
            protected byte[] spriteHash = null;
            public ServerItem()
            {
                Type = ServerItemType.None;
                StackOrder = TileStackOrder.None;
                Movable = true;
                Name = string.Empty;
            }

            public ServerItem(Appearance appearance)
            {
                if (appearance.Flags.Bank != null)
                {
                    Type = ServerItemType.Ground;
                    GroundSpeed = (ushort)appearance.Flags.Bank.Waypoints;
                }
                else if (appearance.Flags.Container)
                    Type = ServerItemType.Container;
                else if (appearance.Flags.Liquidcontainer)
                    Type = ServerItemType.Fluid;
                else if (appearance.Flags.Liquidpool)
                    Type = ServerItemType.Splash;
                else
                    Type = ServerItemType.None;

                if (appearance.Flags.Clip)
                    StackOrder = TileStackOrder.Border;
                else if (appearance.Flags.Bottom)
                    StackOrder = TileStackOrder.Bottom;
                else if (appearance.Flags.Top)
                    StackOrder = TileStackOrder.Top;
                else
                    StackOrder = TileStackOrder.None;

                if (appearance.Flags.Automap != null && appearance.Flags.Automap.HasColor)
                    MinimapColor = (ushort)appearance.Flags.Automap.Color;

                if (appearance.Flags.Write != null && appearance.Flags.Write.HasMaxTextLength)
                {
                    MaxReadWriteChars = (ushort)appearance.Flags.Write.MaxTextLength;
                    Readable = true;
                }

                if (appearance.Flags.WriteOnce != null && appearance.Flags.WriteOnce.HasMaxTextLengthOnce)
                {
                    MaxReadChars = (ushort)appearance.Flags.WriteOnce.MaxTextLengthOnce;
                    Readable = true;
                }
                if(appearance.Flags.Lenshelp != null && appearance.Flags.Lenshelp.Id == 1112)
                    Readable = true;

                if (appearance.Flags.Light != null)
                {
                    LightColor = (ushort)appearance.Flags.Light.Color;
                    LightLevel = (ushort)appearance.Flags.Light.Brightness;
                }

                if (appearance.Flags.Market != null && appearance.Flags.Market.HasTradeAsObjectId)
                    TradeAs = (ushort)appearance.Flags.Market.TradeAsObjectId;

                if (appearance.HasName && !string.IsNullOrEmpty(appearance.Name))
                    Name = appearance.Name;

                if (appearance.HasDescription && !string.IsNullOrEmpty(appearance.Description))
                    Description = appearance.Description;

                spriteHash = GenerateItemSpriteHash(appearance);
                Unpassable = appearance.Flags.Unpass;
                BlockMissiles = appearance.Flags.Unsight;
                BlockPathfinder = appearance.Flags.Avoid;
                HasElevation = appearance.Flags.Height != null;
                ForceUse = appearance.Flags.Forceuse;
                MultiUse = appearance.Flags.Multiuse;
                Pickupable = appearance.Flags.Take;
                Movable = !appearance.Flags.Unmove;
                Stackable = appearance.Flags.Cumulative;
                Rotatable = appearance.Flags.Rotate;
                Hangable = appearance.Flags.Hang;
                HookSouth = appearance.Flags.HookSouth;
                HookEast = appearance.Flags.HookEast;
                IgnoreLook = appearance.Flags.IgnoreLook;
                FullGround = appearance.Flags.Fullbank;

            }

            public ushort ServerId { get; set; }
            public ushort ClientId { get; set; }
            public ServerItemType Type { get; set; }
            public bool HasStackOrder { get; set; }
            public TileStackOrder StackOrder { get; set; }
            public bool Unpassable { get; set; }
            public bool BlockMissiles { get; set; }
            public bool BlockPathfinder { get; set; }
            public bool HasElevation { get; set; }
            public bool ForceUse { get; set; }
            public bool MultiUse { get; set; }
            public bool Pickupable { get; set; }
            public bool Movable { get; set; }
            public bool Stackable { get; set; }
            public bool Readable { get; set; }
            public bool Rotatable { get; set; }
            public bool Hangable { get; set; }
            public bool HookSouth { get; set; }
            public bool HookEast { get; set; }
            public bool HasCharges { get; set; }
            public bool IgnoreLook { get; set; }
            public bool FullGround { get; set; }
            public bool AllowDistanceRead { get; set; }
            public bool IsAnimation { get; set; }
            public ushort GroundSpeed { get; set; }
            public ushort LightLevel { get; set; }
            public ushort LightColor { get; set; }
            public ushort MaxReadChars { get; set; }
            public ushort MaxReadWriteChars { get; set; }
            public ushort MinimapColor { get; set; }
            public ushort TradeAs { get; set; }
            public string Name { get; set; }
            public string Article { get; set; }
            public string Description { get; set; }

            // used to find sprites during updates
            public virtual byte[] SpriteHash
            {
                get
                {
                    return spriteHash;
                }

                set
                {
                    spriteHash = value;
                }
            }
        }
        public static bool GenerateOTB(OtbVersionInfo version, Appearances appearances, string path)
        {
            try
            {
                using (BinaryTreeWriter writer = new BinaryTreeWriter(path))
                {
                    writer.WriteUInt32(0, false); // version, always 0

                    writer.CreateNode(0); // root node
                    writer.WriteUInt32(0, true); // flags, unused for root node


                    MemoryStream ms = new MemoryStream();
                    BinaryWriter property = new BinaryWriter(ms);
                    property.Write(version.MajorVersion);
                    property.Write(version.MinorVersion);
                    property.Write(version.BuildNumber);
                    byte[] CSDVersion = Encoding.ASCII.GetBytes(version.CSDVersion);
                    Array.Resize(ref CSDVersion, 128);
                    property.Write(CSDVersion);

                    writer.WriteProp(RootAttribute.Version, property);
                    int CurrentId = 0;
                    //foreach (var item in appearances.Object)
                    for (int i = 100; i <= appearances.Object[^1].Id; i++)
                    {
                        Appearance item;
                        if (i == appearances.Object[CurrentId].Id)
                        {
                            item = appearances.Object[CurrentId];
                            CurrentId++;
                        }
                        else
                        {
                            item = new Appearance();
                            item.Flags = new AppearanceFlags();
                        }

                        List<ServerItemAttribute> saveAttributeList = new List<ServerItemAttribute>();
                        saveAttributeList.Add(ServerItemAttribute.ServerID);
                        saveAttributeList.Add(ServerItemAttribute.ClientID);
                        saveAttributeList.Add(ServerItemAttribute.SpriteHash);

                        if (item.Flags.Automap != null && item.Flags.Automap.HasColor)
                        {
                            saveAttributeList.Add(ServerItemAttribute.MinimapColor);
                        }

                        if (item.Flags.WriteOnce != null && item.Flags.WriteOnce.HasMaxTextLengthOnce)
                        {
                            saveAttributeList.Add(ServerItemAttribute.MaxReadWriteChars);
                        }

                        if (item.Flags.Write != null && item.Flags.Write.HasMaxTextLength)
                        {
                            saveAttributeList.Add(ServerItemAttribute.MaxReadChars);
                        }

                        if (item.Flags.Light != null && item.Flags.Light.HasColor)
                        {
                            saveAttributeList.Add(ServerItemAttribute.Light);
                        }

                        if (item.Flags.Bank != null && item.Flags.Bank.HasWaypoints)
                        {
                            saveAttributeList.Add(ServerItemAttribute.GroundSpeed);
                        }

                        if (item.Flags.HasClip || item.Flags.HasBottom || item.Flags.HasTop)
                        {
                            saveAttributeList.Add(ServerItemAttribute.StackOrder);
                        }

                        if (item.Flags.Market != null && item.Flags.Market.HasTradeAsObjectId)
                        {
                            saveAttributeList.Add(ServerItemAttribute.TradeAs);
                        }

                        if (item.HasName && !string.IsNullOrEmpty(item.Name))
                        {
                            saveAttributeList.Add(ServerItemAttribute.Name);
                        }

                        if (item.Flags.HasContainer)
                            writer.CreateNode((byte)ServerItemGroup.Container);
                        else if (item.Flags.HasLiquidcontainer)
                            writer.CreateNode((byte)ServerItemGroup.Fluid);
                        else if (item.Flags.Bank != null && item.Flags.Bank.HasWaypoints)
                            writer.CreateNode((byte)ServerItemGroup.Ground);
                        else if (item.Flags.HasLiquidpool)
                            writer.CreateNode((byte)ServerItemGroup.Splash);
                        else
                            writer.CreateNode((byte)ServerItemGroup.None);

                        uint flags = 0;

                        if (item.Flags.HasUnpass)
                        {
                            flags |= (uint)ServerItemFlag.Unpassable;
                        }

                        if (item.Flags.Unsight)
                        {
                            flags |= (uint)ServerItemFlag.BlockMissiles;
                        }

                        if (item.Flags.HasAvoid)
                        {
                            flags |= (uint)ServerItemFlag.BlockPathfinder;
                        }

                        if (item.Flags.Height != null && item.Flags.Height.HasElevation)
                        {
                            flags |= (uint)ServerItemFlag.HasElevation;
                        }

                        if (item.Flags.HasForceuse)
                        {
                            flags |= (uint)ServerItemFlag.ForceUse;
                        }

                        if (item.Flags.HasMultiuse)
                        {
                            flags |= (uint)ServerItemFlag.MultiUse;
                        }

                        if (item.Flags.HasTake)
                        {
                            flags |= (uint)ServerItemFlag.Pickupable;
                        }

                        if (!item.Flags.HasUnmove)
                        {
                            flags |= (uint)ServerItemFlag.Movable;
                        }

                        if (item.Flags.HasCumulative)
                        {
                            flags |= (uint)ServerItemFlag.Stackable;
                        }

                        if (item.Flags.HasClip || item.Flags.HasBottom || item.Flags.HasTop)
                        {
                            flags |= (uint)ServerItemFlag.StackOrder;
                        }

                        if (item.Flags.Write != null || item.Flags.WriteOnce != null)
                        {
                            flags |= (uint)ServerItemFlag.Readable;
                        }

                        if (item.Flags.HasRotate)
                        {
                            flags |= (uint)ServerItemFlag.Rotatable;
                        }

                        if (item.Flags.HasHang)
                        {
                            flags |= (uint)ServerItemFlag.Hangable;
                        }

                        if (item.Flags.HasHookSouth)
                        {
                            flags |= (uint)ServerItemFlag.HookSouth;
                        }

                        if (item.Flags.HasHookEast)
                        {
                            flags |= (uint)ServerItemFlag.HookEast;
                        }

                        if (item.Flags.IgnoreLook)
                        {
                            flags |= (uint)ServerItemFlag.IgnoreLook;
                        }

                        if (item.Flags.HasAnimateAlways)
                        {
                            flags |= (uint)ServerItemFlag.IsAnimation;
                        }

                        if (item.Flags.Fullbank)
                        {
                            flags |= (uint)ServerItemFlag.FullGround;
                        }

                        writer.WriteUInt32(flags, true);


                        foreach (ServerItemAttribute attribute in saveAttributeList)
                        {
                            switch (attribute)
                            {
                                case ServerItemAttribute.ServerID:
                                    property.Write((ushort)i);
                                    writer.WriteProp(ServerItemAttribute.ServerID, property);
                                    break;

                                case ServerItemAttribute.TradeAs:
                                    property.Write((ushort)item.Flags.Market.TradeAsObjectId);
                                    writer.WriteProp(ServerItemAttribute.TradeAs, property);
                                    break;

                                case ServerItemAttribute.ClientID:
                                    property.Write((ushort)item.Id);
                                    writer.WriteProp(ServerItemAttribute.ClientID, property);
                                    break;

                                case ServerItemAttribute.GroundSpeed:
                                    property.Write((ushort)item.Flags.Bank.Waypoints);
                                    writer.WriteProp(ServerItemAttribute.GroundSpeed, property);
                                    break;

                                case ServerItemAttribute.Name:
                                    property.Write(item.Name.ToCharArray());
                                    writer.WriteProp(ServerItemAttribute.Name, property);
                                    break;

                                case ServerItemAttribute.SpriteHash:
                                {
                                    if (item.FrameGroup.Count > 0)
                                        property.Write(GenerateItemSpriteHash(item));
                                    else
                                        property.Write(new byte[16]);

                                    writer.WriteProp(ServerItemAttribute.SpriteHash, property);
                                    break;
                                }
                                case ServerItemAttribute.MinimapColor:
                                    property.Write((ushort)item.Flags.Automap.Color);
                                    writer.WriteProp(ServerItemAttribute.MinimapColor, property);
                                    break;

                                case ServerItemAttribute.MaxReadWriteChars:
                                    property.Write((ushort)item.Flags.WriteOnce.MaxTextLengthOnce);
                                    writer.WriteProp(ServerItemAttribute.MaxReadWriteChars, property);
                                    break;

                                case ServerItemAttribute.MaxReadChars:
                                    property.Write((ushort)item.Flags.Write.MaxTextLength);
                                    writer.WriteProp(ServerItemAttribute.MaxReadChars, property);
                                    break;

                                case ServerItemAttribute.Light:
                                    property.Write((ushort)item.Flags.Light.Brightness);
                                    property.Write((ushort)item.Flags.Light.Color);
                                    writer.WriteProp(ServerItemAttribute.Light, property);
                                    break;

                                case ServerItemAttribute.StackOrder:
                                {
                                    if (item.Flags.HasClip)
                                        property.Write((byte)TileStackOrder.Border);
                                    else if (item.Flags.HasBottom)
                                        property.Write((byte)TileStackOrder.Bottom);
                                    else if (item.Flags.HasTop)
                                        property.Write((byte)TileStackOrder.Top);

                                    writer.WriteProp(ServerItemAttribute.StackOrder, property);
                                    break;
                                }

                            }
                        }

                        writer.CloseNode();
                    }

                    writer.CloseNode();
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        public static byte[] GenerateItemSpriteHash(Appearance appearance)
        {
            MD5 md5 = MD5.Create();
            MemoryStream stream = new MemoryStream();
            byte[] rgbaData = new byte[4096];
            FrameGroup frameGroup = appearance.FrameGroup[0];

            for (int l = 0; l < frameGroup.SpriteInfo.PatternLayers; l++)
            {
                for (int h =  0; h < (int)(frameGroup.SpriteInfo.PatternHeight); h++)
                {
                    for (int w =  0; w < (int)(frameGroup.SpriteInfo.PatternWidth); w++)
                    {
                        int index = (int)(w + h * frameGroup.SpriteInfo.PatternWidth + l * frameGroup.SpriteInfo.PatternWidth * frameGroup.SpriteInfo.PatternHeight);
                        int spriteId = (int)frameGroup.SpriteInfo.SpriteId[index];
                        using Bitmap target = new Bitmap(MainWindow.MainSprStorage.getSpriteStream((uint)spriteId));
                        target.RotateFlip(RotateFlipType.RotateNoneFlipY);
                        var lockedBitmap = new LockBitmap(target);
                        lockedBitmap.LockBits();
                        for (int y = 0; y < target.Height; y++)
                        {
                            for (int x = 0; x < target.Width; x++)
                            {
                                Color c = lockedBitmap.GetPixel(x, y);
                                if (c == Color.FromArgb(0, 0, 0, 0))
                                    c = Color.FromArgb(0, 17, 17, 17);
                                rgbaData[128 * y + x * 4 + 0] = c.B;
                                rgbaData[128 * y + x * 4 + 1] = c.G;
                                rgbaData[128 * y + x * 4 + 2] = c.R;
                                rgbaData[128 * y + x * 4 + 3] = 0;
                            }
                        }
                        lockedBitmap.UnlockBits();
                        stream.Write(rgbaData, 0, 4096);
                    }
                }
            }


            stream.Position = 0;
            byte[] spriteHash = md5.ComputeHash(stream);
            return spriteHash;
        }
    }
}
