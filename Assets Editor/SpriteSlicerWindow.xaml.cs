using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DrawingColor = System.Drawing.Color;
using DrawingRect  = System.Drawing.Rectangle;
using WpfLine      = System.Windows.Shapes.Line;
using WpfRect      = System.Windows.Shapes.Rectangle;
using WpfBrushes   = System.Windows.Media.Brushes;
using WpfColor     = System.Windows.Media.Color;
using Image        = System.Windows.Controls.Image;

namespace Assets_Editor
{
    /// <summary>
    /// Slices an external sprite sheet into individual tiles that can be dragged
    /// onto any managed sheet in <see cref="SprEditor"/>.
    /// </summary>
    public partial class SpriteSlicerWindow : Window
    {
        private Bitmap _sourceBitmap;
        private readonly List<BitmapSource> _slicedSprites = new();
        private int _currentSprW = 32;
        private int _currentSprH = 32;
        private double _previewZoom = 1.0;
        private double _tileZoomFactor = 1.0;
        private bool _isDraggingPreview;
        private System.Windows.Point _dragStartPos;
        private double _dragStartScrollH;
        private double _dragStartScrollV;

        public SpriteSlicerWindow(int defaultSpriteTypeIndex = 0)
        {
            InitializeComponent();

            SpriteSizeCombo.ItemsSource = DatEditor.SpriteSizes
                .Select(s =>
                {
                    string label = $"Width: {s.Width} | Height: {s.Height} ({s.Width / 32}x{s.Height / 32}";
                    if (s.Width > 64 || s.Height > 64)
                        label += ", OTClient";
                    label += ")";
                    return label;
                })
                .ToList();
            SpriteSizeCombo.SelectedIndex =
                Math.Clamp(defaultSpriteTypeIndex, 0, DatEditor.SpriteSizes.Count - 1);
        }

        // file / drop zone 

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Image Files|*.png;*.bmp;*.gif;*.jpg;*.jpeg|All Files|*.*"
            };
            if (dlg.ShowDialog() == true)
                LoadSourceImage(dlg.FileName);
        }

        private async void LoadSourceImage(string path)
        {
            _sourceBitmap?.Dispose();
            _sourceBitmap = null;
            SourcePreview.Visibility = Visibility.Collapsed;
            DropHint.Text = "Loading…";
            DropHint.Visibility = Visibility.Visible;

            try
            {
                // Read bytes and do ALL image decoding on a background thread.
                // Using StreamSource (not UriSource) lets BitmapImage be created and
                // EndInit()'d on a thread-pool thread without needing a Dispatcher.
                (BitmapImage preview, Bitmap fullBmp) = await Task.Run(() =>
                {
                    byte[] bytes = File.ReadAllBytes(path);

                    // Preview: decode at capped resolution so large sheets don't OOM.
                    var bi = new BitmapImage();
                    bi.BeginInit();
                    bi.StreamSource     = new MemoryStream(bytes);
                    bi.DecodePixelWidth = 1024;
                    bi.CacheOption      = BitmapCacheOption.OnLoad;
                    bi.EndInit();
                    bi.Freeze();

                    // Full-res Bitmap for slicing.
                    // IMPORTANT: System.Drawing.Bitmap holds a reference to its source stream
                    // for its entire lifetime, so the MemoryStream must not be disposed until
                    // after Clone() completes — Clone() reads the stream data again internally.
                    var ms   = new MemoryStream(bytes);
                    var raw  = new Bitmap(ms);
                    var full = raw.Clone(new DrawingRect(0, 0, raw.Width, raw.Height),
                                        System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    raw.Dispose();
                    ms.Dispose();

                    return (bi, full);
                });

                _sourceBitmap            = fullBmp;
                SourcePreview.Source     = preview;
                SourcePreview.Visibility = Visibility.Visible;
                DropHint.Text            = "Drop a sprite sheet here, or click Open File";
                DropHint.Visibility      = Visibility.Collapsed;

                // Reset zoom to 1:1 for each new image.
                _previewZoom       = 1.0;
                PreviewScale.ScaleX = 1.0;
                PreviewScale.ScaleY = 1.0;
                ZoomBadge.Visibility = Visibility.Collapsed;

                // Defer until the layout pass has measured the container so ActualWidth/Height are valid.
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render,
                    new Action(UpdateGridOverlay));
            }
            catch (Exception ex)
            {
                DropHint.Text       = "Drop a sprite sheet here, or click Open File";
                DropHint.Visibility = Visibility.Visible;
                SlicerStatusBar.MessageQueue?.Enqueue(
                    $"Failed to load image: {ex.Message}", null, null, null, false, true, TimeSpan.FromSeconds(4));
            }
        }

        private void DropZone_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                DropZone.BorderBrush = new SolidColorBrush(Colors.MediumPurple);
        }

        private void DropZone_DragLeave(object sender, DragEventArgs e) => ResetDropZoneBorder();

        private void DropZone_Drop(object sender, DragEventArgs e)
        {
            ResetDropZoneBorder();
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files?.Length > 0)
                    LoadSourceImage(files[0]);
            }
        }

        private void ResetDropZoneBorder() =>
            DropZone.BorderBrush = new SolidColorBrush(WpfColor.FromRgb(0x67, 0x3A, 0xB7));

        //  grid overlay 

        private void DropZone_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Keep PreviewContainer the same size as the DropZone viewport at zoom=1
            // so that Image Stretch=Uniform has finite bounds to work against.
            double bH = DropZone.BorderThickness.Left + DropZone.BorderThickness.Right;
            double bV = DropZone.BorderThickness.Top  + DropZone.BorderThickness.Bottom;
            double w = DropZone.ActualWidth  - bH;
            double h = DropZone.ActualHeight - bV;
            if (w > 0) PreviewContainer.Width  = w;
            if (h > 0) PreviewContainer.Height = h;
            UpdateGridOverlay();
        }

        private void SpriteSizeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateGridOverlay();

        private void Offset_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e) => UpdateGridOverlay();

        private void HideGridLinesCheck_Changed(object sender, RoutedEventArgs e) => UpdateGridOverlay();

        private void PreviewScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                // Ctrl + wheel → zoom.
                double factor = e.Delta > 0 ? 1.2 : 1.0 / 1.2;
                _previewZoom = Math.Clamp(_previewZoom * factor, 0.25, 12.0);
                ApplyPreviewZoom();
                e.Handled = true;
            }
            // No Ctrl → let the ScrollViewer handle normal vertical scroll.
        }

        private void PreviewScrollViewer_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_sourceBitmap == null) return;
            _isDraggingPreview  = true;
            _dragStartPos       = e.GetPosition(PreviewScrollViewer);
            _dragStartScrollH   = PreviewScrollViewer.HorizontalOffset;
            _dragStartScrollV   = PreviewScrollViewer.VerticalOffset;
            PreviewScrollViewer.CaptureMouse();
            PreviewScrollViewer.Cursor = Cursors.SizeAll;
            e.Handled = true;
        }

        private void PreviewScrollViewer_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDraggingPreview) return;
            var pos = e.GetPosition(PreviewScrollViewer);
            PreviewScrollViewer.ScrollToHorizontalOffset(_dragStartScrollH - (pos.X - _dragStartPos.X));
            PreviewScrollViewer.ScrollToVerticalOffset  (_dragStartScrollV - (pos.Y - _dragStartPos.Y));
            e.Handled = true;
        }

        private void PreviewScrollViewer_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDraggingPreview) return;
            _isDraggingPreview = false;
            PreviewScrollViewer.ReleaseMouseCapture();
            PreviewScrollViewer.Cursor = null;
        }

        private void ApplyPreviewZoom()
        {
            PreviewScale.ScaleX  = _previewZoom;
            PreviewScale.ScaleY  = _previewZoom;
            bool show            = Math.Abs(_previewZoom - 1.0) > 0.01;
            ZoomBadge.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            ZoomLabel.Text       = $"{_previewZoom * 100:F0}%";
            // Recompute grid so StrokeThickness is compensated for the new zoom level.
            UpdateGridOverlay();
        }

        private void UpdateGridOverlay()
        {
            GridOverlay.Children.Clear();

            if (_sourceBitmap == null || SpriteSizeCombo.SelectedIndex < 0)
            {
                GridOverlay.Visibility = Visibility.Collapsed;
                return;
            }

            var size = DatEditor.SpriteSizes[SpriteSizeCombo.SelectedIndex];
            int sprW  = size.Width;
            int sprH  = size.Height;
            int xOff  = XOffset.Value ?? 0;
            int yOff  = YOffset.Value ?? 0;

            // Compute the rendered image rectangle inside the drop zone (Stretch=Uniform, centred).
            double borderH = DropZone.BorderThickness.Left + DropZone.BorderThickness.Right;
            double borderV = DropZone.BorderThickness.Top  + DropZone.BorderThickness.Bottom;
            double availW  = DropZone.ActualWidth  - borderH;
            double availH  = DropZone.ActualHeight - borderV;
            if (availW <= 0 || availH <= 0) return;

            double scale  = Math.Min(availW / _sourceBitmap.Width, availH / _sourceBitmap.Height);
            double imgW   = _sourceBitmap.Width  * scale;
            double imgH   = _sourceBitmap.Height * scale;
            double ox     = (availW - imgW) / 2.0;   // left edge of image in canvas coords
            double oy     = (availH - imgH) / 2.0;   // top edge

            // Outer image boundary
            var outerRect = new WpfRect
            {
                Width           = imgW,
                Height          = imgH,
                Stroke          = new SolidColorBrush(WpfColor.FromArgb(220, 255, 200, 0)),
                StrokeThickness = 1.0,
                Fill            = WpfBrushes.Transparent
            };
            Canvas.SetLeft(outerRect, ox);
            Canvas.SetTop(outerRect,  oy);
            GridOverlay.Children.Add(outerRect);

            var lineBrush     = new SolidColorBrush(WpfColor.FromArgb(190, 255, 80, 0));
            var dashArray     = new DoubleCollection { 4, 3 };
            // Compensate thickness so lines stay ~1 px on screen regardless of zoom.
            double lineThickness = Math.Max(0.1, 0.8 / _previewZoom);

            int colCount  = (int)Math.Ceiling((double)(_sourceBitmap.Width  - xOff) / sprW);
            int rowCount  = (int)Math.Ceiling((double)(_sourceBitmap.Height - yOff) / sprH);
            bool hideLines = HideGridLinesCheck.IsChecked == true;

            if (!hideLines)
            {
                // Vertical slice lines
                for (int x = xOff + sprW; x < _sourceBitmap.Width; x += sprW)
                {
                    double px = ox + x * scale;
                    if (px >= ox + imgW) break;
                    GridOverlay.Children.Add(new WpfLine
                    {
                        X1 = px, Y1 = oy + yOff * scale,
                        X2 = px, Y2 = oy + imgH,
                        Stroke          = lineBrush,
                        StrokeThickness = lineThickness,
                        StrokeDashArray = dashArray
                    });
                }

                // Horizontal slice lines
                for (int y = yOff + sprH; y < _sourceBitmap.Height; y += sprH)
                {
                    double py = oy + y * scale;
                    if (py >= oy + imgH) break;
                    GridOverlay.Children.Add(new WpfLine
                    {
                        X1 = ox + xOff * scale, Y1 = py,
                        X2 = ox + imgW,         Y2 = py,
                        Stroke          = lineBrush,
                        StrokeThickness = lineThickness,
                        StrokeDashArray = dashArray
                    });
                }
            }
            else
            {
                // Lines hidden — show a small info label so the user knows why.
                var note = new System.Windows.Controls.TextBlock
                {
                    Text       = $"{colCount * rowCount} sprites — grid lines hidden",
                    Foreground = WpfBrushes.Yellow,
                    FontSize   = Math.Max(8, 11 / _previewZoom),
                    Background = new SolidColorBrush(WpfColor.FromArgb(140, 0, 0, 0)),
                    Padding    = new Thickness(4, 2, 4, 2)
                };
                Canvas.SetLeft(note, ox + 4);
                Canvas.SetTop(note,  oy + 4);
                GridOverlay.Children.Add(note);
            }

            GridOverlay.Visibility = Visibility.Visible;
        }

        //  slicing 

        private async void Slice_Click(object sender, RoutedEventArgs e)
        {
            if (_sourceBitmap == null)
            {
                SlicerStatusBar.MessageQueue?.Enqueue(
                    "Please load a source sprite sheet first.", null, null, null, false, true, TimeSpan.FromSeconds(3));
                return;
            }

            int spriteTypeIndex = SpriteSizeCombo.SelectedIndex;
            var size = DatEditor.SpriteSizes[spriteTypeIndex];
            _currentSprW = size.Width;
            _currentSprH = size.Height;
            int xOffset = XOffset.Value ?? 0;
            int yOffset = YOffset.Value ?? 0;

            // Disable the button so the user can't trigger a second slice while busy.
            ((System.Windows.Controls.Button)sender).IsEnabled = false;
            SlicerStatusBar.MessageQueue?.Enqueue(
                "Slicing… please wait.", null, null, null, false, true, TimeSpan.FromSeconds(60));

            // Release memory from any previous batch before allocating the new one.
            ClearSlicedSprites();

            // Capture everything the background thread will need before leaving the UI thread.
            Bitmap srcSnapshot    = _sourceBitmap;
            int    sprW           = _currentSprW;
            int    sprH           = _currentSprH;

            // Detect whether the source carries a real alpha channel.
            bool sourceHasAlpha = (srcSnapshot.PixelFormat & System.Drawing.Imaging.PixelFormat.Alpha) != 0
                                  || srcSnapshot.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb
                                  || srcSnapshot.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppPArgb
                                  || srcSnapshot.PixelFormat == System.Drawing.Imaging.PixelFormat.Format64bppArgb;

            DrawingColor bgColor = sourceHasAlpha
                ? DrawingColor.Empty
                : DetectBackgroundColor(srcSnapshot);

            // Run all per-tile work on a thread-pool thread so the UI stays responsive.
            List<BitmapSource> results = await Task.Run(() =>
            {
                var list = new List<BitmapSource>();
                int col = 0, row = 0;
                while (yOffset + row * sprH + sprH <= srcSnapshot.Height)
                {
                    int x = xOffset + col * sprW;
                    int y = yOffset + row * sprH;

                    if (x + sprW > srcSnapshot.Width)
                    {
                        col = 0;
                        row++;
                        continue;
                    }

                    var rect = new DrawingRect(x, y, sprW, sprH);
                    using Bitmap raw       = srcSnapshot.Clone(rect, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    using Bitmap processed = PrepareSlicedBitmap(raw, sourceHasAlpha, bgColor);

                    // Skip tiles that are entirely transparent/background (all magenta after processing).
                    if (!IsBlankSprite(processed))
                    {
                        BitmapSource bs = Utils.BitmapToBitmapImage(processed);
                        bs.Freeze();   // required to hand a BitmapSource across threads
                        list.Add(bs);
                    }
                    col++;
                }
                return list;
            });

            ((System.Windows.Controls.Button)sender).IsEnabled = true;

            if (results.Count == 0)
            {
                SlicerStatusBar.MessageQueue?.Enqueue(
                    "No sprites could be sliced with the current settings.", null, null, null, false, true, TimeSpan.FromSeconds(3));
                return;
            }

            _slicedSprites.AddRange(results);
            RebuildSlicedPanel();
            SlicerStatusBar.MessageQueue?.Enqueue(
                $"Sliced {_slicedSprites.Count} sprites. Drag any tile onto a sheet slot to insert from that position.",
                null, null, null, false, true, TimeSpan.FromSeconds(4));
        }

        private void ClearSlices_Click(object sender, RoutedEventArgs e)
        {
            ClearSlicedSprites();
        }

        /// <summary>
        /// Clears all sliced-sprite data and forces a full GC cycle on a background thread so
        /// the unmanaged WIC/GDI+ memory backing each <see cref="BitmapSource"/> is reclaimed
        /// promptly rather than waiting for the next scheduled collection.
        /// </summary>
        private void ClearSlicedSprites()
        {
            // Drop all UI references first so nothing keeps the BitmapSources alive.
            SlicedSpritesPanel.Children.Clear();
            _slicedSprites.Clear();
            UpdateSliceCountLabel();

            // Run GC on a pool thread to avoid blocking the UI.
            // Two passes are needed: the first collects the BitmapSource wrappers and
            // queues their finalizers; WaitForPendingFinalizers lets the finalizer thread
            // release the unmanaged COM/WIC objects; the second pass collects anything
            // that became unreachable only after finalization.
            Task.Run(() =>
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true);
                GC.WaitForPendingFinalizers();
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true);
            });
        }

        //  transparency helpers 

        /// <summary>
        /// Converts a cropped tile so that transparent (or detected-background) pixels become
        /// opaque magenta — the format the SprEditor pipeline expects.
        /// </summary>
        private static Bitmap PrepareSlicedBitmap(Bitmap crop, bool sourceHasAlpha, DrawingColor bgColor)
        {
            Bitmap result = new Bitmap(crop.Width, crop.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            // Normalise to 96 DPI so WPF displays the tile at the expected DIP size,
            // regardless of whatever DPI metadata the source image file carries.
            result.SetResolution(96, 96);

            // Draw on a magenta backing.  Use the pixel-rectangle overload so GDI+ copies
            // exactly crop.Width x crop.Height pixels without applying any DPI-driven
            // scaling — otherwise a 144-DPI source image would be drawn at ~67 % of the
            // expected size, leaving magenta corners (the "mini sprite in top-left" bug).
            using (Graphics g = Graphics.FromImage(result))
            {
                g.Clear(DrawingColor.FromArgb(255, 255, 0, 255));
                g.DrawImage(crop,
                    new DrawingRect(0, 0, result.Width, result.Height),
                    new DrawingRect(0, 0, crop.Width,   crop.Height),
                    GraphicsUnit.Pixel);
            }

            if (!sourceHasAlpha)
            {
                // Source had no alpha channel: every pixel is fully opaque, so the magenta
                // backing above is completely hidden.  Replace the detected background colour
                // with magenta ourselves.
                ReplaceColorWithMagenta(result, bgColor);
            }

            return result;
        }

        /// <summary>
        /// Returns <see langword="true"/> if every pixel in <paramref name="bmp"/> is opaque magenta
        /// (R=255, G=0, B=255, A=255), meaning the tile contains no real content.
        /// </summary>
        private static bool IsBlankSprite(Bitmap bmp)
        {
            var rect = new DrawingRect(0, 0, bmp.Width, bmp.Height);
            var data = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly,
                                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            try
            {
                int bytes = Math.Abs(data.Stride) * data.Height;
                byte[] px  = new byte[bytes];
                Marshal.Copy(data.Scan0, px, 0, bytes);
                // Memory layout for Format32bppArgb: B G R A
                for (int i = 0; i < bytes; i += 4)
                {
                    if (px[i] != 255 || px[i + 1] != 0 || px[i + 2] != 255 || px[i + 3] != 255)
                        return false;
                }
                return true;
            }
            finally
            {
                bmp.UnlockBits(data);
            }
        }

        /// <summary>Replaces every pixel that exactly matches <paramref name="target"/> with opaque magenta.</summary>
        private static void ReplaceColorWithMagenta(Bitmap bmp, DrawingColor target)
        {
            var rect = new DrawingRect(0, 0, bmp.Width, bmp.Height);
            var data = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite,
                                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            int bytes = Math.Abs(data.Stride) * data.Height;
            byte[] pixels = new byte[bytes];
            Marshal.Copy(data.Scan0, pixels, 0, bytes);

            byte tR = target.R, tG = target.G, tB = target.B;

            for (int i = 0; i < bytes; i += 4)
            {
                // Format32bppArgb in memory layout: B G R A
                if (pixels[i + 2] == tR && pixels[i + 1] == tG && pixels[i] == tB)
                {
                    pixels[i]     = 255; // B
                    pixels[i + 1] = 0;   // G
                    pixels[i + 2] = 255; // R
                    pixels[i + 3] = 255; // A — opaque magenta
                }
            }

            Marshal.Copy(pixels, 0, data.Scan0, bytes);
            bmp.UnlockBits(data);
        }

        /// <summary>
        /// Samples the four corners of <paramref name="bmp"/> and returns the most common colour
        /// (falling back to the top-left pixel) as the background colour to remove.
        /// </summary>
        private static DrawingColor DetectBackgroundColor(Bitmap bmp)
        {
            var corners = new[]
            {
                bmp.GetPixel(0, 0),
                bmp.GetPixel(bmp.Width - 1, 0),
                bmp.GetPixel(0, bmp.Height - 1),
                bmp.GetPixel(bmp.Width - 1, bmp.Height - 1)
            };
            return corners
                .GroupBy(c => c.ToArgb())
                .OrderByDescending(g => g.Count())
                .First().First();
        }

        //  tile panel 

        private void TileZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _tileZoomFactor = e.NewValue;
            if (TileZoomLabel != null)
                TileZoomLabel.Text = $"{_tileZoomFactor:F1}x";
            if (_slicedSprites.Count > 0)
                RebuildSlicedPanel();
        }

        private void RebuildSlicedPanel()
        {
            SlicedSpritesPanel.Children.Clear();
            for (int i = 0; i < _slicedSprites.Count; i++)
                SlicedSpritesPanel.Children.Add(CreateSlicedTile(_slicedSprites[i], i));
            UpdateSliceCountLabel();
        }

        private void UpdateSliceCountLabel()
        {
            SliceCountLabel.Text = $"{_slicedSprites.Count} sprite{(_slicedSprites.Count == 1 ? "" : "s")}";
        }

        private Border CreateSlicedTile(BitmapSource bmpSrc, int index)
        {
            int baseSize    = Math.Max(32, Math.Min(96, Math.Max(_currentSprW, _currentSprH)));
            int displaySize = Math.Max(16, (int)Math.Round(baseSize * _tileZoomFactor));

            Border border = new Border
            {
                Width           = displaySize,
                Height          = displaySize,
                Margin          = new Thickness(2),
                BorderBrush     = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(1),
                Cursor          = Cursors.Hand,
                Tag             = index
            };

            Image img = new Image
            {
                Source              = bmpSrc,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment   = VerticalAlignment.Stretch,
                Stretch             = Stretch.Uniform
            };
            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor);

            border.Child = img;
            border.MouseEnter += (s, _) => ((Border)s).BorderBrush = new SolidColorBrush(Colors.MediumPurple);
            border.MouseLeave += (s, _) => ((Border)s).BorderBrush = new SolidColorBrush(Colors.Transparent);
            border.MouseDown  += SlicedTile_MouseDown;
            return border;
        }

        private void SlicedTile_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            if (sender is not Border border) return;

            int startIndex = (int)border.Tag;
            var remaining  = _slicedSprites.Skip(startIndex).ToList();
            if (remaining.Count == 0) return;

            var data = new SlicedSpritesDragData
            {
                Images      = remaining,
                SourceWindow = this,
                StartIndex  = startIndex
            };

            DragDrop.DoDragDrop(border, data, DragDropEffects.Copy);
        }

        //  called by SprEditor after a successful drop 

        /// <summary>
        /// Removes sprites that were successfully placed into a sheet, starting at
        /// <paramref name="startIndex"/> in the slicer's list, for <paramref name="count"/> items.
        /// </summary>
        public void RemovePlacedSprites(int startIndex, int count)
        {
            int safeCount = Math.Min(count, _slicedSprites.Count - startIndex);
            if (safeCount <= 0) return;

            _slicedSprites.RemoveRange(startIndex, safeCount);
            RebuildSlicedPanel();

            string msg = _slicedSprites.Count == 0
                ? "All sliced sprites have been placed."
                : $"{safeCount} sprite(s) placed. {_slicedSprites.Count} remaining.";
            SlicerStatusBar.MessageQueue?.Enqueue(msg, null, null, null, false, true, TimeSpan.FromSeconds(3));
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            // Clear sprites and free their unmanaged backing memory.
            ClearSlicedSprites();
            _sourceBitmap?.Dispose();
            _sourceBitmap = null;
        }
    }

    /// <summary>
    /// Drag-data object used when moving sliced sprites from <see cref="SpriteSlicerWindow"/>
    /// onto a managed sheet in <see cref="SprEditor"/>.
    /// </summary>
    public class SlicedSpritesDragData
    {
        public List<BitmapSource> Images { get; set; }
        public SpriteSlicerWindow SourceWindow { get; set; }
        /// <summary>Index in the slicer's internal list from which dragging started.</summary>
        public int StartIndex { get; set; }
    }
}
