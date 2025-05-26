using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml.Linq;
using Tibia.Protobuf.Appearances;

namespace Assets_Editor
{
    public class FlagInfo
    {
        public byte Id { get; set; }
        public string Name { get; set; }
    }
    public class DatInfo
    {
        public uint Signature { get; set; }
        public ushort ObjectCount { get; set; }
        public ushort OutfitCount { get; set; }
        public ushort EffectCount { get; set; }
        public ushort MissileCount { get; set; }

    }

    public class VersionInfo
    {
        public int Number { get; set; }
        public int Structure { get; set; }
        public Dictionary<string, FlagInfo> FlagsByName { get; set; } = new Dictionary<string, FlagInfo>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<byte, FlagInfo> FlagsById { get; set; } = new Dictionary<byte, FlagInfo>();
        public bool HasFlag(string flagName)
        {
            return FlagsByName.ContainsKey(flagName);
        }
        public FlagInfo GetFlagInfo(string flagName)
        {
            if (FlagsByName.TryGetValue(flagName, out var flagInfo))
                return flagInfo;
            return null;
        }
        public byte GetFlagId(string flagName)
        {
            if (string.IsNullOrWhiteSpace(flagName))
                throw new ArgumentException("Flag name cannot be null or empty.", nameof(flagName));

            if (FlagsByName.TryGetValue(flagName, out var flagInfo))
                return flagInfo.Id;

            throw new KeyNotFoundException($"Flag '{flagName}' not found in version {Number}.");
        }
    }

    public class DatStructure
    {
        private Dictionary<int, VersionInfo> versions = new Dictionary<int, VersionInfo>();

        public DatStructure()
        {
            string xmlPath = "Appearances.xml";
            if (!File.Exists(xmlPath))
                throw new FileNotFoundException($"Appearances XML not found: {xmlPath}");

            XDocument doc;
            try
            {
                doc = XDocument.Load(xmlPath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load XML file '{xmlPath}'.", ex);
            }

            foreach (var versionElement in doc.Descendants("Version"))
            {
                var numberAttr = versionElement.Attribute("number");
                var structureAttr = versionElement.Attribute("structure");

                if (numberAttr == null || !int.TryParse(numberAttr.Value, out int versionNumber))
                    throw new InvalidOperationException("<Version> is missing a valid 'number' attribute.");

                if (structureAttr == null || !int.TryParse(structureAttr.Value, out int structure))
                    throw new InvalidOperationException($"Version {versionNumber} is missing a valid 'structure' attribute.");

                var versionInfo = new VersionInfo
                {
                    Number = versionNumber,
                    Structure = structure
                };

                var flagsElement = versionElement.Element("Flags");
                if (flagsElement != null)
                {
                    foreach (var flagElement in flagsElement.Descendants("Flag"))
                    {
                        var idAttr = flagElement.Attribute("id");
                        var nameAttr = flagElement.Attribute("name");

                        if (idAttr == null || !byte.TryParse(idAttr.Value, out byte id))
                            throw new InvalidOperationException($"Flag under version {versionNumber} is missing a valid 'id' attribute.");

                        if (nameAttr == null)
                            throw new InvalidOperationException($"Flag with id {id} under version {versionNumber} is missing the 'name' attribute.");

                        string flagName = nameAttr.Value;
                        var flagInfo = new FlagInfo { Id = id, Name = flagName };
                        versionInfo.FlagsByName[flagName] = flagInfo;
                        versionInfo.FlagsById[id] = flagInfo;
                    }
                }

                versions[versionNumber] = versionInfo;
            }
        }

        public VersionInfo GetVersionInfo(int versionNumber)
        {
            if (!versions.TryGetValue(versionNumber, out var vInfo))
                throw new InvalidOperationException($"No data found for version {versionNumber}");
            return vInfo;
        }

        public IEnumerable<VersionInfo> GetAllVersions()
        {
            return versions.Values;
        }

        public bool HasFlag(int versionNumber, string flagName)
        {
            var vInfo = GetVersionInfo(versionNumber);
            return vInfo.FlagsByName.ContainsKey(flagName);
        }

        public int GetStructure(int versionNumber)
        {
            var vInfo = GetVersionInfo(versionNumber);
            return vInfo.Structure;
        }

        public static DatInfo ReadAppearanceInfo(BinaryReader r)
        {
            DatInfo info = new DatInfo
            {
                Signature = r.ReadUInt32(),
                ObjectCount = r.ReadUInt16(),
                OutfitCount = r.ReadUInt16(),
                EffectCount = r.ReadUInt16(),
                MissileCount = r.ReadUInt16()
            };

            return info;
        }

        public static Appearance ReadAppearance(BinaryReader r, APPEARANCE_TYPE type, VersionInfo versionInfo)
        {
            Appearance appearance = new Appearance();
            appearance.AppearanceType = type;
            appearance.Flags = new AppearanceFlags();

            ReadAppearanceAttr(appearance, r, versionInfo);

            if (versionInfo.Structure == 1)
            {
                byte FrameGroupCount = 1;

                for (int i = 0; i < FrameGroupCount; i++)
                {
                    //FIXED_FRAME_GROUP
                    FrameGroup frameGroup = new FrameGroup();
                    frameGroup.SpriteInfo = new SpriteInfo();
                    frameGroup.FixedFrameGroup = FIXED_FRAME_GROUP.OutfitIdle;

                    frameGroup.SpriteInfo.PatternWidth = r.ReadByte();
                    frameGroup.SpriteInfo.PatternHeight = r.ReadByte();

                    if (frameGroup.SpriteInfo.PatternWidth > 1 || frameGroup.SpriteInfo.PatternHeight > 1)
                        frameGroup.SpriteInfo.PatternSize = r.ReadByte();
                    else
                        frameGroup.SpriteInfo.PatternSize = 32;

                    frameGroup.SpriteInfo.PatternLayers = r.ReadByte();

                    frameGroup.SpriteInfo.PatternX = r.ReadByte();

                    frameGroup.SpriteInfo.PatternY = r.ReadByte();

                    frameGroup.SpriteInfo.PatternZ = r.ReadByte();

                    frameGroup.SpriteInfo.PatternFrames = r.ReadByte();

                    if (frameGroup.SpriteInfo.PatternFrames > 1)
                    {
                        SpriteAnimation spriteAnimation = new SpriteAnimation();
                        spriteAnimation.AnimationMode = ANIMATION_ANIMATION_MODE.AnimationAsynchronized;
                        spriteAnimation.LoopCount = 1;
                        spriteAnimation.DefaultStartPhase = 1;

                        for (int k = 0; k < frameGroup.SpriteInfo.PatternFrames; k++)
                        {
                            SpritePhase spritePhase = new SpritePhase();
                            spritePhase.DurationMin = 100;
                            spritePhase.DurationMax = 100;
                            spriteAnimation.SpritePhase.Add(spritePhase);
                        }
                        frameGroup.SpriteInfo.Animation = spriteAnimation;

                    }
                    int NumSprites = (int)(frameGroup.SpriteInfo.PatternWidth * frameGroup.SpriteInfo.PatternHeight * frameGroup.SpriteInfo.PatternLayers * frameGroup.SpriteInfo.PatternX * frameGroup.SpriteInfo.PatternY * frameGroup.SpriteInfo.PatternZ * frameGroup.SpriteInfo.PatternFrames);
                    for (var x = 0; x < NumSprites; x++)
                    {
                        var sprite = r.ReadUInt32();
                        frameGroup.SpriteInfo.SpriteId.Add(sprite);

                    }
                    appearance.FrameGroup.Add(frameGroup);
                }
            }
            else if (versionInfo.Structure == 3)
            {


                byte FrameGroupCount = 1;
                if (type == APPEARANCE_TYPE.AppearanceOutfit)
                    FrameGroupCount = r.ReadByte();

                for (int i = 0; i < FrameGroupCount; i++)
                {
                    //FIXED_FRAME_GROUP
                    FrameGroup frameGroup = new FrameGroup();
                    frameGroup.SpriteInfo = new SpriteInfo();
                    if (type == APPEARANCE_TYPE.AppearanceOutfit)
                    {
                        byte FrameGroupType = r.ReadByte();
                        frameGroup.FixedFrameGroup = (FIXED_FRAME_GROUP)FrameGroupType;
                    }
                    else
                        frameGroup.FixedFrameGroup = FIXED_FRAME_GROUP.OutfitIdle;

                    frameGroup.SpriteInfo.PatternWidth = r.ReadByte();
                    frameGroup.SpriteInfo.PatternHeight = r.ReadByte();

                    if (frameGroup.SpriteInfo.PatternWidth > 1 || frameGroup.SpriteInfo.PatternHeight > 1)
                        frameGroup.SpriteInfo.PatternSize = r.ReadByte();
                    else
                        frameGroup.SpriteInfo.PatternSize = 32;

                    frameGroup.SpriteInfo.PatternLayers = r.ReadByte();

                    frameGroup.SpriteInfo.PatternX = r.ReadByte();

                    frameGroup.SpriteInfo.PatternY = r.ReadByte();

                    frameGroup.SpriteInfo.PatternZ = r.ReadByte();

                    frameGroup.SpriteInfo.PatternFrames = r.ReadByte();

                    if (frameGroup.SpriteInfo.PatternFrames > 1)
                    {
                        SpriteAnimation spriteAnimation = new SpriteAnimation();
                        spriteAnimation.AnimationMode = (ANIMATION_ANIMATION_MODE)r.ReadByte();
                        spriteAnimation.LoopCount = r.ReadUInt32();
                        spriteAnimation.DefaultStartPhase = r.ReadByte();

                        for (int k = 0; k < frameGroup.SpriteInfo.PatternFrames; k++)
                        {
                            SpritePhase spritePhase = new SpritePhase();
                            spritePhase.DurationMin = r.ReadUInt32();
                            spritePhase.DurationMax = r.ReadUInt32();
                            spriteAnimation.SpritePhase.Add(spritePhase);
                        }
                        frameGroup.SpriteInfo.Animation = spriteAnimation;

                    }
                    int NumSprites = (int)(frameGroup.SpriteInfo.PatternWidth * frameGroup.SpriteInfo.PatternHeight * frameGroup.SpriteInfo.PatternLayers * frameGroup.SpriteInfo.PatternX * frameGroup.SpriteInfo.PatternY * frameGroup.SpriteInfo.PatternZ * frameGroup.SpriteInfo.PatternFrames);
                    for (var x = 0; x < NumSprites; x++)
                    {
                        var sprite = r.ReadUInt32();
                        frameGroup.SpriteInfo.SpriteId.Add(sprite);

                    }
                    appearance.FrameGroup.Add(frameGroup);
                }
            }

            return appearance;
        }

        public static void ReadAppearanceAttr(Appearance appearance, BinaryReader r, VersionInfo versionInfo)
        {
            byte opt;
            while ((opt = r.ReadByte()) != 0xFF)
            {
                if (!versionInfo.FlagsById.TryGetValue(opt, out FlagInfo flagInfo))
                {
                    throw new Exception($"Unknown flag {opt} for version {versionInfo}");
                }
                switch (flagInfo.Name)
                {
                    case "Ground":
                        appearance.Flags.Bank = new AppearanceFlagBank
                        {
                            Waypoints = r.ReadUInt16()
                        };
                        break;

                    case "Clip": // order 1
                        appearance.Flags.Clip = true;
                        break;

                    case "Top": // order 5
                        appearance.Flags.Top = true;
                        break;

                    case "Bottom": // order 2
                        appearance.Flags.Bottom = true;
                        break;

                    case "Container":
                        appearance.Flags.Container = true;
                        break;

                    case "Stackable":
                        appearance.Flags.Cumulative = true;
                        break;

                    case "Usable":
                        appearance.Flags.Usable = true;
                        break;

                    case "ForceUse":
                        appearance.Flags.Forceuse = true;
                        break;

                    case "Multiuse":
                        appearance.Flags.Multiuse = true;
                        break;

                    case "Writeable":
                        appearance.Flags.Write = new AppearanceFlagWrite
                        {
                            MaxTextLength = r.ReadUInt16()
                        };
                        break;

                    case "WriteableOnce":
                        appearance.Flags.WriteOnce = new AppearanceFlagWriteOnce
                        {
                            MaxTextLengthOnce = r.ReadUInt16()
                        };
                        break;

                    case "LiquidPool":
                        appearance.Flags.Liquidpool = true;
                        break;

                    case "LiquidContainer":
                        appearance.Flags.Liquidcontainer = true;
                        break;

                    case "Impassable":
                        appearance.Flags.Unpass = true;
                        break;

                    case "Unmovable":
                        appearance.Flags.Unmove = true;
                        break;

                    case "BlocksSight":
                        appearance.Flags.Unsight = true;
                        break;

                    case "BlocksPathfinding":
                        appearance.Flags.Avoid = true;
                        break;

                    case "NoMovementAnimation":
                        appearance.Flags.NoMovementAnimation = true;
                        break;

                    case "Pickupable":
                        appearance.Flags.Take = true;
                        break;

                    case "Hangable":
                        appearance.Flags.Hang = true;
                        break;

                    case "HooksSouth":
                        appearance.Flags.HookSouth = true;
                        break;

                    case "HooksEast":
                        appearance.Flags.HookEast = true;
                        break;

                    case "Rotateable":
                        appearance.Flags.Rotate = true;
                        break;

                    case "LightSource":
                        appearance.Flags.Light = new AppearanceFlagLight
                        {
                            Brightness = r.ReadUInt16(),
                            Color = r.ReadUInt16()
                        };
                        break;

                    case "AlwaysSeen":
                        appearance.Flags.DontHide = true;
                        break;

                    case "Translucent":
                        appearance.Flags.Translucent = true;
                        break;

                    case "Displaced":
                        appearance.Flags.Shift = new AppearanceFlagShift
                        {
                            X = r.ReadUInt16(),
                            Y = r.ReadUInt16()
                        };
                        break;

                    case "Elevated":
                        appearance.Flags.Height = new AppearanceFlagHeight
                        {
                            Elevation = r.ReadUInt16(),
                        };
                        break;

                    case "LyingObject":
                        appearance.Flags.LyingObject = true;
                        break;

                    case "AlwaysAnimated":
                        appearance.Flags.AnimateAlways = true;
                        break;

                    case "MinimapColor":
                        appearance.Flags.Automap = new AppearanceFlagAutomap
                        {
                            Color = r.ReadUInt16(),
                        };
                        break;

                    case "FullTile":
                        appearance.Flags.Fullbank = true;
                        break;

                    case "HelpInfo":
                        appearance.Flags.Lenshelp = new AppearanceFlagLenshelp
                        {
                            Id = r.ReadUInt16(),
                        };
                        break;

                    case "Lookthrough":
                        appearance.Flags.IgnoreLook = true;
                        break;

                    case "Clothes":
                        appearance.Flags.Clothes = new AppearanceFlagClothes
                        {
                            Slot = r.ReadUInt16(),
                        };
                        break;

                    case "DefaultAction":
                        appearance.Flags.DefaultAction = new AppearanceFlagDefaultAction
                        {
                            Action = (PLAYER_ACTION)r.ReadUInt16(),
                        };
                        break;

                    case "Market":
                        appearance.Flags.Market = new AppearanceFlagMarket
                        {
                            Category = (ITEM_CATEGORY)r.ReadUInt16(),
                            TradeAsObjectId = r.ReadUInt16(),
                            ShowAsObjectId = r.ReadUInt16(),
                        };

                        ushort MarketNameSize = r.ReadUInt16();
                        byte[] buffer = r.ReadBytes(MarketNameSize);
                        appearance.Name = Encoding.GetEncoding("ISO-8859-1").GetString(buffer, 0, buffer.Length);

                        ushort MarketProfession = r.ReadUInt16();
                        appearance.Flags.Market.Vocation = (VOCATION)MarketProfession;

                        appearance.Flags.Market.MinimumLevel = r.ReadUInt16();

                        break;

                    case "Wrappable":
                        appearance.Flags.Wrap = true;
                        break;

                    case "UnWrappable":
                        appearance.Flags.Unwrap = true;
                        break;

                    case "TopEffect":
                        appearance.Flags.Topeffect = true;
                        break;

                    case "Default":
                        //ret.Default = true;
                        break;

                    default:
                        throw new Exception("Unknown appearance attribute " + opt);

                }
            }
        }
        public static void WriteAppearance(BinaryWriter w, Appearance item, VersionInfo versionInfo)
        {
            WriteAppearanceAttr(w, item, versionInfo);

            if (versionInfo.Structure == 1)
            {
                byte Width = (byte)item.FrameGroup[0].SpriteInfo.PatternWidth;
                byte Height = (byte)item.FrameGroup[0].SpriteInfo.PatternHeight;

                w.Write(Width);
                w.Write(Height);

                if (Width > 1 || Height > 1)
                    w.Write((byte)item.FrameGroup[0].SpriteInfo.PatternSize);

                w.Write((byte)item.FrameGroup[0].SpriteInfo.PatternLayers);
                w.Write((byte)item.FrameGroup[0].SpriteInfo.PatternX);
                w.Write((byte)item.FrameGroup[0].SpriteInfo.PatternY);
                w.Write((byte)item.FrameGroup[0].SpriteInfo.PatternZ);
                w.Write((byte)item.FrameGroup[0].SpriteInfo.PatternFrames);


                for (var x = 0; x < item.FrameGroup[0].SpriteInfo.SpriteId.Count; x++)
                {
                    w.Write(item.FrameGroup[0].SpriteInfo.SpriteId[x]);
                }
            }
            else if (versionInfo.Structure == 3)
            {
                if (item.AppearanceType == APPEARANCE_TYPE.AppearanceOutfit)
                    w.Write((byte)item.FrameGroup.Count);

                for (int i = 0; i < item.FrameGroup.Count; i++)
                {
                    if (item.AppearanceType == APPEARANCE_TYPE.AppearanceOutfit)
                        w.Write((byte)item.FrameGroup[i].FixedFrameGroup);

                    byte Width = (byte)item.FrameGroup[i].SpriteInfo.PatternWidth;
                    byte Height = (byte)item.FrameGroup[i].SpriteInfo.PatternHeight;

                    w.Write(Width);
                    w.Write(Height);

                    if (Width > 1 || Height > 1)
                        w.Write((byte)item.FrameGroup[i].SpriteInfo.PatternSize);

                    w.Write((byte)item.FrameGroup[i].SpriteInfo.PatternLayers);
                    w.Write((byte)item.FrameGroup[i].SpriteInfo.PatternX);
                    w.Write((byte)item.FrameGroup[i].SpriteInfo.PatternY);
                    w.Write((byte)item.FrameGroup[i].SpriteInfo.PatternZ);
                    w.Write((byte)item.FrameGroup[i].SpriteInfo.PatternFrames);

                    if (item.FrameGroup[i].SpriteInfo.PatternFrames > 1)
                    {
                        w.Write(Convert.ToByte(item.FrameGroup[i].SpriteInfo.Animation.AnimationMode));
                        w.Write(item.FrameGroup[i].SpriteInfo.Animation.LoopCount);
                        w.Write((byte)item.FrameGroup[i].SpriteInfo.Animation.DefaultStartPhase);

                        for (int k = 0; k < item.FrameGroup[i].SpriteInfo.Animation.SpritePhase.Count; k++)
                        {
                            w.Write(item.FrameGroup[i].SpriteInfo.Animation.SpritePhase[k].DurationMin);
                            w.Write(item.FrameGroup[i].SpriteInfo.Animation.SpritePhase[k].DurationMax);
                        }
                    }

                    for (var x = 0; x < item.FrameGroup[i].SpriteInfo.SpriteId.Count; x++)
                    {
                        w.Write(item.FrameGroup[i].SpriteInfo.SpriteId[x]);
                    }
                }
            }

        }
        public static void WriteAppearanceAttr(BinaryWriter w, Appearance item, VersionInfo versionInfo)
        {
            if (item.Flags.Bank != null && versionInfo.HasFlag("Ground"))
            {
                w.Write((byte)versionInfo.GetFlagId("Ground"));
                if (item.Flags.Bank.HasWaypoints)
                    w.Write((ushort)item.Flags.Bank.Waypoints);
            }

            if (item.Flags.Clip && versionInfo.HasFlag("Clip"))
                w.Write((byte)versionInfo.GetFlagId("Clip"));

            if (item.Flags.Top && versionInfo.HasFlag("Top"))
                w.Write((byte)versionInfo.GetFlagId("Top"));

            if (item.Flags.Bottom && versionInfo.HasFlag("Bottom"))
                w.Write((byte)versionInfo.GetFlagId("Bottom"));

            if (item.Flags.Container && versionInfo.HasFlag("Container"))
                w.Write((byte)versionInfo.GetFlagId("Container"));

            if (item.Flags.Cumulative && versionInfo.HasFlag("Stackable"))
                w.Write((byte)versionInfo.GetFlagId("Stackable"));

            if (item.Flags.Forceuse && versionInfo.HasFlag("ForceUse"))
                w.Write((byte)versionInfo.GetFlagId("ForceUse"));

            if (item.Flags.Usable && versionInfo.HasFlag("Usable"))
                w.Write((byte)versionInfo.GetFlagId("Usable"));

            if (item.Flags.Multiuse && versionInfo.HasFlag("Multiuse"))
                w.Write((byte)versionInfo.GetFlagId("Multiuse"));

            if (item.Flags.Write != null && versionInfo.HasFlag("Writeable"))
            {
                w.Write((byte)versionInfo.GetFlagId("Writeable"));
                w.Write((ushort)item.Flags.Write.MaxTextLength);
            }

            if (item.Flags.WriteOnce != null && versionInfo.HasFlag("WriteableOnce"))
            {
                w.Write((byte)versionInfo.GetFlagId("WriteableOnce"));
                w.Write((ushort)item.Flags.WriteOnce.MaxTextLengthOnce);
            }

            if (item.Flags.Liquidpool && versionInfo.HasFlag("LiquidPool"))
                w.Write((byte)versionInfo.GetFlagId("LiquidPool"));

            if (item.Flags.Liquidcontainer && versionInfo.HasFlag("LiquidContainer"))
                w.Write((byte)versionInfo.GetFlagId("LiquidContainer"));

            if (item.Flags.Unpass && versionInfo.HasFlag("Impassable"))
                w.Write((byte)versionInfo.GetFlagId("Impassable"));

            if (item.Flags.Unmove && versionInfo.HasFlag("Unmovable"))
                w.Write((byte)versionInfo.GetFlagId("Unmovable"));

            if (item.Flags.Unsight && versionInfo.HasFlag("BlocksSight"))
                w.Write((byte)versionInfo.GetFlagId("BlocksSight"));

            if (item.Flags.Avoid && versionInfo.HasFlag("BlocksPathfinding"))
                w.Write((byte)versionInfo.GetFlagId("BlocksPathfinding"));

            if (item.Flags.NoMovementAnimation && versionInfo.HasFlag("NoMovementAnimation"))
                w.Write((byte)versionInfo.GetFlagId("NoMovementAnimation"));

            if (item.Flags.Take && versionInfo.HasFlag("Pickupable"))
                w.Write((byte)versionInfo.GetFlagId("Pickupable"));

            if (item.Flags.Hang && versionInfo.HasFlag("Hangable"))
                w.Write((byte)versionInfo.GetFlagId("Hangable"));

            if (item.Flags.HookSouth && versionInfo.HasFlag("HooksSouth"))
                w.Write((byte)versionInfo.GetFlagId("HooksSouth"));

            if (item.Flags.HookEast && versionInfo.HasFlag("HooksEast"))
                w.Write((byte)versionInfo.GetFlagId("HooksEast"));

            if (item.Flags.Rotate && versionInfo.HasFlag("Rotateable"))
                w.Write((byte)versionInfo.GetFlagId("Rotateable"));

            if (item.Flags.Light != null && versionInfo.HasFlag("LightSource"))
            {
                w.Write((byte)versionInfo.GetFlagId("LightSource"));
                w.Write((ushort)item.Flags.Light.Brightness);
                w.Write((ushort)item.Flags.Light.Color);
            }

            if (item.Flags.DontHide && versionInfo.HasFlag("AlwaysSeen"))
                w.Write((byte)versionInfo.GetFlagId("AlwaysSeen"));

            if (item.Flags.Translucent && versionInfo.HasFlag("Translucent"))
                w.Write((byte)versionInfo.GetFlagId("Translucent"));

            if (item.Flags.Shift != null && versionInfo.HasFlag("Displaced"))
            {
                w.Write((byte)versionInfo.GetFlagId("Displaced"));
                w.Write((ushort)item.Flags.Shift.X);
                w.Write((ushort)item.Flags.Shift.Y);
            }

            if (item.Flags.Height != null && versionInfo.HasFlag("Elevated"))
            {
                w.Write((byte)versionInfo.GetFlagId("Elevated"));
                w.Write((ushort)item.Flags.Height.Elevation);
            }

            if (item.Flags.LyingObject && versionInfo.HasFlag("LyingObject"))
                w.Write((byte)versionInfo.GetFlagId("LyingObject"));

            if (item.Flags.AnimateAlways && versionInfo.HasFlag("AlwaysAnimated"))
                w.Write((byte)versionInfo.GetFlagId("AlwaysAnimated"));

            if (item.Flags.Automap != null && versionInfo.HasFlag("MinimapColor"))
            {
                w.Write((byte)versionInfo.GetFlagId("MinimapColor"));
                w.Write((ushort)item.Flags.Automap.Color);
            }

            if (item.Flags.Fullbank && versionInfo.HasFlag("FullTile"))
                w.Write((byte)versionInfo.GetFlagId("FullTile"));

            if (item.Flags.Lenshelp != null && versionInfo.HasFlag("HelpInfo"))
            {
                w.Write((byte)versionInfo.GetFlagId("HelpInfo"));
                w.Write((ushort)item.Flags.Lenshelp.Id);
            }

            if (item.Flags.IgnoreLook && versionInfo.HasFlag("Lookthrough"))
                w.Write((byte)versionInfo.GetFlagId("Lookthrough"));

            if (item.Flags.Clothes != null && versionInfo.HasFlag("Clothes"))
            {
                w.Write((byte)versionInfo.GetFlagId("Clothes"));
                w.Write((ushort)item.Flags.Clothes.Slot);
            }

            if (item.Flags.Market != null && versionInfo.HasFlag("Market"))
            {
                w.Write((byte)versionInfo.GetFlagId("Market"));

                ushort category = (ushort)item.Flags.Market.Category;
                if (category > 22)
                    category = 9;
                w.Write(category);

                w.Write((ushort)item.Flags.Market.TradeAsObjectId);
                w.Write((ushort)item.Flags.Market.ShowAsObjectId);
                w.Write((ushort)item.Name.Length);
                for (UInt16 i = 0; i < item.Name.Length; ++i)
                    w.Write((char)item.Name[i]);
                w.Write((ushort)item.Flags.Market.Vocation);
                w.Write((ushort)item.Flags.Market.MinimumLevel);
            }

            if (item.Flags.DefaultAction != null && versionInfo.HasFlag("DefaultAction"))
            {
                w.Write((byte)versionInfo.GetFlagId("DefaultAction"));
                w.Write((ushort)item.Flags.DefaultAction.Action);
            }

            if (item.Flags.Wrap && versionInfo.HasFlag("Wrappable"))
                w.Write((byte)versionInfo.GetFlagId("Wrappable"));

            if (item.Flags.Unwrap && versionInfo.HasFlag("UnWrappable"))
                w.Write((byte)versionInfo.GetFlagId("UnWrappable"));

            if (item.Flags.Topeffect && versionInfo.HasFlag("TopEffect"))
                w.Write((byte)versionInfo.GetFlagId("TopEffect"));


            w.Write((byte)0xFF);
        }

    }
}