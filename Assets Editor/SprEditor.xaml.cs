using Efundies;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static Assets_Editor.MainWindow;
using Color = System.Windows.Media.Color;
using Image = System.Windows.Controls.Image;

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
            TSpriteType.ItemsSource = DatEditor.SpriteSizes

            .Select(s => {
                string label = $"Width: {s.Width} | Height: {s.Height} ({s.Width / 32}x{s.Height / 32}";

                if (s.Width > 64 || s.Height > 64) {
                    label += " , OTClient";
                }

                label += ")";

                return label;
            })
            .ToList();
        }
        private int SpriteWidth = 32;
        private int SpriteHeight = 32;
        private int SprType = 0;
        private bool EmptyTiles = true;
        private int focusedIndex = 0;
        private BitmapSource emptyBitmapSource;
        public static ObservableCollection<ShowList> CustomSheetsList = new ObservableCollection<ShowList>();
        private Catalog CurrentSheet = null;
        private void CreateNewSheetImg(int sprType, bool modify)
        {
            EmptyTiles = true;
            if (!modify)
                CurrentSheet = null;

            SprType = sprType;
            var sprLayout = DatEditor.GetSpriteLayout(sprType);
            SpriteWidth = sprLayout.SpriteSizeX;
            SpriteHeight = sprLayout.SpriteSizeY;

            SheetWrap.Children.Clear();

            int sprMaxW = DatEditor.SprSheetWidth;
            int sprMaxH = DatEditor.SprSheetHeight;

            int stride = SpriteWidth * (96 / 8);
            emptyBitmapSource = BitmapSource.Create(SpriteWidth, SpriteHeight, 96, 96, PixelFormats.Indexed8,
                new BitmapPalette(new List<Color> { Color.FromArgb(255, 255, 0, 255) }), new byte[SpriteHeight * stride], stride);
            int count = (sprMaxW / SpriteWidth * sprMaxH / SpriteHeight);
            for (int i = 0; i < count; i++)
            {
                Border border = new Border
                {
                    Width = SpriteWidth,
                    Height = SpriteHeight,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    AllowDrop = true
                };
                border.Margin = new Thickness(1, 1, 0, 0);
                border.MouseEnter += Border_MouseEnter;
                border.MouseLeave += Border_MouseLeave;
                border.MouseDown += Border_MouseDown;
                border.DragEnter += Border_DragEnter;
                border.DragLeave += Border_DragLeave;
                border.Drop += Border_Drop;
                border.Tag = i;
                Image img = new Image
                {
                    Width = SpriteWidth,
                    Height = SpriteHeight,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                
                img.Source = emptyBitmapSource;
                img.Tag = i;
                
                border.Child = img;
                RenderOptions.SetBitmapScalingMode(border, BitmapScalingMode.NearestNeighbor);
                SheetWrap.Children.Add(border);
            }
            SheetWrap.Width = sprMaxW + 1 + (sprMaxW / SpriteWidth);
            SheetWrap.Height = sprMaxH + 1 + (sprMaxH / SpriteHeight);
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border targetBorder) return;
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragDrop.DoDragDrop(targetBorder, targetBorder, DragDropEffects.Move);
            }
            
        }

        private void Border_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is not Border targetBorder) return;
            focusedIndex = (int)targetBorder.Tag;
            ApplyBorderFocus(targetBorder);
        }

        private void Border_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is not Border targetBorder) return;
            RemoveBorderFocus(targetBorder);
        }

        private void Border_DragEnter(object sender, DragEventArgs e)
        {
            if (sender is not Border targetBorder) return;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files == null) return;
            
                int targetIndex = (int)targetBorder.Tag;
                for (int i = 0; i < files.Length; i++)
                {
                    if (targetIndex + i < SheetWrap.Children.Count)
                    {
                        if (SheetWrap.Children[targetIndex + i] is Border border)
                        {
                            ApplyBorderFocus(border);
                        }
                    }
                }
            }
            else if (e.Data.GetData(e.Data.GetFormats()[0]) is Border border)
            {
                ApplyBorderDragging(targetBorder);
                ApplyBorderDragging(border);
            }
        }

        private void Border_DragLeave(object sender, DragEventArgs e)
        {
            ClearBorders();
        }

        private void ApplyBorderFocus(Border border)
        {
            border.BorderBrush = new SolidColorBrush(Colors.Blue);
            border.BorderThickness = new Thickness(3);
        }
        
        private void ApplyBorderDragging(Border border)
        {
            border.BorderBrush = new SolidColorBrush(Colors.DarkGreen);
            border.BorderThickness = new Thickness(3);
        }

        private void RemoveBorderFocus(Border border)
        {
            border.BorderBrush = new SolidColorBrush(Colors.Transparent);
            border.BorderThickness = new Thickness(0);
        }

        private void ClearBorders()
        {
            foreach (var child in SheetWrap.Children)
            {
                if (child is Border border)
                {
                    RemoveBorderFocus(border);
                }
            }
        }
        
        private void OnPaste(object sender, ExecutedRoutedEventArgs e)
        {
            if (Clipboard.ContainsFileDropList())
            {
                var files = Clipboard.GetFileDropList();
                MakeFilesAsSprites(files.Cast<string>().ToArray(), focusedIndex, null);
                EmptyTiles = false;
            }
        }
        
        private void OnDelete(object sender, ExecutedRoutedEventArgs e)
        {
            List<Image> images = Utils.GetLogicalChildCollection<Image>(SheetWrap);
            if (focusedIndex >= 0 && focusedIndex < images.Count)
            {
                images[focusedIndex].Source = emptyBitmapSource;
            }
        }

        private void Border_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var targetSpriteIndex = 0;
                if (sender is Border border)
                {
                    targetSpriteIndex = (int)border.Tag;
                }
                
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                MakeFilesAsSprites(files, targetSpriteIndex, e);
                
                EmptyTiles = false;
                ClearBorders();
            } else if (e.Data.GetData(e.Data.GetFormats()[0]) is Border sourceBorder)
            {
                if (sender is Border targetBorder)
                {
                    var sourceImage = Utils.GetLogicalChildCollection<Image>(sourceBorder)[0];
                    var targetImage = Utils.GetLogicalChildCollection<Image>(targetBorder)[0];
                    (targetImage.Source, sourceImage.Source) = (sourceImage.Source, targetImage.Source);
                }
                EmptyTiles = false;
                ClearBorders();
            }
        }

        private void MakeFilesAsSprites(string[] files, int targetImageIndex, DragEventArgs e) {
            BitmapImage sourceBitmap = new(new Uri(files[0]));
            List<Image> images = Utils.GetLogicalChildCollection<Image>(SheetWrap);
            // use PixelWidth/PixelHeight (actual pixel dimensions) instead of Width/Height (DIPs affected by DPI metadata)
            if (sourceBitmap.PixelWidth >= DatEditor.SprSheetWidth &&
                sourceBitmap.PixelHeight >= DatEditor.SprSheetHeight) {
                // load original image and preserve alpha when present
                Bitmap loaded = new(files[0]);
                bool hasAlpha = (loaded.PixelFormat & System.Drawing.Imaging.PixelFormat.Alpha) != 0
                                || loaded.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb
                                || loaded.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppPArgb
                                || loaded.PixelFormat == System.Drawing.Imaging.PixelFormat.Format64bppArgb;

                Bitmap original;
                if (hasAlpha) {
                    // ensure we operate on a 32bpp ARGB copy to avoid losing alpha on operations
                    original = new Bitmap(loaded.Width, loaded.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    using Graphics g = Graphics.FromImage(original);
                    g.Clear(System.Drawing.Color.Transparent);
                    g.DrawImage(loaded, 0, 0, loaded.Width, loaded.Height);
                } else {
                    // keep legacy magenta background behaviour for images with no alpha
                    original = Utils.ConvertBackgroundToMagenta(new(files[0]), true);
                }

                int sprCount = targetImageIndex;
                int xCols = DatEditor.SprSheetWidth / SpriteWidth;
                int yCols = DatEditor.SprSheetHeight / SpriteHeight;
                for (int x = 0; x < yCols; x++) {
                    for (int y = 0; y < xCols; y++) {
                        Rectangle rect = new(y * SpriteWidth, x * SpriteHeight, SpriteWidth, SpriteHeight);
                        Bitmap crop = original.Clone(rect, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                        if (sprCount < images.Count) {
                            images[sprCount].Source = Utils.BitmapToBitmapImage(crop);
                        }

                        crop.Dispose();
                        sprCount++;
                    }
                }

                original.Dispose();
                loaded.Dispose();
            } else {
                if (files.Length > 1) {
                    for (int i = 0; i < files.Length; i++) {
                        Bitmap loaded = new(files[i]);
                        bool hasAlpha = (loaded.PixelFormat & System.Drawing.Imaging.PixelFormat.Alpha) != 0
                                        || loaded.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb
                                        || loaded.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppPArgb
                                        || loaded.PixelFormat == System.Drawing.Imaging.PixelFormat.Format64bppArgb;

                        Bitmap toUse;
                        if (hasAlpha) {
                            toUse = new(loaded.Width, loaded.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                            using Graphics g = Graphics.FromImage(toUse);
                            g.Clear(System.Drawing.Color.Transparent);
                            g.DrawImage(loaded, 0, 0, loaded.Width, loaded.Height);
                        } else {
                            toUse = Utils.ConvertBackgroundToMagenta(new Bitmap(files[i]), true);
                        }

                        if (targetImageIndex + i < images.Count) {
                            images[targetImageIndex + i].Source = Utils.BitmapToBitmapImage(toUse);
                        }

                        toUse.Dispose();
                        loaded.Dispose();
                    }
                } else {
                    Bitmap loaded = new(files[0]);
                    bool hasAlpha = (loaded.PixelFormat & System.Drawing.Imaging.PixelFormat.Alpha) != 0
                                    || loaded.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb
                                    || loaded.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppPArgb
                                    || loaded.PixelFormat == System.Drawing.Imaging.PixelFormat.Format64bppArgb;

                    Bitmap toUse = hasAlpha
                        ? new Bitmap(loaded.Width, loaded.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb)
                        : Utils.ConvertBackgroundToMagenta(new Bitmap(files[0]), true);

                    if (hasAlpha) {
                        using Graphics g = Graphics.FromImage(toUse);
                        g.Clear(System.Drawing.Color.Transparent);
                        g.DrawImage(loaded, 0, 0, loaded.Width, loaded.Height);
                    }

                    // try to get the target Image control more reliably
                    Image? targetImg;
                    if (e?.Source is Border b && Utils.GetLogicalChildCollection<Image>(b).Count > 0)
                        targetImg = Utils.GetLogicalChildCollection<Image>(b)[0];
                    else if (e?.OriginalSource is Image oi)
                        targetImg = oi;
                    else
                        targetImg = Utils.GetLogicalChildCollection<Image>(SheetWrap).ElementAtOrDefault(targetImageIndex);

                    if (targetImg != null) {
                        targetImg.Source = Utils.BitmapToBitmapImage(toUse);
                    }

                    toUse.Dispose();
                    loaded.Dispose();
                }
            }
        }

        private void NewSheetDialogHost_OnDialogClosing(object sender, DialogClosingEventArgs eventArgs)
        {
            if (!Equals(eventArgs.Parameter, true)) return;

            if (TSpriteType.SelectedIndex > -1)
            {
                CreateNewSheetImg(TSpriteType.SelectedIndex, false);
            }
        }


        // this function is used by bulk export to pngs feature
        // may require refactoring for larger spritesheets support
        private Bitmap GetBitmapFromTiles(List<Image> images)
        {
            Bitmap targetImg = new(DatEditor.SprSheetWidth, DatEditor.SprSheetHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            int sprCount = 0;
            int xCols = DatEditor.SprSheetWidth / SpriteWidth;
            int yCols = DatEditor.SprSheetHeight / SpriteHeight;

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
                    LastSpriteid = AllSprList.Count + images.Count - 1,
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
                    for (int i = sprInfo.FirstSpriteid; i <= sprInfo.LastSpriteid; i++)
                    {
                        AllSprList.Add(new ShowList() { Id = (uint)i });
                    }
                }
                SprStatusBar.MessageQueue.Enqueue($"New sheet saved.", null, null, null, false, true, TimeSpan.FromSeconds(3));
                CollectionViewSource.GetDefaultView(_editor.SprListView.ItemsSource).Refresh();
            }
            else
                SprStatusBar.MessageQueue.Enqueue($"Create a new sheet to import.", null, null, null, false, true, TimeSpan.FromSeconds(3));
        }

        private void EditSheet_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (SheetsList.SelectedIndex > -1)
            {               
                ShowList showList = (ShowList)SheetsList.SelectedItem;
                if (showList == null)
                    return;
                EditSheetFor(showList);
            }
        }

        private void EditSheetFor(ShowList showList)
        {
            CurrentSheet = MainWindow.catalog.Find(x => x.File == showList.Name);
            CreateNewSheetImg(CurrentSheet.SpriteType, true);
            EmptyTiles = false;
            List<Image> images = Utils.GetLogicalChildCollection<Image>(SheetWrap);
            Bitmap? original = Utils.BitmapImageToBitmap((BitmapImage)showList.Image);
            if (original == null) {
                return;
            }

            int sprCount = 0;
            int xCols = DatEditor.SprSheetWidth / SpriteWidth;
            int yCols = DatEditor.SprSheetHeight / SpriteHeight;
            for (int x = 0; x < yCols; x++)
            {
                for (int y = 0; y < xCols; y++)
                {
                    Rectangle rect = new(y * SpriteWidth, x * SpriteHeight, SpriteWidth, SpriteHeight);
                    Bitmap crop = new(SpriteWidth, SpriteHeight);
                    crop = original.Clone(rect, crop.PixelFormat);
                    images[sprCount].Source = Utils.BitmapToBitmapImage(crop);
                    sprCount++;
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

        public void OpenForSpriteId(int spriteId)
        {
            foreach (var catalog in MainWindow.catalog)
            {
                if (spriteId >= catalog.FirstSpriteid && spriteId <= catalog.LastSpriteid)
                {
                    string _sprPath = String.Format("{0}{1}", MainWindow._assetsPath, catalog.File);
                    if (File.Exists(_sprPath))
                    {
                        ShowList? foundEntry = CustomSheetsList.FirstOrDefault(showList => showList.Id == (uint)catalog.FirstSpriteid);
                        if (foundEntry == null)
                        {
                            using System.Drawing.Bitmap SheetM = LZMA.DecompressFileLZMA(_sprPath);
                            using Bitmap transparentBitmap = new(SheetM.Width, SheetM.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                            using (Graphics g = Graphics.FromImage(transparentBitmap))
                            {
                                // draw on a transparent background
                                g.Clear(System.Drawing.Color.Transparent);
                                g.DrawImage(SheetM, 0, 0);
                            }

                            // convert magenta (255,0,255,255) -> transparent using LockBits (fast)
                            var rect = new Rectangle(0, 0, transparentBitmap.Width, transparentBitmap.Height);
                            var bmpData = transparentBitmap.LockBits(rect, ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                            int bytes = Math.Abs(bmpData.Stride) * bmpData.Height;
                            byte[] pixels = new byte[bytes];
                            Marshal.Copy(bmpData.Scan0, pixels, 0, bytes);

                            for (int i = 0; i < bytes; i += 4) {
                                byte b = pixels[i];
                                byte gC = pixels[i + 1];
                                byte r = pixels[i + 2];
                                byte a = pixels[i + 3];

                                if (r == 255 && gC == 0 && b == 255 && a == 255) {
                                    // set alpha to 0 -> fully transparent (keep colors if needed)
                                    pixels[i + 3] = 0;
                                }
                            }

                            Marshal.Copy(pixels, 0, bmpData.Scan0, bytes);
                            transparentBitmap.UnlockBits(bmpData);

                            CustomSheetsList.Add(new ShowList() { Id = (uint)catalog.FirstSpriteid, Image = Utils.BitmapToBitmapImage(transparentBitmap), Name = catalog.File });
                        }

                    }
                }
            }
            SheetsList.ItemsSource = SprEditor.CustomSheetsList;
            SheetsList.SelectedIndex = 0;
            if (SheetsList.SelectedIndex > -1)
            {
                ShowList showList = (ShowList)SheetsList.SelectedItem;
                if (showList == null)
                    return;
                EditSheetFor(showList);
            }
        }
    }
}
