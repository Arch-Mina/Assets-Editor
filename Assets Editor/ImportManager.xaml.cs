using Microsoft.Win32;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Tibia.Protobuf.Appearances;

namespace Assets_Editor
{
    /// <summary>
    /// Interaction logic for ImportManager.xaml
    /// </summary>
    public partial class ImportManager : Window
    {
        private LegacyDatEditor _editor;
        public ImportManager(LegacyDatEditor editor)
        {
            InitializeComponent();
            _editor = editor;
            this.Closed += ImportManager_Closed;
        }
        private Appearances ImportAppearances;
        private Dictionary<uint, Sprite> sprites = new Dictionary<uint, Sprite>();
        private List<ShowList> AllSprList = new List<ShowList>();
        private ConcurrentDictionary<int, MemoryStream> SprLists = new ConcurrentDictionary<int, MemoryStream>();
        private ObservableCollection<ShowList> ThingsOutfit = new ObservableCollection<ShowList>();
        private ObservableCollection<ShowList> ThingsItem = new ObservableCollection<ShowList>();
        private ObservableCollection<ShowList> ThingsEffect = new ObservableCollection<ShowList>();
        private ObservableCollection<ShowList> ThingsMissile = new ObservableCollection<ShowList>();
        private static SpriteStorage MainSprStorage;
        private void ImportManager_Closed(object sender, EventArgs e)
        {
            this.Closed -= ImportManager_Closed;
            sprites.Clear();
            SprLists.Clear();
            AllSprList.Clear();
            ThingsOutfit.Clear();
            ThingsItem.Clear();
            ThingsEffect.Clear();
            ThingsMissile.Clear();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private async void OpenClient_Click(object sender, RoutedEventArgs e)
        {
            OpenClient.IsEnabled = false;
            await LoadImportClient();
            
            
        }
        private async Task LoadImportClient()
        {
            System.Windows.Forms.FolderBrowserDialog _assets = new();
            if (_assets.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string _assetsPath = _assets.SelectedPath;
                if (_assetsPath.EndsWith("\\") == false)
                    _assetsPath += "\\";
                ClientPathText.Text = _assetsPath;
                if (_assetsPath != "" && File.Exists(_assetsPath + "Tibia.dat") == true && File.Exists(_assetsPath + "Tibia.spr") == true)
                {
                    var progressReporter = new Progress<int>(value =>
                    {
                        LoadProgress.Value = value;
                    });
                    await Task.Run(() =>
                    {

                        string _datPath = String.Format("{0}{1}", _assetsPath, "Tibia.dat");
                        string _sprPath = String.Format("{0}{1}", _assetsPath, "Tibia.spr");
                        LegacyAppearance Dat = new();
                        Dat.ReadLegacyDat(_datPath, MainWindow.GetCurrentLoadedVersion());
                        ImportAppearances = Dat.Appearances;

                        bool transparency = false;
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            transparency = (bool)SprTransparent.IsChecked;
                        });

                        MainSprStorage = new SpriteStorage(_sprPath, transparency, progressReporter);
                        MainSprStorage.LoadSprites();
                        SprLists = MainSprStorage.SprLists;
                        sprites = MainSprStorage.Sprites;
                        for (uint i = 0; i < sprites.Count; i++)
                        {
                            AllSprList.Add(new ShowList() { Id = i });
                        }

                    });
                    foreach (var outfit in ImportAppearances.Outfit)
                    {
                        ThingsOutfit.Add(new ShowList() { Id = outfit.Id });
                    }
                    foreach (var item in ImportAppearances.Object)
                    {
                        ThingsItem.Add(new ShowList() { Id = item.Id });
                    }
                    foreach (var effect in ImportAppearances.Effect)
                    {
                        ThingsEffect.Add(new ShowList() { Id = effect.Id });
                    }
                    foreach (var missile in ImportAppearances.Missile)
                    {
                        ThingsMissile.Add(new ShowList() { Id = missile.Id });
                    }
                    SprListView.ItemsSource = AllSprList;
                    UpdateShowList(ObjectMenu.SelectedIndex);
                }
            }
        }

        private void UpdateShowList(int selection)
        {
            if (ObjListView != null)
            {
                ObjListViewSelectedIndex.Minimum = 1;
                if (selection == 0)
                    ObjListView.ItemsSource = ThingsOutfit;
                else if (selection == 1)
                {
                    ObjListView.ItemsSource = ThingsItem;
                    ObjListViewSelectedIndex.Minimum = 100;
                }
                else if (selection == 2)
                    ObjListView.ItemsSource = ThingsEffect;
                else if (selection == 3)
                    ObjListView.ItemsSource = ThingsMissile;

                ObjListView.SelectedIndex = 0;
            }
        }
        private void ObjectMenuChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateShowList(ObjectMenu.SelectedIndex);
        }
        private void ObjListView_ScrollChanged(object sender, System.Windows.Controls.ScrollChangedEventArgs e)
        {
            VirtualizingStackPanel panel = Utils.FindVisualChild<VirtualizingStackPanel>(ObjListView);
            if (ObjListView.Items.Count > 0 && panel != null)
            {
                int offset = (int)panel.VerticalOffset;
                for (int i = 0; i < ObjListView.Items.Count; i++)
                {
                    if (i >= offset && i < Math.Min(offset + 20, ObjListView.Items.Count))
                    {
                        if (ObjectMenu.SelectedIndex == 0)
                            ThingsOutfit[i].Image = Utils.BitmapToBitmapImage(LegacyAppearance.GetObjectImage(ImportAppearances.Outfit[i], MainSprStorage));
                        else if (ObjectMenu.SelectedIndex == 1)
                            ThingsItem[i].Image = Utils.BitmapToBitmapImage(LegacyAppearance.GetObjectImage(ImportAppearances.Object[i], MainSprStorage));
                        else if (ObjectMenu.SelectedIndex == 2)
                            ThingsEffect[i].Image = Utils.BitmapToBitmapImage(LegacyAppearance.GetObjectImage(ImportAppearances.Effect[i], MainSprStorage));
                        else if (ObjectMenu.SelectedIndex == 3)
                            ThingsMissile[i].Image = Utils.BitmapToBitmapImage(LegacyAppearance.GetObjectImage(ImportAppearances.Missile[i], MainSprStorage));
                    }
                    else
                    {
                        if (ObjectMenu.SelectedIndex == 0)
                            ThingsOutfit[i].Image = null;
                        else if (ObjectMenu.SelectedIndex == 1)
                            ThingsItem[i].Image = null;
                        else if (ObjectMenu.SelectedIndex == 2)
                            ThingsEffect[i].Image = null;
                        else if (ObjectMenu.SelectedIndex == 3)
                            ThingsMissile[i].Image = null;
                    }
                }
            }
        }
        private void ObjListView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ShowList showList = (ShowList)ObjListView.SelectedItem;
            if (showList != null)
            {
                ObjListViewSelectedIndex.Value = (int)showList.Id;
            }
        }
        private void ObjListViewSelectedIndex_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (ObjListView.IsLoaded)
            {
                foreach (ShowList item in ObjListView.Items)
                {
                    if (item.Id == ObjListViewSelectedIndex.Value)
                    {
                        ObjListView.SelectedItem = item;
                        ScrollViewer scrollViewer = Utils.FindVisualChild<ScrollViewer>(ObjListView);
                        VirtualizingStackPanel panel = Utils.FindVisualChild<VirtualizingStackPanel>(ObjListView);
                        int offset = (int)panel.VerticalOffset;
                        int maxOffset = (int)panel.ViewportHeight;
                        if (ObjListView.SelectedIndex > offset + maxOffset || ObjListView.SelectedIndex < offset)
                        {
                            scrollViewer.ScrollToVerticalOffset(ObjListView.SelectedIndex);
                        }
                        break;
                    }
                }
            }
        }

        private void SprListView_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            VirtualizingStackPanel panel = Utils.FindVisualChild<VirtualizingStackPanel>(SprListView);
            if (SprListView.Items.Count > 0 && panel != null)
            {
                int offset = (int)panel.VerticalOffset;
                for (int i = 0; i < SprListView.Items.Count; i++)
                {
                    if (i >= offset && i < Math.Min(offset + 20, SprListView.Items.Count) && SprLists.ContainsKey(i))
                        AllSprList[i].Image = Utils.BitmapToBitmapImage(MainSprStorage.getSpriteStream((uint)i));
                    else
                        AllSprList[i].Image = null;
                }
            }
        }
        private void SprListViewSelectedIndex_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (SprListView.IsLoaded && e.NewValue != null)
            {
                int nIndex = (int)e.NewValue;
                SprListView.SelectedIndex = nIndex;
                ScrollViewer scrollViewer = Utils.FindVisualChild<ScrollViewer>(SprListView);
                VirtualizingStackPanel panel = Utils.FindVisualChild<VirtualizingStackPanel>(SprListView);
                int offset = (int)panel.VerticalOffset;
                int maxOffset = (int)panel.ViewportHeight;
                if (nIndex - maxOffset == offset)
                    scrollViewer.ScrollToVerticalOffset(offset + 1);
                else if (nIndex + 1 == offset)
                    scrollViewer.ScrollToVerticalOffset(offset - 1);
                else if (nIndex >= offset + maxOffset || nIndex < offset)
                    scrollViewer.ScrollToVerticalOffset(SprListView.SelectedIndex);
            }
        }

        private void ImportSpr_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            List<ShowList> selectedItems = SprListView.SelectedItems.Cast<ShowList>().ToList();
            if (selectedItems.Any())
            {
                SprListViewSelectedIndex.Value = (int)selectedItems.Last().Id;
                foreach (var item in selectedItems)
                {
                    int sprId = MainWindow.SprLists.Count;
                    MainWindow.SprLists[sprId] = MainSprStorage.getSpriteStream(item.Id);
                    MainWindow.AllSprList.Add(new ShowList() { Id = (uint)sprId });

                }
                
                CollectionViewSource.GetDefaultView(_editor.SprListView.ItemsSource).Refresh();
            }
        }

        private void updateObjectAppearanceSprite(Appearance ObjectAppearance, SpriteStorage spriteStorage)
        {
            SpriteInfo spriteInfo = ObjectAppearance.FrameGroup[0].SpriteInfo;
            
            for (uint i = 0; i < ObjectAppearance.FrameGroup.Count; i++)
            {
                for (uint s = 0; s < ObjectAppearance.FrameGroup[(int)i].SpriteInfo.SpriteId.Count; s++)
                {
                    int sprId = MainWindow.SprLists.Count;
                    int indexId = (int)ObjectAppearance.FrameGroup[(int)i].SpriteInfo.SpriteId[(int)s];
                    MainWindow.SprLists[sprId] = spriteStorage.getSpriteStream((uint)indexId);
                    MainWindow.AllSprList.Add(new ShowList() { Id = (uint)sprId });
                    ObjectAppearance.FrameGroup[(int)i].SpriteInfo.SpriteId[(int)s] = (uint)sprId;
                }
            }
            CollectionViewSource.GetDefaultView(_editor.SprListView.ItemsSource).Refresh();
        }

        private void updateObjectAppearanceSprite(Appearance ObjectAppearance, ConcurrentDictionary<int, MemoryStream> list)
        {
            SpriteInfo spriteInfo = ObjectAppearance.FrameGroup[0].SpriteInfo;

            for (uint i = 0; i < ObjectAppearance.FrameGroup.Count; i++)
            {
                for (uint s = 0; s < ObjectAppearance.FrameGroup[(int)i].SpriteInfo.SpriteId.Count; s++)
                {
                    int sprId = MainWindow.SprLists.Count;
                    int indexId = (int)ObjectAppearance.FrameGroup[(int)i].SpriteInfo.SpriteId[(int)s];
                    MainWindow.SprLists[sprId] = list[indexId];
                    MainWindow.AllSprList.Add(new ShowList() { Id = (uint)sprId });
                    ObjectAppearance.FrameGroup[(int)i].SpriteInfo.SpriteId[(int)s] = (uint)sprId;
                }
            }
            
            CollectionViewSource.GetDefaultView(_editor.SprListView.ItemsSource).Refresh();
        }

        private void ObjectImport_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            List<ShowList> selectedItems = ObjListView.SelectedItems.Cast<ShowList>().ToList();
            if (selectedItems.Any())
            {
                ObjListViewSelectedIndex.Value = (int)selectedItems.Last().Id;
                foreach (var item in selectedItems)
                {
                    Appearance CurrentObjectAppearance;
                    
                    if (ObjectMenu.SelectedIndex == 0)
                    {
                        CurrentObjectAppearance = ImportAppearances.Outfit.FirstOrDefault(o => o.Id == item.Id).Clone();
                        updateObjectAppearanceSprite(CurrentObjectAppearance, MainSprStorage);
                        CurrentObjectAppearance.Id = (uint)MainWindow.appearances.Outfit.Count + 1;
                        MainWindow.appearances.Outfit.Add(CurrentObjectAppearance);
                        _editor.ThingsOutfit.Add(new ShowList() { Id = CurrentObjectAppearance.Id });
                    }
                    else if (ObjectMenu.SelectedIndex == 1)
                    {
                        CurrentObjectAppearance = ImportAppearances.Object.FirstOrDefault(o => o.Id == item.Id).Clone();
                        updateObjectAppearanceSprite(CurrentObjectAppearance, MainSprStorage);
                        CurrentObjectAppearance.Id = (uint)MainWindow.appearances.Object.Count + 100;
                        MainWindow.appearances.Object.Add(CurrentObjectAppearance);
                        _editor.ThingsItem.Add(new ShowList() { Id = CurrentObjectAppearance.Id });
                    }
                    else if (ObjectMenu.SelectedIndex == 2)
                    {
                        CurrentObjectAppearance = ImportAppearances.Effect.FirstOrDefault(o => o.Id == item.Id).Clone();
                        updateObjectAppearanceSprite(CurrentObjectAppearance, MainSprStorage);
                        CurrentObjectAppearance.Id = (uint)MainWindow.appearances.Effect.Count + 1;
                        MainWindow.appearances.Effect.Add(CurrentObjectAppearance);
                        _editor.ThingsEffect.Add(new ShowList() { Id = CurrentObjectAppearance.Id });
                    }
                    else if (ObjectMenu.SelectedIndex == 3)
                    {
                        CurrentObjectAppearance = ImportAppearances.Missile.FirstOrDefault(o => o.Id == item.Id).Clone();
                        updateObjectAppearanceSprite(CurrentObjectAppearance, MainSprStorage);
                        CurrentObjectAppearance.Id = (uint)MainWindow.appearances.Missile.Count + 1;
                        MainWindow.appearances.Missile.Add(CurrentObjectAppearance);
                        _editor.ThingsMissile.Add(new ShowList() { Id = CurrentObjectAppearance.Id });
                    }
                }
                _editor.ObjectMenu.SelectedIndex = ObjectMenu.SelectedIndex;
                _editor.UpdateShowList(ObjectMenu.SelectedIndex);

            }
        }

        private void ObjectImportAs_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ImportAsDialogHost.IsOpen = true;
        }
        private void ConvertObject(object sender, RoutedEventArgs e)
        {
            ShowList showList = (ShowList)ObjListView.SelectedItem;
            if (showList != null && ObjectMenu.SelectedIndex == 2)
            {
                Appearance ObjectAppearance = ImportAppearances.Effect[ObjListView.SelectedIndex].Clone();
                SpriteInfo spriteInfo = ObjectAppearance.FrameGroup[0].SpriteInfo;

                for (uint i = 0; i < ObjectAppearance.FrameGroup.Count; i++)
                {
                    for (uint s = 0; s < ObjectAppearance.FrameGroup[(int)i].SpriteInfo.SpriteId.Count; s++)
                    {
                        int sprId = MainWindow.SprLists.Count;
                        int indexId = (int)ObjectAppearance.FrameGroup[(int)i].SpriteInfo.SpriteId[(int)s];
                        MainWindow.SprLists[sprId] = MainSprStorage.getSpriteStream((uint)indexId);
                        MainWindow.AllSprList.Add(new ShowList() { Id = (uint)sprId });
                        ObjectAppearance.FrameGroup[(int)i].SpriteInfo.SpriteId[(int)s] = (uint)sprId;
                    }
                }
                CollectionViewSource.GetDefaultView(_editor.SprListView.ItemsSource).Refresh();
                SpriteInfo NewSpriteInfo = spriteInfo.Clone();
                NewSpriteInfo.SpriteId.Clear();
                int counter = 0;
                for (int frame = 0; frame < spriteInfo.PatternFrames; frame++)
                {
                    for (int dir = 0; dir < 4; dir++)
                    {
                        for (byte w = 0; w < spriteInfo.PatternWidth; w++)
                        {
                            for (byte h = 0; h < spriteInfo.PatternHeight; h++)
                            {
                                int index = LegacyAppearance.GetSpriteIndex(ObjectAppearance.FrameGroup[0], w, h, 0, 0, 0, 0, frame);
                                NewSpriteInfo.SpriteId.Add(spriteInfo.SpriteId[index]);
                                counter++;
                            }
                        }
                    }
                }
                ObjectAppearance.Id = (uint)MainWindow.appearances.Outfit.Count + 1;
                MainWindow.appearances.Outfit.Add(ObjectAppearance);
                _editor.ThingsOutfit.Add(new ShowList() { Id = ObjectAppearance.Id });
                _editor.ObjectMenu.SelectedIndex = 0;
                _editor.UpdateShowList(0);

            }
        }

        private void ImportObject_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "OBD Files (*.obd)|*.obd";
            if (openFileDialog.ShowDialog() == true)
            {
                string selectedFilePath = openFileDialog.FileName;
                ConcurrentDictionary<int, MemoryStream> objectSprList = new ConcurrentDictionary<int, MemoryStream>();
                Appearance appearance = ObdDecoder.Load(selectedFilePath, ref objectSprList);
                if (appearance != null)
                {
                    OBDImage.Source = Utils.BitmapToBitmapImage(LegacyAppearance.GetObjectImage(appearance, objectSprList));
                    if (ObjImportSlider.Value == 1)
                    {
                        if (appearance.AppearanceType == APPEARANCE_TYPE.AppearanceOutfit)
                        {
                            updateObjectAppearanceSprite(appearance, objectSprList);
                            appearance.Id = (uint)MainWindow.appearances.Outfit.Count + 1;
                            MainWindow.appearances.Outfit.Add(appearance);
                            _editor.ThingsOutfit.Add(new ShowList() { Id = appearance.Id });
                            _editor.ObjectMenu.SelectedIndex = 0;
                            _editor.UpdateShowList(0);
                        }
                        else if (appearance.AppearanceType == APPEARANCE_TYPE.AppearanceObject)
                        {
                            updateObjectAppearanceSprite(appearance, objectSprList);
                            appearance.Id = (uint)MainWindow.appearances.Object.Count + 100;
                            MainWindow.appearances.Object.Add(appearance);
                            _editor.ThingsItem.Add(new ShowList() { Id = appearance.Id });
                            _editor.ObjectMenu.SelectedIndex = 1;
                            _editor.UpdateShowList(1);
                        }
                        else if (appearance.AppearanceType == APPEARANCE_TYPE.AppearanceEffect)
                        {
                            updateObjectAppearanceSprite(appearance, objectSprList);
                            appearance.Id = (uint)MainWindow.appearances.Effect.Count + 1;
                            MainWindow.appearances.Effect.Add(appearance);
                            _editor.ThingsEffect.Add(new ShowList() { Id = appearance.Id });
                            _editor.ObjectMenu.SelectedIndex = 2;
                            _editor.UpdateShowList(2);
                        }
                        else if (appearance.AppearanceType == APPEARANCE_TYPE.AppearanceMissile)
                        {
                            updateObjectAppearanceSprite(appearance, objectSprList);
                            appearance.Id = (uint)MainWindow.appearances.Missile.Count + 1;
                            MainWindow.appearances.Missile.Add(appearance);
                            _editor.ThingsMissile.Add(new ShowList() { Id = appearance.Id });
                            _editor.ObjectMenu.SelectedIndex = 3;
                            _editor.UpdateShowList(3);
                        }
                    }
                }

            }
        }
        private void ObjListView_Drag(object sender, MouseEventArgs e)
        {
            
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point dragPosition = e.GetPosition(ObjListView);
                var result = VisualTreeHelper.HitTest(ObjListView, dragPosition);
                ShowList data = null;
                if (result != null)
                {
                    ListViewItem listViewItem = Utils.FindAncestorOrSelf<ListViewItem>(result.VisualHit);
                    if (listViewItem != null)
                    {
                        data = (ShowList)listViewItem.DataContext;

                    }
                    else
                    {
                        base.OnMouseMove(e);
                        return;
                    }
                }
                if (data != null)
                {
                    DragDrop.DoDragDrop(ObjListView, data, DragDropEffects.Link);

                    if (_editor.ReplaceObjectAppearance != null)
                    {
                        Appearance CurrentObjectAppearance;
                        if (ObjectMenu.SelectedIndex == _editor.ObjectMenu.SelectedIndex)
                        {
                            if (_editor.ReplaceObjectAppearance.AppearanceType == APPEARANCE_TYPE.AppearanceOutfit)
                            {
                                CurrentObjectAppearance = ImportAppearances.Outfit[ObjListView.SelectedIndex].Clone();
                                updateObjectAppearanceSprite(CurrentObjectAppearance, MainSprStorage);
                                CurrentObjectAppearance.Id = _editor.ReplaceObjectAppearance.Id;
                                MainWindow.appearances.Outfit[(int)_editor.ReplaceObjectAppearance.Id - 1] = CurrentObjectAppearance.Clone();
                            }
                            else if (_editor.ReplaceObjectAppearance.AppearanceType == APPEARANCE_TYPE.AppearanceObject)
                            {
                                CurrentObjectAppearance = ImportAppearances.Object[ObjListView.SelectedIndex].Clone();
                                updateObjectAppearanceSprite(CurrentObjectAppearance, MainSprStorage);
                                CurrentObjectAppearance.Id = _editor.ReplaceObjectAppearance.Id;
                                MainWindow.appearances.Object[(int)_editor.ReplaceObjectAppearance.Id - 100] = CurrentObjectAppearance.Clone();
                            }
                            else if (_editor.ReplaceObjectAppearance.AppearanceType == APPEARANCE_TYPE.AppearanceEffect)
                            {
                                CurrentObjectAppearance = ImportAppearances.Effect[ObjListView.SelectedIndex].Clone();
                                updateObjectAppearanceSprite(CurrentObjectAppearance, MainSprStorage);
                                CurrentObjectAppearance.Id = _editor.ReplaceObjectAppearance.Id;
                                MainWindow.appearances.Effect[(int)_editor.ReplaceObjectAppearance.Id - 1] = CurrentObjectAppearance.Clone();
                            }
                            else if (_editor.ReplaceObjectAppearance.AppearanceType == APPEARANCE_TYPE.AppearanceMissile)
                            {
                                CurrentObjectAppearance = ImportAppearances.Missile[ObjListView.SelectedIndex].Clone();
                                updateObjectAppearanceSprite(CurrentObjectAppearance, MainSprStorage);
                                CurrentObjectAppearance.Id = _editor.ReplaceObjectAppearance.Id;
                                MainWindow.appearances.Missile[(int)_editor.ReplaceObjectAppearance.Id - 1] = CurrentObjectAppearance.Clone();
                            }
                            ShowList item = (ShowList)_editor.ObjListView.SelectedItem;
                            _editor.AnimateSelectedListItem(item);
                        }else
                            _editor.StatusBar.MessageQueue?.Enqueue($"You can only replace objects of the same type.", null, null, null, false, true, TimeSpan.FromSeconds(2));
                    }
                }
            }
        }
    }
}
