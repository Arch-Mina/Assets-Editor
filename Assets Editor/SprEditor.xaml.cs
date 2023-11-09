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
using System.Linq;

namespace Assets_Editor
{
    /// <summary>
    /// Interaction logic for SprEditor.xaml
    /// </summary>
    public partial class SprEditor : Window
    {
        private DatEditor _editor;
        public SprEditor(DatEditor editor)
        {
            InitializeComponent();
            _editor = editor;
        }
        private int SpriteWidth = 32;
        private int SpriteHeight = 32;
        private int SprSheetWidth = 384;
        private int SprSheetHeight = 384;
        private int SprType = 0;
        private bool EmptyTiles = true;
        public static ObservableCollection<ShowList> CustomSheetsList = new ObservableCollection<ShowList>();
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
                RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor);
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
                    Bitmap original = Utils.ConvertBackgroundToMagenta(new Bitmap(@files[0]), true);
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
                            Bitmap original = Utils.ConvertBackgroundToMagenta(new Bitmap(@files[i]), true);
                            images[i].Source = Utils.BitmapToBitmapImage(original);
                        }
                    }
                    else
                    {
                        Bitmap original = Utils.ConvertBackgroundToMagenta(new Bitmap(@files[0]), true);
                        Image img = e.Source as Image;
                        img.Source = Utils.BitmapToBitmapImage(original);
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
            if(images.Count == 0)
            {
                SprStatusBar.MessageQueue.Enqueue($"Current sheet is empty.", null, null, null, false, true, TimeSpan.FromSeconds(3));
                return;
            }
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
                            LZMA.ExportLzmaFile(targetImg, ref dirPath);
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
                    FirstSpriteid = AllSprList.Count,
                    LastSpriteid = AllSprList.Count + images.Count,
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
                }
                Bitmap targetImg = GetBitmapFromTiles(images);
                string dirPath = _assetsPath;
                LZMA.ExportLzmaFile(targetImg, ref dirPath);

                sprInfo.File = dirPath;
                if (CurrentSheet != null)
                    MainWindow.catalog.Remove(CurrentSheet);

                MainWindow.catalog.Add(sprInfo);

                ShowList foundEntry = SprEditor.CustomSheetsList.FirstOrDefault(showList => showList.Id == (uint)sprInfo.FirstSpriteid);

                if (foundEntry != null)
                {
                    SprEditor.CustomSheetsList.Remove(foundEntry);
                }

                SprEditor.CustomSheetsList.Add(new ShowList() { Id = (uint)sprInfo.FirstSpriteid, Image = Utils.BitmapToBitmapImage(targetImg), Name = sprInfo.File });

                if (CurrentSheet == null)
                {
                    for (int i = sprInfo.FirstSpriteid; i < sprInfo.LastSpriteid; i++)
                    {
                        AllSprList.Add(new ShowList() { Id = (uint)i });
                    }
                }
                SprStatusBar.MessageQueue.Enqueue($"New sheet saved.", null, null, null, false, true, TimeSpan.FromSeconds(3));
                _editor.SprListView.ItemsSource = null;
                _editor.SprListView.ItemsSource = MainWindow.AllSprList;
            }
            else
                SprStatusBar.MessageQueue.Enqueue($"Create a new sheet to import.", null, null, null, false, true, TimeSpan.FromSeconds(3));
        }

        private void EditSheet_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (SheetsList.SelectedIndex > -1)
            {               
                ShowList showList = (ShowList)SheetsList.SelectedItem;
                CurrentSheet = MainWindow.catalog.Find(x => x.File == showList.Name);
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

        private void CreateNewSheet_Click(object sender, RoutedEventArgs e)
        {
            if (TSpriteType.SelectedIndex > -1)
            {
                CreateNewSheetImg(TSpriteType.SelectedIndex, false);
                NewSheetDialogHost.IsOpen = false;
            }
        }

        private void CreateSheet_Click(object sender, RoutedEventArgs e)
        {
            NewSheetDialogHost.IsOpen = true;
        }

        private void SearchSpr_Click(object sender, RoutedEventArgs e)
        {

            foreach (var catalog in MainWindow.catalog)
            {
                if (A_SearchSprId.Value >= catalog.FirstSpriteid && A_SearchSprId.Value <= catalog.LastSpriteid)
                {
                    string _sprPath = String.Format("{0}{1}", MainWindow._assetsPath, catalog.File);
                    if (File.Exists(_sprPath))
                    {
                        ShowList foundEntry = SprEditor.CustomSheetsList.FirstOrDefault(showList => showList.Id == (uint)catalog.FirstSpriteid);
                        if (foundEntry == null)
                        {
                            using System.Drawing.Bitmap SheetM = LZMA.DecompressFileLZMA(_sprPath);
                            using Bitmap transparentBitmap = new Bitmap(SheetM.Width, SheetM.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                            using (Graphics g = Graphics.FromImage(transparentBitmap))
                            {
                                g.Clear(System.Drawing.Color.FromArgb(255, 255, 0, 255));
                                g.DrawImage(SheetM, 0, 0);

                            }

                            SprEditor.CustomSheetsList.Add(new ShowList() { Id = (uint)catalog.FirstSpriteid, Image = Utils.BitmapToBitmapImage(transparentBitmap), Name = catalog.File });
                        }
                        
                    }
                }
            }
            SheetsList.ItemsSource = SprEditor.CustomSheetsList;

        }
    }
}
