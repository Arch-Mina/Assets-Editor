using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Tibia.Protobuf.Appearances;

namespace Assets_Editor
{
    public sealed class ModernAssetSet
    {
        private readonly object loadLock = new();
        private readonly ConcurrentDictionary<int, MemoryStream> spriteStreams = new();

        private ModernAssetSet(string assetsPath, List<MainWindow.Catalog> catalog, Appearances appearances)
        {
            AssetsPath = assetsPath;
            Catalog = catalog;
            Appearances = appearances;
            spriteStreams[0] = CreateBlankSpriteStream();
        }

        public string AssetsPath { get; }
        public List<MainWindow.Catalog> Catalog { get; }
        public Appearances Appearances { get; }

        public static ModernAssetSet Load(string inputPath)
        {
            var assetsPath = ResolveAssetsPath(inputPath);
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            var catalogPath = Path.Combine(assetsPath, "catalog-content.json");
            var catalog = JsonConvert.DeserializeObject<List<MainWindow.Catalog>>(File.ReadAllText(catalogPath), settings);
            if (catalog == null || catalog.Count == 0)
            {
                throw new InvalidOperationException($"No catalog entries found in '{catalogPath}'.");
            }

            var appearanceEntry = catalog.FirstOrDefault(entry => entry.Type == "appearances") ?? catalog[0];
            var appearancePath = Path.Combine(assetsPath, appearanceEntry.File);
            if (!File.Exists(appearancePath))
            {
                throw new FileNotFoundException("Appearance protobuf was not found.", appearancePath);
            }

            using var appearanceStream = File.OpenRead(appearancePath);
            var appearances = Appearances.Parser.ParseFrom(appearanceStream);
            return new ModernAssetSet(assetsPath, catalog, appearances);
        }

        public MemoryStream GetSpriteStream(int spriteId)
        {
            if (spriteStreams.TryGetValue(spriteId, out var cached) && cached != null)
            {
                return CloneStream(cached);
            }

            var sheet = Catalog.FirstOrDefault(entry =>
                entry.Type == "sprite" &&
                spriteId >= entry.FirstSpriteid &&
                spriteId <= entry.LastSpriteid);

            if (sheet == null)
            {
                return CloneStream(spriteStreams[0]);
            }

            lock (loadLock)
            {
                if (!spriteStreams.TryGetValue(spriteId, out cached) || cached == null)
                {
                    var spritePath = Path.Combine(AssetsPath, sheet.File);
                    if (!File.Exists(spritePath))
                    {
                        throw new FileNotFoundException("Sprite sheet was not found.", spritePath);
                    }

                    using var bitmap = LZMA.DecompressFileLZMA(spritePath);
                    GenerateTileSetImageList(bitmap, sheet);
                }
            }

            if (!spriteStreams.TryGetValue(spriteId, out cached) || cached == null)
            {
                return CloneStream(spriteStreams[0]);
            }

            return CloneStream(cached);
        }

        public int GetSheetType(uint spriteId)
        {
            var sheet = Catalog.FirstOrDefault(entry =>
                entry.Type == "sprite" &&
                entry.FirstSpriteid <= spriteId &&
                entry.LastSpriteid >= spriteId);

            return sheet?.SpriteType ?? 0;
        }

        public IEnumerable<MainWindow.Catalog> GetSpriteSheets()
        {
            return Catalog.Where(entry => entry.Type == "sprite" && entry.SpriteType >= 0);
        }

        private void GenerateTileSetImageList(Bitmap bitmap, MainWindow.Catalog sheet)
        {
            var tileCount = sheet.LastSpriteid - sheet.FirstSpriteid;
            var spriteCount = 0;

            var xCols = sheet.SpriteType is 0 or 1 ? 12 : 6;
            var yCols = sheet.SpriteType is 0 or 2 ? 12 : 6;
            var tileWidth = sheet.SpriteType is 0 or 1 ? 32 : 64;
            var tileHeight = sheet.SpriteType is 0 or 2 ? 32 : 64;

            for (var row = 0; row < yCols; row++)
            {
                for (var column = 0; column < xCols; column++)
                {
                    if (spriteCount > tileCount)
                    {
                        break;
                    }

                    using var tile = new Bitmap(tileWidth, tileHeight, bitmap.PixelFormat);
                    using var graphics = Graphics.FromImage(tile);
                    var sourceRect = new Rectangle(column * tileWidth, row * tileHeight, tileWidth, tileHeight);
                    graphics.DrawImage(bitmap, new Rectangle(0, 0, tileWidth, tileHeight), sourceRect, GraphicsUnit.Pixel);

                    var stream = new MemoryStream();
                    tile.Save(stream, ImageFormat.Png);
                    stream.Position = 0;
                    spriteStreams[sheet.FirstSpriteid + spriteCount] = stream;
                    spriteCount++;
                }
            }
        }

        private static string ResolveAssetsPath(string inputPath)
        {
            if (string.IsNullOrWhiteSpace(inputPath))
            {
                throw new ArgumentException("Input path is required.", nameof(inputPath));
            }

            var fullPath = Path.GetFullPath(inputPath);
            var candidates = new[]
            {
                fullPath,
                Path.Combine(fullPath, "assets"),
                Path.GetFullPath(Path.Combine(fullPath, "..", "assets")),
            };

            foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "catalog-content.json")))
                {
                    return candidate;
                }
            }

            throw new DirectoryNotFoundException($"Could not find catalog-content.json from input path '{inputPath}'.");
        }

        private static MemoryStream CloneStream(MemoryStream source)
        {
            lock (source)
            {
                return new MemoryStream(source.ToArray());
            }
        }

        private static MemoryStream CreateBlankSpriteStream()
        {
            using var bitmap = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
            var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            stream.Position = 0;
            return stream;
        }
    }
}
