using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Tibia.Protobuf.Appearances;

namespace Assets_Editor
{
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

        public void ReadLegacyDat(string file, int version)
        {
            var versionInfo = MainWindow.datStructure.GetVersionInfo(version);
            using var stream = File.OpenRead(file);
            using var r = new BinaryReader(stream);
            try
            {
                DatInfo info = DatStructure.ReadAppearanceInfo(r);
                Signature = info.Signature;
                ObjectCount = info.ObjectCount;
                OutfitCount = info.OutfitCount;
                EffectCount = info.EffectCount;
                MissileCount = info.MissileCount;

                PresetSettings preset = MainWindow.GetCurrentPreset() ?? new();

                // raw rd file has 2 bytes at the front of every item
                bool rdBytes = versionInfo.UseRDBytes;

                for (int i = 100; i <= ObjectCount; i++)
                {
                    if (rdBytes) {
                        r.ReadUInt16();
                    }

                    var appearance = DatStructure.ReadAppearance(r, APPEARANCE_TYPE.AppearanceObject, versionInfo, preset);
                    appearance.Id = (uint)i;
                    Appearances.Object.Add(appearance);
                }

                for (int i = 1; i <= OutfitCount; i++)
                {
                    if (rdBytes) {
                        r.ReadUInt16();
                    }

                    var appearance = DatStructure.ReadAppearance(r, APPEARANCE_TYPE.AppearanceOutfit, versionInfo, preset);
                    appearance.Id = (uint)i;
                    Appearances.Outfit.Add(appearance);
                }

                for (int i = 1; i <= EffectCount; i++)
                {
                    if (rdBytes) {
                        r.ReadUInt16();
                    }

                    var appearance = DatStructure.ReadAppearance(r, APPEARANCE_TYPE.AppearanceEffect, versionInfo, preset);
                    appearance.Id = (uint)i;
                    Appearances.Effect.Add(appearance);
                }

                for (int i = 1; i <= MissileCount; i++)
                {
                    if (rdBytes) {
                        r.ReadUInt16();
                    }

                    var appearance = DatStructure.ReadAppearance(r, APPEARANCE_TYPE.AppearanceMissile, versionInfo, preset);
                    appearance.Id = (uint)i;
                    Appearances.Missile.Add(appearance);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Couldn't load dat file", e);
            }
        }
        public static bool WriteLegacyDat(string fn, uint signature, Appearances appearances, int version)
        {
            try
            {
                VersionInfo versionInfo = MainWindow.datStructure.GetVersionInfo(version);
                FileStream datFile = new(fn, FileMode.Create, FileAccess.Write);
                using var w = new BinaryWriter(datFile);
                w.Write(signature);
                w.Write((ushort)(appearances.Object.Count + 99));
                w.Write((ushort)appearances.Outfit.Count);
                w.Write((ushort)appearances.Effect.Count);
                w.Write((ushort)appearances.Missile.Count);

                PresetSettings preset = MainWindow.GetCurrentPreset() ?? new();

                // raw rd file has 2 bytes at the front of every item
                bool rdBytes = versionInfo.UseRDBytes;

                foreach (Appearance appearance in appearances.Object.OrderBy(a => a.Id)) {
                    if (rdBytes) {
                        w.Write((ushort)0);
                    }

                    DatStructure.WriteAppearance(w, appearance, versionInfo, preset);
                }
                foreach (Appearance appearance in appearances.Outfit.OrderBy(a => a.Id)) {
                    if (rdBytes) {
                        w.Write((ushort)0);
                    }

                    DatStructure.WriteAppearance(w, appearance, versionInfo, preset);
                }
                foreach (Appearance appearance in appearances.Effect.OrderBy(a => a.Id)) {
                    if (rdBytes) {
                        w.Write((ushort)0);
                    }

                    DatStructure.WriteAppearance(w, appearance, versionInfo, preset);
                }
                foreach (Appearance appearance in appearances.Missile.OrderBy(a => a.Id)) {
                    if (rdBytes) {
                        w.Write((ushort)0);
                    }

                    DatStructure.WriteAppearance(w, appearance, versionInfo, preset);
                }

                return true;
            }
            catch (UnauthorizedAccessException exception)
            {
                ErrorManager.ShowError(exception.Message);
                return false;
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
        public static MemoryStream GetObjectImage(Appearance appearance, SpriteStorage spriteStorage)
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

                        using Bitmap _bmp = new Bitmap(spriteStorage.getSpriteStream((uint)spriteId));
                        g.DrawImage(_bmp, new Rectangle(px, py, Sprite.DefaultSize, Sprite.DefaultSize));
                    }
                }
            }
            MemoryStream combinedStream = new MemoryStream();
            bitmap.Save(combinedStream, ImageFormat.Png);
            return combinedStream;
        }
        public static MemoryStream GetObjectImage(Appearance appearance, SpriteStorage spriteStorage, int frame)
        {
            int width = (int)(Sprite.DefaultSize * appearance.FrameGroup[0].SpriteInfo.PatternWidth);
            int height = (int)(Sprite.DefaultSize * appearance.FrameGroup[0].SpriteInfo.PatternHeight);

            // handle bad sprite
            if (width == 0 || height == 0) {
                return new();
            }

            using Bitmap bitmap = new(width, height, PixelFormat.Format32bppArgb);
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

                        using Bitmap _bmp = new Bitmap(spriteStorage.getSpriteStream((uint)spriteId));
                        g.DrawImage(_bmp, new Rectangle(px, py, Sprite.DefaultSize, Sprite.DefaultSize));
                    }
                }
            }
            MemoryStream combinedStream = new();
            bitmap.Save(combinedStream, ImageFormat.Png);
            return combinedStream;
        }
    }
}
