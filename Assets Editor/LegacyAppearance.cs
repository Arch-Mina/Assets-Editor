using Efundies;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tibia.Protobuf.Appearances;

namespace Assets_Editor
{
    enum AppearanceFlag1098 : byte
    {
        Ground = 0,
        Clip = 1,
        Bottom = 2,
        Top = 3,
        Container = 4,
        Stackable = 5,
        ForceUse = 6,
        Usable = 254,
        Multiuse = 7,
        Writeable = 8,
        WriteableOnce = 9,
        LiquidContainer = 10,
        LiquidPool = 11,
        Impassable = 12,
        Unmovable = 13,
        BlocksSight = 14,
        BlocksPathfinding = 15,
        NoMovementAnimation = 16,
        Pickupable = 17,
        Hangable = 18,
        HooksSouth = 19,
        HooksEast = 20,
        Rotateable = 21,
        LightSource = 22,
        AlwaysSeen = 23,
        Translucent = 24,
        Displaced = 25,
        Elevated = 26,
        LyingObject = 27,
        AlwaysAnimated = 28,
        MinimapColor = 29,
        HelpInfo = 30,
        FullTile = 31,
        Lookthrough = 32,
        Clothes = 33,
        Market = 34,
        DefaultAction = 35,
        Wrappable = 36,
        UnWrappable = 37,
        TopEffect = 38,
        Default = 255
    }
    public class LegacyAppearance
    {
        public LegacyAppearance()
        {
            Signature = 0;
            Appearances = new Appearances();
        }

        public uint Signature;
        public ushort ObjectCount;
        public ushort OutfitCount;
        public ushort EffectCount;
        public ushort MissileCount;
        public Appearances Appearances;

        public void ReadLegacyDat(string file)
        {
            using var stream = File.OpenRead(file);
            using var r = new BinaryReader(stream);
            try
            {
                Signature = r.ReadUInt32();
                ObjectCount = r.ReadUInt16();
                OutfitCount = r.ReadUInt16();
                EffectCount = r.ReadUInt16();
                MissileCount = r.ReadUInt16();

                for (int i = 100; i <= ObjectCount; i++)
                {
                    var appearance = ReadAppearance1098(r, APPEARANCE_TYPE.AppearanceObject);
                    appearance.Id = (uint)i;
                    Appearances.Object.Add(appearance);
                }

                for (int i = 1; i <= OutfitCount; i++)
                {
                    var appearance = ReadAppearance1098(r, APPEARANCE_TYPE.AppearanceOutfit);
                    appearance.Id = (uint)i;
                    Appearances.Outfit.Add(appearance);
                }

                for (int i = 1; i <= EffectCount; i++)
                {
                    var appearance = ReadAppearance1098(r, APPEARANCE_TYPE.AppearanceEffect);
                    appearance.Id = (uint)i;
                    Appearances.Effect.Add(appearance);
                }

                for (int i = 1; i <= MissileCount; i++)
                {
                    var appearance = ReadAppearance1098(r, APPEARANCE_TYPE.AppearanceMissile);
                    appearance.Id = (uint)i;
                    Appearances.Missile.Add(appearance);
                }
            }
            catch
            {
                throw new Exception("Couldn't load dat file");
            }
        }

        private Appearance ReadAppearance1098(BinaryReader r, APPEARANCE_TYPE type)
        {
            Appearance appearance = new Appearance();
            appearance.AppearanceType = type;
            appearance.Flags = new AppearanceFlags();
            byte opt;
            while ((opt = r.ReadByte()) != 0xFF)
            {
                switch ((AppearanceFlag1098)opt)
                {
                    case AppearanceFlag1098.Ground:
                        appearance.Flags.Bank = new AppearanceFlagBank
                        {
                            Waypoints = r.ReadUInt16()
                        };
                        break;

                    case AppearanceFlag1098.Clip: // order 1
                        appearance.Flags.Clip = true;
                        break;

                    case AppearanceFlag1098.Top: // order 5
                        appearance.Flags.Top = true;
                        break;

                    case AppearanceFlag1098.Bottom: // order 2
                        appearance.Flags.Bottom = true;
                        break;

                    case AppearanceFlag1098.Container:
                        appearance.Flags.Container = true;
                        break;

                    case AppearanceFlag1098.Stackable:
                        appearance.Flags.Cumulative = true;
                        break;

                    case AppearanceFlag1098.Usable:
                        appearance.Flags.Usable = true;
                        break;

                    case AppearanceFlag1098.ForceUse:
                        appearance.Flags.Forceuse = true;
                        break;

                    case AppearanceFlag1098.Multiuse:
                        appearance.Flags.Multiuse = true;
                        break;

                    case AppearanceFlag1098.Writeable:
                        appearance.Flags.Write = new AppearanceFlagWrite
                        {
                            MaxTextLength = r.ReadUInt16()
                        };
                        break;

                    case AppearanceFlag1098.WriteableOnce:
                        appearance.Flags.WriteOnce = new AppearanceFlagWriteOnce
                        {
                            MaxTextLengthOnce = r.ReadUInt16()
                        };
                        break;

                    case AppearanceFlag1098.LiquidPool:
                        appearance.Flags.Liquidpool = true;
                        break;

                    case AppearanceFlag1098.LiquidContainer:
                        appearance.Flags.Liquidcontainer = true;
                        break;

                    case AppearanceFlag1098.Impassable:
                        appearance.Flags.Unpass = true;
                        break;

                    case AppearanceFlag1098.Unmovable:
                        appearance.Flags.Unmove = true;
                        break;

                    case AppearanceFlag1098.BlocksSight:
                        appearance.Flags.Unsight = true;
                        break;

                    case AppearanceFlag1098.BlocksPathfinding:
                        appearance.Flags.Avoid = true;
                        break;

                    case AppearanceFlag1098.NoMovementAnimation:
                        appearance.Flags.NoMovementAnimation = true;
                        break;

                    case AppearanceFlag1098.Pickupable:
                        appearance.Flags.Take = true;
                        break;

                    case AppearanceFlag1098.Hangable:
                        appearance.Flags.Hang = true;
                        break;

                    case AppearanceFlag1098.HooksSouth:
                        appearance.Flags.HookSouth = true;
                        break;

                    case AppearanceFlag1098.HooksEast:
                        appearance.Flags.HookEast = true;
                        break;

                    case AppearanceFlag1098.Rotateable:
                        appearance.Flags.Rotate = true;
                        break;

                    case AppearanceFlag1098.LightSource:
                        appearance.Flags.Light = new AppearanceFlagLight
                        {
                            Brightness = r.ReadUInt16(),
                            Color = r.ReadUInt16()
                        };
                        break;

                    case AppearanceFlag1098.AlwaysSeen:
                        appearance.Flags.DontHide = true;
                        break;

                    case AppearanceFlag1098.Translucent:
                        appearance.Flags.Translucent = true;
                        break;

                    case AppearanceFlag1098.Displaced:
                        appearance.Flags.Shift = new AppearanceFlagShift
                        {
                            X = r.ReadUInt16(),
                            Y = r.ReadUInt16()
                        };
                        break;

                    case AppearanceFlag1098.Elevated:
                        appearance.Flags.Height = new AppearanceFlagHeight
                        {
                            Elevation = r.ReadUInt16(),
                        };
                        break;

                    case AppearanceFlag1098.LyingObject:
                        appearance.Flags.LyingObject = true;
                        break;

                    case AppearanceFlag1098.AlwaysAnimated:
                        appearance.Flags.AnimateAlways = true;
                        break;

                    case AppearanceFlag1098.MinimapColor:
                        appearance.Flags.Automap = new AppearanceFlagAutomap
                        {
                            Color = r.ReadUInt16(),
                        };
                        break;

                    case AppearanceFlag1098.FullTile:
                        appearance.Flags.Fullbank = true;
                        break;

                    case AppearanceFlag1098.HelpInfo:
                        appearance.Flags.Lenshelp = new AppearanceFlagLenshelp
                        {
                            Id = r.ReadUInt16(),
                        };
                        break;

                    case AppearanceFlag1098.Lookthrough:
                        appearance.Flags.IgnoreLook = true;
                        break;

                    case AppearanceFlag1098.Clothes:
                        appearance.Flags.Clothes = new AppearanceFlagClothes
                        {
                            Slot = r.ReadUInt16(),
                        };
                        break;

                    case AppearanceFlag1098.DefaultAction:
                        appearance.Flags.DefaultAction = new AppearanceFlagDefaultAction
                        {
                            Action = (PLAYER_ACTION)r.ReadUInt16(),
                        };
                        break;

                    case AppearanceFlag1098.Market:
                        appearance.Flags.Market = new AppearanceFlagMarket
                        {
                            Category = (ITEM_CATEGORY)r.ReadUInt16(),
                            TradeAsObjectId = r.ReadUInt16(),
                            ShowAsObjectId = r.ReadUInt16(),
                        };

                        ushort MarketNameSize = r.ReadUInt16();
                        byte[] buffer = r.ReadBytes(MarketNameSize);
                        appearance.Name = Encoding.Default.GetString(buffer, 0, buffer.Length);
                        
                        ushort MarketProfession = r.ReadUInt16();
                        appearance.Flags.Market.Vocation = (VOCATION)MarketProfession;

                        appearance.Flags.Market.MinimumLevel = r.ReadUInt16();

                        break;

                    case AppearanceFlag1098.Wrappable:
                        appearance.Flags.Wrap = true;
                        break;

                    case AppearanceFlag1098.UnWrappable:
                        appearance.Flags.Unwrap = true;
                        break;

                    case AppearanceFlag1098.TopEffect:
                        appearance.Flags.Topeffect = true;
                        break;

                    case AppearanceFlag1098.Default:
                        //ret.Default = true;
                        break;

                    default:
                        throw new Exception("Unknown appearance attribute " + opt);
                }
            }
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
                }else
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

            return appearance;
        }

        public static bool WriteLegacyDat(string fn, uint signature, Appearances appearances)
        {
            try
            {
                var datFile = new FileStream(fn, FileMode.Create, FileAccess.Write);
                using (var w = new BinaryWriter(datFile))
                {
                    w.Write(signature);
                    w.Write((ushort)(appearances.Object.Count + 99));
                    w.Write((ushort)appearances.Outfit.Count);
                    w.Write((ushort)appearances.Effect.Count);
                    w.Write((ushort)appearances.Missile.Count);

                    foreach (Appearance appearance in appearances.Object)
                    {
                        LegacyAppearance.WriteAppearance1098(w, appearance);
                    }
                    foreach (Appearance appearance in appearances.Outfit)
                    {
                        LegacyAppearance.WriteAppearance1098(w, appearance);
                    }
                    foreach (Appearance appearance in appearances.Effect)
                    {
                        LegacyAppearance.WriteAppearance1098(w, appearance);
                    }
                    foreach (Appearance appearance in appearances.Missile)
                    {
                        LegacyAppearance.WriteAppearance1098(w, appearance);
                    }

                    return true;
                }
            }
            catch (UnauthorizedAccessException exception)
            {
                return false;
            }
        }

        public static void WriteAppearance1098(BinaryWriter w, Appearance item)
        {
            if (item.Flags.Bank != null)
            {
                w.Write((byte)AppearanceFlag1098.Ground);
                if (item.Flags.Bank.HasWaypoints)
                    w.Write((ushort)item.Flags.Bank.Waypoints);
            }

            if (item.Flags.Clip)
                w.Write((byte)AppearanceFlag1098.Clip);

            if (item.Flags.Top)
                w.Write((byte)AppearanceFlag1098.Top);

            if (item.Flags.Bottom)
                w.Write((byte)AppearanceFlag1098.Bottom);

            if (item.Flags.Container)
                w.Write((byte)AppearanceFlag1098.Container);

            if (item.Flags.Cumulative)
                w.Write((byte)AppearanceFlag1098.Stackable);

            if (item.Flags.Forceuse)
                w.Write((byte)AppearanceFlag1098.ForceUse);

            if (item.Flags.Usable)
                w.Write((byte)AppearanceFlag1098.Usable);

            if (item.Flags.Multiuse)
                w.Write((byte)AppearanceFlag1098.Multiuse);

            if (item.Flags.Write != null)
            {
                w.Write((byte)AppearanceFlag1098.Writeable);
                w.Write((ushort)item.Flags.Write.MaxTextLength);
            }

            if (item.Flags.WriteOnce != null)
            {
                w.Write((byte)AppearanceFlag1098.WriteableOnce);
                w.Write((ushort)item.Flags.WriteOnce.MaxTextLengthOnce);
            }

            if (item.Flags.Liquidpool)
                w.Write((byte)AppearanceFlag1098.LiquidPool);

            if (item.Flags.Liquidcontainer)
                w.Write((byte)AppearanceFlag1098.LiquidContainer);

            if (item.Flags.Unpass)
                w.Write((byte)AppearanceFlag1098.Impassable);

            if (item.Flags.Unmove)
                w.Write((byte)AppearanceFlag1098.Unmovable);

            if (item.Flags.Unsight)
                w.Write((byte)AppearanceFlag1098.BlocksSight);

            if (item.Flags.Avoid)
                w.Write((byte)AppearanceFlag1098.BlocksPathfinding);

            if (item.Flags.NoMovementAnimation)
                w.Write((byte)AppearanceFlag1098.NoMovementAnimation);

            if (item.Flags.Take)
                w.Write((byte)AppearanceFlag1098.Pickupable);

            if (item.Flags.Hang)
                w.Write((byte)AppearanceFlag1098.Hangable);

            if (item.Flags.HookSouth)
                w.Write((byte)AppearanceFlag1098.HooksSouth);

            if (item.Flags.HookEast)
                w.Write((byte)AppearanceFlag1098.HooksEast);

            if (item.Flags.Rotate)
                w.Write((byte)AppearanceFlag1098.Rotateable);

            if (item.Flags.Light != null)
            {
                w.Write((byte)AppearanceFlag1098.LightSource);
                w.Write((ushort)item.Flags.Light.Brightness);
                w.Write((ushort)item.Flags.Light.Color);
            }

            if (item.Flags.DontHide)
                w.Write((byte)AppearanceFlag1098.AlwaysSeen);

            if (item.Flags.Translucent)
                w.Write((byte)AppearanceFlag1098.Translucent);

            if (item.Flags.Shift != null)
            {
                w.Write((byte)AppearanceFlag1098.Displaced);
                w.Write((ushort)item.Flags.Shift.X);
                w.Write((ushort)item.Flags.Shift.Y);
            }

            if (item.Flags.Height != null)
            {
                w.Write((byte)AppearanceFlag1098.Elevated);
                w.Write((ushort)item.Flags.Height.Elevation);
            }

            if (item.Flags.LyingObject)
                w.Write((byte)AppearanceFlag1098.LyingObject);

            if (item.Flags.AnimateAlways)
                w.Write((byte)AppearanceFlag1098.AlwaysAnimated);

            if (item.Flags.Automap != null)
            {
                w.Write((byte)AppearanceFlag1098.MinimapColor);
                w.Write((ushort)item.Flags.Automap.Color);
            }

            if (item.Flags.Fullbank)
                w.Write((byte)AppearanceFlag1098.FullTile);

            if (item.Flags.Lenshelp != null)
            {
                w.Write((byte)AppearanceFlag1098.HelpInfo);
                w.Write((ushort)item.Flags.Lenshelp.Id);
            }

            if (item.Flags.IgnoreLook)
                w.Write((byte)AppearanceFlag1098.Lookthrough);

            if (item.Flags.Clothes != null)
            {
                w.Write((byte)AppearanceFlag1098.Clothes);
                w.Write((ushort)item.Flags.Clothes.Slot);
            }

            if (item.Flags.Market != null)
            {
                w.Write((byte)AppearanceFlag1098.Market);

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

            if (item.Flags.DefaultAction != null)
            {
                w.Write((byte)AppearanceFlag1098.DefaultAction);
                w.Write((ushort)item.Flags.DefaultAction.Action);
            }

            if (item.Flags.Wrap)
                w.Write((byte)AppearanceFlag1098.Wrappable);

            if (item.Flags.Unwrap)
                w.Write((byte)AppearanceFlag1098.UnWrappable);

            if (item.Flags.Topeffect)
                w.Write((byte)AppearanceFlag1098.TopEffect);


            w.Write((byte)0xFF);

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

        public static int GetSpriteIndex(FrameGroup frameGroup, int width, int height, int layers, int patternX, int patternY, int patternZ, int frames)
        {
            var spriteInfo = frameGroup.SpriteInfo;
            int index = (int)(frames % spriteInfo.PatternFrames);
            index = index * (int)spriteInfo.PatternZ + patternZ;
            index = index * (int)spriteInfo.PatternY + patternY;
            index = index * (int)spriteInfo.PatternX + patternX;
            index = index * (int)spriteInfo.PatternLayers + layers;
            index = index * (int)spriteInfo.PatternHeight + height;
            index = index * (int)spriteInfo.PatternWidth + width;

            return index;
        }


        public static MemoryStream GetObjectImage(Appearance appearance, ConcurrentDictionary<int, MemoryStream> sprList)
        {
            int width = (int)(Sprite.DefaultSize * appearance.FrameGroup[0].SpriteInfo.PatternWidth);
            int height = (int)(Sprite.DefaultSize * appearance.FrameGroup[0].SpriteInfo.PatternHeight);

            using Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using Graphics g = Graphics.FromImage(bitmap);

            byte layers = (byte)(appearance.FrameGroup[0].SpriteInfo.PatternLayers);
            byte x = 0;

            if (appearance.AppearanceType == APPEARANCE_TYPE.AppearanceOutfit)
            {
                layers = 1;
                x = (byte)(2 % appearance.FrameGroup[0].SpriteInfo.PatternX);
            }

            // draw sprite
            for (byte l = 0; l < layers; l++)
            {
                for (byte w = 0; w < appearance.FrameGroup[0].SpriteInfo.PatternWidth; w++)
                {
                    for (byte h = 0; h < appearance.FrameGroup[0].SpriteInfo.PatternHeight; h++)
                    {
                        int index = GetSpriteIndex(appearance.FrameGroup[0], w, h, l, x, 0, 0, 0);
                        int spriteId = (int)appearance.FrameGroup[0].SpriteInfo.SpriteId[index];
                        int px = (int)((appearance.FrameGroup[0].SpriteInfo.PatternWidth - w - 1) * Sprite.DefaultSize);
                        int py = (int)((appearance.FrameGroup[0].SpriteInfo.PatternHeight - h - 1) * Sprite.DefaultSize);

                        using Bitmap _bmp = new Bitmap(sprList[spriteId]);
                        g.DrawImage(_bmp, new Rectangle(px, py, Sprite.DefaultSize, Sprite.DefaultSize));
                    }
                }
            }
            MemoryStream combinedStream = new MemoryStream();
            bitmap.Save(combinedStream, ImageFormat.Png);
            return combinedStream;
        }
        public static MemoryStream GetObjectImage(Appearance appearance, ConcurrentDictionary<int, MemoryStream> sprList, int frame)
        {
            int width = (int)(Sprite.DefaultSize * appearance.FrameGroup[0].SpriteInfo.PatternWidth);
            int height = (int)(Sprite.DefaultSize * appearance.FrameGroup[0].SpriteInfo.PatternHeight);

            using Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using Graphics g = Graphics.FromImage(bitmap);

            byte layers = (byte)(appearance.FrameGroup[0].SpriteInfo.PatternLayers);
            byte x = 0;

            if (appearance.AppearanceType == APPEARANCE_TYPE.AppearanceOutfit)
            {
                layers = 1;
                x = (byte)(2 % appearance.FrameGroup[0].SpriteInfo.PatternX);
            }

            // draw sprite
            for (byte l = 0; l < layers; l++)
            {
                for (byte w = 0; w < appearance.FrameGroup[0].SpriteInfo.PatternWidth; w++)
                {
                    for (byte h = 0; h < appearance.FrameGroup[0].SpriteInfo.PatternHeight; h++)
                    {
                        int index = GetSpriteIndex(appearance.FrameGroup[0], w, h, l, x, 0, 0, frame);
                        int spriteId = (int)appearance.FrameGroup[0].SpriteInfo.SpriteId[index];
                        int px = (int)((appearance.FrameGroup[0].SpriteInfo.PatternWidth - w - 1) * Sprite.DefaultSize);
                        int py = (int)((appearance.FrameGroup[0].SpriteInfo.PatternHeight - h - 1) * Sprite.DefaultSize);

                        using Bitmap _bmp = new Bitmap(sprList[spriteId]);
                        g.DrawImage(_bmp, new Rectangle(px, py, Sprite.DefaultSize, Sprite.DefaultSize));
                    }
                }
            }
            MemoryStream combinedStream = new MemoryStream();
            bitmap.Save(combinedStream, ImageFormat.Png);
            return combinedStream;
        }
    }
}
