using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Tibia.Protobuf.Appearances;
using System;
using ImageMagick;
using System.Windows.Media.Media3D;

namespace Assets_Editor;

/// <summary>
/// This class is responsible for exporting individual assets as GIFs of PNGs
/// </summary>
public static class ImageExporter {
    /// <summary>
    /// Reads the duration of the animation. Defaults to 30ms if no duration was found.
    /// </summary>
    /// <param name="frameGroup">framegroup to be checked</param>
    /// <param name="index">frame index to be checked for duration</param>
    /// <returns>Frame duration in milliseconds</returns>
    private static uint SafeGetFrameDuration(FrameGroup frameGroup, int index) {
        var animation = frameGroup.SpriteInfo.Animation;
        if (animation == null || animation.SpritePhase.Count == 0) {
            return 30;
        }

        if (index >= animation.SpritePhase.Count) {
            index = animation.SpritePhase.Count - 1;
        }

        return Math.Max(30, frameGroup.SpriteInfo.Animation.SpritePhase[index].DurationMin);
    }

    /// <summary>
    /// Reads the highest frame index in the animation. Defaults to 0 (as indexing is from 0) if framegroup has no animation.
    /// </summary>
    /// <param name="frameGroup">framegroup to be checked</param>
    /// <returns>highest frame index</returns>
    private static uint SafeGetAnimationFrameCount(FrameGroup frameGroup) {
        var animation = frameGroup.SpriteInfo.Animation;
        if (animation == null) {
            return 0;
        }

        return (uint)animation.SpritePhase.Count;
    }

    /// <summary>
    /// Helper function for item saving
    /// </summary>
    /// <param name="exportPath">path where the item is supposed to be exported</param>
    /// <param name="appearance">item to be exported</param>
    private static void SaveItemAsGIFBasic(string exportPath, Appearance appearance) {
        // framegroup id - always 2
        foreach (var frameGroup in appearance.FrameGroup) {
            var spriteInfo = frameGroup.SpriteInfo;

            // width and height correspond item state
            // examples:
            // currently held fluid 0 - empty, 1 - water
            // current stack state: 0 - 1 coin, 4 - 5 coins, 5 - 10 coins, 6 - 25 coins)
            for (int width = 0; width < spriteInfo.PatternWidth; ++width) {
                for (int height = 0; height < spriteInfo.PatternHeight; ++height) {
                    // depth correspond item appearance on [Z % layers == layer] floors
                    // example: zaoan roof varies in shade depending on Z coordinate
                    for (int depth = 0; depth < spriteInfo.PatternDepth; ++depth) {
                        // frames in a GIF
                        List<(Bitmap, uint)> frames = [];

                        // item animation
                        for (int frame = 0; frame <= SafeGetAnimationFrameCount(frameGroup); ++frame) {
                            // layer count - always 1
                            for (int layers = 0; layers < spriteInfo.Layers; ++layers) {
                                try {
                                    // the position of the sprite in the spriteinfo array of sprite ids
                                    int spriteIndex = DatEditor.GetSpriteIndex(frameGroup, layers, width, height, depth, frame);

                                    // create a bitmap
                                    var imageFrame = Utils.BitmapToBitmapImage(MainWindow.getSpriteStream((int)spriteInfo.SpriteId[spriteIndex]));
                                    Bitmap bitmap = new(imageFrame.StreamSource);
                                    Bitmap magentaBitmap = new(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);
                                    using (Graphics g = Graphics.FromImage(magentaBitmap)) {
                                        g.Clear(Color.Transparent);
                                        g.DrawImage(bitmap, 0, 0, bitmap.Width, bitmap.Height);
                                    }
                                    bitmap.Dispose();

                                    frames.Add((magentaBitmap, 100));
                                } catch (Exception ex) {
                                    MainWindow.Log($"Failed to save image {frameGroup.Id} {height} {depth} {width} {frame} {layers} for object {appearance.Id}: {ex.Message}");
                                }
                            }
                        }

                        // 2d pattern to subtype index conversion
                        int subTypeIndex = height * (int)spriteInfo.PatternWidth + width;

                        // filename
                        // add _depth only when the item has multiple depth layers
                        string fileName = $"{subTypeIndex}{(depth > 0 ? ("_" + depth) : "")}.gif";

                        // write a GIF file
                        SaveFramesAsGIF(frames, Path.Combine(exportPath, fileName));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Helper function for item saving
    /// </summary>
    /// <param name="exportPath">path where the item is supposed to be exported</param>
    /// <param name="appearance">item to be exported</param>
    private static void SaveItemAsGIFLoot(string exportPath, Appearance appearance) {
        // framegroup id - always 2
        foreach (var frameGroup in appearance.FrameGroup) {
            var spriteInfo = frameGroup.SpriteInfo;

            // frames in a GIF
            List<(Bitmap, uint)> frames = [];

            uint currentFrame = 0;
            uint frameCount = SafeGetAnimationFrameCount(frameGroup);

            // if the stackable item is animated, play the animation but keep it within 200ms
            // before jumping to next state
            uint currentSequenceDuration = 0;

            // layer count - always 1
            for (int layers = 0; layers < spriteInfo.Layers; ++layers) {

                // depth correspond item appearance on [Z % layers == layer] floors
                // example: zaoan roof varies in shade depending on Z coordinate
                for (int depth = 0; depth < spriteInfo.PatternDepth; ++depth) {

                    // filename
                    // add _depth only when the item has multiple depth layers
                    // add _loot - this is a preview for all states of this item
                    string fileName = $"{(depth > 0 ? ("_" + depth) : "")}_loot.gif";

                    // width and height correspond item state
                    // examples:
                    // currently held fluid 0 - empty, 1 - water
                    // current stack state: 0 - 1 coin, 4 - 5 coins, 5 - 10 coins, 6 - 25 coins)
                    for (int width = 0; width < spriteInfo.PatternWidth; ++width) {
                        for (int height = 0; height < spriteInfo.PatternHeight; ++height) {
                            if (frameCount > 1) {
                                // stackable/fluid + animated
                            } else {
                                // just stackable/fluid

                            }

                            // frameCount


                            /*
                                                             for (int frame = 0; frame <= SafeGetAnimationFrameCount(frameGroup); ++frame) {
                                    try {
                                        // the position of the sprite in the spriteinfo array of sprite ids
                                        int spriteIndex = DatEditor.GetSpriteIndex(frameGroup, layers, width, height, depth, frame);

                                        // create a bitmap
                                        var imageFrame = Utils.BitmapToBitmapImage(MainWindow.getSpriteStream((int)spriteInfo.SpriteId[spriteIndex]));
                                        Bitmap bitmap = new(imageFrame.StreamSource);
                                        Bitmap magentaBitmap = new(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);
                                        using (Graphics g = Graphics.FromImage(magentaBitmap)) {
                                            g.Clear(Color.Transparent);
                                            g.DrawImage(bitmap, 0, 0, bitmap.Width, bitmap.Height);
                                        }
                                        bitmap.Dispose();

                                        frames.Add((magentaBitmap, 100));
                                    } catch (Exception ex) {
                                        MainWindow.Log($"Failed to save image {frameGroup.Id} {height} {depth} {width} {frame} {layers} for object {appearance.Id}: {ex.Message}");
                                    }
                                }
                             */


                        }



                        // write a GIF file
                        SaveFramesAsGIF(frames, Path.Combine(exportPath, fileName));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Creates gifs for various item states in /clientid/ directory
    /// </summary>
    /// <param name="exportPath">path where export action is occurring</param>
    /// <param name="appearance">item to be exported</param>
    public static void SaveItemAsGIF(string exportPath, Appearance appearance) {
        string itemPath = Path.Combine(exportPath, Convert.ToString(appearance.Id));
        Directory.CreateDirectory(itemPath);

        SaveItemAsGIFBasic(itemPath, appearance);

        // if stackable or liquid, save it as 
        if (appearance.Flags.Cumulative || appearance.Flags.Liquidcontainer || appearance.Flags.Liquidpool) {
            // UNFINISHED
            //SaveItemAsGIFLoot(itemPath, appearance);
        }
    }

    /// <summary>
    /// Saves outfits in a format similar to Gesior's outfit.php
    /// Only difference is that it has 0 for idle and 1 for movement animation at the front of file name
    /// </summary>
    /// <param name="exportPath">path where export action is occurring</param>
    /// <param name="appearance">outfit to be exported</param>
    public static void SaveOutfitAsImages(string exportPath, Appearance appearance) {
        string outfitPath = Path.Combine(exportPath, Convert.ToString(appearance.Id));
        Directory.CreateDirectory(outfitPath);

        foreach (var frameGroup in appearance.FrameGroup) {
            var spriteInfo = frameGroup.SpriteInfo;

            for (int height = 0; height < spriteInfo.PatternHeight; ++height) {
                for (int depth = 0; depth < spriteInfo.PatternDepth; ++depth) {
                    for (int width = 0; width < spriteInfo.PatternWidth; ++width) {
                        for (int frames = 0; frames <= SafeGetAnimationFrameCount(frameGroup); ++frames) {
                            for (int layers = 0; layers < spriteInfo.Layers; ++layers) {
                                try {
                                    int index = DatEditor.GetSpriteIndex(frameGroup, layers, width, height, depth, frames);

                                    // set the filename style to n_n_n_n_n_template.png  
                                    // indexing from 1 is the standard in Gesior outfit.php
                                    // the difference from outfit.php is that framegroup id is added here at the front
                                    // in order to distinguish between idle (0) and move (1) animation
                                    // removing framegroup id from the front of filename will make the move animation glitchy
                                    // and will make the idle animation show a wrong frame
                                    string fileName = $"{frameGroup.Id}_{height + 1}_{depth + 1}_{width + 1}_{frames + 1}{(layers == 1 ? "_template" : "")}.png";

                                    var bitmapImage = Utils.BitmapToBitmapImage(MainWindow.getSpriteStream((int)spriteInfo.SpriteId[index]));
                                    Bitmap bitmap = new(bitmapImage.StreamSource);

                                    // ensure the bitmap supports transparency  
                                    Bitmap transparentBitmap = new(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);
                                    using Graphics g = Graphics.FromImage(transparentBitmap);
                                    g.Clear(Color.Transparent); // use transparent background  
                                    g.DrawImage(bitmap, 0, 0, bitmap.Width, bitmap.Height);
                                    transparentBitmap.Save(Path.Combine(outfitPath, fileName), ImageFormat.Png);
                                    transparentBitmap.Dispose();
                                    bitmap.Dispose();
                                } catch (Exception ex) {
                                    MainWindow.Log($"Failed to save image {frameGroup.Id} {height} {depth} {width} {frames} {layers} for object {appearance.Id}: {ex.Message}");
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Saves magic effects as GIFs
    /// </summary>
    /// <param name="exportPath">path where export action is occurring</param>
    /// <param name="appearance">effect to be exported</param>
    public static void SaveEffectAsGIF(string exportPath, Appearance appearance) {
        List<(Bitmap, uint)> frames = [];
        int layer = 0; // always 0
        int depth = 0; // always 0

        // taking the animation from (0, 0)
        // if you intend to capture all variants of mas frigo
        // or other large area effects, you will have to loop over these
        // for reference see how items are saved
        int height = 0;
        int width = 0;

        // default frame group
        var frameGroup = appearance.FrameGroup[0];

        try {
            for (int animationFrame = 0; animationFrame < appearance.FrameGroup[0].SpriteInfo.SpriteId.Count; animationFrame++) {
                int index = DatEditor.GetSpriteIndex(frameGroup, layer, (int)Math.Min(width, frameGroup.SpriteInfo.PatternWidth - 1), height, depth, animationFrame);
                var imageFrame = Utils.BitmapToBitmapImage(MainWindow.getSpriteStream((int)frameGroup.SpriteInfo.SpriteId[index]));
                uint frameDuration = SafeGetFrameDuration(frameGroup, index);

                using Bitmap bitmap = new(imageFrame.StreamSource);
                Bitmap transparentBitmap = new(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(transparentBitmap)) {
                    g.Clear(Color.Transparent);
                    g.DrawImage(bitmap, 0, 0, bitmap.Width, bitmap.Height);
                }

                frames.Add((transparentBitmap, frameDuration));
            }

            SaveFramesAsGIF(frames, Path.Combine(exportPath, "" + appearance.Id + ".gif"));
        } catch {
            MainWindow.Log("Invalid animation for sprite " + appearance.Id + ".");
        } finally {
            // dispose frames if neccessary
            foreach (var (frame, _) in frames) {
                frame.Dispose();
            }
        }
    }

    /// <summary>
    /// Saves missile effects as GIFs, rotating them clockwise starting from the north
    /// </summary>
    /// <param name="exportPath">path where export action is occurring</param>
    /// <param name="appearance">missile effect to be exported</param>
    public static void SaveMissileAsGIF(string exportPath, Appearance appearance) {
        // Frame order for missiles (indexed from 0): 1, 2, 5, 8, 7, 6, 3, 0
        // This corresponds to the missile aiming north and rotating clockwise
        int[] frameOrder = [1, 2, 5, 8, 7, 6, 3, 0];

        List<(Bitmap, uint)> frames = [];

        try {
            var spriteInfo = appearance.FrameGroup[0].SpriteInfo;

            // loop over frames in proper order
            foreach (int frameIndex in frameOrder) {
                if (frameIndex < spriteInfo.SpriteId.Count) {
                    var imageFrame = Utils.BitmapToBitmapImage(
                        MainWindow.getSpriteStream((int)spriteInfo.SpriteId[frameIndex])
                    );

                    using Bitmap bitmap = new(imageFrame.StreamSource);
                    Bitmap transparentBitmap = new(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);
                    using (Graphics g = Graphics.FromImage(transparentBitmap)) {
                        g.Clear(Color.Transparent);
                        g.DrawImage(bitmap, 0, 0, bitmap.Width, bitmap.Height);
                    }

                    frames.Add((transparentBitmap, 100));
                }
            }

            SaveFramesAsGIF(frames, Path.Combine(exportPath, "" + appearance.Id + ".gif"));
        } catch {
            MainWindow.Log("Invalid animation for sprite " + appearance.Id + ".");
        } finally {
            // Dispose of frames if necessary  
            foreach (var (frame, _) in frames) {
                frame.Dispose();
            }
        }
    }

    /// <summary>
    /// Common function to save as GIF
    /// </summary>
    /// <param name="frames">animation frames</param>
    /// <param name="fullFilePath">file path including file name</param>
    public static void SaveFramesAsGIF(List<(Bitmap, uint)> frames, string fullFilePath) {
        if (frames.Count == 0) {
            return;
        }

        var collection = new MagickImageCollection();

        foreach (var (bitmap, duration) in frames) {
            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png); // Use PNG to preserve transparency
            stream.Position = 0; // Reset stream position before reading

            MagickImage magickImage = new(stream);
            magickImage.AnimationDelay = duration / 10; // in 1/100th second
            magickImage.GifDisposeMethod = GifDisposeMethod.Background; // use REPLACE instead of COMBINE for next frame
            collection.Add(magickImage);
        }

        collection[0].AnimationIterations = 0; // loop forever
        collection.Optimize();
        collection.Write(fullFilePath);
    }
}
