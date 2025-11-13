using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Assets_Editor
{
    public static class Utils
    {
        public static System.Windows.Media.Color Get8Bit(int color)
        {
            System.Windows.Media.Color RgbColor = new System.Windows.Media.Color
            {
                R = (byte)(color / 36 % 6 * 51),
                G = (byte)(color / 6 % 6 * 51),
                B = (byte)(color % 6 * 51)
            };
            return RgbColor;
        }
        public static System.Drawing.Color GetOutfitColor(int color)
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
                return System.Drawing.Color.FromArgb(0, 0, 0);

            if (loc2 == 0)
            {
                int loc7 = (int)(loc3 * 255);
                return System.Drawing.Color.FromArgb(loc7, loc7, loc7);
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

            return System.Drawing.Color.FromArgb((int)(red * 255), (int)(green * 255), (int)(blue * 255));
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
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            stream.Seek(0, System.IO.SeekOrigin.Begin);
            bitmap.StreamSource = stream;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            return bitmap;
        }
        public static BitmapImage BitmapToBitmapImage(System.Drawing.Bitmap bitmap)
        {
            using var memory = new MemoryStream();
            bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
            memory.Position = 0;
            var bitmapImage = new BitmapImage();
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
        public static System.Drawing.Bitmap BitmapImageToBitmap(BitmapImage bitmapImage)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(outStream);

                return new System.Drawing.Bitmap(bitmap);
            }
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
            // PATCH: Preserve RGBA data without altering transparency
            return original.Clone(new Rectangle(0, 0, original.Width, original.Height), System.Drawing.Imaging.PixelFormat.Format32bppArgb);
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

    }
}

