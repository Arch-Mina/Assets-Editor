using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MediaColor = System.Windows.Media.Color;
using DrawingColor = System.Drawing.Color;
using Appearance = Tibia.Protobuf.Appearances.Appearance;

namespace Assets_Editor;

public static class Utils
{
    public static void SafeSetColor(int? colorIndex, Xceed.Wpf.Toolkit.ColorPicker colorPicker)
    {
        if (colorIndex > 0 && colorIndex < colorPicker.AvailableColors.Count) {
            colorPicker.SelectedColor = colorPicker.AvailableColors[colorIndex ?? 0].Color;
        }
    }

    public static MediaColor Get8Bit(int color)
    {
        MediaColor RgbColor = new()
        {
            R = (byte)(color / 36 % 6 * 51),
            G = (byte)(color / 6 % 6 * 51),
            B = (byte)(color % 6 * 51)
        };
        return RgbColor;
    }
    public static DrawingColor GetOutfitColor(int color)
    {
        const int HSI_SI_VALUES = 7;
        const int HSI_H_STEPS = 19;

        if (color >= HSI_H_STEPS * HSI_SI_VALUES)
            color = 0;

        float loc1 = 0, loc2 = 0, loc3 = 0;
        if (color % HSI_H_STEPS != 0)
        {
            loc1 = color % HSI_H_STEPS * 1.0f / 18.0f;
            loc2 = 1;
            loc3 = 1;

            switch (color / HSI_H_STEPS)
            {
                case 0:
                    loc2 = 0.25f;
                    loc3 = 1.00f;
                    break;
                case 1:
                    loc2 = 0.25f;
                    loc3 = 0.75f;
                    break;
                case 2:
                    loc2 = 0.50f;
                    loc3 = 0.75f;
                    break;
                case 3:
                    loc2 = 0.667f;
                    loc3 = 0.75f;
                    break;
                case 4:
                    loc2 = 1.00f;
                    loc3 = 1.00f;
                    break;
                case 5:
                    loc2 = 1.00f;
                    loc3 = 0.75f;
                    break;
                case 6:
                    loc2 = 1.00f;
                    loc3 = 0.50f;
                    break;
            }
        }
        else
        {
            loc1 = 0;
            loc2 = 0;
            loc3 = 1 - (float)color / HSI_H_STEPS / HSI_SI_VALUES;
        }

        if (loc3 == 0)
            return DrawingColor.FromArgb(0, 0, 0);

        if (loc2 == 0)
        {
            int loc7 = (int)(loc3 * 255);
            return DrawingColor.FromArgb(loc7, loc7, loc7);
        }

        float red = 0, green = 0, blue = 0;

        // Color calculation logic
        if (loc1 < 1.0 / 6.0)
        {
            red = loc3;
            blue = loc3 * (1 - loc2);
            green = blue + (loc3 - blue) * 6 * loc1;
        }
        else if (loc1 < 2.0 / 6.0)
        {
            green = loc3;
            blue = loc3 * (1 - loc2);
            red = green - (loc3 - blue) * (6 * loc1 - 1);
        }
        else if (loc1 < 3.0 / 6.0)
        {
            green = loc3;
            red = loc3 * (1 - loc2);
            blue = red + (loc3 - red) * (6 * loc1 - 2);
        }
        else if (loc1 < 4.0 / 6.0)
        {
            blue = loc3;
            red = loc3 * (1 - loc2);
            green = blue - (loc3 - red) * (6 * loc1 - 3);
        }
        else if (loc1 < 5.0 / 6.0)
        {
            blue = loc3;
            green = loc3 * (1 - loc2);
            red = green + (loc3 - green) * (6 * loc1 - 4);
        }
        else
        {
            red = loc3;
            green = loc3 * (1 - loc2);
            blue = red - (loc3 - green) * (6 * loc1 - 5);
        }

        return DrawingColor.FromArgb((int)(red * 255), (int)(green * 255), (int)(blue * 255));
    }
    public static childItem FindVisualChild<childItem>(DependencyObject obj) where childItem : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(obj, i);
            if (child != null && child is childItem)
            {
                return (childItem)child;
            }
            else
            {
                childItem childOfChild = FindVisualChild<childItem>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
        }
        return null;
    }

    public static BitmapImage BitmapToBitmapImage(MemoryStream stream)
    {
        if (stream == null)
        {
            return null;
        }
        BitmapImage bitmap = new();
        bitmap.BeginInit();
        stream.Seek(0, SeekOrigin.Begin);
        bitmap.StreamSource = stream;
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        return bitmap;
    }
    public static BitmapImage BitmapToBitmapImage(System.Drawing.Bitmap bitmap)
    {
        using MemoryStream memory = new();

        // save as PNG to preserve alpha channel
        bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
        memory.Position = 0;
        BitmapImage bitmapImage = new();
        bitmapImage.BeginInit();
        bitmapImage.StreamSource = memory;
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.EndInit();
        bitmapImage.Freeze();
        return bitmapImage;
    }
    public static List<T> GetLogicalChildCollection<T>(this DependencyObject parent) where T : DependencyObject
    {
        List<T> logicalCollection = new List<T>();
        GetLogicalChildCollection(parent, logicalCollection);
        return logicalCollection;
    }

    private static void GetLogicalChildCollection<T>(DependencyObject parent, List<T> logicalCollection) where T : DependencyObject
    {
        IEnumerable children = LogicalTreeHelper.GetChildren(parent);
        foreach (object child in children)
        {
            if (child is DependencyObject)
            {
                DependencyObject depChild = child as DependencyObject;
                if (child is T)
                {
                    logicalCollection.Add(child as T);
                }
                GetLogicalChildCollection(depChild, logicalCollection);
            }
        }
    }
    public static BitmapSource BitmapFromControl(Visual target, double dpiX, double dpiY, int width, int height)
    {
        if (target == null)
        {
            return null;
        }
        Rect bounds = new Rect(VisualTreeHelper.GetDescendantBounds(target).X, VisualTreeHelper.GetDescendantBounds(target).Y, width, height);
        RenderTargetBitmap rtb = new RenderTargetBitmap((int)bounds.Width, (int)bounds.Height, dpiX, dpiY, PixelFormats.Pbgra32);
        DrawingVisual dv = new DrawingVisual();
        using (DrawingContext ctx = dv.RenderOpen())
        {
            VisualBrush vb = new VisualBrush(target);
            ctx.DrawRectangle(vb, null, new Rect(new System.Windows.Point(), bounds.Size));
        }
        rtb.Render(dv);
        return rtb;
    }
    public static System.Drawing.Bitmap? BitmapImageToBitmap(BitmapImage bitmapImage)
    {
        if (bitmapImage == null) return null;

        using var ms = new MemoryStream();

        // use PNG so alpha is preserved
        BitmapEncoder enc = new PngBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(bitmapImage));
        enc.Save(ms);
        ms.Position = 0;
        using var tmp = new System.Drawing.Bitmap(ms);

        // return a copy that's not tied to the stream
        return new System.Drawing.Bitmap(tmp);
    }
    public static bool ByteArrayCompare(byte[] a1, byte[] a2)
    {
        if (a1.Length != a2.Length)
        {
            return false;
        }

        for (int i = 0; i < a1.Length; i++)
        {
            if (a1[i] != a2[i])
            {
                return false;
            }
        }

        return true;
    }

    public static Bitmap ConvertBackgroundToMagenta(Bitmap original, bool opaque)
    {
        DrawingColor magentaOpaque = DrawingColor.FromArgb(255, 255, 0, 255); // Opaque magenta
        DrawingColor magentaTransparent = DrawingColor.FromArgb(0, 255, 0, 255); // Transparent magenta
        DrawingColor magenta = opaque ? magentaOpaque : magentaTransparent;

        // Create a bitmap with the same size as the original
        Bitmap bmpWithMagentaBackground = new Bitmap(original.Width, original.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        using (Graphics graphics = Graphics.FromImage(bmpWithMagentaBackground))
        {
            // Fill the background with magenta
            graphics.Clear(magenta);

            // Draw the original image on top of the magenta background
            graphics.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height));
        }

        return bmpWithMagentaBackground;
    }

    public static double GetDpiScale() {
        var window = Application.Current.MainWindow;
        var windowSource = PresentationSource.FromVisual(window);
        if (windowSource != null) {
            // system scale
            return windowSource.CompositionTarget.TransformToDevice.M11;
        }

        return 1.0;
    }

    public static BitmapImage ResizeForUI(MemoryStream stream) {
        if (stream == null)
            return null;

        double scale = GetDpiScale();

        using Bitmap original = new(stream);
        int newWidth = (int)(original.Width * scale);
        int newHeight = (int)(original.Height * scale);
        Bitmap resized = new(newWidth, newHeight);

        using (var g = Graphics.FromImage(resized)) {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            g.DrawImage(original, 0, 0, newWidth, newHeight);
        }

        var ms = new MemoryStream();
        resized.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        ms.Seek(0, SeekOrigin.Begin);

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.StreamSource = ms;
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        return bitmap;
    }

    public static void ColorizeOutfit(Bitmap imageTemplate, Bitmap imageOutfit, MediaColor head, MediaColor body, MediaColor legs, MediaColor feet) {
        for (int i = 0; i < imageTemplate.Height; i++) {
            for (int j = 0; j < imageTemplate.Width; j++) {
                DrawingColor templatePixel = imageTemplate.GetPixel(j, i);
                DrawingColor outfitPixel = imageOutfit.GetPixel(j, i);

                if (templatePixel == outfitPixel)
                    continue;

                int rt = templatePixel.R;
                int gt = templatePixel.G;
                int bt = templatePixel.B;
                int ro = outfitPixel.R;
                int go = outfitPixel.G;
                int bo = outfitPixel.B;

                if (rt > 0 && gt > 0 && bt == 0) // yellow == head
                {
                    ColorizePixel(ref ro, ref go, ref bo, head);
                } else if (rt > 0 && gt == 0 && bt == 0) // red == body
                  {
                    ColorizePixel(ref ro, ref go, ref bo, body);
                } else if (rt == 0 && gt > 0 && bt == 0) // green == legs
                  {
                    ColorizePixel(ref ro, ref go, ref bo, legs);
                } else if (rt == 0 && gt == 0 && bt > 0) // blue == feet
                  {
                    ColorizePixel(ref ro, ref go, ref bo, feet);
                } else {
                    continue; // if nothing changed, skip the change of pixel
                }

                imageOutfit.SetPixel(j, i, DrawingColor.FromArgb(ro, go, bo));
            }
        }
    }

    private static void ColorizePixel(ref int r, ref int g, ref int b, MediaColor colorPart) {
        r = (r * colorPart.R) / 255;
        g = (g * colorPart.G) / 255;
        b = (b * colorPart.B) / 255;
    }

    public static T FindAncestorOrSelf<T>(DependencyObject obj) where T : DependencyObject
    {
        while (obj != null)
        {
            if (obj is T objTyped)
            {
                return objTyped;
            }

            obj = VisualTreeHelper.GetParent(obj);
        }

        return null;
    }
    public class Outfit
    {
        public Outfit(ushort type, byte head, byte body, byte legs, byte feet, byte addons)
        {
            Type = type;
            Head = head;
            Body = body;
            Legs = legs;
            Feet = feet;
            Addons = addons;
        }

        public Outfit() : this(0, 0, 0, 0, 0, 0)
        {
        }
        public ushort Type { get; set; }

        public byte Head { get; set; }

        public byte Body { get; set; }

        public byte Legs { get; set; }

        public byte Feet { get; set; }

        public byte Addons { get; set; }
    }

    public static ushort GetLastIdOrZero<T>(IList<T> list, Func<T, uint> idSelector)
    {
        return (ushort)(list.Count > 0 ? idSelector(list[^1]) : 0);
    }

    /// <summary>
    /// handles updating the inner values when cloning an appearance
    /// </summary>
    /// <param name="origItem">original appearance</param>
    /// <param name="clonedItem">cloned appearance</param>
    public static void OnAppearanceCloned(Appearance origItem, ref Appearance clonedItem)
    {
        if (origItem == null || clonedItem == null)
            return;

        uint oldId = origItem.Id;
        uint newId = clonedItem.Id;

        // market flags
        if (clonedItem.Flags.Market != null)
        {
            if (clonedItem.Flags.Market.HasTradeAsObjectId && clonedItem.Flags.Market.TradeAsObjectId == oldId)
                clonedItem.Flags.Market.TradeAsObjectId = newId;

            if (clonedItem.Flags.Market.HasShowAsObjectId && clonedItem.Flags.Market.ShowAsObjectId == oldId)
                clonedItem.Flags.Market.ShowAsObjectId = newId;
        }

        // cyclopedia flags
        if (clonedItem.Flags.Cyclopediaitem != null)
        {
            if (clonedItem.Flags.Cyclopediaitem.HasCyclopediaType && clonedItem.Flags.Cyclopediaitem.CyclopediaType == oldId)
                clonedItem.Flags.Cyclopediaitem.CyclopediaType = newId;
        }
    }
}

