using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
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

    enum AppearanceFlag860 : byte
    {
        Ground = 0,
        Clip = 1,
        Bottom = 2,
        Top = 3,
        Container = 4,
        Stackable = 5,
        ForceUse = 6,
        Multiuse = 7,
        Writeable = 8,
        WriteableOnce = 9,
        LiquidContainer = 10,
        LiquidPool = 11,
        Impassable = 12,
        Unmovable = 13,
        BlocksSight = 14,
        BlocksPathfinding = 15,
        Pickupable = 16,
        Hangable = 17,
        HooksSouth = 18,
        HooksEast = 19,
        Rotateable = 20,
        LightSource = 21,
        AlwaysSeen = 22,
        Translucent = 23,
        Displaced = 24,
        Elevated = 25,
        LyingObject = 26,
        AlwaysAnimated = 27,
        MinimapColor = 28,
        HelpInfo = 29,
        FullTile = 30,
        Lookthrough = 31,
        Clothes = 32,
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

        public static bool WriteLegacyDat(string fn, uint signature, Appearances appearances)
        {
            return WriteLegacyDat(fn, new LegacyAssetExportProfile
            {
                Id = "legacy1098-custom",
                DisplayName = "Legacy 10.98 compatible",
                DatLayout = LegacyDatLayout.Tibia1098,
                DatSignature = signature,
                SprSignature = 0,
                Transparency = false,
                SpriteIdsU32 = true,
                IncludeEnhancedAnimations = true,
                IncludeFrameGroups = true,
                IncludeModernFlags = true,
            }, appearances);
        }

        public static bool WriteLegacyDat(string fn, LegacyAssetExportProfile profile, Appearances appearances)
        {
            try
            {
                var datFile = new FileStream(fn, FileMode.Create, FileAccess.Write);
                using (var w = new BinaryWriter(datFile, Encoding.GetEncoding("ISO-8859-1")))
                {
                    w.Write(profile.DatSignature);
                    w.Write((ushort)(appearances.Object.Count + 99));
                    w.Write((ushort)appearances.Outfit.Count);
                    w.Write((ushort)appearances.Effect.Count);
                    w.Write((ushort)appearances.Missile.Count);
                    foreach (Appearance appearance in appearances.Object.OrderBy(item => item.Id))
                    {
                        WriteAppearance(w, appearance, profile);
                    }
                    foreach (Appearance appearance in appearances.Outfit.OrderBy(item => item.Id))
                    {
                        WriteAppearance(w, appearance, profile);
                    }
                    foreach (Appearance appearance in appearances.Effect.OrderBy(item => item.Id))
                    {
                        WriteAppearance(w, appearance, profile);
                    }
                    foreach (Appearance appearance in appearances.Missile.OrderBy(item => item.Id))
                    {
                        WriteAppearance(w, appearance, profile);
                    }

                    return true;
                }
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        private static void WriteAppearance(BinaryWriter w, Appearance appearance, LegacyAssetExportProfile profile)
        {
            if (profile.DatLayout == LegacyDatLayout.Tibia860)
            {
                WriteAppearance860(w, appearance, profile);
                return;
            }

            WriteAppearance1098(w, appearance);
        }

        public static void WriteAppearance860(BinaryWriter w, Appearance item, LegacyAssetExportProfile profile)
        {
            item.Flags ??= new AppearanceFlags();

            if (item.Flags.Bank != null)
            {
                w.Write((byte)AppearanceFlag860.Ground);
                w.Write((ushort)(item.Flags.Bank.HasWaypoints ? item.Flags.Bank.Waypoints : 0));
            }

            if (item.Flags.Clip)
                w.Write((byte)AppearanceFlag860.Clip);

            if (item.Flags.Top)
                w.Write((byte)AppearanceFlag860.Top);

            if (item.Flags.Bottom)
                w.Write((byte)AppearanceFlag860.Bottom);

            if (item.Flags.Container)
                w.Write((byte)AppearanceFlag860.Container);

            if (item.Flags.Cumulative)
                w.Write((byte)AppearanceFlag860.Stackable);

            if (item.Flags.Forceuse || item.Flags.Usable)
                w.Write((byte)AppearanceFlag860.ForceUse);

            if (item.Flags.Multiuse)
                w.Write((byte)AppearanceFlag860.Multiuse);

            if (item.Flags.Write != null)
            {
                w.Write((byte)AppearanceFlag860.Writeable);
                w.Write((ushort)item.Flags.Write.MaxTextLength);
            }

            if (item.Flags.WriteOnce != null)
            {
                w.Write((byte)AppearanceFlag860.WriteableOnce);
                w.Write((ushort)item.Flags.WriteOnce.MaxTextLengthOnce);
            }

            if (item.Flags.Liquidcontainer)
                w.Write((byte)AppearanceFlag860.LiquidContainer);

            if (item.Flags.Liquidpool)
                w.Write((byte)AppearanceFlag860.LiquidPool);

            if (item.Flags.Unpass)
                w.Write((byte)AppearanceFlag860.Impassable);

            if (item.Flags.Unmove)
                w.Write((byte)AppearanceFlag860.Unmovable);

            if (item.Flags.Unsight)
                w.Write((byte)AppearanceFlag860.BlocksSight);

            if (item.Flags.Avoid)
                w.Write((byte)AppearanceFlag860.BlocksPathfinding);

            if (item.Flags.Take)
                w.Write((byte)AppearanceFlag860.Pickupable);

            if (item.Flags.Hang)
                w.Write((byte)AppearanceFlag860.Hangable);

            if (item.Flags.HookSouth)
                w.Write((byte)AppearanceFlag860.HooksSouth);

            if (item.Flags.HookEast)
                w.Write((byte)AppearanceFlag860.HooksEast);

            if (item.Flags.Rotate)
                w.Write((byte)AppearanceFlag860.Rotateable);

            if (item.Flags.Light != null)
            {
                w.Write((byte)AppearanceFlag860.LightSource);
                w.Write((ushort)item.Flags.Light.Brightness);
                w.Write((ushort)item.Flags.Light.Color);
            }

            if (item.Flags.DontHide)
                w.Write((byte)AppearanceFlag860.AlwaysSeen);

            if (item.Flags.Translucent)
                w.Write((byte)AppearanceFlag860.Translucent);

            if (item.Flags.Shift != null)
            {
                w.Write((byte)AppearanceFlag860.Displaced);
                w.Write((ushort)item.Flags.Shift.X);
                w.Write((ushort)item.Flags.Shift.Y);
            }

            if (item.Flags.Height != null)
            {
                w.Write((byte)AppearanceFlag860.Elevated);
                w.Write((ushort)item.Flags.Height.Elevation);
            }

            if (item.Flags.LyingObject)
                w.Write((byte)AppearanceFlag860.LyingObject);

            if (item.Flags.AnimateAlways)
                w.Write((byte)AppearanceFlag860.AlwaysAnimated);

            if (item.Flags.Automap != null)
            {
                w.Write((byte)AppearanceFlag860.MinimapColor);
                w.Write((ushort)item.Flags.Automap.Color);
            }

            if (item.Flags.Lenshelp != null)
            {
                w.Write((byte)AppearanceFlag860.HelpInfo);
                w.Write((ushort)item.Flags.Lenshelp.Id);
            }

            if (item.Flags.Fullbank)
                w.Write((byte)AppearanceFlag860.FullTile);

            if (item.Flags.IgnoreLook)
                w.Write((byte)AppearanceFlag860.Lookthrough);

            if (profile.IncludeModernFlags && item.Flags.Clothes != null)
            {
                w.Write((byte)AppearanceFlag860.Clothes);
                w.Write((ushort)item.Flags.Clothes.Slot);
            }

            w.Write((byte)AppearanceFlag860.Default);
            WriteSpriteInfo860(w, SelectLegacyFrameGroup(item).SpriteInfo, profile);
        }

        private static FrameGroup SelectLegacyFrameGroup(Appearance item)
        {
            if (item.FrameGroup.Count == 0)
            {
                var frameGroup = new FrameGroup
                {
                    FixedFrameGroup = FIXED_FRAME_GROUP.OutfitIdle,
                    SpriteInfo = new SpriteInfo
                    {
                        PatternWidth = 1,
                        PatternHeight = 1,
                        PatternSize = 32,
                        PatternLayers = 1,
                        PatternX = 1,
                        PatternY = 1,
                        PatternZ = 1,
                        PatternFrames = 1,
                    }
                };
                frameGroup.SpriteInfo.SpriteId.Add(0);
                return frameGroup;
            }

            if (item.AppearanceType == APPEARANCE_TYPE.AppearanceOutfit)
            {
                var movingGroup = item.FrameGroup.FirstOrDefault(group => group.FixedFrameGroup == FIXED_FRAME_GROUP.OutfitMoving);
                if (movingGroup != null)
                {
                    return movingGroup;
                }
            }

            return item.FrameGroup[0];
        }

        private static void WriteSpriteInfo860(BinaryWriter w, SpriteInfo spriteInfo, LegacyAssetExportProfile profile)
        {
            byte width = (byte)Math.Max(1, Math.Min(byte.MaxValue, spriteInfo.PatternWidth));
            byte height = (byte)Math.Max(1, Math.Min(byte.MaxValue, spriteInfo.PatternHeight));

            w.Write(width);
            w.Write(height);

            if (width > 1 || height > 1)
                w.Write((byte)Math.Max(1, Math.Min(byte.MaxValue, spriteInfo.PatternSize)));

            byte layers = (byte)Math.Max(1, Math.Min(byte.MaxValue, spriteInfo.PatternLayers));
            byte patternX = (byte)Math.Max(1, Math.Min(byte.MaxValue, spriteInfo.PatternX));
            byte patternY = (byte)Math.Max(1, Math.Min(byte.MaxValue, spriteInfo.PatternY));
            byte patternZ = (byte)Math.Max(1, Math.Min(byte.MaxValue, spriteInfo.PatternZ));
            byte frames = (byte)Math.Max(1, Math.Min(byte.MaxValue, spriteInfo.PatternFrames));

            w.Write(layers);
            w.Write(patternX);
            w.Write(patternY);
            w.Write(patternZ);
            w.Write(frames);

            var numSprites = width * height * layers * patternX * patternY * patternZ * frames;
            for (var i = 0; i < numSprites; i++)
            {
                var spriteId = i < spriteInfo.SpriteId.Count ? spriteInfo.SpriteId[i] : 0;
                if (profile.SpriteIdsU32)
                {
                    w.Write(spriteId);
                }
                else
                {
                    w.Write((ushort)Math.Min(ushort.MaxValue, spriteId));
                }
            }
        }

        public static void WriteAppearance1098(BinaryWriter w, Appearance item)
        {
            if (item.Flags == null)
            {
                item.Flags = new();
                MainWindow.Log("Export to 1098: missing flags for client id " + item.Id);
            }

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
                FrameGroup frameGroup = item.FrameGroup[i];

                if (item.AppearanceType == APPEARANCE_TYPE.AppearanceOutfit)
                    w.Write((byte)frameGroup.FixedFrameGroup);

                SpriteInfo spriteInfo = frameGroup.SpriteInfo;
                byte width = (byte)spriteInfo.PatternWidth;
                byte height = (byte)spriteInfo.PatternHeight;

                w.Write(width);
                w.Write(height);

                if (width > 1 || height > 1)
                    w.Write((byte)spriteInfo.PatternSize);

                w.Write((byte)spriteInfo.PatternLayers);
                w.Write((byte)spriteInfo.PatternX);
                w.Write((byte)spriteInfo.PatternY);
                w.Write((byte)spriteInfo.PatternZ);
                w.Write((byte)spriteInfo.PatternFrames);

                if (spriteInfo.PatternFrames > 1)
                {
                    w.Write(Convert.ToByte(spriteInfo.Animation.AnimationMode));
                    w.Write(spriteInfo.Animation.LoopCount);
                    w.Write((byte)spriteInfo.Animation.DefaultStartPhase);

                    for (int k = 0; k < spriteInfo.Animation.SpritePhase.Count; k++)
                    {
                        w.Write(spriteInfo.Animation.SpritePhase[k].DurationMin);
                        w.Write(spriteInfo.Animation.SpritePhase[k].DurationMax);
                    }
                }

                uint numSprites = spriteInfo.PatternWidth * spriteInfo.PatternHeight * spriteInfo.PatternLayers * spriteInfo.PatternX * spriteInfo.PatternY * spriteInfo.PatternZ * spriteInfo.PatternFrames;
                for (var x = 0; x < numSprites; x++)
                {
                    if (x < spriteInfo.SpriteId.Count)
                        w.Write(spriteInfo.SpriteId[(int)x]);
                    else
                        w.Write((uint)0);
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
