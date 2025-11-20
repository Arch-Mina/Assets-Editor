using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
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
            var versionInfo = MainWindow.datStructure.GetVersionInfo(1098);
            DatStructure.ReadAppearanceAttr(appearance, r, versionInfo);

            return appearance;
        }

        private static bool WriteAppearance1098(BinaryWriter w, Appearance item)
        {
            var versionInfo = MainWindow.datStructure.GetVersionInfo(1098);
            DatStructure.WriteAppearanceAttr(w, item, versionInfo);

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
