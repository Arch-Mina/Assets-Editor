using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;

using System.Windows.Media.Imaging;
using System.Drawing;
using System.Windows.Media;
using Image = System.Windows.Controls.Image;
using Color = System.Windows.Media.Color;
using System.Collections.ObjectModel;
using System.Diagnostics;
using static Assets_Editor.MainWindow;
using System.Runtime.InteropServices;
using Efundies;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Assets_Editor
{
    /// <summary>
    /// Interaction logic for SprEditor.xaml
    /// </summary>
    public partial class SprEditor : Window
    {
        public SprEditor()
        {
            InitializeComponent();
            CustomCatalog = new List<Catalog>();
        }
        private int SpriteWidth = 32;
        private int SpriteHeight = 32;
        private int SprSheetWidth = 384;
        private int SprSheetHeight = 384;
        private int SprType = 0;
        private bool EmptyTiles = true;
        public static ObservableCollection<ShowList> CustomSheetsList = new ObservableCollection<ShowList>();
        public static List<Catalog> CustomCatalog;
        private Catalog CurrentSheet = null;
        private void CreateNewSheetImg(int sprType, bool modify)
        {
            EmptyTiles = true;
            if (!modify)
                CurrentSheet = null;

            SprType = sprType;
            if (sprType == 0)
            {
                SpriteWidth = 32;
                SpriteHeight = 32;
            }
            else if (sprType == 1)
            {
                SpriteWidth = 32;
                SpriteHeight = 64;
            }
            else if (sprType == 2)
            {
                SpriteWidth = 64;
                SpriteHeight = 32;
            }
            else if (sprType == 3)
            {
                SpriteWidth = 64;
                SpriteHeight = 64;
            }

            List<Image> images = Utils.GetLogicalChildCollection<Image>(SheetWrap);
            foreach (Image child in images)
            {
                SheetWrap.Children.Remove(child);
            }

            int stride = SpriteWidth * (96 / 8);
            BitmapSource image = BitmapSource.Create(SpriteWidth, SpriteHeight, 96, 96, PixelFormats.Indexed8, new BitmapPalette(new List<Color> { Colors.White }), new byte[SpriteHeight * stride], stride);
            int count = (SprSheetWidth / SpriteWidth * SprSheetHeight / SpriteHeight);
            for (int i = 0; i < count; i++)
            {
                Image img = new Image
                {
                    Width = SpriteWidth,
                    Height = SpriteHeight,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    AllowDrop = true
                };
                img.DragOver += Img_DragOverSheet;
                img.Drop += Img_Drop;
                img.Source = image;
                img.Tag = i;
                img.Margin = new Thickness(1, 1, 0, 0);
                SheetWrap.Children.Add(img);
            }
            SheetWrap.Width = SprSheetWidth + 1 + (SprSheetWidth / SpriteWidth);
            SheetWrap.Height = SprSheetHeight + 1 + (SprSheetHeight / SpriteHeight);
        }

        private void Img_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                BitmapImage sourceBitmap = new BitmapImage(new Uri(files[0]));
                List<Image> images = Utils.GetLogicalChildCollection<Image>(SheetWrap);
                if (Math.Ceiling(sourceBitmap.Width) >= SprSheetWidth && Math.Ceiling(sourceBitmap.Height) >= SprSheetHeight)
                {
                    Bitmap original = new Bitmap(@files[0]);
                    int sprCount = 0;
                    int xCols = SpriteWidth == 32 ? 12 : 6;
                    int yCols = SpriteHeight == 32 ? 12 : 6;
                    for (int x = 0; x < yCols; x++)
                    {
                        for (int y = 0; y < xCols; y++)
                        {
                            Rectangle rect = new Rectangle(y * SpriteWidth, x * SpriteHeight, SpriteWidth, SpriteHeight);
                            Bitmap crop = original.Clone(rect, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                            images[sprCount].Source = Utils.BitmapToBitmapImage(crop);
                            sprCount++;
                        }
                    }

                }
                else
                {
                    if (files.Length > 1)
                    {
                        for (int i = 0; i < files.Length; i++)
                        {
                            sourceBitmap = new BitmapImage(new Uri(files[i]));
                            images[i].Source = sourceBitmap;
                        }
                    }
                    else
                    {
                        sourceBitmap = new BitmapImage(new Uri(files[0]));
                        Image img = e.Source as Image;
                        img.Source = sourceBitmap;
                    }
                }
                EmptyTiles = false;
            }
        }

        private void Img_DragOverSheet(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
        }
        private void NewSheetDialogHost_OnDialogClosing(object sender, DialogClosingEventArgs eventArgs)
        {
            if (!Equals(eventArgs.Parameter, true)) return;

            if (TSpriteType.SelectedIndex > -1)
            {
                CreateNewSheetImg(TSpriteType.SelectedIndex, false);
            }
        }
        private void ExportLzmaFile(Bitmap targetImg, ref string filename)
        {
            Bitmap targetBmp = targetImg.Clone(new Rectangle(0, 0, targetImg.Width, targetImg.Height), System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            MemoryStream ms = new MemoryStream();
            targetBmp.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            ms.Position = 0xA;
            int bmpData = ms.ReadByte();
            MemoryStream outFile = new MemoryStream();
            //BITMAPV$HEADER
            outFile.WriteByte(0x42);                                    //Magic value identifying bitmap file
            outFile.WriteByte(0x4D);                                    //Magic value identifying bitmap file
            outFile.Write(BitConverter.GetBytes(0));                    // Bitmap Size
            outFile.Write(BitConverter.GetBytes((ushort)0));            // Reserved
            outFile.Write(BitConverter.GetBytes((ushort)0));            // Reserved
            outFile.Write(BitConverter.GetBytes(122));                  // Offset of pixel array
            outFile.Write(BitConverter.GetBytes(108));                  // bV4Size
            outFile.Write(BitConverter.GetBytes(targetBmp.Width));      // bV4Width 
            outFile.Write(BitConverter.GetBytes(targetBmp.Height));     // bV4Height  
            outFile.Write(BitConverter.GetBytes((ushort)1));            // bV4Planes
            outFile.Write(BitConverter.GetBytes((ushort)32));           // bV4BitCount
            outFile.Write(BitConverter.GetBytes(3));                    // bV4V4Compression
            outFile.Write(BitConverter.GetBytes(0));                    // bV4SizeImage
            outFile.Write(BitConverter.GetBytes(0));                    // bV4XPelsPerMeter
            outFile.Write(BitConverter.GetBytes(0));                    // bV4YPelsPerMeter
            outFile.Write(BitConverter.GetBytes(0));                    // bV4ClrUsed
            outFile.Write(BitConverter.GetBytes(0));                    // bV4ClrImportant
            outFile.Write(new byte[] { 0x0, 0x0, 0xFF, 0x0 }, 0, 4);    // bV4RedMask
            outFile.Write(new byte[] { 0x0, 0xFF, 0x0, 0x0 }, 0, 4);    // bV4GreenMask
            outFile.Write(new byte[] { 0xFF, 0x0, 0x0, 0x0 }, 0, 4);    // bV4BlueMask
            outFile.Write(new byte[] { 0x0, 0x0, 0x0, 0xFF }, 0, 4);    // bV4AlphaMask
            outFile.Write(new byte[] { 0x57, 0x69, 0x6E, 0x20 }, 0, 4); //(0x73524742); // bV4CSType (0x42475273 = "BGRs")
            outFile.Write(BitConverter.GetBytes(0));                    // X coordinate of red endpoint
            outFile.Write(BitConverter.GetBytes(0));                    // Y coordinate of red endpoint
            outFile.Write(BitConverter.GetBytes(0));                    // Z coordinate of red endpoint
            outFile.Write(BitConverter.GetBytes(0));                    // X coordinate of green endpoint
            outFile.Write(BitConverter.GetBytes(0));                    // Y coordinate of green endpoint
            outFile.Write(BitConverter.GetBytes(0));                    // Z coordinate of green endpoint
            outFile.Write(BitConverter.GetBytes(0));                    // X coordinate of blue endpoint
            outFile.Write(BitConverter.GetBytes(0));                    // Y coordinate of blue endpoint
            outFile.Write(BitConverter.GetBytes(0));                    // Z coordinate of blue endpoint
            outFile.Write(BitConverter.GetBytes(0));                    // bV4GammaRed
            outFile.Write(BitConverter.GetBytes(0));                    // bV4GammaGreen
            outFile.Write(BitConverter.GetBytes(0));                    // bV4GammaBlue

            outFile.Write(ms.ToArray(), bmpData, (int)ms.Length - bmpData);
            outFile.Seek(2, SeekOrigin.Begin);
            outFile.Write(BitConverter.GetBytes(outFile.Length));
            outFile.Position = 0;
            using HashAlgorithm crypt = SHA256.Create();
            byte[] hashValue = crypt.ComputeHash(outFile);
            string hash = String.Empty;
            foreach (byte theByte in hashValue)
            {
                hash += theByte.ToString("x2");
            }
            outFile.Position = 0;
            string fullpath = filename + "\\sprites-" + hash + ".bmp.lzma";
            filename = "sprites-" + hash + ".bmp.lzma";
            LZMA.CompressFileLZMA(outFile, fullpath);
        }

        private Bitmap GetBitmapFromTiles(List<Image> images)
        {
            Bitmap targetImg = new Bitmap(SprSheetWidth, SprSheetHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            int sprCount = 0;
            int xCols = SpriteWidth == 32 ? 12 : 6;
            int yCols = SpriteHeight == 32 ? 12 : 6;

            for (int x = 0; x < yCols; x++)
            {
                for (int y = 0; y < xCols; y++)
                {
                    BitmapSource bitmapSource = Utils.BitmapFromControl(images[sprCount], 96, 96, SpriteWidth, SpriteHeight);
                    PngBitmapEncoder png = new PngBitmapEncoder();
                    png.Frames.Add(BitmapFrame.Create(bitmapSource));
                    using MemoryStream stream = new MemoryStream();
                    png.Save(stream);
                    System.Drawing.Image image = System.Drawing.Image.FromStream(stream);
                    Rectangle destRect = new Rectangle(y * SpriteWidth, x * SpriteHeight, SpriteWidth, SpriteHeight);
                    Graphics g = Graphics.FromImage(targetImg);
                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    g.DrawImage(image, destRect, new Rectangle(0, 0, SpriteWidth, SpriteHeight), GraphicsUnit.Pixel);
                    g.Dispose();
                    sprCount++;
                }
            }

            var lockedBitmap = new LockBitmap(targetImg);
            lockedBitmap.LockBits();
            for (int y = 0; y < lockedBitmap.Height; y++)
            {
                for (int x = 0; x < lockedBitmap.Width; x++)
                {
                    if (lockedBitmap.GetPixel(x, y) == System.Drawing.Color.FromArgb(255, 255, 0, 255))
                    {
                        lockedBitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(0, 255, 0, 255));
                    }
                }
            }
            lockedBitmap.UnlockBits();
            return targetImg;
        }
        private void ExportSheet_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            List<Image> images = Utils.GetLogicalChildCollection<Image>(SheetWrap);
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Bitmap Image (.bmp)|*.bmp|Gif Image (.gif)|*.gif|JPEG Image (.jpeg)|*.jpeg|Png Image (.png)|*.png|Lzma Archive (.lzma)|*.lzma"
            };
            if (saveFileDialog.ShowDialog() == true)
            {

                Bitmap targetImg = GetBitmapFromTiles(images);

                switch (saveFileDialog.FilterIndex)
                {
                    case 1:
                        targetImg.Save(saveFileDialog.FileName, System.Drawing.Imaging.ImageFormat.Bmp);
                        break;
                    case 2:
                        targetImg.Save(saveFileDialog.FileName, System.Drawing.Imaging.ImageFormat.Gif);
                        break;
                    case 3:
                        targetImg.Save(saveFileDialog.FileName, System.Drawing.Imaging.ImageFormat.Jpeg);
                        break;
                    case 4:
                        targetImg.Save(saveFileDialog.FileName, System.Drawing.Imaging.ImageFormat.Png);
                        break;
                    case 5:
                        {
                            FileInfo fileInfo = new FileInfo(saveFileDialog.FileName);
                            string dirPath = fileInfo.DirectoryName;
                            ExportLzmaFile(targetImg, ref dirPath);
                            break;
                        }
                }
                targetImg.Dispose();
            }
        }

        private void ImportSheet_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            List<Image> images = Utils.GetLogicalChildCollection<Image>(SheetWrap);
            if (images.Count > 0 && EmptyTiles == false)
            {
                EmptyTiles = true;
                Catalog sprInfo = new Catalog
                {
                    Type = "sprite",
                    File = "",
                    SpriteType = SprType,
                    FirstSpriteid = MainWindow.CustomSprLastId + 1,
                    LastSpriteid = MainWindow.CustomSprLastId + images.Count,
                    Area = 0
                };

                if (CurrentSheet != null)
                {
                    sprInfo.Type = CurrentSheet.Type;
                    sprInfo.File = "";
                    sprInfo.SpriteType = CurrentSheet.SpriteType;
                    sprInfo.FirstSpriteid = CurrentSheet.FirstSpriteid;
                    sprInfo.LastSpriteid = CurrentSheet.LastSpriteid;
                    sprInfo.Area = CurrentSheet.Area;
                }
                int counter = 0;
                foreach (Image child in images)
                {

                    BitmapSource bitmapSource = Utils.BitmapFromControl(child, 96, 96, SpriteWidth, SpriteHeight);
                    PngBitmapEncoder png = new PngBitmapEncoder();
                    png.Frames.Add(BitmapFrame.Create(bitmapSource));
                    MemoryStream stream = new MemoryStream();
                    png.Save(stream);
                    Bitmap imported = new Bitmap(stream);
                    var lockedBitmap = new LockBitmap(imported);
                    lockedBitmap.LockBits();
                    for (int y = 0; y < imported.Height; y++)
                    {
                        for (int x = 0; x < imported.Width; x++)
                        {
                            if (lockedBitmap.GetPixel(x, y) == System.Drawing.Color.FromArgb(255, 255, 0, 255))
                            {
                                lockedBitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(0, 255, 0, 255));
                            }
                        }
                    }
                    lockedBitmap.UnlockBits();
                    stream = new MemoryStream();
                    imported.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                    SprLists[sprInfo.FirstSpriteid + counter] = stream;
                    counter++;
                    if (CurrentSheet == null)
                        MainWindow.CustomSprLastId++;
                }
                Bitmap targetImg = GetBitmapFromTiles(images);
                string dirPath = _assetsPath;
                ExportLzmaFile(targetImg, ref dirPath);

                sprInfo.File = dirPath;
                if (CurrentSheet != null)
                    CustomCatalog.Remove(CurrentSheet);

                MainWindow.catalog.Add(sprInfo);
                SprEditor.CustomSheetsList.Add(new ShowList() { Id = (uint)sprInfo.FirstSpriteid, Image = Utils.BitmapToBitmapImage(targetImg), Name = sprInfo.File });

                for (int i = AllSprList.Count; i < CustomSprLastId; i++)
                {
                    AllSprList.Add(new ShowList() { Id = (uint)i });
                }
                SprStatusBar.MessageQueue.Enqueue($"New sheet saved.", null, null, null, false, true, TimeSpan.FromSeconds(3));
            }
            else
                SprStatusBar.MessageQueue.Enqueue($"Create a new sheet to import.", null, null, null, false, true, TimeSpan.FromSeconds(3));
        }

        private void EditSheet_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (SheetsList.SelectedIndex > -1)
            {               
                ShowList showList = (ShowList)SheetsList.SelectedItem;
                CurrentSheet = CustomCatalog.Find(x => x.File == showList.Name);
                CreateNewSheetImg(CurrentSheet.SpriteType, true);
                EmptyTiles = false;
                List<Image> images = Utils.GetLogicalChildCollection<Image>(SheetWrap);
                Bitmap original = Utils.BitmapImageToBitmap((BitmapImage)showList.Image);

                int sprCount = 0;
                int xCols = SpriteWidth == 32 ? 12 : 6;
                int yCols = SpriteHeight == 32 ? 12 : 6;
                for (int x = 0; x < yCols; x++)
                {
                    for (int y = 0; y < xCols; y++)
                    {
                        Rectangle rect = new Rectangle(y * SpriteWidth, x * SpriteHeight, SpriteWidth, SpriteHeight);
                        Bitmap crop = new Bitmap(SpriteWidth, SpriteHeight);
                        crop = original.Clone(rect, crop.PixelFormat);
                        images[sprCount].Source = Utils.BitmapToBitmapImage(crop);
                        sprCount++;
                    }
                }
            }
        }
    }
}
