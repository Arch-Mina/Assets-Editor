using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Tibia.Protobuf.Appearances;

namespace Assets_Editor
{
    public sealed class LegacyAssetExportOptions
    {
        public required string InputPath { get; init; }
        public required string OutputPath { get; init; }
        public required LegacyAssetExportProfile Profile { get; init; }
        public bool Overwrite { get; init; }
        public bool Backup { get; init; } = true;
    }

    public sealed class LegacyAssetExportResult
    {
        public required string DatPath { get; init; }
        public required string SprPath { get; init; }
        public required LegacyAssetExportProfile Profile { get; init; }
        public IReadOnlyList<string> Backups { get; init; } = [];
    }

    public sealed class LegacyAssetExporter
    {
        public async Task<LegacyAssetExportResult> ExportAsync(LegacyAssetExportOptions options, IProgress<string> log = null, IProgress<int> spriteProgress = null, IProgress<int> sprCompileProgress = null)
        {
            ArgumentNullException.ThrowIfNull(options);
            var outputPath = Path.GetFullPath(options.OutputPath);
            Directory.CreateDirectory(outputPath);

            var datPath = Path.Combine(outputPath, "Tibia.dat");
            var sprPath = Path.Combine(outputPath, "Tibia.spr");
            var backups = PrepareOutputFiles(options, datPath, sprPath);

            log?.Report($"Loading modern assets from '{options.InputPath}'");
            using var assets = ModernAssetSet.Load(options.InputPath);
            var spriteStorage = new SpriteStorage();
            var spriteOffsets = new ConcurrentDictionary<string, uint>();

            await Task.Run(() => BuildSpriteStorage(assets, spriteStorage, spriteOffsets, spriteProgress, log));

            log?.Report("Building legacy appearance table");
            var legacyAppearances = assets.Appearances.Clone();
            BuildLegacyAppearances(assets, legacyAppearances, spriteOffsets, options.Profile, log);

            log?.Report($"Writing {options.Profile.DisplayName} dat");
            LegacyAppearance.WriteLegacyDat(datPath, options.Profile, legacyAppearances);
            LegacyDatValidator.ValidateExport(datPath, options.Profile, (uint)Math.Max(0, spriteStorage.SprLists.Count - 1));

            log?.Report($"Writing {options.Profile.DisplayName} spr");
            await Sprite.CompileSpritesAsync(sprPath, spriteStorage, options.Profile.Transparency, options.Profile.SprSignature, sprCompileProgress);

            return new LegacyAssetExportResult
            {
                DatPath = datPath,
                SprPath = sprPath,
                Profile = options.Profile,
                Backups = backups,
            };
        }

        private static List<string> PrepareOutputFiles(LegacyAssetExportOptions options, params string[] paths)
        {
            var backups = new List<string>();
            foreach (var path in paths)
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                if (!options.Overwrite)
                {
                    throw new IOException($"Output file already exists: '{path}'. Pass --overwrite to replace it.");
                }

                if (options.Backup)
                {
                    var backupPath = $"{path}.{DateTime.Now:yyyyMMddHHmmss}.bak";
                    File.Copy(path, backupPath, overwrite: false);
                    backups.Add(backupPath);
                }
            }

            return backups;
        }

        private static void BuildSpriteStorage(ModernAssetSet assets, SpriteStorage spriteStorage, ConcurrentDictionary<string, uint> spriteOffsets, IProgress<int> progress, IProgress<string> log)
        {
            var sheets = assets.GetSpriteSheets().ToList();
            var totalSprites = sheets.Sum(sheet => Math.Max(0, sheet.LastSpriteid - sheet.FirstSpriteid + 1));
            var processedSprites = 0;
            var lastProgress = -1;
            uint nextSpriteId = 1;

            foreach (var sheet in sheets)
            {
                for (var spriteId = sheet.FirstSpriteid; spriteId <= sheet.LastSpriteid; spriteId++)
                {
                    using var sourceStream = assets.GetSpriteStream(spriteId);
                    using var bitmap = new Bitmap(sourceStream);
                    var slices = SplitImage(bitmap);

                    for (var sliceIndex = 0; sliceIndex < slices.Count; sliceIndex++)
                    {
                        using var slice = slices[sliceIndex];
                        var spriteKey = $"{spriteId}_{sliceIndex}";
                        if (IsImageFullyTransparent(slice))
                        {
                            spriteOffsets[spriteKey] = 0;
                            continue;
                        }

                        var memoryStream = new MemoryStream();
                        slice.Save(memoryStream, ImageFormat.Png);
                        memoryStream.Position = 0;
                        spriteStorage.SprLists[(int)nextSpriteId] = memoryStream;
                        spriteOffsets[spriteKey] = nextSpriteId;
                        nextSpriteId++;
                    }

                    processedSprites++;
                    var currentProgress = totalSprites == 0 ? 100 : processedSprites * 100 / totalSprites;
                    if (currentProgress != lastProgress)
                    {
                        progress?.Report(currentProgress);
                        lastProgress = currentProgress;
                    }
                }
            }
        }

        private static void BuildLegacyAppearances(ModernAssetSet assets, Appearances appearances, ConcurrentDictionary<string, uint> spriteOffsets, LegacyAssetExportProfile profile, IProgress<string> log)
        {
            var objectCount = appearances.Object.Count > 0 ? appearances.Object.Max(item => item.Id) : 99;
            var outfitCount = appearances.Outfit.Count > 0 ? appearances.Outfit.Max(item => item.Id) : 0;
            var effectCount = appearances.Effect.Count > 0 ? appearances.Effect.Max(item => item.Id) : 0;
            var missileCount = appearances.Missile.Count > 0 ? appearances.Missile.Max(item => item.Id) : 0;

            for (uint id = 100; id <= objectCount; id++)
            {
                var appearance = appearances.Object.FirstOrDefault(item => item.Id == id);
                if (appearance == null)
                {
                    appearances.Object.Add(CreateBlankObject(id, APPEARANCE_TYPE.AppearanceObject));
                    continue;
                }

                appearance.AppearanceType = APPEARANCE_TYPE.AppearanceObject;
                NormalizeHookFlags(appearance);
                UpdateAppearanceObject(assets, appearance, spriteOffsets, profile, log);
            }

            for (uint id = 1; id <= outfitCount; id++)
            {
                var appearance = appearances.Outfit.FirstOrDefault(item => item.Id == id);
                if (appearance == null)
                {
                    appearances.Outfit.Add(CreateBlankObject(id, APPEARANCE_TYPE.AppearanceOutfit));
                    continue;
                }

                appearance.AppearanceType = APPEARANCE_TYPE.AppearanceOutfit;
                UpdateAppearanceObject(assets, appearance, spriteOffsets, profile, log);
            }

            for (uint id = 1; id <= effectCount; id++)
            {
                var appearance = appearances.Effect.FirstOrDefault(item => item.Id == id);
                if (appearance == null)
                {
                    appearances.Effect.Add(CreateBlankObject(id, APPEARANCE_TYPE.AppearanceEffect));
                    continue;
                }

                appearance.AppearanceType = APPEARANCE_TYPE.AppearanceEffect;
                UpdateAppearanceObject(assets, appearance, spriteOffsets, profile, log);
            }

            for (uint id = 1; id <= missileCount; id++)
            {
                var appearance = appearances.Missile.FirstOrDefault(item => item.Id == id);
                if (appearance == null)
                {
                    appearances.Missile.Add(CreateBlankObject(id, APPEARANCE_TYPE.AppearanceMissile));
                    continue;
                }

                appearance.AppearanceType = APPEARANCE_TYPE.AppearanceMissile;
                UpdateAppearanceObject(assets, appearance, spriteOffsets, profile, log);
            }
        }

        private static List<Bitmap> SplitImage(Bitmap originalImage)
        {
            var splitImages = new List<Bitmap>();
            var rows = originalImage.Height / Sprite.DefaultSize;
            var columns = originalImage.Width / Sprite.DefaultSize;

            for (var row = 0; row < rows; row++)
            {
                for (var column = 0; column < columns; column++)
                {
                    var cloneRect = new Rectangle(column * Sprite.DefaultSize, row * Sprite.DefaultSize, Sprite.DefaultSize, Sprite.DefaultSize);
                    splitImages.Add(originalImage.Clone(cloneRect, originalImage.PixelFormat));
                }
            }

            return splitImages;
        }

        private static bool IsImageFullyTransparent(Bitmap image)
        {
            if (Image.GetPixelFormatSize(image.PixelFormat) < 32)
            {
                return false;
            }

            var rect = new Rectangle(0, 0, image.Width, image.Height);
            var bitmapData = image.LockBits(rect, ImageLockMode.ReadOnly, image.PixelFormat);
            try
            {
                var bytes = Math.Abs(bitmapData.Stride) * image.Height;
                var values = new byte[bytes];
                Marshal.Copy(bitmapData.Scan0, values, 0, bytes);

                for (var index = 0; index < values.Length; index += 4)
                {
                    if (values[index + 3] != 0)
                    {
                        return false;
                    }
                }

                return true;
            }
            finally
            {
                image.UnlockBits(bitmapData);
            }
        }

        private static Appearance CreateBlankObject(uint id, APPEARANCE_TYPE type)
        {
            var appearance = new Appearance
            {
                AppearanceType = type,
                Id = id,
                Flags = new AppearanceFlags(),
            };

            var frameGroup = new FrameGroup
            {
                FixedFrameGroup = FIXED_FRAME_GROUP.OutfitIdle,
                SpriteInfo = new SpriteInfo
                {
                    PatternWidth = 1,
                    PatternHeight = 1,
                    PatternSize = Sprite.DefaultSize,
                    PatternLayers = 1,
                    PatternX = 1,
                    PatternY = 1,
                    PatternZ = 1,
                    PatternFrames = 1,
                }
            };
            frameGroup.SpriteInfo.SpriteId.Add(0);
            appearance.FrameGroup.Add(frameGroup);
            return appearance;
        }

        private static void NormalizeHookFlags(Appearance appearance)
        {
            if (appearance.Flags?.Hook == null)
            {
                return;
            }

            if (appearance.Flags.Hook.Direction == HOOK_TYPE.South)
            {
                appearance.Flags.HookSouth = true;
            }

            if (appearance.Flags.Hook.Direction == HOOK_TYPE.East)
            {
                appearance.Flags.HookEast = true;
            }
        }

        private static void UpdateAppearanceObject(ModernAssetSet assets, Appearance appearance, ConcurrentDictionary<string, uint> spriteOffsets, LegacyAssetExportProfile profile, IProgress<string> log)
        {
            if (appearance.FrameGroup.Count == 0)
            {
                appearance.FrameGroup.Add(CreateBlankObject(appearance.Id, appearance.AppearanceType).FrameGroup[0]);
            }

            for (var frameIndex = 0; frameIndex < appearance.FrameGroup.Count; frameIndex++)
            {
                var frameGroup = appearance.FrameGroup[frameIndex];
                var spriteInfo = frameGroup.SpriteInfo;
                if (spriteInfo == null || spriteInfo.SpriteId.Count == 0)
                {
                    frameGroup.SpriteInfo = CreateBlankObject(appearance.Id, appearance.AppearanceType).FrameGroup[0].SpriteInfo;
                    continue;
                }

                uint width = 1;
                uint height = 1;
                uint exactSize = Sprite.DefaultSize;
                var sliceType = assets.GetSheetType(spriteInfo.SpriteId[0]);
                if (sliceType == 1)
                {
                    height = 2;
                }
                else if (sliceType == 2)
                {
                    width = 2;
                }
                else if (sliceType == 3)
                {
                    width = 2;
                    height = 2;
                }

                if (width > 1 || height > 1)
                {
                    if (spriteInfo.BoundingBoxPerDirection.Count > 0)
                    {
                        exactSize = Math.Max(spriteInfo.BoundingBoxPerDirection[0].Width, spriteInfo.BoundingBoxPerDirection[0].Height);
                    }
                    else
                    {
                        log?.Report($"Missing bounding box for appearance {appearance.Id}; using 32px exact size.");
                    }
                }

                var sourcePatternWidth = spriteInfo.PatternWidth == 0 ? 1 : spriteInfo.PatternWidth;
                var sourcePatternHeight = spriteInfo.PatternHeight == 0 ? 1 : spriteInfo.PatternHeight;

                spriteInfo.PatternX = sourcePatternWidth;
                spriteInfo.PatternY = sourcePatternHeight;
                spriteInfo.PatternWidth = width;
                spriteInfo.PatternHeight = height;
                spriteInfo.PatternSize = exactSize;
                spriteInfo.PatternLayers = spriteInfo.Layers == 0 ? Math.Max(spriteInfo.PatternLayers, 1) : spriteInfo.Layers;
                spriteInfo.PatternZ = spriteInfo.PatternDepth == 0 ? Math.Max(spriteInfo.PatternZ, 1) : spriteInfo.PatternDepth;
                spriteInfo.PatternFrames = spriteInfo.Animation?.SpritePhase.Count > 0
                    ? (uint)spriteInfo.Animation.SpritePhase.Count
                    : Math.Max(spriteInfo.PatternFrames, 1);

                RemapSprites(spriteInfo, spriteOffsets, sliceType, appearance.Id, log);

                if (!profile.IncludeEnhancedAnimations)
                {
                    spriteInfo.Animation = null;
                }
            }
        }

        private static void RemapSprites(SpriteInfo spriteInfo, ConcurrentDictionary<string, uint> spriteOffsets, int sliceType, uint appearanceId, IProgress<string> log)
        {
            var remappedSprites = new List<uint>();
            foreach (var spriteId in spriteInfo.SpriteId)
            {
                var spriteName = spriteId.ToString();
                switch (sliceType)
                {
                    case 1:
                    case 2:
                        AddSpriteOffset(remappedSprites, spriteOffsets, $"{spriteName}_1", appearanceId, spriteId, log);
                        AddSpriteOffset(remappedSprites, spriteOffsets, $"{spriteName}_0", appearanceId, spriteId, log);
                        break;
                    case 3:
                        AddSpriteOffset(remappedSprites, spriteOffsets, $"{spriteName}_3", appearanceId, spriteId, log);
                        AddSpriteOffset(remappedSprites, spriteOffsets, $"{spriteName}_2", appearanceId, spriteId, log);
                        AddSpriteOffset(remappedSprites, spriteOffsets, $"{spriteName}_1", appearanceId, spriteId, log);
                        AddSpriteOffset(remappedSprites, spriteOffsets, $"{spriteName}_0", appearanceId, spriteId, log);
                        break;
                    default:
                        AddSpriteOffset(remappedSprites, spriteOffsets, $"{spriteName}_0", appearanceId, spriteId, log);
                        break;
                }
            }

            spriteInfo.SpriteId.Clear();
            foreach (var spriteId in remappedSprites)
            {
                spriteInfo.SpriteId.Add(spriteId);
            }
        }

        private static void AddSpriteOffset(List<uint> remappedSprites, ConcurrentDictionary<string, uint> spriteOffsets, string spriteKey, uint appearanceId, uint sourceSpriteId, IProgress<string> log)
        {
            if (spriteOffsets.TryGetValue(spriteKey, out var remappedSpriteId))
            {
                remappedSprites.Add(remappedSpriteId);
                return;
            }

            log?.Report($"Missing sliced sprite offset for appearance {appearanceId}, sprite {sourceSpriteId}, slice {spriteKey}; using blank sprite.");
            remappedSprites.Add(0);
        }
    }
}
