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
            bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
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
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(outStream);

                return new System.Drawing.Bitmap(bitmap);
            }
        }
    }
}
