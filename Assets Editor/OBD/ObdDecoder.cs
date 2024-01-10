using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Tibia.Protobuf.Appearances;

namespace Assets_Editor
{
    public class ObdDecoder
    {
        private static Appearance ReadAppearance1098(BinaryReader r, APPEARANCE_TYPE type)
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
            
            return appearance;
        }

        private static bool WriteAppearance1098(BinaryWriter w, Appearance item)
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

            return true;
        }
        private static Appearance DecodeV2(BinaryReader reader, ref ConcurrentDictionary<int, MemoryStream> list)
        {
            ushort clientVersion = reader.ReadUInt16();
            byte category = reader.ReadByte();
            uint texturePos = reader.ReadUInt32();
            Appearance appearance = ReadAppearance1098(reader, (APPEARANCE_TYPE)category);

            //FIXED_FRAME_GROUP
            FrameGroup frameGroup = new FrameGroup();
            frameGroup.SpriteInfo = new SpriteInfo();
                
            frameGroup.FixedFrameGroup = FIXED_FRAME_GROUP.OutfitIdle;

            frameGroup.SpriteInfo.PatternWidth = reader.ReadByte();
            frameGroup.SpriteInfo.PatternHeight = reader.ReadByte();

            if (frameGroup.SpriteInfo.PatternWidth > 1 || frameGroup.SpriteInfo.PatternHeight > 1)
                frameGroup.SpriteInfo.PatternSize = reader.ReadByte();
            else
                frameGroup.SpriteInfo.PatternSize = 32;

            frameGroup.SpriteInfo.PatternLayers = reader.ReadByte();

            frameGroup.SpriteInfo.PatternX = reader.ReadByte();

            frameGroup.SpriteInfo.PatternY = reader.ReadByte();

            frameGroup.SpriteInfo.PatternZ = reader.ReadByte();

            frameGroup.SpriteInfo.PatternFrames = reader.ReadByte();
            
            if (frameGroup.SpriteInfo.PatternFrames > 1)
            {
                SpriteAnimation spriteAnimation = new SpriteAnimation();
                spriteAnimation.AnimationMode = (ANIMATION_ANIMATION_MODE)reader.ReadByte();
                spriteAnimation.LoopCount = reader.ReadUInt32();
                spriteAnimation.DefaultStartPhase = reader.ReadByte();

                for (int k = 0; k < frameGroup.SpriteInfo.PatternFrames; k++)
                {
                    SpritePhase spritePhase = new SpritePhase();
                    spritePhase.DurationMin = reader.ReadUInt32();
                    spritePhase.DurationMax = reader.ReadUInt32();
                    spriteAnimation.SpritePhase.Add(spritePhase);
                }
                frameGroup.SpriteInfo.Animation = spriteAnimation;
            }

            int NumSprites = (int)(frameGroup.SpriteInfo.PatternWidth * frameGroup.SpriteInfo.PatternHeight * frameGroup.SpriteInfo.PatternLayers * frameGroup.SpriteInfo.PatternX * frameGroup.SpriteInfo.PatternY * frameGroup.SpriteInfo.PatternZ * frameGroup.SpriteInfo.PatternFrames);
            for (var x = 0; x < NumSprites; x++)
            {
                var spriteId = reader.ReadUInt32();
                frameGroup.SpriteInfo.SpriteId.Add(spriteId);
                var dataSize = Sprite.ARGBPixelsDataSize;

                byte[] pixels = reader.ReadBytes(dataSize);

                Sprite spr = new Sprite();
                spr.Transparent = true;
                spr.CompressedPixels = Sprite.CompressPixelsARGB(pixels, true);
                spr.Size = (uint)spr.CompressedPixels.Length;
                using System.Drawing.Bitmap _bmp = spr.GetBitmap();
                MemoryStream memoryStream = new MemoryStream();
                //_bmp.Save(MainWindow._assetsPath + x + i + "test.png", System.Drawing.Imaging.ImageFormat.Png);
                _bmp.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                spr.CompressedPixels = null;
                list.TryAdd((int)spriteId, memoryStream);
            }
            
            appearance.FrameGroup.Add(frameGroup);
            return appearance;
        }
        private static Appearance DecodeV3(BinaryReader reader, ref ConcurrentDictionary<int, MemoryStream> list)
        {
            ushort clientVersion = reader.ReadUInt16();
            byte category = reader.ReadByte();
            uint texturePos = reader.ReadUInt32();
            Appearance appearance = ReadAppearance1098(reader, (APPEARANCE_TYPE)category);

            byte FrameGroupCount = 1;
            if (appearance.AppearanceType == APPEARANCE_TYPE.AppearanceOutfit)
                FrameGroupCount = reader.ReadByte();

            for (int i = 0; i < FrameGroupCount; i++)
            {
                //FIXED_FRAME_GROUP
                FrameGroup frameGroup = new FrameGroup();
                frameGroup.SpriteInfo = new SpriteInfo();
                if (appearance.AppearanceType == APPEARANCE_TYPE.AppearanceOutfit)
                {
                    byte FrameGroupType = reader.ReadByte();
                    frameGroup.FixedFrameGroup = (FIXED_FRAME_GROUP)FrameGroupType;
                }
                else
                    frameGroup.FixedFrameGroup = FIXED_FRAME_GROUP.OutfitIdle;

                frameGroup.SpriteInfo.PatternWidth = reader.ReadByte();
                frameGroup.SpriteInfo.PatternHeight = reader.ReadByte();

                if (frameGroup.SpriteInfo.PatternWidth > 1 || frameGroup.SpriteInfo.PatternHeight > 1)
                    frameGroup.SpriteInfo.PatternSize = reader.ReadByte();
                else
                    frameGroup.SpriteInfo.PatternSize = 32;

                frameGroup.SpriteInfo.PatternLayers = reader.ReadByte();

                frameGroup.SpriteInfo.PatternX = reader.ReadByte();

                frameGroup.SpriteInfo.PatternY = reader.ReadByte();

                frameGroup.SpriteInfo.PatternZ = reader.ReadByte();

                frameGroup.SpriteInfo.PatternFrames = reader.ReadByte();

                if (frameGroup.SpriteInfo.PatternFrames > 1)
                {
                    SpriteAnimation spriteAnimation = new SpriteAnimation();
                    spriteAnimation.AnimationMode = (ANIMATION_ANIMATION_MODE)reader.ReadByte();
                    spriteAnimation.LoopCount = reader.ReadUInt32();
                    spriteAnimation.DefaultStartPhase = reader.ReadByte();

                    for (int k = 0; k < frameGroup.SpriteInfo.PatternFrames; k++)
                    {
                        SpritePhase spritePhase = new SpritePhase();
                        spritePhase.DurationMin = reader.ReadUInt32();
                        spritePhase.DurationMax = reader.ReadUInt32();
                        spriteAnimation.SpritePhase.Add(spritePhase);
                    }
                    frameGroup.SpriteInfo.Animation = spriteAnimation;

                }

                int NumSprites = (int)(frameGroup.SpriteInfo.PatternWidth * frameGroup.SpriteInfo.PatternHeight * frameGroup.SpriteInfo.PatternLayers * frameGroup.SpriteInfo.PatternX * frameGroup.SpriteInfo.PatternY * frameGroup.SpriteInfo.PatternZ * frameGroup.SpriteInfo.PatternFrames);
                for (var x = 0; x < NumSprites; x++)
                {
                    var spriteId = reader.ReadUInt32();
                    frameGroup.SpriteInfo.SpriteId.Add(spriteId);
                    var dataSize = reader.ReadUInt32();
                    if (dataSize > Sprite.ARGBPixelsDataSize)
                        throw new Exception("Invalid sprite data size.");

                    byte[] pixels = reader.ReadBytes((int)dataSize);

                    Sprite spr = new Sprite();
                    spr.Transparent = true;
                    spr.CompressedPixels = Sprite.CompressPixelsARGB(pixels, true);
                    spr.Size = (uint)spr.CompressedPixels.Length;
                    using System.Drawing.Bitmap _bmp = spr.GetBitmap();
                    MemoryStream memoryStream = new MemoryStream();
                    //_bmp.Save(MainWindow._assetsPath + x + i + "test.png", System.Drawing.Imaging.ImageFormat.Png);
                    _bmp.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                    spr.CompressedPixels = null;
                    list.TryAdd((int)spriteId, memoryStream);
                }

                appearance.FrameGroup.Add(frameGroup);
            }
            return appearance;
        }

        public static Appearance Load(string path, ref ConcurrentDictionary<int, MemoryStream> list)
        {
            try
            {
                using BinaryReader br = new BinaryReader(new FileStream(path, FileMode.Open));
                byte[] bytes = br.ReadBytes((int)br.BaseStream.Length);
                br.Close();
                if (bytes == null)
                {
                    return null;
                }
                bytes = LZMA.Uncompress(bytes);
                using BinaryReader reader = new BinaryReader(new MemoryStream(bytes));
                ushort version = reader.ReadUInt16();

                if (version == 200)
                {
                    return DecodeV2(reader, ref list);
                }
                else if (version == 300)
                {
                    return DecodeV3(reader, ref list);
                }
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.StackTrace);
                return null;
            }
        }

        private static byte[] EncodeV2(Appearance data)
        {
            using (BinaryWriter writer = new BinaryWriter(new MemoryStream()))
            {
                // write obd version
                writer.Write((ushort)200);

                // write client version
                writer.Write((ushort)1098);

                // write category
                writer.Write((byte)data.AppearanceType);

                // skipping the texture patterns position.
                int patternsPosition = (int)writer.BaseStream.Position;
                writer.Seek(4, SeekOrigin.Current);

                if (!WriteAppearance1098(writer, data))
                {
                    return null;
                }

                // write the texture patterns position.
                int position = (int)writer.BaseStream.Position;
                writer.Seek(patternsPosition, SeekOrigin.Begin);
                writer.Write((uint)writer.BaseStream.Position);
                writer.Seek(position, SeekOrigin.Begin);

                FrameGroup group = data.FrameGroup[0];

                writer.Write((byte)group.SpriteInfo.PatternWidth);
                writer.Write((byte)group.SpriteInfo.PatternHeight);

                if (group.SpriteInfo.PatternWidth > 1 || group.SpriteInfo.PatternHeight > 1)
                {
                    writer.Write((byte)group.SpriteInfo.PatternSize);
                }

                writer.Write((byte)group.SpriteInfo.PatternLayers);
                writer.Write((byte)group.SpriteInfo.PatternX);
                writer.Write((byte)group.SpriteInfo.PatternY);
                writer.Write((byte)group.SpriteInfo.PatternZ);
                writer.Write((byte)group.SpriteInfo.PatternFrames);

                if (group.SpriteInfo.PatternFrames > 1)
                {
                    writer.Write((byte)group.SpriteInfo.Animation.AnimationMode);
                    writer.Write(group.SpriteInfo.Animation.LoopCount);
                    writer.Write((byte)group.SpriteInfo.Animation.DefaultStartPhase);

                    for (int i = 0; i < group.SpriteInfo.PatternFrames; i++)
                    {
                        writer.Write(group.SpriteInfo.Animation.SpritePhase[i].DurationMin);
                        writer.Write(group.SpriteInfo.Animation.SpritePhase[i].DurationMax);
                    }
                }

                for (int i = 0; i < group.SpriteInfo.SpriteId.Count; i++)
                {
                    byte[] pixels = Sprite.UncompressPixelsARGB(Sprite.CompressBitmap(new Bitmap(MainWindow.MainSprStorage.getSpriteStream(group.SpriteInfo.SpriteId[i])), true), true);
                    writer.Write(group.SpriteInfo.SpriteId[i]);
                    writer.Write(pixels);
                }

                return LZMA.Compress(((MemoryStream)writer.BaseStream).ToArray());
            }
        }

        private static byte[] EncodeV3(Appearance data)
        {
            using (BinaryWriter writer = new BinaryWriter(new MemoryStream()))
            {
                // write obd version
                writer.Write((ushort)300);

                // write client version
                writer.Write((ushort)1098);

                // write category
                writer.Write((byte)data.AppearanceType);

                // skipping the texture patterns position.
                int patternsPosition = (int)writer.BaseStream.Position;
                writer.Seek(4, SeekOrigin.Current);

                if (!WriteAppearance1098(writer, data))
                {
                    return null;
                }

                // write the texture patterns position.
                int position = (int)writer.BaseStream.Position;
                writer.Seek(patternsPosition, SeekOrigin.Begin);
                writer.Write((uint)writer.BaseStream.Position);
                writer.Seek(position, SeekOrigin.Begin);

                byte FrameGroupCount = 1;
                if (data.AppearanceType == APPEARANCE_TYPE.AppearanceOutfit)
                {
                    FrameGroupCount = (byte)data.FrameGroup.Count;
                    writer.Write(FrameGroupCount);
                }
                for (int frame = 0; frame < FrameGroupCount; frame++)
                {

                    FrameGroup group = data.FrameGroup[frame];

                    writer.Write((byte)group.FixedFrameGroup);
                    writer.Write((byte)group.SpriteInfo.PatternWidth);
                    writer.Write((byte)group.SpriteInfo.PatternHeight);

                    if (group.SpriteInfo.PatternWidth > 1 || group.SpriteInfo.PatternHeight > 1)
                    {
                        writer.Write((byte)group.SpriteInfo.PatternSize);
                    }

                    writer.Write((byte)group.SpriteInfo.PatternLayers);
                    writer.Write((byte)group.SpriteInfo.PatternX);
                    writer.Write((byte)group.SpriteInfo.PatternY);
                    writer.Write((byte)group.SpriteInfo.PatternZ);
                    writer.Write((byte)group.SpriteInfo.PatternFrames);

                    if (group.SpriteInfo.PatternFrames > 1)
                    {
                        writer.Write((byte)group.SpriteInfo.Animation.AnimationMode);
                        writer.Write((int)group.SpriteInfo.Animation.LoopCount);
                        writer.Write((byte)group.SpriteInfo.Animation.DefaultStartPhase);

                        for (int i = 0; i < group.SpriteInfo.PatternFrames; i++)
                        {
                            writer.Write(group.SpriteInfo.Animation.SpritePhase[i].DurationMin);
                            writer.Write(group.SpriteInfo.Animation.SpritePhase[i].DurationMax);
                        }
                    }

                    for (int i = 0; i < group.SpriteInfo.SpriteId.Count; i++)
                    {
                        byte[] pixels = Sprite.UncompressPixelsARGB(Sprite.CompressBitmap(new Bitmap(MainWindow.MainSprStorage.getSpriteStream(group.SpriteInfo.SpriteId[i])), true), true);
                        writer.Write(group.SpriteInfo.SpriteId[i]);
                        writer.Write(pixels.Length);
                        writer.Write(pixels);
                    }
                }
                return LZMA.Compress(((MemoryStream)writer.BaseStream).ToArray());
            }
        }
        public static bool Export(List<Appearance> appearances)
        {
            System.Windows.Forms.FolderBrowserDialog folder = new System.Windows.Forms.FolderBrowserDialog();
            if (folder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string exportFolder = folder.SelectedPath;
                foreach (var appearance in appearances)
                {
                    string fileName = "//" + appearance.Id + "_" + appearance.AppearanceType + (appearance.AppearanceType == APPEARANCE_TYPE.AppearanceOutfit ? "_v3" : "_v2") + ".obd";
                    using (BinaryWriter writer = new BinaryWriter(new FileStream(exportFolder + fileName, FileMode.Create)))
                    {
                        if (appearance.AppearanceType == APPEARANCE_TYPE.AppearanceOutfit)
                            writer.Write(EncodeV3(appearance));
                        else
                            writer.Write(EncodeV2(appearance));
                        writer.Close();
                    }
                }
                return true;
            }
            else
                return false;
        }
    }


}
