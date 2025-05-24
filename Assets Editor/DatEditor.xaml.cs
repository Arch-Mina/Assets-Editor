using Efundies;
using Google.Protobuf;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Tibia.Protobuf.Appearances;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;

namespace Assets_Editor
{
    /// <summary>
    /// Interaction logic for DatEditor.xaml
    /// </summary>
    public partial class DatEditor : Window
    {
        private static ObservableCollection<ShowList> ThingsOutfit = new ObservableCollection<ShowList>();
        private static ObservableCollection<ShowList> ThingsItem = new ObservableCollection<ShowList>();
        private static ObservableCollection<ShowList> ThingsEffect = new ObservableCollection<ShowList>();
        private static ObservableCollection<ShowList> ThingsMissile = new ObservableCollection<ShowList>();
        public Appearance CurrentObjectAppearance;
        public AppearanceFlags CurrentFlags = null;
        List<AppearanceFlagNPC> NpcDataList = new List<AppearanceFlagNPC>();
        public ObservableCollection<Box> BoundingBoxList = new ObservableCollection<Box>();
        private int CurrentSprDir = 0;
        private bool isPageLoaded = false;
        private uint blankSpr = 0;
        private bool isObjectLoaded = false;
        private Appearances exportObjects = new Appearances();
        private uint exportSprCounter = 0;
        private int importSprCounter = 0;
        private List<ShowList> SelectedSprites = [];
        private Dictionary<uint, uint> importSprIdList = new Dictionary<uint, uint>();
        private List<CatalogTransparency> transparentSheets = new List<CatalogTransparency>();
        private class ImportSpriteInfo
        {
            public MemoryStream Stream { get; set; }
            public int OriginalIndex { get; set; }
            public System.Drawing.Size Size { get; set; }
        }

        public class CatalogTransparency
        {
            public MainWindow.Catalog Catalog { get; set; }
            public List<uint> SpriteIds { get; set; }
            public byte AlphaValue { get; set; }
            public APPEARANCE_TYPE ObjectType { get; set; }
            public uint ObjectId { get; set; }
            public byte BaseAlpha { get; set; }

            public CatalogTransparency(MainWindow.Catalog catalog, List<uint> spriteIds, byte alphaValue, APPEARANCE_TYPE objectType, uint objectId)
            {
                Catalog = catalog;
                SpriteIds = spriteIds;
                AlphaValue = alphaValue;
                ObjectType = objectType;
                ObjectId = objectId;
            }

            public CatalogTransparency()
            {
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Application.Current.Shutdown();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            isPageLoaded = true;
        }
        public DatEditor()
        {
            InitializeComponent();
            A_FlagAutomapColorPicker.AvailableColors.Clear();
            for (int x = 0; x <= 215; x++)
            {
                Color myRgbColor = Utils.Get8Bit(x);
                A_FlagAutomapColorPicker.AvailableColors.Add(new Xceed.Wpf.Toolkit.ColorItem(System.Windows.Media.Color.FromRgb(myRgbColor.R, myRgbColor.G, myRgbColor.B), x.ToString()));
            }
        }
        private void DarkModeToggle_Checked(object sender, RoutedEventArgs e)
        {
            PaletteHelper palette = new PaletteHelper();

            ITheme theme = palette.GetTheme();
            if ((bool)DarkModeToggle.IsChecked)
            {
                theme.SetBaseTheme(Theme.Dark);
            }
            else
            {
                theme.SetBaseTheme(Theme.Light);
            }
            palette.SetTheme(theme);
        }
        public DatEditor(Appearances appearances)
            :this()
        {
            foreach (var outfit in appearances.Outfit)
            {
                ThingsOutfit.Add(new ShowList() { Id = outfit.Id});
            }
            foreach (var item in appearances.Object)
            {
                ThingsItem.Add(new ShowList() { Id = item.Id});
            }
            foreach (var effect in appearances.Effect)
            {
                ThingsEffect.Add(new ShowList() { Id = effect.Id});
            }
            foreach (var missile in appearances.Missile)
            {
                ThingsMissile.Add(new ShowList() { Id = missile.Id});
            }
            SprListView.ItemsSource = MainWindow.AllSprList;
            SprListView.AddHandler(MouseLeftButtonDownEvent, new MouseButtonEventHandler(SprListView_MouseLeftButtonDown), true);
            UpdateShowList(ObjectMenu.SelectedIndex);
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
        private void SprListView_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            VirtualizingStackPanel panel = Utils.FindVisualChild<VirtualizingStackPanel>(SprListView);
            if (SprListView.Items.Count > 0 && panel != null)
            {
                int offset = (int)panel.VerticalOffset;
                //int maxOffset = (int)panel.ViewportHeight;
                for (int i = 0; i < SprListView.Items.Count; i++)
                {
                    if (i >= offset && i < Math.Min(offset + 20, SprListView.Items.Count) && MainWindow.SprLists.ContainsKey(i))
                        try
                        {
                            MainWindow.AllSprList[i].Image = Utils.BitmapToBitmapImage(MainWindow.getSpriteStream(i));
                        }
                        catch (Exception ex)
                        {
                            MainWindow.AllSprList[i].Image = null;
                        }
                    else
                        MainWindow.AllSprList[i].Image = null;
                }
            }
        }
        private void SprListView_DragSpr(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var item = FindAncestorOrSelf<ListViewItem>((DependencyObject)e.OriginalSource);
                if (item == null) return;
                
                DataObject data = new DataObject(SelectedSprites);
                DragDrop.DoDragDrop(SprListView, data, DragDropEffects.Copy);
            }
        }
        
        private static T FindAncestorOrSelf<T>(DependencyObject obj) where T : DependencyObject
        {
            while (obj != null)
            {
                if (obj is T tObj)
                    return tObj;
                obj = VisualTreeHelper.GetParent(obj);
            }
            return null;
        }
        
        private void SprListView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SelectedSprites = SprListView.SelectedItems.Cast<ShowList>().ToList();
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
                if(nIndex - maxOffset == offset)
                    scrollViewer.ScrollToVerticalOffset(offset+1);
                else if(nIndex + 1 == offset)
                    scrollViewer.ScrollToVerticalOffset(offset - 1);
                else if (nIndex >= offset + maxOffset || nIndex < offset)
                    scrollViewer.ScrollToVerticalOffset(SprListView.SelectedIndex);
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
                    ShowList item = (ShowList)ObjListView.Items[i];
                    if (i >= offset && i < Math.Min(offset + 20, ObjListView.Items.Count))
                    {
                        try
                        {
                            if (ObjectMenu.SelectedIndex == 0)
                                ThingsOutfit[i].Image = Utils.BitmapToBitmapImage(MainWindow.getSpriteStream((int)MainWindow.appearances.Outfit[i].FrameGroup[0].SpriteInfo.SpriteId[0]));
                            else if (ObjectMenu.SelectedIndex == 1)
                                ThingsItem[i].Image = Utils.BitmapToBitmapImage(MainWindow.getSpriteStream((int)MainWindow.appearances.Object[i].FrameGroup[0].SpriteInfo.SpriteId[0]));
                            else if (ObjectMenu.SelectedIndex == 2)
                                ThingsEffect[i].Image = Utils.BitmapToBitmapImage(MainWindow.getSpriteStream((int)MainWindow.appearances.Effect[i].FrameGroup[0].SpriteInfo.SpriteId[0]));
                            else if (ObjectMenu.SelectedIndex == 3)
                                ThingsMissile[i].Image = Utils.BitmapToBitmapImage(MainWindow.getSpriteStream((int)MainWindow.appearances.Missile[i].FrameGroup[0].SpriteInfo.SpriteId[0]));
                        }
                        catch (Exception)
                        {
                            MainWindow.Log("Invalid appearance properties for id " + i + ", crash prevented.", "Critical");
                        }
                        AnimateSelectedListItem(item);
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

                        if (item.Storyboard != null)
                        {
                            item.Storyboard.Stop();
                        }

                    }
                }
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
        private void ObjListView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ShowList showList = (ShowList)ObjListView.SelectedItem;
            if (showList != null)
            {
                ObjListViewSelectedIndex.Value = (int)showList.Id;
                if (ObjectMenu.SelectedIndex == 0)
                    LoadSelectedObjectAppearances(MainWindow.appearances.Outfit[ObjListView.SelectedIndex]);
                else if (ObjectMenu.SelectedIndex == 1)
                    LoadSelectedObjectAppearances(MainWindow.appearances.Object[ObjListView.SelectedIndex]);
                else if (ObjectMenu.SelectedIndex == 2)
                    LoadSelectedObjectAppearances(MainWindow.appearances.Effect[ObjListView.SelectedIndex]);
                else if (ObjectMenu.SelectedIndex == 3)
                    LoadSelectedObjectAppearances(MainWindow.appearances.Missile[ObjListView.SelectedIndex]);

                if (ObjectMenu.SelectedIndex == 0)
                {
                    SprUpArrow.Visibility = Visibility.Visible;
                    SprDownArrow.Visibility = Visibility.Visible;
                    SprLeftArrow.Visibility = Visibility.Visible;
                    SprRightArrow.Visibility = Visibility.Visible;
                    SprAddonSlider.IsEnabled = true;
                }
                else
                {
                    SprUpArrow.Visibility = Visibility.Hidden;
                    SprDownArrow.Visibility = Visibility.Hidden;
                    SprLeftArrow.Visibility = Visibility.Hidden;
                    SprRightArrow.Visibility = Visibility.Hidden;
                    SprAddonSlider.IsEnabled = false;
                }

            }
        }

        private void LoadSelectedObjectAppearances(Appearance ObjectAppearance)
        {
            CurrentObjectAppearance = ObjectAppearance.Clone();
            LoadCurrentObjectAppearances();
            SprGroupSlider.ValueChanged -= SprGroupSlider_ValueChanged;
            SprGroupSlider.Value = 0;
            ChangeGroupType(0);
            SprGroupSlider.ValueChanged += SprGroupSlider_ValueChanged;

        }

        private void ChangeGroupType(int group)
        {
            isObjectLoaded = false;
            A_SprGroups.Value = CurrentObjectAppearance.FrameGroup.Count;
            try
            {
                A_SprLayers.Value = (int)CurrentObjectAppearance.FrameGroup[group].SpriteInfo.Layers;
                A_SprPaternX.Value = (int)CurrentObjectAppearance.FrameGroup[group].SpriteInfo.PatternWidth;
                A_SprPaternY.Value = (int)CurrentObjectAppearance.FrameGroup[group].SpriteInfo.PatternHeight;
                A_SprPaternZ.Value = (int)CurrentObjectAppearance.FrameGroup[group].SpriteInfo.PatternDepth;
                A_SprAnimation.Value = CurrentObjectAppearance.FrameGroup[group].SpriteInfo.Animation != null ? (int)CurrentObjectAppearance.FrameGroup[group].SpriteInfo.Animation.SpritePhase.Count : 1;
            }
            catch (Exception)
            {
                MainWindow.Log("Invalid appearance properties for id " + CurrentObjectAppearance.Id + ", crash prevented.");
            }
            CurrentSprDir = 2;
            ButtonProgressAssist.SetIsIndicatorVisible(SprUpArrow, false);
            ButtonProgressAssist.SetIsIndicatorVisible(SprRightArrow, false);
            ButtonProgressAssist.SetIsIndicatorVisible(SprDownArrow, true);
            ButtonProgressAssist.SetIsIndicatorVisible(SprLeftArrow, false);
            ForceSliderChange();
            SprFramesSlider.Maximum = (double)A_SprAnimation.Value - 1;

            try
            {
                AnimationTab.IsEnabled = CurrentObjectAppearance.FrameGroup[group].SpriteInfo.Animation != null;
                SpriteFrameAnimationTab.IsEnabled = CurrentObjectAppearance.FrameGroup[group].SpriteInfo.Animation != null;

                if (A_SprAnimation.Value > 1)
                {
                    SprDefaultPhase.Value = CurrentObjectAppearance.FrameGroup[group].SpriteInfo.Animation.HasDefaultStartPhase ? (int)CurrentObjectAppearance.FrameGroup[group].SpriteInfo.Animation.DefaultStartPhase : 0;
                    SprRandomPhase.IsChecked = CurrentObjectAppearance.FrameGroup[group].SpriteInfo.Animation.HasRandomStartPhase ? CurrentObjectAppearance.FrameGroup[group].SpriteInfo.Animation.RandomStartPhase : false;
                    SprSynchronized.IsChecked = CurrentObjectAppearance.FrameGroup[group].SpriteInfo.Animation.HasSynchronized ? CurrentObjectAppearance.FrameGroup[group].SpriteInfo.Animation.Synchronized : false;
                    if (CurrentObjectAppearance.FrameGroup[group].SpriteInfo.Animation.HasLoopType)
                    {
                        if (CurrentObjectAppearance.FrameGroup[group].SpriteInfo.Animation.LoopType == ANIMATION_LOOP_TYPE.Pingpong)
                            SprLoopType.SelectedIndex = 0;
                        else if (CurrentObjectAppearance.FrameGroup[group].SpriteInfo.Animation.LoopType == ANIMATION_LOOP_TYPE.Infinite)
                            SprLoopType.SelectedIndex = 1;
                        else if (CurrentObjectAppearance.FrameGroup[group].SpriteInfo.Animation.LoopType == ANIMATION_LOOP_TYPE.Counted)
                            SprLoopType.SelectedIndex = 2;
                        else
                            SprLoopType.SelectedIndex = -1;
                    }
                    SprLoopCount.Value = CurrentObjectAppearance.FrameGroup[group].SpriteInfo.Animation.HasLoopCount ? (int)CurrentObjectAppearance.FrameGroup[group].SpriteInfo.Animation.LoopCount : 0;

                }


                A_SprOpaque.IsChecked = CurrentObjectAppearance.FrameGroup[group].SpriteInfo.IsOpaque;
                A_SprBounding.Value = CurrentObjectAppearance.FrameGroup[group].SpriteInfo.HasBoundingSquare ? (int)CurrentObjectAppearance.FrameGroup[group].SpriteInfo.BoundingSquare : 0;

                if (CurrentObjectAppearance.FrameGroup[group].SpriteInfo.BoundingBoxPerDirection != null)
                {
                    BoundingBoxList.Clear();
                    foreach (var box in CurrentObjectAppearance.FrameGroup[group].SpriteInfo.BoundingBoxPerDirection)
                        BoundingBoxList.Add(new Box() { X = box.X, Y = box.Y, Width = box.Width, Height = box.Height });
                    BoxPerDirection.ItemsSource = null;
                    BoxPerDirection.ItemsSource = BoundingBoxList;

                }
            }
            catch (Exception)
            {
                MainWindow.Log("Invalid appearance properties for id " + CurrentObjectAppearance.Id + ", crash prevented.");
            }
            SprGroupType.Content = SprGroupSlider.Value == 0 ? "Idle" : "Walking";
            isObjectLoaded = true;
        }
        private void ForceSliderChange()
        {
            SprFramesSlider.ValueChanged -= SprFramesSlider_ValueChanged;
            SprFramesSlider.Minimum = -1;
            SprFramesSlider.Value = -1;
            SprFramesSlider.ValueChanged += SprFramesSlider_ValueChanged;
            SprFramesSlider.Minimum = 0;
        }

        private void LoadCurrentObjectAppearances()
        {
            if (CurrentObjectAppearance.Flags == null)
            {
                CurrentObjectAppearance.Flags = new();
                MainWindow.Log("Missing flags for appearance id " + CurrentObjectAppearance.Id);
            }
            A_FlagId.Value = (int)CurrentObjectAppearance.Id;
            A_FlagGround.IsChecked = CurrentObjectAppearance.Flags.Bank != null;
            A_FlagGroundSpeed.Value = (CurrentObjectAppearance.Flags.Bank != null && CurrentObjectAppearance.Flags.Bank.HasWaypoints) ? (int)CurrentObjectAppearance.Flags.Bank.Waypoints : 0;
            A_FlagClip.IsChecked = CurrentObjectAppearance.Flags.Clip;
            A_FlagBottom.IsChecked = CurrentObjectAppearance.Flags.Bottom;
            A_FlagTop.IsChecked = CurrentObjectAppearance.Flags.Top;
            A_FlagContainer.IsChecked = CurrentObjectAppearance.Flags.Container;
            A_FlagCumulative.IsChecked = CurrentObjectAppearance.Flags.Cumulative;
            A_FlagUsable.IsChecked = CurrentObjectAppearance.Flags.Usable;
            A_FlagForceuse.IsChecked = CurrentObjectAppearance.Flags.Forceuse;
            A_FlagMultiuse.IsChecked = CurrentObjectAppearance.Flags.Multiuse;
            A_FlagWrite.IsChecked = CurrentObjectAppearance.Flags.Write != null;
            A_FlagMaxTextLength.Value = (CurrentObjectAppearance.Flags.Write != null && CurrentObjectAppearance.Flags.Write.HasMaxTextLength) ? (int)CurrentObjectAppearance.Flags.Write.MaxTextLength : 0;
            A_FlagWriteOnce.IsChecked = CurrentObjectAppearance.Flags.WriteOnce != null;
            A_FlagMaxTextLengthOnce.Value = (CurrentObjectAppearance.Flags.WriteOnce != null && CurrentObjectAppearance.Flags.WriteOnce.HasMaxTextLengthOnce) ? (int)CurrentObjectAppearance.Flags.WriteOnce.MaxTextLengthOnce : 0;
            A_FlagLiquidpool.IsChecked = CurrentObjectAppearance.Flags.HasLiquidpool;
            A_FlagUnpass.IsChecked = CurrentObjectAppearance.Flags.HasUnpass;
            A_FlagUnmove.IsChecked = CurrentObjectAppearance.Flags.HasUnmove;
            A_FlagUnsight.IsChecked = CurrentObjectAppearance.Flags.HasUnsight;
            A_FlagAvoid.IsChecked = CurrentObjectAppearance.Flags.HasAvoid;
            A_FlagNoMoveAnimation.IsChecked = CurrentObjectAppearance.Flags.HasNoMovementAnimation;
            A_FlagTake.IsChecked = CurrentObjectAppearance.Flags.HasTake;
            A_FlagLiquidcontainer.IsChecked = CurrentObjectAppearance.Flags.HasLiquidcontainer;
            A_FlagHang.IsChecked = CurrentObjectAppearance.Flags.HasHang;
            A_FlagHook.IsChecked = CurrentObjectAppearance.Flags.Hook != null;
            A_FlagHookType.SelectedIndex = (CurrentObjectAppearance.Flags.Hook != null && CurrentObjectAppearance.Flags.Hook.HasDirection) ? (int)CurrentObjectAppearance.Flags.Hook.Direction - 1 : -1;
            A_FlagRotate.IsChecked = CurrentObjectAppearance.Flags.HasRotate;
            A_FlagLight.IsChecked = CurrentObjectAppearance.Flags.Light != null;
            A_FlagLightBrightness.Value = (CurrentObjectAppearance.Flags.Light != null && CurrentObjectAppearance.Flags.Light.HasBrightness) ? (int)CurrentObjectAppearance.Flags.Light.Brightness : 0;
            A_FlagLightColor.Value = (CurrentObjectAppearance.Flags.Light != null && CurrentObjectAppearance.Flags.Light.HasColor) ? (int)CurrentObjectAppearance.Flags.Light.Color : 0;
            A_FlagDontHide.IsChecked = CurrentObjectAppearance.Flags.HasDontHide;
            A_FlagTranslucent.IsChecked = CurrentObjectAppearance.Flags.HasTranslucent;
            A_FlagShift.IsChecked = CurrentObjectAppearance.Flags.Shift != null;
            A_FlagShiftX.Value = (CurrentObjectAppearance.Flags.Shift != null && CurrentObjectAppearance.Flags.Shift.HasX) ? (int)CurrentObjectAppearance.Flags.Shift.X : 0;
            A_FlagShiftY.Value = (CurrentObjectAppearance.Flags.Shift != null && CurrentObjectAppearance.Flags.Shift.HasY) ? (int)CurrentObjectAppearance.Flags.Shift.Y : 0;
            A_FlagHeight.IsChecked = CurrentObjectAppearance.Flags.Height != null;
            A_FlagElevation.Value = (CurrentObjectAppearance.Flags.Height != null && CurrentObjectAppearance.Flags.Height.HasElevation) ? (int)CurrentObjectAppearance.Flags.Height.Elevation : 0;
            A_FlagLyingObject.IsChecked = CurrentObjectAppearance.Flags.HasLyingObject;
            A_FlagAnimateAlways.IsChecked = CurrentObjectAppearance.Flags.HasAnimateAlways;
            A_FlagAutomap.IsChecked = CurrentObjectAppearance.Flags.Automap != null;
            A_FlagAutomapColor.Value = (CurrentObjectAppearance.Flags.Automap != null && CurrentObjectAppearance.Flags.Automap.HasColor) ? (int)CurrentObjectAppearance.Flags.Automap.Color : 0;
            A_FlagLenshelp.IsChecked = CurrentObjectAppearance.Flags.Lenshelp != null;
            A_FlagLenshelpId.SelectedIndex = (CurrentObjectAppearance.Flags.Lenshelp != null && CurrentObjectAppearance.Flags.Lenshelp.HasId) ? (int)CurrentObjectAppearance.Flags.Lenshelp.Id - 1100 : -1;
            A_FlagFullGround.IsChecked = CurrentObjectAppearance.Flags.HasFullbank;
            A_FlagIgnoreLook.IsChecked = CurrentObjectAppearance.Flags.HasIgnoreLook;
            A_FlagClothes.IsChecked = (CurrentObjectAppearance.Flags.Clothes != null && CurrentObjectAppearance.Flags.Clothes.HasSlot) ? true : false;
            A_FlagClothesSlot.SelectedIndex = (CurrentObjectAppearance.Flags.Clothes != null && CurrentObjectAppearance.Flags.Clothes.HasSlot) ? (int)CurrentObjectAppearance.Flags.Clothes.Slot : -1;
            A_FlagDefaultAction.IsChecked = CurrentObjectAppearance.Flags.DefaultAction != null;
            A_FlagDefaultActionType.SelectedIndex = (CurrentObjectAppearance.Flags.DefaultAction != null && CurrentObjectAppearance.Flags.DefaultAction.HasAction) ? (int)CurrentObjectAppearance.Flags.DefaultAction.Action : -1;
            A_FlagMarket.IsChecked = CurrentObjectAppearance.Flags.Market != null;
            A_FlagMarketCategory.SelectedIndex = (CurrentObjectAppearance.Flags.Market != null && CurrentObjectAppearance.Flags.Market.HasCategory) ? (int)CurrentObjectAppearance.Flags.Market.Category - 1 : -1;
            A_FlagMarketTrade.Value = (CurrentObjectAppearance.Flags.Market != null && CurrentObjectAppearance.Flags.Market.HasTradeAsObjectId) ? (int)CurrentObjectAppearance.Flags.Market.TradeAsObjectId : 0;
            A_FlagMarketShow.Value = (CurrentObjectAppearance.Flags.Market != null && CurrentObjectAppearance.Flags.Market.HasShowAsObjectId) ? (int)CurrentObjectAppearance.Flags.Market.ShowAsObjectId : 0;
            A_FlagProfessionAny.IsChecked = false;
            A_FlagProfessionNone.IsChecked = false;
            A_FlagProfessionKnight.IsChecked = false;
            A_FlagProfessionPaladin.IsChecked = false;
            A_FlagProfessionSorcerer.IsChecked = false;
            A_FlagProfessionDruid.IsChecked = false;
            A_FlagProfessionPromoted.IsChecked = false;
            if (CurrentObjectAppearance.Flags.Market != null && CurrentObjectAppearance.Flags.Market.RestrictToVocation.Count > 0)
            {
                foreach (var profession in CurrentObjectAppearance.Flags.Market.RestrictToVocation)
                {
                    if (profession == VOCATION.Any)
                        A_FlagProfessionAny.IsChecked = true;
                    else if (profession == VOCATION.None)
                        A_FlagProfessionNone.IsChecked = true;
                    else if (profession == VOCATION.Knight)
                        A_FlagProfessionKnight.IsChecked = true;
                    else if (profession == VOCATION.Paladin)
                        A_FlagProfessionPaladin.IsChecked = true;
                    else if (profession == VOCATION.Sorcerer)
                        A_FlagProfessionSorcerer.IsChecked = true;
                    else if (profession == VOCATION.Druid)
                        A_FlagProfessionDruid.IsChecked = true;
                    else if (profession == VOCATION.Promoted)
                        A_FlagProfessionPromoted.IsChecked = true;
                }
            }
            A_FlagMarketlevel.Value = (CurrentObjectAppearance.Flags.Market != null && CurrentObjectAppearance.Flags.Market.HasMinimumLevel) ? (int)CurrentObjectAppearance.Flags.Market.MinimumLevel : 0;
            A_FlagName.Text = CurrentObjectAppearance.HasName ? CurrentObjectAppearance.Name : null;
            A_FlagDescription.Text = CurrentObjectAppearance.HasDescription ? CurrentObjectAppearance.Description : null;
            A_FlagWrap.IsChecked = CurrentObjectAppearance.Flags.HasWrap;
            A_FlagUnwrap.IsChecked = CurrentObjectAppearance.Flags.HasUnwrap;
            A_FlagDecoItemKit.IsChecked = CurrentObjectAppearance.Flags.HasDecoItemKit;
            A_FlagTopeffect.IsChecked = CurrentObjectAppearance.Flags.HasTop;
            A_FlagChangedToExpire.IsChecked = CurrentObjectAppearance.Flags.Changedtoexpire != null;
            A_FlagChangedToExpireId.Value = (CurrentObjectAppearance.Flags.Changedtoexpire != null && CurrentObjectAppearance.Flags.Changedtoexpire.HasFormerObjectTypeid) ? (int)CurrentObjectAppearance.Flags.Changedtoexpire.FormerObjectTypeid : 0;
            A_FlagCorpse.IsChecked = CurrentObjectAppearance.Flags.Corpse;
            A_FlagCyclopedia.IsChecked = CurrentObjectAppearance.Flags.Cyclopediaitem != null;
            A_FlagCyclopediaItem.Value = (CurrentObjectAppearance.Flags.Cyclopediaitem != null && CurrentObjectAppearance.Flags.Cyclopediaitem.HasCyclopediaType) ? (int)CurrentObjectAppearance.Flags.Cyclopediaitem.CyclopediaType : 0;
            A_FlagAmmo.IsChecked = CurrentObjectAppearance.Flags.HasAmmo;
            A_FlagShowOffSocket.IsChecked = CurrentObjectAppearance.Flags.ShowOffSocket;
            A_FlagReportable.IsChecked = CurrentObjectAppearance.Flags.Reportable;
            A_FlagReverseAddonEast.IsChecked = CurrentObjectAppearance.Flags.ReverseAddonsEast;
            A_FlagReverseAddonWest.IsChecked = CurrentObjectAppearance.Flags.ReverseAddonsWest;
            A_FlagReverseAddonNorth.IsChecked = CurrentObjectAppearance.Flags.ReverseAddonsNorth;
            A_FlagReverseAddonSouth.IsChecked = CurrentObjectAppearance.Flags.ReverseAddonsSouth;
            A_FlagWearOut.IsChecked = CurrentObjectAppearance.Flags.Wearout;
            A_FlagClockExpire.IsChecked = CurrentObjectAppearance.Flags.Clockexpire;
            A_FlagExpire.IsChecked = CurrentObjectAppearance.Flags.Expire;
            A_FlagExpireStop.IsChecked = CurrentObjectAppearance.Flags.Expirestop;
            A_FlagUpgradeClassification.IsChecked = CurrentObjectAppearance.Flags.Upgradeclassification != null;
            A_FlagUpgradeClassificationAmount.Value = (CurrentObjectAppearance.Flags.Upgradeclassification != null && CurrentObjectAppearance.Flags.Upgradeclassification.HasUpgradeClassification) ? (int)CurrentObjectAppearance.Flags.Upgradeclassification.UpgradeClassification : 0;

            NpcDataList.Clear();

            if (CurrentObjectAppearance.Flags.Npcsaledata.Count > 0)
            {
                A_FlagNPC.IsChecked = true;
                foreach (var npcdata in CurrentObjectAppearance.Flags.Npcsaledata)
                    NpcDataList.Add(npcdata);
            }
            else
                A_FlagNPC.IsChecked = false;

            A_FlagNPCData.ItemsSource = null;
            A_FlagNPCData.ItemsSource = NpcDataList;
            A_FullInfo.Text = CurrentObjectAppearance.ToString();
            A_FlagTransparency.IsChecked = CurrentObjectAppearance.Flags.Transparencylevel != null;
            A_FlagTransparencyLevel.Value = (CurrentObjectAppearance.Flags.Transparencylevel != null && CurrentObjectAppearance.Flags.Transparencylevel.HasLevel) ? (int)CurrentObjectAppearance.Flags.Transparencylevel.Level : 0;
        }
        private void A_FlagLightColorPickerChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            foreach (var color in A_FlagLightColorPicker.AvailableColors)
            {
                if (color.Color.Value.ToString() == A_FlagLightColorPicker.SelectedColor.ToString())
                    A_FlagLightColor.Value = int.Parse(color.Name);
            }

        }
        private void A_FlagAutomapColorPickerChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            foreach (var color in A_FlagAutomapColorPicker.AvailableColors)
            {
                if (color.Color.Value.ToString() == A_FlagAutomapColorPicker.SelectedColor.ToString())
                    A_FlagAutomapColor.Value = int.Parse(color.Name);
            }
        }
        private void A_FlagAutomapColor_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            A_FlagAutomapColorPicker.SelectedColor = A_FlagAutomapColorPicker.AvailableColors[(int)A_FlagAutomapColor.Value].Color;
        }
        private void A_FlagLightColor_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            A_FlagLightColorPicker.SelectedColor = A_FlagLightColorPicker.AvailableColors[(int)A_FlagLightColor.Value].Color;
        }
        private void A_FlagMarketProfession_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            A_FlagMarketProfession.SelectedIndex = -1;
        }
        private void SprFramesSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {

                FrameGroup frameGroup = CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value];
                if (frameGroup.SpriteInfo.Animation != null)
                {
                    SprPhaseMin.Value = (int)frameGroup.SpriteInfo.Animation.SpritePhase[(int)SprFramesSlider.Value].DurationMin;
                    SprPhaseMax.Value = (int)frameGroup.SpriteInfo.Animation.SpritePhase[(int)SprFramesSlider.Value].DurationMax;
                }

                SpriteViewerGrid.Children.Clear();
                SpriteViewerGrid.RowDefinitions.Clear();
                SpriteViewerGrid.ColumnDefinitions.Clear();
                int gridWidth = 1;
                int gridHeight = 1;
                if (ObjectMenu.SelectedIndex != 0)
                {
                    gridWidth = (int)frameGroup.SpriteInfo.PatternHeight;
                    gridHeight = (int)frameGroup.SpriteInfo.PatternWidth;
                }
                int imgWidth = (int)Utils.BitmapToBitmapImage(MainWindow.getSpriteStream((int)frameGroup.SpriteInfo.SpriteId[0])).Width;
                int imgHeight = (int)Utils.BitmapToBitmapImage(MainWindow.getSpriteStream((int)frameGroup.SpriteInfo.SpriteId[0])).Height;

                for (int i = 0; i < gridWidth; i++)
                {
                    RowDefinition rowDef = new RowDefinition();
                    rowDef.Height = new GridLength(imgHeight);
                    SpriteViewerGrid.RowDefinitions.Add(rowDef);
                }
                for (int i = 0; i < gridHeight; i++)
                {
                    ColumnDefinition colDef = new ColumnDefinition();
                    colDef.Width = new GridLength(imgWidth);
                    SpriteViewerGrid.ColumnDefinitions.Add(colDef);
                }

                if (IsOutfitsMenuOpened())
                {
                    int layer = SprBlendLayer.IsChecked == true ? (int)frameGroup.SpriteInfo.Layers - 1 : 0;
                    int mount = SprMount.IsChecked == true ? (int)frameGroup.SpriteInfo.PatternDepth - 1 : 0;
                    int addon = frameGroup.SpriteInfo.PatternWidth > 1 ? (int)SprAddonSlider.Value : 0;
                    int index = GetSpriteIndex(frameGroup, layer, (int)Math.Min(CurrentSprDir, frameGroup.SpriteInfo.PatternWidth - 1), addon, mount, (int)SprFramesSlider.Value);
                    int spriteId = (int)frameGroup.SpriteInfo.SpriteId[index];
                    SetImageInGrid(SpriteViewerGrid, gridWidth, gridHeight, Utils.BitmapToBitmapImage(MainWindow.getSpriteStream(spriteId)), 1, spriteId, index);
                }
                else
                {
                    int counter = 1;
                    int mount = SprMount.IsChecked == true ? (int)frameGroup.SpriteInfo.PatternDepth - 1 : 0;
                    for (int ph = 0; ph < frameGroup.SpriteInfo.PatternHeight; ph++)
                    {
                        for (int pw = 0; pw < frameGroup.SpriteInfo.PatternWidth; pw++)
                        {
                            int index = GetSpriteIndex(frameGroup, 0, pw, ph, mount, (int)SprFramesSlider.Value);
                            int spriteId = (int)frameGroup.SpriteInfo.SpriteId[index];
                            SetImageInGrid(SpriteViewerGrid, gridWidth, gridHeight, Utils.BitmapToBitmapImage(MainWindow.getSpriteStream(spriteId)), counter, spriteId, index);
                            counter++;
                        }
                    }
                }
            }
            catch (Exception)
            {
                MainWindow.Log("Unable to view appearance id " + CurrentObjectAppearance.Id + ", crash prevented.");
            }
        }

        private bool IsOutfitsMenuOpened()
        {
            return ObjectMenu.SelectedIndex == 0;
        }

        private void SetImageInGrid(Grid grid, int gridWidth, int gridHeight, BitmapImage image, int id, int spriteId, int index)
        {
            // Get the row and column of the cell based on the ID number
            int row = (id - 1) / gridHeight;
            int col = (id - 1) % gridHeight;

            // Get the existing Image control in the cell, or create a new one if it doesn't exist
            Image existingImage = null;
            foreach (UIElement element in grid.Children)
            {
                if (Grid.GetRow(element) == row && Grid.GetColumn(element) == col && element is Image)
                {
                    existingImage = element as Image;
                    break;
                }
            }
            if (existingImage == null)
            {
                existingImage = new Image();
                existingImage.Width = image.Width;
                existingImage.Height = image.Height;
                AllowDrop = true;
                Grid.SetRow(existingImage, row);
                Grid.SetColumn(existingImage, col);
                grid.Children.Add(existingImage);
            }
            existingImage.MouseLeftButtonDown += Img_PreviewMouseLeftButtonDown;
            existingImage.Drop += Spr_Drop;
            existingImage.ToolTip = spriteId.ToString();
            existingImage.Tag = index;
            // Set the Source property of the Image control to the specified Image
            existingImage.Source = image;
            RenderOptions.SetBitmapScalingMode(existingImage, BitmapScalingMode.NearestNeighbor);
        }
        private void Spr_Drop(object sender, DragEventArgs e)
        {
            List<ShowList> data = (List<ShowList>)e.Data.GetData(typeof(List<ShowList>));
            
            if (data == null) return;
            
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                var frameIndex = (int)SprFramesSlider.Value;
                var maxFrames = (int)SprFramesSlider.Maximum;
                
                for (var i = 0; i < data.Count; i++)
                {
                    var dataItem = data[i];
                    var index = i + frameIndex;
                    if (index > maxFrames) break;

                    var frameGroup = CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value];
                    var targetIndex = GetSpriteIndex(frameGroup, 0,
                        IsOutfitsMenuOpened() ? CurrentSprDir : 0, 0, 0, index);
                    if (i == 0)
                    {
                        var img = (Image)SpriteViewerGrid.Children[0];
                        img.Source = dataItem.Image;
                        img.ToolTip = dataItem.Id.ToString();
                    }
                    
                    frameGroup.SpriteInfo.SpriteId[targetIndex] = dataItem.Id;
                }
            }
            else
            {
                var targetSpriteIndex = (int)((Image)sender).Tag;
                var gridSize = SpriteViewerGrid.Children.Count;

                var maxSpriteWidth = 0;
                var maxSpriteHeight = 0;

                for (var i = 0; i < data.Count; i++)
                {
                    var dataItem = data[i];
                    var gridIndex = targetSpriteIndex % gridSize + i;
                    if (gridIndex >= gridSize) break;

                    var img = (Image)SpriteViewerGrid.Children[gridIndex];
                    img.Source = dataItem.Image;
                    img.ToolTip = dataItem.Id.ToString();
                    CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.SpriteId[(int)img.Tag] =
                        dataItem.Id;
                    if (dataItem.Image is not null)
                    {
                        maxSpriteWidth = Math.Max(maxSpriteWidth, (int)dataItem.Image.Width);
                        maxSpriteHeight = Math.Max(maxSpriteHeight, (int)dataItem.Image.Height);
                    }
                }


                foreach (var column in SpriteViewerGrid.ColumnDefinitions)
                {
                    column.Width = new GridLength(Math.Max(maxSpriteWidth, column.Width.Value));
                }

                foreach (var row in SpriteViewerGrid.RowDefinitions)
                {
                    row.Height = new GridLength(Math.Max(maxSpriteHeight, row.Height.Value));
                }
            }

            e.Handled = true;
        }

        public static int GetSpriteIndex(FrameGroup frameGroup, int layers, int patternX, int patternY, int patternZ, int frames)
        {
            var spriteInfo = frameGroup.SpriteInfo;
            int index = 0;

            if (spriteInfo.Animation != null)
                index = (int)(frames % spriteInfo.Animation.SpritePhase.Count);
            index = index * (int)spriteInfo.PatternDepth + patternZ;
            index = index * (int)spriteInfo.PatternHeight + patternY;
            index = index * (int)spriteInfo.PatternWidth + patternX;
            index = index * (int)spriteInfo.Layers + layers;
            return index;
        }

        private void SprGroupSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ChangeGroupType((int)SprGroupSlider.Value);
        }

        private void ChangeDirection(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Button _dir = (Button)sender;

            CurrentSprDir = int.Parse(_dir.Uid);
            ForceSliderChange();
            ButtonProgressAssist.SetIsIndicatorVisible(SprUpArrow, false);
            ButtonProgressAssist.SetIsIndicatorVisible(SprRightArrow, false);
            ButtonProgressAssist.SetIsIndicatorVisible(SprDownArrow, false);
            ButtonProgressAssist.SetIsIndicatorVisible(SprLeftArrow, false);
            ButtonProgressAssist.SetIsIndicatorVisible(_dir, true);
        }


        private void Img_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Image img = e.Source as Image;
            SprListView.SelectedIndex = int.Parse((string)img.ToolTip);
            ScrollViewer scrollViewer = Utils.FindVisualChild<ScrollViewer>(SprListView);
            scrollViewer.ScrollToVerticalOffset(SprListView.SelectedIndex);
        }

        private void SprMount_Click(object sender, RoutedEventArgs e)
        {
            ForceSliderChange();
        }

        private void SprBlendLayer_Click(object sender, RoutedEventArgs e)
        {
            ForceSliderChange();
        }

        private void SprAddonSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ForceSliderChange();
        }

        private void BoxPerDirection_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (BoundingBoxList.Count > 4)
                BoundingBoxList.RemoveAt(BoundingBoxList.Count - 1);

            if (BoundingBoxList.Count > 0)
            {
                CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.BoundingBoxPerDirection.Clear();
                foreach (var box in BoundingBoxList)
                    CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.BoundingBoxPerDirection.Add(box);
            }
        }
        private void FixSpritesCount()
        {
            SpriteInfo spriteInfo = CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo;
            int NumSprites = (int)(spriteInfo.PatternWidth * spriteInfo.PatternHeight * spriteInfo.PatternDepth * spriteInfo.Layers * A_SprAnimation.Value);

            if (spriteInfo.SpriteId.Count > NumSprites)
            {
                int excessCount = spriteInfo.SpriteId.Count - NumSprites;
                for (int i = 0; i < excessCount; i++)
                {
                    spriteInfo.SpriteId.RemoveAt(spriteInfo.SpriteId.Count - 1);
                }
            }
            else if (spriteInfo.SpriteId.Count < NumSprites)
            {
                int missingCount = NumSprites - spriteInfo.SpriteId.Count;
                for (int i = 0; i < missingCount; i++)
                {
                    spriteInfo.SpriteId.Add(blankSpr);
                }
            }
        }
        private void A_Texture_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!isPageLoaded || isObjectLoaded == false)
                return;

            FrameworkElement frameworkElement = sender as FrameworkElement;
            SpriteInfo spriteInfo = CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo;
            if (frameworkElement.Name == "A_SprGroups")
            {
                if (A_SprGroups.Value == 1 && CurrentObjectAppearance.FrameGroup.Count == 2)
                {
                    CurrentObjectAppearance.FrameGroup.RemoveAt(1);
                }
                else if (A_SprGroups.Value == 2 && CurrentObjectAppearance.FrameGroup.Count == 1)
                {
                    FrameGroup newFrameGroup = CurrentObjectAppearance.FrameGroup[0].Clone();
                    newFrameGroup.FixedFrameGroup = FIXED_FRAME_GROUP.OutfitMoving;
                    CurrentObjectAppearance.FrameGroup.Add(newFrameGroup);
                }
            }
            else if (frameworkElement.Name == "A_SprLayers")
            {
                spriteInfo.Layers = (uint)A_SprLayers.Value;
                FixSpritesCount();
                SprBlendLayer.IsEnabled = A_SprLayers.Value > 1 ? true : false;
            }
            else if (frameworkElement.Name == "A_SprPaternX")
            {
                spriteInfo.PatternWidth = (uint)A_SprPaternX.Value;
                FixSpritesCount();
            }
            else if (frameworkElement.Name == "A_SprPaternY")
            {
                spriteInfo.PatternHeight = (uint)A_SprPaternY.Value;
                FixSpritesCount();
            }
            else if (frameworkElement.Name == "A_SprPaternZ")
            {
                spriteInfo.PatternDepth = (uint)A_SprPaternZ.Value;
                FixSpritesCount();
                SprMount.IsEnabled = A_SprPaternZ.Value > 1 ? true : false;
            }
            else if (frameworkElement.Name == "A_SprAnimation")
            {
                if (A_SprAnimation.Value == 1)
                {
                    spriteInfo.Animation = null;
                }
                else
                {
                    SpriteAnimation spriteAnimation = new SpriteAnimation();
                    spriteAnimation.AnimationMode = ANIMATION_ANIMATION_MODE.AnimationAsynchronized;
                    spriteAnimation.DefaultStartPhase = 0;
                    spriteAnimation.LoopType = ANIMATION_LOOP_TYPE.Infinite;
                    for (int i = 0; i < A_SprAnimation.Value; i++)
                        spriteAnimation.SpritePhase.Add(new SpritePhase() { DurationMin = 100, DurationMax = 100 });
                    spriteInfo.Animation = spriteAnimation;
                }

                FixSpritesCount();
                SpriteAnimationGroup.IsEnabled = A_SprAnimation.Value > 1;
                SpriteFrameAnimationGroup.IsEnabled = A_SprAnimation.Value > 1;
                SprFramesSlider.Maximum = (double)A_SprAnimation.Value - 1;
            }
        }

        private void ObjectSave_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(A_FlagName.Text))
                CurrentObjectAppearance.Name = A_FlagName.Text;

            if (!string.IsNullOrWhiteSpace(A_FlagDescription.Text))
                CurrentObjectAppearance.Description = A_FlagDescription.Text;

            if ((bool)A_FlagGround.IsChecked) {
                CurrentObjectAppearance.Flags.Bank = new AppearanceFlagBank
                {
                    Waypoints = (uint)A_FlagGroundSpeed.Value
                };
            }
            else
                CurrentObjectAppearance.Flags.Bank = null;

            if ((bool)A_FlagClip.IsChecked)
                CurrentObjectAppearance.Flags.Clip = true;
            else if(CurrentObjectAppearance.Flags.HasClip)
                CurrentObjectAppearance.Flags.ClearClip();

            if ((bool)A_FlagBottom.IsChecked)
                CurrentObjectAppearance.Flags.Bottom = true;
            else if(CurrentObjectAppearance.Flags.HasBottom)
                CurrentObjectAppearance.Flags.ClearBottom();

            if ((bool)A_FlagTop.IsChecked)
                CurrentObjectAppearance.Flags.Top = true;
            else if(CurrentObjectAppearance.Flags.HasTop)
                CurrentObjectAppearance.Flags.ClearTop();

            if ((bool)A_FlagContainer.IsChecked)
                CurrentObjectAppearance.Flags.Container = true;
            else if(CurrentObjectAppearance.Flags.HasContainer)
                CurrentObjectAppearance.Flags.ClearContainer();

            if ((bool)A_FlagCumulative.IsChecked)
                CurrentObjectAppearance.Flags.Cumulative = true;
            else if (CurrentObjectAppearance.Flags.HasCumulative)
                CurrentObjectAppearance.Flags.ClearCumulative();

            if ((bool)A_FlagUsable.IsChecked)
                CurrentObjectAppearance.Flags.Usable = true;
            else if (CurrentObjectAppearance.Flags.HasUsable)
                CurrentObjectAppearance.Flags.ClearUsable();

            if ((bool)A_FlagForceuse.IsChecked)
                CurrentObjectAppearance.Flags.Forceuse = true;
            else if (CurrentObjectAppearance.Flags.HasForceuse)
                CurrentObjectAppearance.Flags.ClearForceuse();

            if ((bool)A_FlagMultiuse.IsChecked)
                CurrentObjectAppearance.Flags.Multiuse = true;
            else if (CurrentObjectAppearance.Flags.HasMultiuse)
                CurrentObjectAppearance.Flags.ClearMultiuse();

            if ((bool)A_FlagWrite.IsChecked)
            {
                CurrentObjectAppearance.Flags.Write = new AppearanceFlagWrite
                {
                    MaxTextLength = (uint)A_FlagMaxTextLength.Value
                };
            }
            else
                CurrentObjectAppearance.Flags.Write = null;

            if ((bool)A_FlagWriteOnce.IsChecked)
            {
                CurrentObjectAppearance.Flags.WriteOnce = new AppearanceFlagWriteOnce
                {
                    MaxTextLengthOnce = (uint)A_FlagMaxTextLengthOnce.Value
                };
            }
            else CurrentObjectAppearance.Flags.WriteOnce = null;

            if ((bool)A_FlagShowOffSocket.IsChecked)
                CurrentObjectAppearance.Flags.ShowOffSocket = true;
            else
                CurrentObjectAppearance.Flags.ClearShowOffSocket();

            if ((bool)A_FlagReportable.IsChecked)
                CurrentObjectAppearance.Flags.Reportable = true;
            else
                CurrentObjectAppearance.Flags.ClearReportable();

            if ((bool)A_FlagReverseAddonEast.IsChecked)
                CurrentObjectAppearance.Flags.ReverseAddonsEast = true;
            else
                CurrentObjectAppearance.Flags.ClearReverseAddonsEast();

            if ((bool)A_FlagReverseAddonWest.IsChecked)
                CurrentObjectAppearance.Flags.ReverseAddonsWest = true;
            else
                CurrentObjectAppearance.Flags.ClearReverseAddonsWest();

            if ((bool)A_FlagReverseAddonNorth.IsChecked)
                CurrentObjectAppearance.Flags.ReverseAddonsNorth = true;
            else
                CurrentObjectAppearance.Flags.ClearReverseAddonsNorth();

            if ((bool)A_FlagReverseAddonSouth.IsChecked)
                CurrentObjectAppearance.Flags.ReverseAddonsSouth = true;
            else
                CurrentObjectAppearance.Flags.ClearReverseAddonsSouth();

            if ((bool)A_FlagWearOut.IsChecked)
                CurrentObjectAppearance.Flags.Wearout = true;
            else
                CurrentObjectAppearance.Flags.ClearWearout();

            if ((bool)A_FlagClockExpire.IsChecked)
                CurrentObjectAppearance.Flags.Clockexpire = true;
            else
                CurrentObjectAppearance.Flags.ClearClockexpire();

            if ((bool)A_FlagExpire.IsChecked)
                CurrentObjectAppearance.Flags.Expire = true;
            else
                CurrentObjectAppearance.Flags.ClearExpire();

            if ((bool)A_FlagExpireStop.IsChecked)
                CurrentObjectAppearance.Flags.Expirestop = true;
            else
                CurrentObjectAppearance.Flags.ClearExpirestop();

            if ((bool)A_FlagUpgradeClassification.IsChecked)
            {
                CurrentObjectAppearance.Flags.Upgradeclassification = new AppearanceFlagUpgradeClassification
                {
                    UpgradeClassification = (uint)A_FlagUpgradeClassificationAmount.Value
                };
            }

            else CurrentObjectAppearance.Flags.Upgradeclassification = null;

            if ((bool)A_FlagLiquidpool.IsChecked)
                CurrentObjectAppearance.Flags.Liquidpool = true;
            else if (CurrentObjectAppearance.Flags.HasLiquidpool)
                CurrentObjectAppearance.Flags.ClearLiquidpool();

            if ((bool)A_FlagUnpass.IsChecked)
                CurrentObjectAppearance.Flags.Unpass = true;
            else if (CurrentObjectAppearance.Flags.HasUnpass)
                CurrentObjectAppearance.Flags.ClearUnpass();

            if ((bool)A_FlagUnmove.IsChecked)
                CurrentObjectAppearance.Flags.Unmove = true;
            else if (CurrentObjectAppearance.Flags.HasUnmove)
                CurrentObjectAppearance.Flags.ClearUnmove();

            if ((bool)A_FlagUnsight.IsChecked)
                CurrentObjectAppearance.Flags.Unsight = true;
            else if (CurrentObjectAppearance.Flags.HasUnsight)
                CurrentObjectAppearance.Flags.ClearUnsight();

            if ((bool)A_FlagAvoid.IsChecked)
                CurrentObjectAppearance.Flags.Avoid = true;
            else if (CurrentObjectAppearance.Flags.HasAvoid)
                CurrentObjectAppearance.Flags.ClearAvoid();

            if ((bool)A_FlagNoMoveAnimation.IsChecked)
                CurrentObjectAppearance.Flags.NoMovementAnimation = true;

            else if (CurrentObjectAppearance.Flags.HasNoMovementAnimation)
                CurrentObjectAppearance.Flags.ClearNoMovementAnimation();

            if ((bool)A_FlagTake.IsChecked)
                CurrentObjectAppearance.Flags.Take = true;
            else if (CurrentObjectAppearance.Flags.HasTake)
                CurrentObjectAppearance.Flags.ClearTake();

            if ((bool)A_FlagLiquidcontainer.IsChecked)
                CurrentObjectAppearance.Flags.Liquidcontainer = true;
            else if (CurrentObjectAppearance.Flags.HasLiquidcontainer)
                CurrentObjectAppearance.Flags.ClearLiquidcontainer();

            if ((bool)A_FlagHang.IsChecked)
                CurrentObjectAppearance.Flags.Hang = true;
            else if (CurrentObjectAppearance.Flags.HasHang)
                CurrentObjectAppearance.Flags.ClearHang();

            if ((bool)A_FlagHook.IsChecked)
            {
                CurrentObjectAppearance.Flags.Hook = new AppearanceFlagHook
                {
                    Direction = (HOOK_TYPE)(A_FlagHookType.SelectedIndex + 1)
                };
            }
            else CurrentObjectAppearance.Flags.Hook = null;

            if ((bool)A_FlagRotate.IsChecked)
                CurrentObjectAppearance.Flags.Rotate = true;
            else if (CurrentObjectAppearance.Flags.HasRotate)
                CurrentObjectAppearance.Flags.ClearRotate();

            if ((bool)A_FlagLight.IsChecked)
            {
                CurrentObjectAppearance.Flags.Light = new AppearanceFlagLight
                {
                    Brightness = (uint)A_FlagLightBrightness.Value,
                    Color = (uint)A_FlagLightColor.Value
                };
            }
            else 
                CurrentObjectAppearance.Flags.Light = null;


            if ((bool)A_FlagDontHide.IsChecked)
                CurrentObjectAppearance.Flags.DontHide = true;
            else if (CurrentObjectAppearance.Flags.HasDontHide)
                CurrentObjectAppearance.Flags.ClearDontHide();

            if ((bool)A_FlagTranslucent.IsChecked)
                CurrentObjectAppearance.Flags.Translucent = true;
            else if (CurrentObjectAppearance.Flags.HasTranslucent)
                CurrentObjectAppearance.Flags.ClearTranslucent();

            if ((bool)A_FlagShift.IsChecked)
            {
                CurrentObjectAppearance.Flags.Shift = new AppearanceFlagShift
                {
                    X = (uint)A_FlagShiftX.Value,
                    Y = (uint)A_FlagShiftY.Value
                };
            }
            else 
                CurrentObjectAppearance.Flags.Shift = null;

            if ((bool)A_FlagHeight.IsChecked)
            {
                CurrentObjectAppearance.Flags.Height = new AppearanceFlagHeight
                {
                    Elevation = (uint)A_FlagElevation.Value,
                };
            }
            else CurrentObjectAppearance.Flags.Height = null;

            if ((bool)A_FlagLyingObject.IsChecked)
                CurrentObjectAppearance.Flags.LyingObject = true;
            else if (CurrentObjectAppearance.Flags.HasLyingObject)
                CurrentObjectAppearance.Flags.ClearLyingObject();

            if ((bool)A_FlagAnimateAlways.IsChecked)
                CurrentObjectAppearance.Flags.AnimateAlways = true;
            else if (CurrentObjectAppearance.Flags.HasAnimateAlways)
                CurrentObjectAppearance.Flags.ClearAnimateAlways();

            if ((bool)A_FlagAutomap.IsChecked)
            {
                CurrentObjectAppearance.Flags.Automap = new AppearanceFlagAutomap
                {
                    Color = (uint)A_FlagAutomapColor.Value,
                };
            }
            else CurrentObjectAppearance.Flags.Automap = null;

            if ((bool)A_FlagLenshelp.IsChecked)
            {
                CurrentObjectAppearance.Flags.Lenshelp = new AppearanceFlagLenshelp
                {
                    Id = (uint)A_FlagLenshelpId.SelectedIndex + 1100
                };
            }
            else CurrentObjectAppearance.Flags.Lenshelp = null;

            if ((bool)A_FlagFullGround.IsChecked)
                CurrentObjectAppearance.Flags.Fullbank = true;
            else if (CurrentObjectAppearance.Flags.HasFullbank)
                CurrentObjectAppearance.Flags.ClearFullbank();

            if ((bool)A_FlagIgnoreLook.IsChecked)
                CurrentObjectAppearance.Flags.IgnoreLook = true;
            else if (CurrentObjectAppearance.Flags.HasIgnoreLook)
                CurrentObjectAppearance.Flags.ClearIgnoreLook();

            if ((bool)A_FlagClothes.IsChecked)
            {
                CurrentObjectAppearance.Flags.Clothes = new AppearanceFlagClothes
                {
                    Slot = (uint)A_FlagClothesSlot.SelectedIndex
                };
            }
            else CurrentObjectAppearance.Flags.Clothes = null;

            if ((bool)A_FlagDefaultAction.IsChecked)
            {
                CurrentObjectAppearance.Flags.DefaultAction = new AppearanceFlagDefaultAction
                {
                    Action = (PLAYER_ACTION)A_FlagDefaultActionType.SelectedIndex
                };
            }
            else CurrentObjectAppearance.Flags.DefaultAction = null;

            if ((bool)A_FlagMarket.IsChecked)
            {
                CurrentObjectAppearance.Flags.Market = new AppearanceFlagMarket
                {
                    Category = (ITEM_CATEGORY)(A_FlagMarketCategory.SelectedIndex + 1),
                    TradeAsObjectId = (uint)A_FlagMarketTrade.Value,
                    ShowAsObjectId = (uint)A_FlagMarketShow.Value,
                    MinimumLevel = (uint)A_FlagMarketlevel.Value,
                };
                CurrentObjectAppearance.Flags.Market.RestrictToVocation.Clear();
                if ((bool)A_FlagProfessionAny.IsChecked) CurrentObjectAppearance.Flags.Market.RestrictToVocation.Add(VOCATION.Any);
                if ((bool)A_FlagProfessionNone.IsChecked) CurrentObjectAppearance.Flags.Market.RestrictToVocation.Add(VOCATION.None);
                if ((bool)A_FlagProfessionKnight.IsChecked) CurrentObjectAppearance.Flags.Market.RestrictToVocation.Add(VOCATION.Knight);
                if ((bool)A_FlagProfessionPaladin.IsChecked) CurrentObjectAppearance.Flags.Market.RestrictToVocation.Add(VOCATION.Paladin);
                if ((bool)A_FlagProfessionSorcerer.IsChecked) CurrentObjectAppearance.Flags.Market.RestrictToVocation.Add(VOCATION.Sorcerer);
                if ((bool)A_FlagProfessionDruid.IsChecked) CurrentObjectAppearance.Flags.Market.RestrictToVocation.Add(VOCATION.Druid);
                if ((bool)A_FlagProfessionPromoted.IsChecked) CurrentObjectAppearance.Flags.Market.RestrictToVocation.Add(VOCATION.Promoted);
            }
            else
                CurrentObjectAppearance.Flags.Market = null;

            if ((bool)A_FlagWrap.IsChecked)
                CurrentObjectAppearance.Flags.Wrap = true;
            else if (CurrentObjectAppearance.Flags.HasWrap)
                CurrentObjectAppearance.Flags.ClearWrap();

            if ((bool)A_FlagUnwrap.IsChecked)
                CurrentObjectAppearance.Flags.Unwrap = true;
            else if (CurrentObjectAppearance.Flags.HasUnwrap)
                CurrentObjectAppearance.Flags.ClearUnwrap();

            if ((bool)A_FlagDecoItemKit.IsChecked)
                CurrentObjectAppearance.Flags.DecoItemKit = true;
            else if (CurrentObjectAppearance.Flags.HasDecoItemKit)
                CurrentObjectAppearance.Flags.ClearDecoItemKit();

            if ((bool)A_FlagTopeffect.IsChecked)
                CurrentObjectAppearance.Flags.Topeffect = true;
            else if (CurrentObjectAppearance.Flags.HasTopeffect)
                CurrentObjectAppearance.Flags.ClearTopeffect();

            if ((bool)A_FlagChangedToExpire.IsChecked)
            {
                CurrentObjectAppearance.Flags.Changedtoexpire = new AppearanceFlagChangedToExpire
                {
                    FormerObjectTypeid = (uint)A_FlagChangedToExpireId.Value
                };
            }
            else CurrentObjectAppearance.Flags.Changedtoexpire = null;

            if ((bool)A_FlagCorpse.IsChecked)
                CurrentObjectAppearance.Flags.Corpse = true;
            else if (CurrentObjectAppearance.Flags.HasCorpse)
                CurrentObjectAppearance.Flags.ClearCorpse();

            if ((bool)A_FlagPlayerCorpse.IsChecked)
                CurrentObjectAppearance.Flags.PlayerCorpse = true;
            else if (CurrentObjectAppearance.Flags.HasPlayerCorpse)
                CurrentObjectAppearance.Flags.ClearPlayerCorpse();

            if ((bool)A_FlagCyclopedia.IsChecked)
            {
                CurrentObjectAppearance.Flags.Cyclopediaitem = new AppearanceFlagCyclopedia
                {
                    CyclopediaType = (uint)A_FlagCyclopediaItem.Value
                };
            }
            else CurrentObjectAppearance.Flags.Cyclopediaitem = null;

            if ((bool)A_FlagAmmo.IsChecked)
                CurrentObjectAppearance.Flags.Ammo = true;
            else if (CurrentObjectAppearance.Flags.HasAmmo)
                CurrentObjectAppearance.Flags.ClearAmmo();

            CurrentObjectAppearance.Flags.Npcsaledata.Clear();
            if ((bool)A_FlagNPC.IsChecked)
            {
                foreach (var npcdata in NpcDataList)
                {
                    CurrentObjectAppearance.Flags.Npcsaledata.Add(npcdata);
                }
            }

            if ((bool)A_FlagTransparency.IsChecked)
            {
                CurrentObjectAppearance.Flags.Transparencylevel = new AppearanceFlagTransparencyLevel
                {
                    Level = (uint)A_FlagTransparencyLevel.Value
                };
            }
            else CurrentObjectAppearance.Flags.Transparencylevel = null;

            Appearance oldAppearance = null;

            if (ObjectMenu.SelectedIndex == 0)
                oldAppearance = MainWindow.appearances.Outfit.FirstOrDefault(a => a.Id == CurrentObjectAppearance.Id);
            else if (ObjectMenu.SelectedIndex == 1)
                oldAppearance = MainWindow.appearances.Object.FirstOrDefault(a => a.Id == CurrentObjectAppearance.Id);
            else if (ObjectMenu.SelectedIndex == 2)
                oldAppearance = MainWindow.appearances.Effect.FirstOrDefault(a => a.Id == CurrentObjectAppearance.Id);
            else if (ObjectMenu.SelectedIndex == 3)
                oldAppearance = MainWindow.appearances.Missile.FirstOrDefault(a => a.Id == CurrentObjectAppearance.Id);

            if (oldAppearance != null)
            {
                uint oldTransparency = (oldAppearance.Flags.Transparencylevel != null && oldAppearance.Flags.Transparencylevel.HasLevel) ? oldAppearance.Flags.Transparencylevel.Level : 0;
                uint currentTransparency = (bool)A_FlagTransparency.IsChecked ? (uint)A_FlagTransparencyLevel.Value : 0;
                if (oldTransparency != currentTransparency)
                    SetObjectTransparency(CurrentObjectAppearance, (byte)oldTransparency);
            }

            if (ObjectMenu.SelectedIndex == 0)
                MainWindow.appearances.Outfit[ObjListView.SelectedIndex] = CurrentObjectAppearance.Clone();
            else if (ObjectMenu.SelectedIndex == 1)
                MainWindow.appearances.Object[ObjListView.SelectedIndex] = CurrentObjectAppearance.Clone();
            else if (ObjectMenu.SelectedIndex == 2)
                MainWindow.appearances.Effect[ObjListView.SelectedIndex] = CurrentObjectAppearance.Clone();
            else if (ObjectMenu.SelectedIndex == 3)
                MainWindow.appearances.Missile[ObjListView.SelectedIndex] = CurrentObjectAppearance.Clone();

            ShowList showList = ObjListView.SelectedItem as ShowList;
            if (showList.Id != CurrentObjectAppearance.Id)
            {
                showList.Id = CurrentObjectAppearance.Id;
                List<Appearance> sortedAppearances = new List<Appearance>();
                if (ObjectMenu.SelectedIndex == 0)
                {
                    sortedAppearances = MainWindow.appearances.Outfit.OrderBy(item => item.Id).ToList();
                    MainWindow.appearances.Outfit.Clear();
                    foreach (Appearance appearance in sortedAppearances)
                    {
                        MainWindow.appearances.Outfit.Add(appearance);
                    }
                    ThingsOutfit = new ObservableCollection<ShowList>(ThingsOutfit.OrderBy(item => item.Id));
                }
                else if (ObjectMenu.SelectedIndex == 1)
                {
                    sortedAppearances = MainWindow.appearances.Object.OrderBy(item => item.Id).ToList();
                    MainWindow.appearances.Object.Clear();
                    foreach (Appearance appearance in sortedAppearances)
                    {
                        MainWindow.appearances.Object.Add(appearance);
                    }
                    ThingsItem = new ObservableCollection<ShowList>(ThingsItem.OrderBy(item => item.Id));
                }
                else if (ObjectMenu.SelectedIndex == 2)
                {
                    sortedAppearances = MainWindow.appearances.Effect.OrderBy(item => item.Id).ToList();
                    MainWindow.appearances.Effect.Clear();
                    foreach (Appearance appearance in sortedAppearances)
                    {
                        MainWindow.appearances.Effect.Add(appearance);
                    }
                    ThingsEffect = new ObservableCollection<ShowList>(ThingsEffect.OrderBy(item => item.Id));
                }
                else if (ObjectMenu.SelectedIndex == 3)
                {
                    sortedAppearances = MainWindow.appearances.Missile.OrderBy(item => item.Id).ToList();
                    MainWindow.appearances.Missile.Clear();
                    foreach (Appearance appearance in sortedAppearances)
                    {
                        MainWindow.appearances.Missile.Add(appearance);
                    }
                    ThingsMissile = new ObservableCollection<ShowList>(ThingsMissile.OrderBy(item => item.Id));
                }
            }

            AnimateSelectedListItem(showList);

            StatusBar.MessageQueue.Enqueue($"Saved Current Object.", null, null, null, false, true, TimeSpan.FromSeconds(2));
        }
        private void CopyObjectFlags(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            CurrentFlags = CurrentObjectAppearance.Flags.Clone();
            StatusBar.MessageQueue.Enqueue($"Copied Current Object Flags.", null, null, null, false, true, TimeSpan.FromSeconds(2));
        }
        private void PasteObjectFlags(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (CurrentFlags != null)
            {
                CurrentObjectAppearance.Flags = CurrentFlags.Clone();
                LoadCurrentObjectAppearances();
                StatusBar.MessageQueue.Enqueue($"Pasted Object Flags.", null, null, null, false, true, TimeSpan.FromSeconds(2));
            }
            else
                StatusBar.MessageQueue.Enqueue($"Copy Flags First.", null, null, null, false, true, TimeSpan.FromSeconds(2));
        }

        private void SprPhaseMin_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.Animation != null)
                CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.Animation.SpritePhase[(int)(SprFramesSlider.Value / SprFramesSlider.TickFrequency)].DurationMin = (uint)SprPhaseMin.Value;
        }

        private void SprPhaseMax_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.Animation != null)
                CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.Animation.SpritePhase[(int)(SprFramesSlider.Value / SprFramesSlider.TickFrequency)].DurationMax = (uint)SprPhaseMax.Value;
        }


        private void OpenSpriteManager_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SprEditor sprEditor = new SprEditor(this);
            SprEditor.CustomSheetsList.Clear();
            sprEditor.Show();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            base.OnClosed(e);
            Application.Current.Shutdown();
        }
        public class LowercaseContractResolver : DefaultContractResolver
        {
            protected override string ResolvePropertyName(string propertyName)
            {
                return propertyName.ToLower();
            }
        }

        private void Compile_Click(object sender, RoutedEventArgs e)
        {
            File.Copy(System.IO.Path.Combine(MainWindow._assetsPath , "catalog-content.json"), System.IO.Path.Combine(MainWindow._assetsPath, "catalog-content.json-bak"), true);
            File.Copy(MainWindow._datPath, MainWindow._datPath + "-bak", true);
            ProcessTransparentSheets();
            using (StreamWriter file = File.CreateText(MainWindow._assetsPath + "\\catalog-content.json"))
            {
                JsonSerializer serializer = new JsonSerializer
                {
                    Formatting = Formatting.Indented,
                    ContractResolver = new LowercaseContractResolver(),
                    NullValueHandling = NullValueHandling.Ignore,
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore,
                };
                serializer.Serialize(file, MainWindow.catalog);
            }
            var output = File.Create(MainWindow._datPath);
            MainWindow.appearances.WriteTo(output);
            output.Close();
            StatusBar.MessageQueue.Enqueue($"Compiled.", null, null, null, false, true, TimeSpan.FromSeconds(2));
        }
        public List<System.Drawing.Bitmap> SplitImage(System.Drawing.Bitmap originalImage)
        {
            List<System.Drawing.Bitmap> splitImages = new List<System.Drawing.Bitmap>();

            int rows = originalImage.Height / 32;
            int cols = originalImage.Width / 32;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    System.Drawing.Rectangle cloneRect = new System.Drawing.Rectangle(col * 32, row * 32, 32, 32);
                    System.Drawing.Imaging.PixelFormat format = originalImage.PixelFormat;

                    System.Drawing.Bitmap clonedSegment = originalImage.Clone(cloneRect, format);
                    splitImages.Add(clonedSegment);
                }
            }

            return splitImages;
        }
        public bool IsImageFullyTransparent(System.Drawing.Bitmap image)
        {
            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, image.Width, image.Height);
            System.Drawing.Imaging.BitmapData bmpData = image.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, image.PixelFormat);

            int bytes = Math.Abs(bmpData.Stride) * image.Height;
            byte[] rgbValues = new byte[bytes];

            Marshal.Copy(bmpData.Scan0, rgbValues, 0, bytes);

            image.UnlockBits(bmpData);

            for (int index = 0; index < rgbValues.Length; index += 4)
            {
                if (rgbValues[index + 3] != 0)
                {
                    return false;
                }
            }
            return true;
        }
        private Appearance CreateBlankObject(uint id, APPEARANCE_TYPE type)
        {
            Appearance newObject = new Appearance();

            newObject.Flags = new AppearanceFlags();

            FrameGroup frameGroup = new FrameGroup();
            frameGroup.SpriteInfo = new SpriteInfo();
            frameGroup.FixedFrameGroup = FIXED_FRAME_GROUP.OutfitIdle;

            frameGroup.SpriteInfo.PatternWidth = 1;
            frameGroup.SpriteInfo.PatternHeight = 1;
            frameGroup.SpriteInfo.PatternSize = 32;

            frameGroup.SpriteInfo.PatternLayers = 1;
            frameGroup.SpriteInfo.PatternX = 1;
            frameGroup.SpriteInfo.PatternY = 1;
            frameGroup.SpriteInfo.PatternZ = 1;
            frameGroup.SpriteInfo.PatternFrames = 1;

            frameGroup.SpriteInfo.SpriteId.Add(0);

            newObject.FrameGroup.Add(frameGroup);

            newObject.AppearanceType = type;
            newObject.Id = id;

            return newObject;
        }
        public int GetSheetType(uint sprId)
        {
            foreach (MainWindow.Catalog sheet in MainWindow.catalog)
            {
                if (sheet.FirstSpriteid <= sprId && sheet.LastSpriteid >= sprId)
                    return sheet.SpriteType;
            }

            return 0;
        }
        private void UpdateAppearanceObject(Appearance appearance, ConcurrentDictionary<string, uint> offset)
        {
            for (int i = 0; i < appearance.FrameGroup.Count; i++)
            {
                uint Width = 1;
                uint Height = 1;
                uint ExactSize = 32;
                int sliceType = GetSheetType(appearance.FrameGroup[i].SpriteInfo.SpriteId[0]);
                if (sliceType == 1)
                {
                    Width = 1;
                    Height = 2;
                }
                else if (sliceType == 2)
                {
                    Width = 2;
                    Height = 1;
                }
                else if (sliceType == 3)
                {
                    Width = 2;
                    Height = 2;
                }
                if (Width > 1 || Height > 1)
                {
                    try
                    {
                        ExactSize = Math.Max(appearance.FrameGroup[i].SpriteInfo.BoundingBoxPerDirection[0].Width, appearance.FrameGroup[i].SpriteInfo.BoundingBoxPerDirection[0].Height);
                    }
                    catch
                    {
                        MainWindow.Log("Error exporting sprite " + appearance.Id + ", crash prevented.");
                    }
                }
                appearance.FrameGroup[i].SpriteInfo.PatternX = appearance.FrameGroup[i].SpriteInfo.PatternWidth;
                appearance.FrameGroup[i].SpriteInfo.PatternY = appearance.FrameGroup[i].SpriteInfo.PatternHeight;
                appearance.FrameGroup[i].SpriteInfo.PatternWidth = Width;
                appearance.FrameGroup[i].SpriteInfo.PatternHeight = Height;
                appearance.FrameGroup[i].SpriteInfo.PatternSize = ExactSize;
                appearance.FrameGroup[i].SpriteInfo.PatternLayers = appearance.FrameGroup[i].SpriteInfo.Layers;
                appearance.FrameGroup[i].SpriteInfo.PatternZ = appearance.FrameGroup[i].SpriteInfo.PatternDepth;
                if (appearance.FrameGroup[i].SpriteInfo.Animation != null)
                    appearance.FrameGroup[i].SpriteInfo.PatternFrames = (uint)appearance.FrameGroup[i].SpriteInfo.Animation.SpritePhase.Count;
                else
                    appearance.FrameGroup[i].SpriteInfo.PatternFrames = 1;

                List<uint> groupSpr = new List<uint>();

                try
                {
                    if (sliceType == 0)
                    {
                        for (int j = 0; j < appearance.FrameGroup[i].SpriteInfo.SpriteId.Count; j++)
                        {
                            string sprName = appearance.FrameGroup[i].SpriteInfo.SpriteId[j].ToString();
                            groupSpr.Add(offset[sprName + "_0"]);
                        }
                        appearance.FrameGroup[i].SpriteInfo.SpriteId.Clear();
                        for (int j = 0; j < groupSpr.Count; j++)
                        {
                            appearance.FrameGroup[i].SpriteInfo.SpriteId.Add(groupSpr[j]);
                        }
                    }
                    else if (sliceType == 1 || sliceType == 2)
                    {
                        for (int j = 0; j < appearance.FrameGroup[i].SpriteInfo.SpriteId.Count; j++)
                        {
                            string sprName = appearance.FrameGroup[i].SpriteInfo.SpriteId[j].ToString();
                            groupSpr.Add(offset[sprName + "_1"]);
                            groupSpr.Add(offset[sprName + "_0"]);
                        }
                        appearance.FrameGroup[i].SpriteInfo.SpriteId.Clear();
                        for (int j = 0; j < groupSpr.Count; j++)
                        {
                            appearance.FrameGroup[i].SpriteInfo.SpriteId.Add(groupSpr[j]);
                        }

                    }
                    else if (sliceType == 3)
                    {
                        for (int j = 0; j < appearance.FrameGroup[i].SpriteInfo.SpriteId.Count; j++)
                        {
                            string sprName = appearance.FrameGroup[i].SpriteInfo.SpriteId[j].ToString();
                            groupSpr.Add(offset[sprName + "_3"]);
                            groupSpr.Add(offset[sprName + "_2"]);
                            groupSpr.Add(offset[sprName + "_1"]);
                            groupSpr.Add(offset[sprName + "_0"]);
                        }
                        appearance.FrameGroup[i].SpriteInfo.SpriteId.Clear();
                        for (int j = 0; j < groupSpr.Count; j++)
                        {
                            appearance.FrameGroup[i].SpriteInfo.SpriteId.Add(groupSpr[j]);
                        }
                    }
                }
                catch (Exception)
                {
                    MainWindow.Log("Error exporting animation for sprite " + appearance.Id + ", crash prevented.");
                }
            }
        }

        private void CompileLegacy_Click(object sender, RoutedEventArgs e)
        {
            ComppileDialogHost.IsOpen = true;
            CompileBox.IsEnabled = true;
            LoadProgress1.Value = 0;
            LoadProgress2.Value = 0;
        }

        private async void CompileLegacy(object sender, RoutedEventArgs e)
        {
            var progress = new Progress<int>(percent =>
            {
                LoadProgress1.Value = percent;
            });
            await ExportLegacy(progress);
        }

        private void CompileToImages_Click(object sender, RoutedEventArgs e)
        {
            CompileToImagesDialogHost.IsOpen = true;
            CompileToImagesBox.IsEnabled = true;
            ImgExportProgress.Value = 0;
            Appearances a = MainWindow.appearances;

            // there is a problem with making this show actual export count
            // a.Object is a list, not map so there is no direct way to iterate through ids unless pre-allocated
            // because of that, this text displays total amount of objects scanned and processed instead
            // for example assets that have 30000 items can have ids as high as 50000
            // this would make the progress text stop at roughly 60%
            // this could be replaced with a progress bar in the future
            ExportItemsProgressText.Text = $"0/{a.Object.Count}";
            ExportOutfitsProgressText.Text = $"0/{a.Outfit.Count}";
            ExportEffectsProgressText.Text = $"0/{a.Effect.Count}";
            ExportMissilesProgressText.Text = $"0/{a.Missile.Count}";

            // controls for exporting
            uint maxItems = a.Object.Max(obj => obj.Id);
            uint maxOutfits = a.Outfit.Max(obj => obj.Id);
            uint maxEffects = a.Effect.Max(obj => obj.Id);
            uint maxMissiles = a.Missile.Max(obj => obj.Id);

            // control for starting id
            ExportItemsMinId.Maximum = (int)maxItems;
            ExportOutfitsMinId.Maximum = (int)maxOutfits;
            ExportEffectsMinId.Maximum = (int)maxEffects;
            ExportMissilesMinId.Maximum = (int)maxMissiles;

            // starting id default values
            ExportItemsMinId.Value = 100;
            ExportOutfitsMinId.Value = 1;
            ExportEffectsMinId.Value = 1;
            ExportMissilesMinId.Value = 1;

            // control for final id
            ExportItemsMaxId.Maximum = (int)maxItems;
            ExportOutfitsMaxId.Maximum = (int)maxOutfits;
            ExportEffectsMaxId.Maximum = (int)maxEffects;
            ExportMissilesMaxId.Maximum = (int)maxMissiles;

            // final id default values
            ExportItemsMaxId.Value = (int)maxItems;
            ExportOutfitsMaxId.Value = (int)maxOutfits;
            ExportEffectsMaxId.Value = (int)maxEffects;
            ExportMissilesMaxId.Value = (int)maxMissiles;
        }
        
        private async void CompileAsImages(object sender, RoutedEventArgs e)
        {
            // cut max fields if max < min
            (int min, int max) GetMinMax(Xceed.Wpf.Toolkit.IntegerUpDown minControl, Xceed.Wpf.Toolkit.IntegerUpDown maxControl, int defaultValue) {
                int min = minControl.Value ?? defaultValue;
                int max = maxControl.Value ?? defaultValue;
                maxControl.Value = Math.Max(min, max); // Ensure max >= min
                return (min, Math.Max(min, max));
            }
            var itemScope = GetMinMax(ExportItemsMinId, ExportItemsMaxId, 100);
            var outfitScope = GetMinMax(ExportOutfitsMinId, ExportOutfitsMaxId, 1);
            var effectScope = GetMinMax(ExportEffectsMinId, ExportEffectsMaxId, 1);
            var missileScope = GetMinMax(ExportMissilesMinId, ExportMissilesMaxId, 1);

            await ExportAsImages(itemScope, outfitScope, effectScope, missileScope);
        }

        private void ExportAsImageDirectoryPicker_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "Select the directory to export objects.";
            dialog.ShowNewFolderButton = true;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                MainWindow._imgExportPath = dialog.SelectedPath;
                ExportImageDirectoryTextBox.Text = MainWindow._imgExportPath;
            }
        }

        private async Task ExportAsImages((int min, int max) itemScope, (int min, int max) outfitScope, (int min, int max) effectScope, (int min, int max) missileScope) {
            // lock the ui before exporting
            CompileToImagesBox.IsEnabled = false;

            Appearances tmpAppearances = new();

            // access the copy of appearances index
            tmpAppearances = MainWindow.appearances.Clone();
            var tasks = new List<Task>();

            // count the total amount of items to process
            int totalItems = 0;
            if (ExportItemsCheckBox.IsChecked == true) totalItems += 1 + itemScope.max - itemScope.min; // +1 because n - n = 0
            if (ExportOutfitsCheckBox.IsChecked == true) totalItems += 1 + outfitScope.max - outfitScope.min;
            if (ExportEffectsCheckBox.IsChecked == true) totalItems += 1 + effectScope.max - effectScope.min;
            if (ExportMissilesCheckBox.IsChecked == true) totalItems += 1 + missileScope.max - missileScope.min;

            // processed items counter
            int processedItems = 0;

            // report progress to the UI
            void ReportProgress() {
                int percentage = (int)((double)processedItems / totalItems * 100);
                ImgExportProgress.Value = percentage;
            }

            // helper function to avoid code duplication
            void EnqueueExportTask(bool? isChecked, string subDirectory, IList<Appearance> objects, (int min, int max) scope, Action<string, Appearance> exportAction, Action<int, int> progressUpdateAction) {
                if (isChecked == true) {
                    tasks.Add(Task.Run(() => {
                        string exportPath = Path.Combine(MainWindow._imgExportPath, subDirectory);
                        Directory.CreateDirectory(exportPath);
                        int totalObjects = objects.Count;
                        int loopProgress = 0;

                        Parallel.ForEach(objects, () => 0, (obj, state, localProgress) =>
                        {
                            if (obj.Id >= scope.min && obj.Id <= scope.max) {
                                exportAction(exportPath, obj);
                            }
                            Interlocked.Increment(ref processedItems);
                            localProgress++;
                            Dispatcher.Invoke(() => {
                                int currentProgress = Interlocked.Add(ref loopProgress, 1);
                                progressUpdateAction(currentProgress, totalObjects);
                                ReportProgress();
                            });

                            return localProgress;
                        },
                        localProgress => { /* no final action needed for local progress */ });
                    }));
                }
            }

            // items
            EnqueueExportTask(ExportItemsCheckBox.IsChecked, "items", tmpAppearances.Object, itemScope, ImageExporter.SaveItemAsGIF, (loopProgress, totalObjects) => {
                ExportItemsProgressText.Text = $"{loopProgress}/{totalObjects}";
            });

            // outfits
            EnqueueExportTask(ExportOutfitsCheckBox.IsChecked, "outfits", tmpAppearances.Outfit, outfitScope, ImageExporter.SaveOutfitAsImages, (loopProgress, totalObjects) => {
                ExportOutfitsProgressText.Text = $"{loopProgress}/{totalObjects}";
            });

            // effects
            EnqueueExportTask(ExportEffectsCheckBox.IsChecked, "effects", tmpAppearances.Effect, effectScope, ImageExporter.SaveEffectAsGIF, (loopProgress, totalObjects) => {
                ExportEffectsProgressText.Text = $"{loopProgress}/{totalObjects}";
            });

            // missiles
            EnqueueExportTask(ExportMissilesCheckBox.IsChecked, "missiles", tmpAppearances.Missile, missileScope, ImageExporter.SaveMissileAsGIF, (loopProgress, totalObjects) => {
                ExportMissilesProgressText.Text = $"{loopProgress}/{totalObjects}";
            });

            // Wait for all tasks to complete
            await Task.WhenAll(tasks);

            // close the window after successful export
            CompileToImagesDialogHost.IsOpen = false;
        }

        private async Task ExportLegacy(IProgress<int> progress)
        {
            CompileBox.IsEnabled = false;
            SpriteStorage tmpSprStorage = new SpriteStorage();
            Appearances tmpAppearances = new Appearances();
            ConcurrentDictionary<int, List<MemoryStream>> SlicedSprList = new ConcurrentDictionary<int, List<MemoryStream>>();
            ConcurrentDictionary<string, uint> SpriteOffsetList = new ConcurrentDictionary<string, uint>();

            await Task.Run(() =>
            {
                int percentageComplete = 0;
                int currentPercentage = 0;
                int FullProgress = MainWindow.catalog.Count;
                var options = new ParallelOptions()
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                };
                int progressCount = 0;
                Parallel.ForEach(MainWindow.catalog, options, (sheet, state) =>
                {
                    if (sheet.Type == "sprite")
                    {
                        string lzma = String.Format("{0}{1}", MainWindow._assetsPath, sheet.File);
                        if (File.Exists(lzma) == false)
                        {
                            Debug.WriteLine("File Doesn't exists: " + sheet.File);
                            return;
                        }
                        for (int i = sheet.FirstSpriteid; i <= sheet.LastSpriteid; i++)
                        {
                            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(MainWindow.getSpriteStream(i));
                            List<System.Drawing.Bitmap> slices = SplitImage(bitmap);
                            SlicedSprList[i] = new List<MemoryStream>();
                            for (int j = 0; j < slices.Count; j++)
                            {
                                if (IsImageFullyTransparent(slices[j]) == false)
                                {
                                    MemoryStream ms = new MemoryStream();
                                    slices[j].Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                    ms.Position = 0;
                                    SlicedSprList[i].Add(ms);
                                }
                                else
                                {
                                    SlicedSprList[i].Add(null);
                                }

                            }
                        }
                        progressCount++;
                        percentageComplete = (int)(progressCount * 100 / FullProgress);
                        if (percentageComplete > currentPercentage)
                        {
                            progress?.Report(percentageComplete);
                            currentPercentage = percentageComplete;
                        }
                    }
                });

                uint count = 1;
                foreach (int key in SlicedSprList.Keys)
                {
                    List<MemoryStream> memoryStreams = SlicedSprList[key];
                    int streamCounter = 0;
                    foreach (MemoryStream stream in memoryStreams)
                    {
                        string sprName = key.ToString() + "_" + streamCounter.ToString();
                        if (stream == null)
                        {
                            SpriteOffsetList[sprName] = 0;
                        }
                        else
                        {
                            tmpSprStorage.SprLists[(int)count] = stream;
                            SpriteOffsetList[sprName] = count;
                            count++;
                        }
                        streamCounter++;
                    }
                }


                tmpAppearances = MainWindow.appearances.Clone();

                uint ObjectCount = tmpAppearances.Object[^1].Id;
                uint OutfitCount = tmpAppearances.Outfit[^1].Id;
                uint EffectCount = tmpAppearances.Effect[^1].Id;
                uint MissileCount = tmpAppearances.Missile[^1].Id;
                for (uint i = 100; i <= ObjectCount; i++)
                {
                    Appearance appearance = tmpAppearances.Object.FirstOrDefault(a => a.Id == i);
                    if (appearance == null)
                    {
                        Appearance newObject = CreateBlankObject(i, APPEARANCE_TYPE.AppearanceObject);
                        tmpAppearances.Object.Add(newObject);
                    }
                    else
                    {
                        appearance.AppearanceType = APPEARANCE_TYPE.AppearanceObject;
                        if (appearance.Flags.Hook != null)
                        {
                            if (appearance.Flags.Hook.Direction == HOOK_TYPE.South)
                                appearance.Flags.HookSouth = true;

                            if (appearance.Flags.Hook.Direction == HOOK_TYPE.East)
                                appearance.Flags.HookEast = true;
                        }
                        UpdateAppearanceObject(appearance, SpriteOffsetList);
                    }
                }
                for (uint i = 1; i <= OutfitCount; i++)
                {
                    Appearance appearance = tmpAppearances.Outfit.FirstOrDefault(a => a.Id == i);
                    if (appearance == null)
                    {
                        Appearance newObject = CreateBlankObject(i, APPEARANCE_TYPE.AppearanceOutfit);
                        tmpAppearances.Outfit.Add(newObject);
                    }
                    else
                    {
                        appearance.AppearanceType = APPEARANCE_TYPE.AppearanceOutfit;
                        UpdateAppearanceObject(appearance, SpriteOffsetList);
                    }
                }
                for (uint i = 1; i <= EffectCount; i++)
                {
                    Appearance appearance = tmpAppearances.Effect.FirstOrDefault(a => a.Id == i);
                    if (appearance == null)
                    {
                        Appearance newObject = CreateBlankObject(i, APPEARANCE_TYPE.AppearanceEffect);
                        tmpAppearances.Effect.Add(newObject);
                    }
                    else
                    {
                        appearance.AppearanceType = APPEARANCE_TYPE.AppearanceEffect;
                        UpdateAppearanceObject(appearance, SpriteOffsetList);
                    }
                }
                for (uint i = 1; i <= MissileCount; i++)
                {
                    Appearance appearance = tmpAppearances.Missile.FirstOrDefault(a => a.Id == i);
                    if (appearance == null)
                    {
                        Appearance newObject = CreateBlankObject(i, APPEARANCE_TYPE.AppearanceMissile);
                        tmpAppearances.Missile.Add(newObject);
                    }
                    else
                    {
                        appearance.AppearanceType = APPEARANCE_TYPE.AppearanceMissile;
                        UpdateAppearanceObject(appearance, SpriteOffsetList);
                    }
                }

                string datfile = MainWindow._assetsPath + "Tibia.dat";
                LegacyAppearance.WriteLegacyDat(datfile, 0x42A3, tmpAppearances);
                
            });
            var progress1 = new Progress<int>(percent =>
            {
                LoadProgress2.Value = percent;
            });
            string sprfile = MainWindow._assetsPath + "Tibia.spr";
            await Sprite.CompileSpritesAsync(sprfile, tmpSprStorage, false, 0x53159CA9, progress1);
            ComppileDialogHost.IsOpen = false;
        }
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void SpriteExport_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            List<ShowList> selectedItems = SprListView.SelectedItems.Cast<ShowList>().ToList();
            if (selectedItems.Any())
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Bitmap Image (.bmp)|*.bmp|Gif Image (.gif)|*.gif|JPEG Image (.jpeg)|*.jpeg|Png Image (.png)|*.png",
                    FileName = " "
                };
                if (saveFileDialog.ShowDialog() == true)
                {
                    foreach (var item in selectedItems)
                    {
                        if (item.Image != null)
                        {
                            System.Drawing.Bitmap targetImg = new System.Drawing.Bitmap((int)item.Image.Width, (int)item.Image.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                            System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(targetImg);
                            if (saveFileDialog.FilterIndex != 4)
                                g.Clear(System.Drawing.Color.FromArgb(255, 255, 0, 255));
                            System.Drawing.Image image = System.Drawing.Image.FromStream(MainWindow.getSpriteStream((int)item.Id));
                            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                            g.DrawImage(image, new System.Drawing.Rectangle(0, 0, targetImg.Width, targetImg.Height), new System.Drawing.Rectangle(0, 0, targetImg.Width, targetImg.Height), System.Drawing.GraphicsUnit.Pixel);
                            g.Dispose();
                            string directoryPath = System.IO.Path.GetDirectoryName(saveFileDialog.FileName);
                            switch (saveFileDialog.FilterIndex)
                            {
                                case 1:
                                    targetImg.Save(directoryPath + "\\" + item.Id.ToString() + ".bmp", System.Drawing.Imaging.ImageFormat.Bmp);
                                    break;
                                case 2:
                                    targetImg.Save(directoryPath + "\\" + item.Id.ToString() + ".gif", System.Drawing.Imaging.ImageFormat.Gif);
                                    break;
                                case 3:
                                    targetImg.Save(directoryPath + "\\" + item.Id.ToString() + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                                    break;
                                case 4:
                                    targetImg.Save(directoryPath + "\\" + item.Id.ToString() + ".png", System.Drawing.Imaging.ImageFormat.Png);
                                    break;
                            }
                            targetImg.Dispose();
                        }
                    }
                }
            }
        }

        private void NewObject_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Appearance newObject = new Appearance();

            newObject.Flags = new AppearanceFlags();

            FrameGroup frameGroup = new FrameGroup();
            frameGroup.SpriteInfo = new SpriteInfo();

            frameGroup.SpriteInfo.Layers = 1;
            frameGroup.SpriteInfo.PatternWidth = 1;
            frameGroup.SpriteInfo.PatternHeight = 1;
            frameGroup.SpriteInfo.PatternDepth = 1;

            if (ObjectMenu.SelectedIndex == 0)
                frameGroup.FixedFrameGroup = FIXED_FRAME_GROUP.OutfitIdle;
            else
                frameGroup.FixedFrameGroup = FIXED_FRAME_GROUP.ObjectInitial;

            frameGroup.SpriteInfo.SpriteId.Add(blankSpr);
            newObject.FrameGroup.Add(frameGroup);

            if (ObjectMenu.SelectedIndex == 0)
            {
                newObject.Id = (uint)(MainWindow.appearances.Outfit[^1].Id + 1);
                MainWindow.appearances.Outfit.Add(newObject);
                ThingsOutfit.Add(new ShowList() { Id = newObject.Id });
            }
            else if (ObjectMenu.SelectedIndex == 1)
            {
                newObject.Id = (uint)(MainWindow.appearances.Object[^1].Id + 1);
                MainWindow.appearances.Object.Add(newObject);
                ThingsItem.Add(new ShowList() { Id = newObject.Id });
            }
            else if (ObjectMenu.SelectedIndex == 2)
            {
                newObject.Id = (uint)(MainWindow.appearances.Effect[^1].Id + 1);
                MainWindow.appearances.Effect.Add(newObject);
                ThingsEffect.Add(new ShowList() { Id = newObject.Id });

            }
            else if (ObjectMenu.SelectedIndex == 3)
            {
                newObject.Id = (uint)(MainWindow.appearances.Missile[^1].Id + 1);
                MainWindow.appearances.Missile.Add(newObject);
                ThingsMissile.Add(new ShowList() { Id = newObject.Id });

            }
            ObjListView.SelectedItem = ObjListView.Items[^1];

            StatusBar.MessageQueue.Enqueue($"Object successfully created.", null, null, null, false, true, TimeSpan.FromSeconds(2));
        }

        private void ObjectClone_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            List<ShowList> selectedItems = ObjListView.SelectedItems.Cast<ShowList>().ToList();
            if (selectedItems.Any())
            {
                ObjListViewSelectedIndex.Value = (int)selectedItems.Last().Id;

                foreach (var item in selectedItems)
                {
                    Appearance NewObject = new Appearance();
                    if (ObjectMenu.SelectedIndex == 0)
                    {
                        NewObject = MainWindow.appearances.Outfit.FirstOrDefault(o => o.Id == item.Id).Clone();
                        NewObject.Id = (uint)MainWindow.appearances.Outfit[^1].Id + 1;
                        MainWindow.appearances.Outfit.Add(NewObject);
                        ThingsOutfit.Add(new ShowList() { Id = NewObject.Id });
                    }
                    else if (ObjectMenu.SelectedIndex == 1)
                    {
                        NewObject = MainWindow.appearances.Object.FirstOrDefault(o => o.Id == item.Id).Clone();
                        NewObject.Id = (uint)MainWindow.appearances.Object[^1].Id + 1;
                        MainWindow.appearances.Object.Add(NewObject);
                        ThingsItem.Add(new ShowList() { Id = NewObject.Id });

                    }
                    else if (ObjectMenu.SelectedIndex == 2)
                    {
                        NewObject = MainWindow.appearances.Effect.FirstOrDefault(o => o.Id == item.Id).Clone();
                        NewObject.Id = (uint)MainWindow.appearances.Effect[^1].Id + 1;
                        MainWindow.appearances.Effect.Add(NewObject);
                        ThingsEffect.Add(new ShowList() { Id = NewObject.Id });

                    }
                    else if (ObjectMenu.SelectedIndex == 3)
                    {
                        NewObject = MainWindow.appearances.Missile.FirstOrDefault(o => o.Id == item.Id).Clone();
                        NewObject.Id = (uint)MainWindow.appearances.Missile[^1].Id + 1;
                        MainWindow.appearances.Missile.Add(NewObject);
                        ThingsMissile.Add(new ShowList() { Id = NewObject.Id });

                    }

                }
                ObjListView.SelectedItem = ObjListView.Items[^1];
                StatusBar.MessageQueue.Enqueue($"Successfully duplicated {selectedItems.Count} {(selectedItems.Count == 1 ? "object" : "objects")}.", null, null, null, false, true, TimeSpan.FromSeconds(2));
            }
        }

        private void DeleteObject_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            List<ShowList> selectedItems = ObjListView.SelectedItems.Cast<ShowList>().ToList();
            if (selectedItems.Any())
            {
                ObjListViewSelectedIndex.Value = (int)selectedItems.Last().Id;
                int currentIndex = ObjListView.SelectedIndex;
                foreach (var item in selectedItems)
                {
                    Appearance DelObject = new Appearance();
                    if (ObjectMenu.SelectedIndex == 0)
                    {
                        DelObject = MainWindow.appearances.Outfit.FirstOrDefault(o => o.Id == item.Id);
                        MainWindow.appearances.Outfit.Remove(DelObject);
                        ThingsOutfit.Remove(item);
                    }
                    else if (ObjectMenu.SelectedIndex == 1)
                    {
                        DelObject = MainWindow.appearances.Object.FirstOrDefault(o => o.Id == item.Id);
                        MainWindow.appearances.Object.Remove(DelObject);
                        ThingsItem.Remove(item);
                    }
                    else if (ObjectMenu.SelectedIndex == 2)
                    {
                        DelObject = MainWindow.appearances.Effect.FirstOrDefault(o => o.Id == item.Id);
                        MainWindow.appearances.Effect.Remove(DelObject);
                        ThingsEffect.Remove(item);

                    }
                    else if (ObjectMenu.SelectedIndex == 3)
                    {
                        DelObject = MainWindow.appearances.Missile.FirstOrDefault(o => o.Id == item.Id);
                        MainWindow.appearances.Missile.Remove(DelObject);
                        ThingsMissile.Remove(item);

                    }
                }
                ObjListView.SelectedIndex = Math.Min(currentIndex, ObjListView.Items.Count - 1);
                StatusBar.MessageQueue.Enqueue($"Successfully deleted {selectedItems.Count} {(selectedItems.Count == 1 ? "object" : "objects")}.", null, null, null, false, true, TimeSpan.FromSeconds(2));
            }
        }

        private void SprDefaultPhase_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.Animation != null)
                CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.Animation.DefaultStartPhase = (uint)SprDefaultPhase.Value;
        }

        private void SprLoopCount_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.Animation != null)
                CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.Animation.LoopCount = (uint)SprLoopCount.Value;

        }

        private void SprSynchronized_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.Animation != null)
                CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.Animation.Synchronized = (bool)SprSynchronized.IsChecked;

        }

        private void SprRandomPhase_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.Animation != null)
                CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.Animation.RandomStartPhase = (bool)SprRandomPhase.IsChecked;

        }

        private void SprLoopType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.Animation.HasLoopType)
            {
                if (SprLoopType.SelectedIndex == 0)
                    CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.Animation.LoopType = ANIMATION_LOOP_TYPE.Pingpong;
                else if (SprLoopType.SelectedIndex == 1)
                    CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.Animation.LoopType = ANIMATION_LOOP_TYPE.Infinite;
                else if (SprLoopType.SelectedIndex == 2)
                    CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.Animation.LoopType = ANIMATION_LOOP_TYPE.Counted;
            }
        }
        private void SearchItem_Click(object sender, RoutedEventArgs e)
        {
            SearchWindow searchWindow = new SearchWindow(this, false);
            searchWindow.Show();
        }
        private void OTBEditor_Click(object sender, RoutedEventArgs e)
        {
            OTBEditor oTBEditor = new OTBEditor(this, false);
            oTBEditor.Show();
        }

        private List<MainWindow.Catalog> CreateSpriteSheetsAndSaveAsPng(List<MemoryStream> spriteStreams, string outputDirectory)
        {
            Directory.CreateDirectory(outputDirectory);

            var spriteSizes = new List<System.Drawing.Size>
            {
                new System.Drawing.Size(32, 32),
                new System.Drawing.Size(32, 64),
                new System.Drawing.Size(64, 32),
                new System.Drawing.Size(64, 64)
            };

            var spriteInfos = spriteStreams.Select((stream, index) =>
            {
                stream.Position = 0;
                using (var image = System.Drawing.Image.FromStream(stream))
                {
                    return new ImportSpriteInfo { Stream = stream, OriginalIndex = index, Size = new System.Drawing.Size(image.Width, image.Height) };
                }
            }).ToList();

            var catalogs = new List<MainWindow.Catalog>();
            foreach (var size in spriteSizes)
            {
                var matchingSprites = spriteInfos.Where(si => si.Size == size).ToList();
                if (!matchingSprites.Any()) continue;

                var spriteType = spriteSizes.IndexOf(size);
                var spriteSheetResults = ProcessSpriteGroup(matchingSprites, size, outputDirectory, spriteType);
                catalogs.AddRange(spriteSheetResults);
            }

            return catalogs;
        }
        private List<MainWindow.Catalog> ProcessSpriteGroup(List<ImportSpriteInfo> spriteInfos, System.Drawing.Size spriteSize, string outputDirectory, int spriteType)
        {
            int spritesPerRow = 384 / spriteSize.Width;
            int spritesPerColumn = 384 / spriteSize.Height;
            int spritesPerSheet = spritesPerRow * spritesPerColumn;

            var catalogs = new List<MainWindow.Catalog>();
            System.Drawing.Bitmap currentSheet = null;
            System.Drawing.Graphics graphics = null;
            int sheetNumber = 0;
            int currentSpriteIndex = 0; // Counter for sprites in the current sheet

            Action finalizeSheet = () =>
            {
                string GenerateUniqueFilePath(string path)
                {
                    var random = new Random();
                    string directory = Path.GetDirectoryName(path);
                    string filenameWithoutExt = Path.GetFileNameWithoutExtension(path);
                    string extension = Path.GetExtension(path);
                    string newFilePath;

                    do
                    {
                        int randomNumber = random.Next(1000, 9999); // You can choose the range for random numbers
                        newFilePath = $"{filenameWithoutExt.Substring(0, filenameWithoutExt.Length - 9)}_{randomNumber}.bmp.lzma";
                    }
                    while (MainWindow.catalog.Any(catalog => catalog.File.Equals(newFilePath, StringComparison.OrdinalIgnoreCase)));

                    return newFilePath;
                }

                if (currentSheet != null && graphics != null && currentSpriteIndex > 0) // Make sure the sheet has sprites
                {
                    string fileName = MainWindow._assetsPath;
                    LZMA.ExportLzmaFile(currentSheet, ref fileName);
                    bool catalogExists = MainWindow.catalog.Any(catalog => catalog.File.Equals(fileName, StringComparison.OrdinalIgnoreCase));

                    if (catalogExists)
                    {
                        string uniqueFilePath = GenerateUniqueFilePath(MainWindow._assetsPath + "\\" + fileName);
                        File.Copy(MainWindow._assetsPath + "\\" + fileName, MainWindow._assetsPath + "\\" + uniqueFilePath);
                        fileName = uniqueFilePath;
                    }
                    catalogs.Add(new MainWindow.Catalog
                    {
                        Type = "sprite",
                        File = fileName,
                        SpriteType = spriteType,
                        FirstSpriteid = importSprCounter - currentSpriteIndex,
                        LastSpriteid = importSprCounter - 1,
                        Area = 0
                    });
                    sheetNumber++;
                }

                if (currentSheet != null)
                {
                    currentSheet.Dispose();
                    currentSheet = null;
                }
                if (graphics != null)
                {
                    graphics.Dispose();
                    graphics = null;
                }
                currentSpriteIndex = 0; // Reset the current sprite index for the next sheet
            };

            foreach (var spriteInfo in spriteInfos)
            {
                if (currentSheet == null || currentSpriteIndex >= spritesPerSheet)
                {
                    finalizeSheet(); // Finalize the current sheet before starting a new one

                    currentSheet = new System.Drawing.Bitmap(384, 384, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    graphics = System.Drawing.Graphics.FromImage(currentSheet);
                    graphics.Clear(System.Drawing.Color.FromArgb(0, 255, 0, 255));
                }

                int x = (currentSpriteIndex % spritesPerRow) * spriteSize.Width;
                int y = (currentSpriteIndex / spritesPerRow) * spriteSize.Height;
                
                graphics.DrawImage(System.Drawing.Image.FromStream(spriteInfo.Stream), x, y, spriteSize.Width, spriteSize.Height);
                MainWindow.SprLists[importSprCounter] = spriteInfo.Stream;
                MainWindow.AllSprList.Add(new ShowList() { Id = (uint)importSprCounter });
                importSprIdList[(uint)spriteInfo.OriginalIndex] = (uint)importSprCounter;
                importSprCounter++;
                currentSpriteIndex++;
            }

            // Handle any remaining sprites in the last sheet
            finalizeSheet();
            CollectionViewSource.GetDefaultView(SprListView.ItemsSource).Refresh();

            if (currentSheet != null) currentSheet.Dispose();
            if (graphics != null) graphics.Dispose();

            return catalogs;
        }

        private void ImportObject_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Appearances appearances = new Appearances();
            importSprCounter = MainWindow.AllSprList.Count;
            importSprIdList.Clear();
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "assets editor container (*.aec)|*.aec" // Add appropriate filter here
            };

            if (openFileDialog.ShowDialog() == true)
            {
                using (FileStream fileStream = new FileStream(openFileDialog.FileName, FileMode.Open, FileAccess.Read))
                {
                    appearances = Appearances.Parser.ParseFrom(fileStream);
                    var spriteStreams = new List<MemoryStream>();
                    foreach (Appearance appearance in appearances.Outfit)
                    {
                        foreach (var spr in appearance.SpriteData)
                        {
                            spriteStreams.Add(new MemoryStream(spr.ToByteArray()));
                            importSprIdList.Add((uint)importSprIdList.Count, 0);
                        }
                    }
                    foreach (Appearance appearance in appearances.Object)
                    {
                        foreach (var spr in appearance.SpriteData)
                        {
                            spriteStreams.Add(new MemoryStream(spr.ToByteArray()));
                            importSprIdList.Add((uint)importSprIdList.Count, 0);
                        }
                    }
                    foreach (Appearance appearance in appearances.Effect)
                    {
                        foreach (var spr in appearance.SpriteData)
                        {
                            spriteStreams.Add(new MemoryStream(spr.ToByteArray()));
                            importSprIdList.Add((uint)importSprIdList.Count, 0);
                        }
                    }
                    foreach (Appearance appearance in appearances.Missile)
                    {
                        foreach (var spr in appearance.SpriteData)
                        {
                            spriteStreams.Add(new MemoryStream(spr.ToByteArray()));
                            importSprIdList.Add((uint)importSprIdList.Count, 0);
                        }
                    }

                    var outputDirectory = Path.GetDirectoryName(openFileDialog.FileName);
                    List<MainWindow.Catalog> catalogList = CreateSpriteSheetsAndSaveAsPng(spriteStreams, outputDirectory);
                    int counter = 0;
                    foreach (Appearance appearance in appearances.Outfit)
                    {
                        for (int i = 0; i < appearance.FrameGroup.Count; i++)
                        {
                            for (int s = 0; s < appearance.FrameGroup[i].SpriteInfo.SpriteId.Count; s++)
                            {
                                appearance.FrameGroup[i].SpriteInfo.SpriteId[s] = importSprIdList[(uint)counter];
                                counter++;
                            }
                        }
                    }
                    
                    foreach (Appearance appearance in appearances.Object)
                    {
                        for (int i = 0; i < appearance.FrameGroup.Count; i++)
                        {
                            for (int s = 0; s < appearance.FrameGroup[i].SpriteInfo.SpriteId.Count; s++)
                            {
                                appearance.FrameGroup[i].SpriteInfo.SpriteId[s] = importSprIdList[(uint)counter];
                                counter++;
                            }
                        }
                    }
                    
                    foreach (Appearance appearance in appearances.Effect)
                    {
                        for (int i = 0; i < appearance.FrameGroup.Count; i++)
                        {
                            for (int s = 0; s < appearance.FrameGroup[i].SpriteInfo.SpriteId.Count; s++)
                            {
                                appearance.FrameGroup[i].SpriteInfo.SpriteId[s] = importSprIdList[(uint)counter];
                                counter++;
                            }
                        }
                    }
                    
                    foreach (Appearance appearance in appearances.Missile)
                    {
                        for (int i = 0; i < appearance.FrameGroup.Count; i++)
                        {
                            for (int s = 0; s < appearance.FrameGroup[i].SpriteInfo.SpriteId.Count; s++)
                            {
                                appearance.FrameGroup[i].SpriteInfo.SpriteId[s] = importSprIdList[(uint)counter];
                                counter++;
                            }
                        }
                    }

                    foreach (var catalog in catalogList)
                    {
                        MainWindow.catalog.Add(catalog);
                    }

                    foreach (Appearance appearance in appearances.Outfit)
                    {
                        appearance.SpriteData.Clear();
                        
                        if (!MainWindow.appearances.Outfit.Any(a => a.Id == appearance.Id) && appearance.Id > 100)
                        {
                            appearance.Id = (uint)appearance.Id;
                        }
                        else
                        {
                            appearance.Id = (uint)MainWindow.appearances.Outfit[^1].Id + 1;
                        }
                        MainWindow.appearances.Outfit.Add(appearance.Clone());
                        ThingsOutfit.Add(new ShowList() { Id = appearance.Id });
                    }

                    foreach (Appearance appearance in appearances.Object)
                    {
                        appearance.SpriteData.Clear();

                        if (!MainWindow.appearances.Object.Any(a => a.Id == appearance.Id) && appearance.Id > 100)
                        {
                            appearance.Id = (uint)appearance.Id;
                        } else
                        {
                            appearance.Id = (uint)MainWindow.appearances.Object[^1].Id + 1;
                        }

                        if (appearance.Flags.Market != null && appearance.Flags.Market.HasTradeAsObjectId)
                            appearance.Flags.Market.TradeAsObjectId = appearance.Id;

                        if (appearance.Flags.Market != null && appearance.Flags.Market.HasShowAsObjectId)
                            appearance.Flags.Market.ShowAsObjectId = appearance.Id;

                        if(appearance.Flags.Cyclopediaitem != null && appearance.Flags.Cyclopediaitem.HasCyclopediaType)
                            appearance.Flags.Cyclopediaitem.CyclopediaType = appearance.Id;

                        MainWindow.appearances.Object.Add(appearance.Clone());
                        ThingsItem.Add(new ShowList() { Id = appearance.Id });
                    }
                    
                    foreach (Appearance appearance in appearances.Effect)
                    {
                        appearance.SpriteData.Clear();
                        if (!MainWindow.appearances.Effect.Any(a => a.Id == appearance.Id) && appearance.Id > 100)
                        {
                            appearance.Id = (uint)appearance.Id;
                        }
                        else
                        {
                            appearance.Id = (uint)MainWindow.appearances.Effect[^1].Id + 1;
                        }
                        MainWindow.appearances.Effect.Add(appearance.Clone());
                        ThingsEffect.Add(new ShowList() { Id = appearance.Id });
                    }
                    
                    foreach (Appearance appearance in appearances.Missile)
                    {
                        appearance.SpriteData.Clear();
                        if (!MainWindow.appearances.Missile.Any(a => a.Id == appearance.Id) && appearance.Id > 100)
                        {
                            appearance.Id = (uint)appearance.Id;
                        }
                        else
                        {
                            appearance.Id = (uint)MainWindow.appearances.Missile[^1].Id + 1;
                        }
                        MainWindow.appearances.Missile.Add(appearance.Clone());
                        ThingsMissile.Add(new ShowList() { Id = appearance.Id });
                    }
                }
            }
        }

        private void AddExportObject_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            List<ShowList> selectedItems = ObjListView.SelectedItems.Cast<ShowList>().ToList();
            if (selectedItems.Any())
            {
                ObjListViewSelectedIndex.Value = (int)selectedItems.Last().Id;

                foreach (var item in selectedItems)
                {
                    Appearance appearance = new Appearance();
                    if (ObjectMenu.SelectedIndex == 0)
                    {
                        appearance = MainWindow.appearances.Outfit.FirstOrDefault(o => o.Id == item.Id).Clone();
                        if (!exportObjects.Outfit.Any(a => a.Id == appearance.Id))
                        {
                            exportObjects.Outfit.Add(appearance);
                            AddExportObjectCounter.Badge = int.Parse(AddExportObjectCounter.Badge.ToString() ?? "0") + 1;

                        }
                    }
                    else if (ObjectMenu.SelectedIndex == 1)
                    {
                        appearance = MainWindow.appearances.Object.FirstOrDefault(o => o.Id == item.Id).Clone();
                        if (!exportObjects.Object.Any(a => a.Id == appearance.Id))
                        {
                            exportObjects.Object.Add(appearance);
                            AddExportObjectCounter.Badge = int.Parse(AddExportObjectCounter.Badge.ToString() ?? "0") + 1;
                        }
                    }
                    else if (ObjectMenu.SelectedIndex == 2)
                    {
                        appearance = MainWindow.appearances.Effect.FirstOrDefault(o => o.Id == item.Id).Clone();
                        if (!exportObjects.Effect.Any(a => a.Id == appearance.Id))
                        {
                            exportObjects.Effect.Add(appearance);
                            AddExportObjectCounter.Badge = int.Parse(AddExportObjectCounter.Badge.ToString() ?? "0") + 1;
                        }
                        
                    }
                    else if (ObjectMenu.SelectedIndex == 3)
                    {
                        appearance = MainWindow.appearances.Missile.FirstOrDefault(o => o.Id == item.Id).Clone();
                        if (!exportObjects.Missile.Any(a => a.Id == appearance.Id))
                        {
                            exportObjects.Missile.Add(appearance);
                            AddExportObjectCounter.Badge = int.Parse(AddExportObjectCounter.Badge.ToString() ?? "0") + 1;
                        }
                        
                    }

                    for (int i = 0; i < appearance.FrameGroup.Count; i++)
                    {
                        for (int s = 0; s < appearance.FrameGroup[i].SpriteInfo.SpriteId.Count; s++)
                        {
                            ByteString sprData = ByteString.CopyFrom(MainWindow.getSpriteStream((int)appearance.FrameGroup[i].SpriteInfo.SpriteId[s]).ToArray());
                            appearance.SpriteData.Add(sprData);
                            appearance.FrameGroup[i].SpriteInfo.SpriteId[s] = exportSprCounter;
                            exportSprCounter++;
                        }
                    }
                }
            }
        }

        private void ExportObject_PreviewMouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            if(int.Parse(AddExportObjectCounter.Badge.ToString() ?? "0") == 0)
            {
                StatusBar.MessageQueue.Enqueue($"Export list is empty.", null, null, null, false, true, TimeSpan.FromSeconds(2));
                return;
            }    

            System.Windows.Forms.FolderBrowserDialog exportPath = new System.Windows.Forms.FolderBrowserDialog();
            if (exportPath.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string fullPath = Path.Combine(exportPath.SelectedPath, "Appearances.aec");
                var output = File.Create(fullPath);
                exportObjects.WriteTo(output);
                output.Close();
                AddExportObjectCounter.Badge = 0;
                exportSprCounter = 0;
                exportObjects = new Appearances();
                StatusBar.MessageQueue.Enqueue($"Successfully exported objects.", null, null, null, false, true, TimeSpan.FromSeconds(2));
            }
        }

        private void AddExportObject_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            AddExportObjectCounter.Badge = 0;
            exportSprCounter = 0;
            exportObjects = new Appearances();
        }

        private void A_FlagIdCheck_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            uint newId = (uint)A_FlagId.Value;

            if (ObjectMenu.SelectedIndex == 0)
            {
                if (!MainWindow.appearances.Outfit.Any(a => a.Id == newId))
                {
                    CurrentObjectAppearance.Id = newId;
                    StatusBar.MessageQueue.Enqueue($"Valid Id.", null, null, null, false, true, TimeSpan.FromSeconds(2));
                }
                else
                    StatusBar.MessageQueue.Enqueue($"Invalid Id, make sure the Id is unique.", null, null, null, false, true, TimeSpan.FromSeconds(2));
            }
            else if (ObjectMenu.SelectedIndex == 1)
            {
                if (!MainWindow.appearances.Object.Any(a => a.Id == newId) && newId > 100)
                {
                    CurrentObjectAppearance.Id = newId;
                    StatusBar.MessageQueue.Enqueue($"Valid Id.", null, null, null, false, true, TimeSpan.FromSeconds(2));
                }
                else
                    StatusBar.MessageQueue.Enqueue($"Invalid Id, make sure the Id is unique.", null, null, null, false, true, TimeSpan.FromSeconds(2));
            }
            else if (ObjectMenu.SelectedIndex == 2)
            {
                if (!MainWindow.appearances.Effect.Any(a => a.Id == newId))
                {
                    CurrentObjectAppearance.Id = newId;
                    StatusBar.MessageQueue.Enqueue($"Valid Id.", null, null, null, false, true, TimeSpan.FromSeconds(2));
                }
                else
                    StatusBar.MessageQueue.Enqueue($"Invalid Id, make sure the Id is unique.", null, null, null, false, true, TimeSpan.FromSeconds(2));

            }
            else if (ObjectMenu.SelectedIndex == 3)
            {
                if (!MainWindow.appearances.Missile.Any(a => a.Id == newId))
                {
                    CurrentObjectAppearance.Id = newId;
                    StatusBar.MessageQueue.Enqueue($"Valid Id.", null, null, null, false, true, TimeSpan.FromSeconds(2));
                }
                else
                    StatusBar.MessageQueue.Enqueue($"Invalid Id, make sure the Id is unique.", null, null, null, false, true, TimeSpan.FromSeconds(2));
            }
        }


        private void StartSpriteAnimation(Image imageControl, TimeSpan frameRate, ShowList showList, IEnumerable<BitmapImage> imageFrames)
        {
            if (imageControl == null) throw new ArgumentNullException(nameof(imageControl));

            var animation = new ObjectAnimationUsingKeyFrames();
            TimeSpan currentTime = TimeSpan.Zero;

            foreach (BitmapImage imageFrame in imageFrames)
            {
                var keyFrame = new DiscreteObjectKeyFrame(imageFrame, currentTime);
                animation.KeyFrames.Add(keyFrame);
                currentTime += frameRate;
            }

            Storyboard.SetTarget(animation, imageControl);
            Storyboard.SetTargetProperty(animation, new PropertyPath(Image.SourceProperty));

            var storyboard = new Storyboard();
            storyboard.Children.Add(animation);
            storyboard.RepeatBehavior = RepeatBehavior.Forever;
            storyboard.Begin();
            showList.Storyboard = storyboard;
        }

        private void AnimateSelectedListItem(ShowList showList)
        {
            // Find the ListViewItem for the selected item
            var listViewItem = ObjListView.ItemContainerGenerator.ContainerFromItem(showList) as ListViewItem;
            if (listViewItem != null)
            {
                // Find the Image control within the ListViewItem
                var imageControl = Utils.FindVisualChild<Image>(listViewItem);
                if (imageControl != null)
                {
                    List<BitmapImage> imageFrames = new List<BitmapImage>();

                    Appearance appearance = null;

                    if (ObjectMenu.SelectedIndex == 0)
                        appearance = MainWindow.appearances.Outfit.FirstOrDefault(o => o.Id == showList.Id);
                    else if (ObjectMenu.SelectedIndex == 1)
                        appearance = MainWindow.appearances.Object.FirstOrDefault(o => o.Id == showList.Id);
                    else if (ObjectMenu.SelectedIndex == 2)
                        appearance = MainWindow.appearances.Effect.FirstOrDefault(o => o.Id == showList.Id);
                    else if (ObjectMenu.SelectedIndex == 3)
                        appearance = MainWindow.appearances.Missile.FirstOrDefault(o => o.Id == showList.Id);

                    TimeSpan frameRate = TimeSpan.FromMilliseconds(200);

                    try
                    {
                        for (int i = 0; i < appearance.FrameGroup[0].SpriteInfo.SpriteId.Count; i++)
                        {
                            int index = GetSpriteIndex(appearance.FrameGroup[0], 0, (ObjectMenu.SelectedIndex == 0 || ObjectMenu.SelectedIndex == 2) ? (int)Math.Min(2, appearance.FrameGroup[0].SpriteInfo.PatternWidth - 1) : 0, ObjectMenu.SelectedIndex == 2 ? (int)Math.Min(1, appearance.FrameGroup[0].SpriteInfo.PatternHeight - 1) : 0, 0, i);
                            BitmapImage imageFrame = Utils.BitmapToBitmapImage(MainWindow.getSpriteStream((int)appearance.FrameGroup[0].SpriteInfo.SpriteId[index]));
                            imageFrames.Add(imageFrame);
                        }
                    }
                    catch
                    {
                        MainWindow.Log("Error animation for sprite " + appearance.Id + ", crash prevented.");
                    }

                    StartSpriteAnimation(imageControl, frameRate, showList, imageFrames);
                }
            }
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.Show();
        }

        private void EditSprite_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem) return;
            if (menuItem.DataContext is not ShowList showList) return;
            
            SprEditor sprEditor = new SprEditor(this);
            SprEditor.CustomSheetsList.Clear();
            sprEditor.Show();
            sprEditor.OpenForSpriteId((int)showList.Id);
        }

        private void SprApplyForAll_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.Animation == null) return;

            var minDuration = (uint)SprPhaseMin.Value;
            var maxDuration = (uint)SprPhaseMax.Value;
            var animationSpritePhases = CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo
                .Animation.SpritePhase;
            
            foreach (var spritePhase in animationSpritePhases)
            {
                spritePhase.DurationMin = minDuration;
                spritePhase.DurationMax = maxDuration;
            }
            
            StatusBar.MessageQueue.Enqueue($"Successfully applied to {animationSpritePhases.Count} frames.", null, null, null, false, true, TimeSpan.FromSeconds(3));
        }

        private void SpriteListHelp_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            const string message =
                "You can drag and drop a single or multiple sprites at once onto a Texture. \n" +
                "Normal dragging - treats sprites as elements of the current frame. \n" +
                "Ctrl dragging - treats sprites as a sequence of frames.";
            MessageBox.Show(message, "Sprite List Help", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void ShowLogger(object sender, RoutedEventArgs e)
        {
            MainWindow.logView.Show();
        }

        private void SetObjectTransparency(Appearance appearance, byte baseAlpha)
        {
            var existingEntries = transparentSheets.Where(ct => ct.ObjectType == appearance.AppearanceType && ct.ObjectId == appearance.Id).ToList();

            bool add = false;
            if (existingEntries.Any())
            {
                if (baseAlpha != existingEntries.First().BaseAlpha)
                    add = true;

                foreach (var existing in existingEntries)
                {
                    transparentSheets.Remove(existing);
                }
            }
            else
            {
                add = true;
            }

            if (add)
            {
                Dictionary<MainWindow.Catalog, List<uint>> catalogSpriteIdMap = new Dictionary<MainWindow.Catalog, List<uint>>();
                foreach (var frameGroup in appearance.FrameGroup)
                {
                    foreach (uint spriteId in frameGroup.SpriteInfo.SpriteId)
                    {
                        MainWindow.Catalog catalog = GetSheetData(spriteId);

                        if (catalog != null)
                        {
                            if (!catalogSpriteIdMap.ContainsKey(catalog))
                            {
                                catalogSpriteIdMap[catalog] = new List<uint>();
                            }
                            catalogSpriteIdMap[catalog].Add(spriteId);
                        }
                        else
                        {
                            MainWindow.Log("Catalog not found for sprite ID " + spriteId + ".");
                        }
                    }
                }

                foreach (var kvp in catalogSpriteIdMap)
                {
                    MainWindow.Catalog catalog = kvp.Key;
                    List<uint> spriteIds = kvp.Value;

                    CatalogTransparency modification = new CatalogTransparency()
                    {
                        Catalog = catalog,
                        SpriteIds = spriteIds,
                        AlphaValue = (appearance.Flags.Transparencylevel != null && appearance.Flags.Transparencylevel.HasLevel) ? (byte)appearance.Flags.Transparencylevel.Level : (byte)255,
                        BaseAlpha = baseAlpha,
                        ObjectType = appearance.AppearanceType,
                        ObjectId = appearance.Id
                    };

                    transparentSheets.Add(modification);
                }

            }
        }

        private void ProcessTransparentSheets()
        {
            Dictionary<MainWindow.Catalog, Dictionary<uint, byte>> catalogModifications = new Dictionary<MainWindow.Catalog, Dictionary<uint, byte>>();

            foreach (var modification in transparentSheets)
            {
                MainWindow.Catalog catalog = modification.Catalog;

                if (!catalogModifications.ContainsKey(catalog))
                {
                    catalogModifications[catalog] = new Dictionary<uint, byte>();
                }

                foreach (uint spriteId in modification.SpriteIds)
                {
                    catalogModifications[catalog][spriteId] = modification.AlphaValue;
                }
            }

            foreach (var catalogEntry in catalogModifications)
            {
                MainWindow.Catalog catalog = catalogEntry.Key;
                Dictionary<uint, byte> spriteAlphaValues = catalogEntry.Value;

                string _sprPath = String.Format("{0}{1}", MainWindow._assetsPath, catalog.File);

                System.Drawing.Bitmap bmpImage = LZMA.DecompressFileLZMA(_sprPath);

                ChangeSpritesAlpha(bmpImage, spriteAlphaValues, catalog.SpriteType, catalog.FirstSpriteid);

                string dirPath = MainWindow._assetsPath;
                LZMA.ExportLzmaFile(bmpImage, ref dirPath);

                MainWindow.Catalog CurrentCatalog = MainWindow.catalog.Find(x => x.File == catalog.File);
                CurrentCatalog.File = dirPath;
                
                bmpImage.Dispose();
            }
            MainWindow.Log("All sprite modifications have been processed.");
        }

        public MainWindow.Catalog GetSheetData(uint sprId)
        {
            foreach (MainWindow.Catalog sheet in MainWindow.catalog)
            {
                if (sheet.FirstSpriteid <= sprId && sheet.LastSpriteid >= sprId)
                    return sheet;
            }

            return null;
        }

        public void ChangeSpritesAlpha(System.Drawing.Bitmap bmpImage, Dictionary<uint, byte> spriteAlphaValues, int spriteType, int firstSpriteId)
        {
            int spriteWidth, spriteHeight, spritesPerRow, spritesPerColumn;

            switch (spriteType)
            {
                case 0:
                    spriteWidth = 32;
                    spriteHeight = 32;
                    spritesPerRow = 12;
                    spritesPerColumn = 12;
                    break;
                case 1:
                    spriteWidth = 32;
                    spriteHeight = 64;
                    spritesPerRow = 12;
                    spritesPerColumn = 6;
                    break;
                case 2:
                    spriteWidth = 64;
                    spriteHeight = 32;
                    spritesPerRow = 6;
                    spritesPerColumn = 12;
                    break;
                case 3:
                    spriteWidth = 64;
                    spriteHeight = 64;
                    spritesPerRow = 6;
                    spritesPerColumn = 6;
                    break;
                default:
                    throw new ArgumentException("Invalid sprite type.");
            }

            System.Drawing.Bitmap bmpWithAlpha = ConvertToFormat32bppArgb(bmpImage);

            System.Drawing.Imaging.BitmapData bmpData = bmpWithAlpha.LockBits(
                new System.Drawing.Rectangle(0, 0, bmpWithAlpha.Width, bmpWithAlpha.Height),
                System.Drawing.Imaging.ImageLockMode.ReadWrite,
                bmpWithAlpha.PixelFormat
            );

            int bytesPerPixel = System.Drawing.Image.GetPixelFormatSize(bmpWithAlpha.PixelFormat) / 8;
            int height = bmpWithAlpha.Height;
            int stride = bmpData.Stride;
            byte[] pixels = new byte[stride * height];

            Marshal.Copy(bmpData.Scan0, pixels, 0, pixels.Length);

            foreach (var entry in spriteAlphaValues)
            {
                uint spriteId = entry.Key;
                byte newAlpha = entry.Value;

                int spriteIndex = (int)(spriteId - firstSpriteId);
                if (spriteIndex < 0 || spriteIndex >= spritesPerRow * spritesPerColumn)
                {
                    continue;
                }

                int row = spriteIndex / spritesPerRow;
                int column = spriteIndex % spritesPerRow;

                System.Drawing.Rectangle spriteRect = new System.Drawing.Rectangle(
                    column * spriteWidth,
                    row * spriteHeight,
                    spriteWidth,
                    spriteHeight
                );

                ChangeAlphaInRegion(pixels, stride, bytesPerPixel, spriteRect, newAlpha);
            }

            Marshal.Copy(pixels, 0, bmpData.Scan0, pixels.Length);

            bmpWithAlpha.UnlockBits(bmpData);

            if (bmpImage != bmpWithAlpha)
            {
                bmpImage.Dispose();
                bmpImage = bmpWithAlpha;
            }
        }
        private void ChangeAlphaInRegion(byte[] pixels, int stride, int bytesPerPixel, System.Drawing.Rectangle region, byte newAlpha)
        {
            for (int y = region.Top; y < region.Bottom; y++)
            {
                int yPos = y * stride;
                for (int x = region.Left; x < region.Right; x++)
                {
                    int position = yPos + x * bytesPerPixel;

                    byte blue = pixels[position];
                    byte green = pixels[position + 1];
                    byte red = pixels[position + 2];
                    byte alpha = pixels[position + 3];
                    if (!(red == 255 && green == 0 && blue == 255) && !(red == 0 && green == 0 && blue == 0 && alpha == 0))
                    {
                        pixels[position + 3] = newAlpha;
                    }
                }
            }
        }

        private System.Drawing.Bitmap ConvertToFormat32bppArgb(System.Drawing.Bitmap bmp)
        {
            if (bmp.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb)
                return bmp;

            System.Drawing.Bitmap newBitmap = new System.Drawing.Bitmap(bmp.Width, bmp.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(newBitmap))
            {
                g.DrawImage(bmp, 0, 0, bmp.Width, bmp.Height);
            }
            return newBitmap;
        }
    }
}
