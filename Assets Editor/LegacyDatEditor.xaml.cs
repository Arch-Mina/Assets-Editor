using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Tibia.Protobuf.Appearances;

namespace Assets_Editor
{
    /// <summary>
    /// Interaction logic for LegacyDatEditor.xaml
    /// </summary>
    public partial class LegacyDatEditor : Window
    {
        public ObservableCollection<ShowList> ThingsOutfit = [];
        public ObservableCollection<ShowList> ThingsItem = [];
        public ObservableCollection<ShowList> ThingsEffect = [];
        public ObservableCollection<ShowList> ThingsMissile = [];
        public Appearance CurrentObjectAppearance;
        public Appearance ReplaceObjectAppearance;
        public AppearanceFlags CurrentFlags = null;
        private int CurrentSprDir = 2;
        private bool isPageLoaded = false;
        private bool isObjectLoaded = false;
        private bool isUpdatingFrame = false;
        private VersionInfo loadedVersion;

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Application.Current.Shutdown();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            isPageLoaded = true;
        }

        public LegacyDatEditor()
        {
            InitializeComponent();

            // set current theme
            DarkModeToggle.IsChecked = MainWindow.IsDarkModeSet();

            A_FlagAutomapColorPicker.AvailableColors.Clear();
            for (int x = 0; x <= 215; x++)
            {
                Color myRgbColor = Utils.Get8Bit(x);
                A_FlagAutomapColorPicker.AvailableColors.Add(new Xceed.Wpf.Toolkit.ColorItem(Color.FromRgb(myRgbColor.R, myRgbColor.G, myRgbColor.B), x.ToString()));
            }
            ObservableCollection<Xceed.Wpf.Toolkit.ColorItem> outfitColors = [];
            SprLayerHeadPicker.AvailableColors = outfitColors;
            SprLayerHeadPicker.AvailableColors.Clear();

            for (int x = 0; x <= 132; x++)
            {
                System.Drawing.Color myRgbColor = Utils.GetOutfitColor(x);
                outfitColors.Add(new Xceed.Wpf.Toolkit.ColorItem(Color.FromRgb(myRgbColor.R, myRgbColor.G, myRgbColor.B), x.ToString()));
            }
            SprLayerHeadPicker.AvailableColors = outfitColors;
            SprLayerBodyPicker.AvailableColors = outfitColors;
            SprLayerLegsPicker.AvailableColors = outfitColors;
            SprLayerFeetPicker.AvailableColors = outfitColors;
        }
        private void DarkModeToggle_Checked(object sender, RoutedEventArgs e)
        {
            MainWindow.SetCurrentTheme(DarkModeToggle.IsChecked ?? false);
        }
        public LegacyDatEditor(Appearances appearances, VersionInfo versionInfo)
            : this()
        {
            foreach (var outfit in appearances.Outfit)
            {
                ThingsOutfit.Add(new ShowList() { Id = outfit.Id });
            }
            foreach (var item in appearances.Object)
            {
                ThingsItem.Add(new ShowList() { Id = item.Id });
            }
            foreach (var effect in appearances.Effect)
            {
                ThingsEffect.Add(new ShowList() { Id = effect.Id });
            }
            foreach (var missile in appearances.Missile)
            {
                ThingsMissile.Add(new ShowList() { Id = missile.Id });
            }
            SprListView.ItemsSource = MainWindow.AllSprList;
            UpdateShowList(ObjectMenu.SelectedIndex);

            loadedVersion = versionInfo;
            SetThingViewLayout();
        }

        private void UpdateFlagVisibility(Control control, string flagName) {
            control.Visibility = loadedVersion.HasFlag(flagName) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SetThingViewLayout() {
            // ground + friction
            UpdateFlagVisibility(A_FlagGround, "Ground");
            UpdateFlagVisibility(A_FlagGroundSpeed, "Ground");

            UpdateFlagVisibility(A_FlagClip, "Clip");
            UpdateFlagVisibility(A_FlagTop, "Top");
            UpdateFlagVisibility(A_FlagBottom, "Bottom");
            UpdateFlagVisibility(A_FlagContainer, "Container");
            UpdateFlagVisibility(A_FlagCumulative, "Stackable");
            UpdateFlagVisibility(A_FlagUsable, "Usable");
            UpdateFlagVisibility(A_FlagForceuse, "Forceuse");
            UpdateFlagVisibility(A_FlagMultiuse, "Multiuse");
            UpdateFlagVisibility(A_FlagWrite, "Writeable");
            UpdateFlagVisibility(A_FlagMaxTextLength, "Writeable");
            UpdateFlagVisibility(A_FlagWriteOnce, "WriteableOnce");
            UpdateFlagVisibility(A_FlagMaxTextLengthOnce, "WriteableOnce");
            UpdateFlagVisibility(A_FlagLiquidpool, "LiquidPool");
            UpdateFlagVisibility(A_FlagLiquidcontainer, "LiquidContainer");
            UpdateFlagVisibility(A_FlagUnpass, "Impassable");
            UpdateFlagVisibility(A_FlagUnmove, "Unmovable");
            UpdateFlagVisibility(A_FlagUnsight, "BlocksSight");
            UpdateFlagVisibility(A_FlagAvoid, "BlocksPathfinding");
            UpdateFlagVisibility(A_FlagNoMoveAnimation, "NoMovementAnimation");
            UpdateFlagVisibility(A_FlagTake, "Pickupable");
            UpdateFlagVisibility(A_FlagHang, "Hangable");
            UpdateFlagVisibility(A_FlagHookSouth, "HooksSouth");
            UpdateFlagVisibility(A_FlagHookEast, "HooksEast");
            UpdateFlagVisibility(A_FlagRotate, "Rotateable");

            // light source fields
            UpdateFlagVisibility(A_FlagLight, "LightSource");
            UpdateFlagVisibility(A_FlagLightBrightness, "LightSource");
            UpdateFlagVisibility(A_FlagLightColor, "LightSource");

            UpdateFlagVisibility(A_FlagDontHide, "AlwaysSeen");
            UpdateFlagVisibility(A_FlagTranslucent, "Translucent");

            // versioned flag
            FlagInfo? displaced = loadedVersion.GetFlagInfo("Displaced");
            if (displaced != null) {
                A_FlagShift.Visibility = Visibility.Visible;

                switch(displaced.Version) {
                    case 2:
                        // 1098 standard - offsets configurable
                        A_StandardShiftCoords.Visibility = Visibility.Visible;
                        A_ExtendedShiftCoords.Visibility = Visibility.Collapsed;
                        break;
                    case 3:
                        // RD - extra parameters
                        A_StandardShiftCoords.Visibility = Visibility.Visible;
                        A_ExtendedShiftCoords.Visibility = Visibility.Visible;
                        break;
                    default:
                        // old version - not configurable
                        A_StandardShiftCoords.Visibility = Visibility.Collapsed;
                        A_ExtendedShiftCoords.Visibility = Visibility.Collapsed;
                        break;
                }
            } else {
                A_FlagShift.Visibility = Visibility.Collapsed;
                A_StandardShiftCoords.Visibility = Visibility.Collapsed;
                A_ExtendedShiftCoords.Visibility = Visibility.Collapsed;
            }

            // item height
            UpdateFlagVisibility(A_FlagHeight, "Elevated");
            UpdateFlagVisibility(A_FlagElevation, "Elevated");

            UpdateFlagVisibility(A_FlagLyingObject, "LyingObject");
            UpdateFlagVisibility(A_FlagAnimateAlways, "AlwaysAnimated");

            // minimap
            UpdateFlagVisibility(A_FlagAutomap, "MinimapColor");
            UpdateFlagVisibility(A_FlagAutomapColor, "MinimapColor");

            UpdateFlagVisibility(A_FlagFullGround, "FullTile");

            // lenshelp
            UpdateFlagVisibility(A_FlagLenshelp, "HelpInfo");
            UpdateFlagVisibility(A_FlagLenshelpId, "HelpInfo");

            UpdateFlagVisibility(A_FlagIgnoreLook, "Lookthrough");

            // hotkey equip slot
            UpdateFlagVisibility(A_FlagClothes, "Clothes");
            UpdateFlagVisibility(A_FlagClothesSlot, "Clothes");

            // default action
            UpdateFlagVisibility(A_FlagDefaultAction, "DefaultAction");
            UpdateFlagVisibility(A_FlagDefaultActionType, "DefaultAction");

            // market
            UpdateFlagVisibility(A_FlagMarket, "Market");
            UpdateFlagVisibility(A_FlagMarketCategory, "Market");
            UpdateFlagVisibility(A_FlagMarketTrade, "Market");
            UpdateFlagVisibility(A_FlagMarketShow, "Market");
            UpdateFlagVisibility(A_FlagMarketProfession, "Market");
            UpdateFlagVisibility(A_FlagMarketlevel, "Market");
            UpdateFlagVisibility(A_FlagName, "Market");
            UpdateFlagVisibility(A_FlagDescription, "Market");

            UpdateFlagVisibility(A_FlagWrap, "Wrappable");
            UpdateFlagVisibility(A_FlagUnwrap, "UnWrappable");
            UpdateFlagVisibility(A_FlagTopeffect, "TopEffect");

            // flag "rune charges visible" - dat structure 7.8 - 8.54
            UpdateFlagVisibility(A_FlagWearout, "ShowCharges");

            // otc wings
            FlagInfo? otcWings = loadedVersion.GetFlagInfo("WingsOffset");
            A_FlagWingsCoords.Visibility = otcWings != null ? Visibility.Visible : Visibility.Collapsed;
        }

        public void UpdateShowList(int selection)
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
                for (int i = 0; i < SprListView.Items.Count; i++)
                {
                    if (i >= offset && i < Math.Min(offset + 20, SprListView.Items.Count) && MainWindow.SprLists.ContainsKey(i))
                        MainWindow.AllSprList[i].Image = Utils.ResizeForUI(MainWindow.MainSprStorage.getSpriteStream((uint)i));
                    else
                        MainWindow.AllSprList[i].Image = null;
                }
            }
        }
        private void SprListView_DragSpr(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                ShowList data = (ShowList)SprListView.SelectedItem;
                if (data != null && data.Image != null)
                {
                    SprListView.SelectedItem = data;
                    DragDrop.DoDragDrop(SprListView, data, DragDropEffects.Copy);
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
                    Appearance appearance = null;
                    ShowList item = (ShowList)ObjListView.Items[i];
                    if (i >= offset && i < Math.Min(offset + 20, ObjListView.Items.Count))
                    {
                        if (ObjectMenu.SelectedIndex == 0)
                            appearance = MainWindow.appearances.Outfit[i];
                        else if (ObjectMenu.SelectedIndex == 1)
                            appearance = MainWindow.appearances.Object[i];
                        else if (ObjectMenu.SelectedIndex == 2)
                            appearance = MainWindow.appearances.Effect[i];
                        else if (ObjectMenu.SelectedIndex == 3)
                            appearance = MainWindow.appearances.Missile[i];

                        AnimateSelectedListItem(item);
                    }
                    else
                    {
                        item.StopAnimation();
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
                    SprGroupSlider.IsEnabled = true;
                    A_SprGroups.IsEnabled = true;
                }
                else
                {
                    SprUpArrow.Visibility = Visibility.Hidden;
                    SprDownArrow.Visibility = Visibility.Hidden;
                    SprLeftArrow.Visibility = Visibility.Hidden;
                    SprRightArrow.Visibility = Visibility.Hidden;
                    SprAddonSlider.IsEnabled = false;
                    SprGroupSlider.IsEnabled = false;
                    A_SprGroups.IsEnabled = false;
                }

            }
        }
        private void LoadSelectedObjectAppearances(Appearance ObjectAppearance)
        {
            CurrentObjectAppearance = ObjectAppearance.Clone();
            LoadCurrentObjectAppearances();
            isUpdatingFrame = true;
            try {
                SprGroupSlider.Value = 0;
                ChangeGroupType(0);
            } finally {
                isUpdatingFrame = false;
            }
        }

        private void ChangeGroupType(int group)
        {
            var spriteInfo = CurrentObjectAppearance.FrameGroup[group].SpriteInfo;

            isObjectLoaded = false;
            A_SprGroups.Value = CurrentObjectAppearance.FrameGroup.Count;
            A_SprWidth.Value = (int)spriteInfo.PatternWidth;
            A_SprHeight.Value = (int)spriteInfo.PatternHeight;
            A_SprSize.Value = (int)spriteInfo.PatternSize;
            A_SprLayers.Value = (int)spriteInfo.PatternLayers;
            A_SprPaternX.Value = (int)spriteInfo.PatternX;
            A_SprPaternY.Value = (int)spriteInfo.PatternY;
            A_SprPaternZ.Value = (int)spriteInfo.PatternZ;
            A_SprAnimation.Value = (int)spriteInfo.PatternFrames;
            CurrentSprDir = 2;
            ButtonProgressAssist.SetIsIndicatorVisible(SprUpArrow, false);
            ButtonProgressAssist.SetIsIndicatorVisible(SprRightArrow, false);
            ButtonProgressAssist.SetIsIndicatorVisible(SprDownArrow, true);
            ButtonProgressAssist.SetIsIndicatorVisible(SprLeftArrow, false);
            ForceSliderChange();
            SprFramesSlider.Maximum = (double)A_SprAnimation.Value - 1;
            if (A_SprAnimation.Value > 1)
            {
                SprDefaultPhase.IsEnabled = true;
                SprSynchronized.IsEnabled = true;
                SprPhaseMin.IsEnabled = true;
                SprPhaseMax.IsEnabled = true;
                SprLoopCount.IsEnabled = true;

                var animation = spriteInfo.Animation;
                SprDefaultPhase.Value = (int?)animation.DefaultStartPhase;
                SprSynchronized.IsChecked = animation.AnimationMode == ANIMATION_ANIMATION_MODE.AnimationSynchronized;
                SprPhaseMin.Value = (int?)animation.SpritePhase[0].DurationMin;
                SprPhaseMax.Value = (int?)animation.SpritePhase[0].DurationMax;
                SprLoopCount.Value = (int?)animation.LoopCount;
            }
            else
            {
                SprDefaultPhase.IsEnabled = false;
                SprSynchronized.IsEnabled = false;
                SprPhaseMin.Value = 0;
                SprPhaseMax.Value = 0;
                SprPhaseMin.IsEnabled = false;
                SprPhaseMax.IsEnabled = false;
                SprLoopCount.IsEnabled = false;
            }

            SprGroupType.Content = SprGroupSlider.Value == 0 ? "Idle" : "Walking";
            isObjectLoaded = true;
        }

        private void ForceSliderChange()
        {
            isUpdatingFrame = true;

            try {
                SprFramesSlider.Minimum = -1;
                SprFramesSlider.Value = -1;
            } finally {
                isUpdatingFrame = false;
            }

            SprFramesSlider.Minimum = 0;
        }

        private void LoadCurrentObjectAppearances()
        {
            var flags = CurrentObjectAppearance.Flags;
            A_FlagGround.IsChecked = flags.Bank != null;
            A_FlagGroundSpeed.Value = (flags.Bank != null) ? (int)flags.Bank.Waypoints : 0;
            A_FlagClip.IsChecked = flags.Clip;
            A_FlagBottom.IsChecked = flags.Bottom;
            A_FlagTop.IsChecked = flags.Top;
            A_FlagContainer.IsChecked = flags.Container;
            A_FlagCumulative.IsChecked = flags.Cumulative;
            A_FlagUsable.IsChecked = flags.Usable;
            A_FlagForceuse.IsChecked = flags.Forceuse;
            A_FlagMultiuse.IsChecked = flags.Multiuse;
            A_FlagWrite.IsChecked = flags.Write != null;
            A_FlagMaxTextLength.Value = (flags.Write != null && flags.Write.HasMaxTextLength) ? (int)flags.Write.MaxTextLength : 0;
            A_FlagWriteOnce.IsChecked = flags.WriteOnce != null;
            A_FlagMaxTextLengthOnce.Value = (flags.WriteOnce != null && flags.WriteOnce.HasMaxTextLengthOnce) ? (int)flags.WriteOnce.MaxTextLengthOnce : 0;
            A_FlagLiquidpool.IsChecked = flags.HasLiquidpool;
            A_FlagUnpass.IsChecked = flags.HasUnpass;
            A_FlagUnmove.IsChecked = flags.HasUnmove;
            A_FlagUnsight.IsChecked = flags.HasUnsight;
            A_FlagAvoid.IsChecked = flags.HasAvoid;
            A_FlagNoMoveAnimation.IsChecked = flags.HasNoMovementAnimation;
            A_FlagTake.IsChecked = flags.HasTake;
            A_FlagLiquidcontainer.IsChecked = flags.HasLiquidcontainer;
            A_FlagHang.IsChecked = flags.HasHang;
            A_FlagHookSouth.IsChecked = flags.HookSouth;
            A_FlagHookEast.IsChecked = flags.HookEast;
            A_FlagRotate.IsChecked = flags.HasRotate;
            A_FlagLight.IsChecked = flags.Light != null;
            A_FlagLightBrightness.Value = (flags.Light != null && flags.Light.HasBrightness) ? (int)flags.Light.Brightness : 0;
            A_FlagLightColor.Value = (flags.Light != null && flags.Light.HasColor) ? (int)flags.Light.Color : 0;
            A_FlagDontHide.IsChecked = flags.HasDontHide;
            A_FlagTranslucent.IsChecked = flags.HasTranslucent;
            A_FlagShift.IsChecked = flags.Shift != null;
            A_FlagShiftX.Value = (flags.Shift != null && flags.Shift.HasX) ? (int)flags.Shift.X : 0;
            A_FlagShiftY.Value = (flags.Shift != null && flags.Shift.HasY) ? (int)flags.Shift.Y : 0;
            A_FlagShiftA.Value = (flags.Shift != null && flags.Shift.HasA) ? (int)flags.Shift.A : 0;
            A_FlagShiftB.Value = (flags.Shift != null && flags.Shift.HasB) ? (int)flags.Shift.B : 0;
            A_FlagHeight.IsChecked = flags.Height != null;
            A_FlagElevation.Value = (flags.Height != null && flags.Height.HasElevation) ? (int)flags.Height.Elevation : 0;
            A_FlagLyingObject.IsChecked = flags.HasLyingObject;
            A_FlagAnimateAlways.IsChecked = flags.HasAnimateAlways;
            A_FlagAutomap.IsChecked = flags.Automap != null;
            A_FlagAutomapColor.Value = (flags.Automap != null && flags.Automap.HasColor) ? (int)flags.Automap.Color : 0;
            A_FlagLenshelp.IsChecked = flags.Lenshelp != null;

            // select from dropdown only when the argument has valid value
            if (flags.Lenshelp != null) {
                int lensHelpId = flags.Lenshelp.HasId ? (int)flags.Lenshelp.Id : -1;
                if (lensHelpId >= 1100) {
                    A_FlagLenshelpId.SelectedIndex = lensHelpId - 1100;
                } else {
                    A_FlagLenshelpId.SelectedIndex = -1;
                }
            }

            A_FlagFullGround.IsChecked = flags.HasFullbank;
            A_FlagIgnoreLook.IsChecked = flags.HasIgnoreLook;
            A_FlagClothes.IsChecked = (flags.Clothes != null && flags.Clothes.HasSlot) ? true : false;
            A_FlagClothesSlot.SelectedIndex = (flags.Clothes != null && flags.Clothes.HasSlot) ? (int)flags.Clothes.Slot : -1;
            A_FlagDefaultAction.IsChecked = flags.DefaultAction != null;
            A_FlagDefaultActionType.SelectedIndex = (flags.DefaultAction != null && flags.DefaultAction.HasAction) ? (int)flags.DefaultAction.Action : -1;
            A_FlagMarket.IsChecked = flags.Market != null;
            A_FlagMarketCategory.SelectedIndex = (flags.Market != null && flags.Market.HasCategory) ? (int)flags.Market.Category - 1 : -1;
            A_FlagMarketTrade.Value = (flags.Market != null && flags.Market.HasTradeAsObjectId) ? (int)flags.Market.TradeAsObjectId : 0;
            A_FlagMarketShow.Value = (flags.Market != null && flags.Market.HasShowAsObjectId) ? (int)flags.Market.ShowAsObjectId : 0;
            A_FlagMarketProfession.SelectedIndex = 0;
            if (flags.Market != null && flags.Market.HasVocation)
            {
                A_FlagMarketProfession.SelectedIndex = (int)flags.Market.Vocation;
            }
            A_FlagMarketlevel.Value = (flags.Market != null && flags.Market.HasMinimumLevel) ? (int)flags.Market.MinimumLevel : 0;
            A_FlagName.Text = CurrentObjectAppearance.HasName ? CurrentObjectAppearance.Name : null;
            A_FlagDescription.Text = CurrentObjectAppearance.HasDescription ? CurrentObjectAppearance.Description : null;
            A_FlagWrap.IsChecked = flags.HasWrap;
            A_FlagUnwrap.IsChecked = flags.HasUnwrap;
            A_FlagTopeffect.IsChecked = flags.HasTop;
            A_FlagWearout.IsChecked = flags.HasWearout;

            A_FlagWingsOffset.IsChecked = flags.WingsOffset != null;
            A_FlagWingsNorthX.Value = (flags.WingsOffset != null && flags.WingsOffset.HasNorthX) ? (int)flags.WingsOffset.NorthX : 0;
            A_FlagWingsNorthY.Value = (flags.WingsOffset != null && flags.WingsOffset.HasNorthY) ? (int)flags.WingsOffset.NorthY : 0;
            A_FlagWingsEastX.Value = (flags.WingsOffset != null && flags.WingsOffset.HasEastX) ? (int)flags.WingsOffset.EastX : 0;
            A_FlagWingsEastY.Value = (flags.WingsOffset != null && flags.WingsOffset.HasEastY) ? (int)flags.WingsOffset.EastY : 0;
            A_FlagWingsSouthX.Value = (flags.WingsOffset != null && flags.WingsOffset.HasSouthX) ? (int)flags.WingsOffset.SouthX : 0;
            A_FlagWingsSouthY.Value = (flags.WingsOffset != null && flags.WingsOffset.HasSouthY) ? (int)flags.WingsOffset.SouthY : 0;
            A_FlagWingsWestX.Value = (flags.WingsOffset != null && flags.WingsOffset.HasWestX) ? (int)flags.WingsOffset.WestX : 0;
            A_FlagWingsWestY.Value = (flags.WingsOffset != null && flags.WingsOffset.HasWestY) ? (int)flags.WingsOffset.WestY : 0;
            A_FullInfo.Text = CurrentObjectAppearance.ToString();
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
            Utils.SafeSetColor(A_FlagAutomapColor.Value, A_FlagAutomapColorPicker);
        }
        private void A_FlagLightColor_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            Utils.SafeSetColor(A_FlagLightColor.Value, A_FlagLightColorPicker);
        }
        private void A_FlagMarketProfession_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            A_FlagMarketProfession.SelectedIndex = -1;
        }
        private void Randomize_Click(object sender, RoutedEventArgs e)
        {
            Random rnd = new Random();
            SprLayerHeadPicker.SelectedColor = SprLayerHeadPicker.AvailableColors[rnd.Next(0, SprLayerHeadPicker.AvailableColors.Count)].Color;
            SprLayerBodyPicker.SelectedColor = SprLayerBodyPicker.AvailableColors[rnd.Next(0, SprLayerBodyPicker.AvailableColors.Count)].Color;
            SprLayerLegsPicker.SelectedColor = SprLayerLegsPicker.AvailableColors[rnd.Next(0, SprLayerLegsPicker.AvailableColors.Count)].Color;
            SprLayerFeetPicker.SelectedColor = SprLayerFeetPicker.AvailableColors[rnd.Next(0, SprLayerFeetPicker.AvailableColors.Count)].Color;
        }

        private void SprFramesSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isUpdatingFrame)
                return;

            InternalUpdateThingPreview();
        }

        private void InternalUpdateThingPreview()
        {
            try {
                var frameGroup = CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value];
                var spriteInfo = frameGroup.SpriteInfo;
                if (spriteInfo.PatternFrames > 1) {
                    SprPhaseMin.Value = (int)spriteInfo.Animation.SpritePhase[(int)SprFramesSlider.Value].DurationMin;
                    SprPhaseMax.Value = (int)spriteInfo.Animation.SpritePhase[(int)SprFramesSlider.Value].DurationMax;
                }

                SpriteViewerGrid.Children.Clear();
                SpriteViewerGrid.RowDefinitions.Clear();
                SpriteViewerGrid.ColumnDefinitions.Clear();
                int gridWidth = 1;
                int gridHeight = 1;
                if (CurrentObjectAppearance.AppearanceType == APPEARANCE_TYPE.AppearanceOutfit) {
                    gridWidth = (int)spriteInfo.PatternHeight;
                    gridHeight = (int)spriteInfo.PatternWidth;
                } else {
                    gridWidth = (int)(spriteInfo.PatternHeight * spriteInfo.PatternY);
                    gridHeight = (int)(spriteInfo.PatternWidth * spriteInfo.PatternX);
                }
                for (int i = 0; i < gridWidth; i++) {
                    RowDefinition rowDef = new RowDefinition();
                    rowDef.Height = new GridLength(32);
                    SpriteViewerGrid.RowDefinitions.Add(rowDef);
                }
                for (int i = 0; i < gridHeight; i++) {
                    ColumnDefinition colDef = new ColumnDefinition();
                    colDef.Width = new GridLength(32);
                    SpriteViewerGrid.ColumnDefinitions.Add(colDef);
                }

                if (CurrentObjectAppearance.AppearanceType == APPEARANCE_TYPE.AppearanceOutfit) {
                    if ((bool)SprBlendLayers.IsChecked == false) {
                        int counter = 1;
                        int layer = SprBlendLayer.IsChecked == true ? (int)spriteInfo.PatternLayers - 1 : 0;
                        int mount = SprMount.IsChecked == true ? (int)spriteInfo.PatternZ - 1 : 0;
                        int addon = spriteInfo.PatternY > 1 ? (int)SprAddonSlider.Value : 0;
                        for (int h = (int)spriteInfo.PatternHeight - 1; h >= 0; h--) {
                            for (int w = (int)spriteInfo.PatternWidth - 1; w >= 0; w--) {
                                int index = LegacyAppearance.GetSpriteIndex(frameGroup, w, h, layer, (int)Math.Min(CurrentSprDir, spriteInfo.PatternX - 1), addon, mount, (int)SprFramesSlider.Value);
                                int spriteId = (int)spriteInfo.SpriteId[index];
                                SetImageInGrid(SpriteViewerGrid, gridHeight, Utils.ResizeForUI(MainWindow.MainSprStorage.getSpriteStream((uint)spriteId)), counter, spriteId, index);
                                counter++;
                            }
                        }
                    } else {
                        int counter = 1;
                        for (int h = (int)spriteInfo.PatternHeight - 1; h >= 0; h--) {
                            for (int w = (int)spriteInfo.PatternWidth - 1; w >= 0; w--) {
                                int baseIndex = LegacyAppearance.GetSpriteIndex(frameGroup, w, h, 0, (int)Math.Min(CurrentSprDir, spriteInfo.PatternX - 1), 0, 0, (int)SprFramesSlider.Value);
                                int baseSpriteId = (int)spriteInfo.SpriteId[baseIndex];
                                System.Drawing.Bitmap baseBitmap = new(MainWindow.MainSprStorage.getSpriteStream((uint)baseSpriteId));
                                if (spriteInfo.PatternLayers > 1) {
                                    int baseLayerIndex = LegacyAppearance.GetSpriteIndex(frameGroup, w, h, 1, (int)Math.Min(CurrentSprDir, spriteInfo.PatternX - 1), 0, 0, (int)SprFramesSlider.Value);
                                    int baseLayerSpriteId = (int)spriteInfo.SpriteId[baseLayerIndex];
                                    System.Drawing.Bitmap baseLayerBitmap = new(MainWindow.MainSprStorage.getSpriteStream((uint)baseLayerSpriteId));
                                    Utils.ColorizeOutfit(
                                        baseLayerBitmap,
                                        baseBitmap,
                                        SprLayerHeadPicker.SelectedColor.Value,
                                        SprLayerBodyPicker.SelectedColor.Value,
                                        SprLayerLegsPicker.SelectedColor.Value,
                                        SprLayerFeetPicker.SelectedColor.Value
                                    );
                                }
                                using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(baseBitmap)) {
                                    g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;

                                    if ((bool)SprFullAddons.IsChecked) {
                                        for (int x = 1; x <= (int)SprAddonSlider.Maximum; x++) {
                                            int addonIndex = LegacyAppearance.GetSpriteIndex(frameGroup, w, h, 0, (int)Math.Min(CurrentSprDir, spriteInfo.PatternX - 1), x, 0, (int)SprFramesSlider.Value);
                                            int addonSpriteId = (int)spriteInfo.SpriteId[addonIndex];
                                            System.Drawing.Bitmap addonBitmap = new(MainWindow.MainSprStorage.getSpriteStream((uint)addonSpriteId));

                                            int addonLayerIndex = LegacyAppearance.GetSpriteIndex(frameGroup, w, h, 1, (int)Math.Min(CurrentSprDir, spriteInfo.PatternX - 1), x, 0, (int)SprFramesSlider.Value);
                                            int addonLayerSpriteId = (int)spriteInfo.SpriteId[addonLayerIndex];
                                            System.Drawing.Bitmap addonLayerBitmap = new(MainWindow.MainSprStorage.getSpriteStream((uint)addonLayerSpriteId));

                                            Utils.ColorizeOutfit(
                                                addonLayerBitmap,
                                                addonBitmap,
                                                SprLayerHeadPicker.SelectedColor.Value,
                                                SprLayerBodyPicker.SelectedColor.Value,
                                                SprLayerLegsPicker.SelectedColor.Value,
                                                SprLayerFeetPicker.SelectedColor.Value
                                            );
                                            g.DrawImage(addonBitmap, new System.Drawing.Point(0, 0));
                                        }
                                    }
                                }

                                MemoryStream memoryStream = new();
                                baseBitmap.Save(memoryStream, ImageFormat.Png);
                                SetImageInGrid(SpriteViewerGrid, gridHeight, Utils.ResizeForUI(memoryStream), counter, 0, 0);
                                counter++;
                            }
                        }
                    }
                } else {
                    int counter = 1;
                    int layer = SprBlendLayer.IsChecked == true ? (int)spriteInfo.PatternLayers - 1 : 0;
                    int mount = SprMount.IsChecked == true ? (int)spriteInfo.PatternZ - 1 : 0;
                    for (int ph = 0; ph < spriteInfo.PatternY; ph++) {
                        for (int pw = 0; pw < spriteInfo.PatternX; pw++) {
                            for (int h = (int)(spriteInfo.PatternHeight - 1); h >= 0; h--) {
                                for (int w = (int)(spriteInfo.PatternWidth - 1); w >= 0; w--) {
                                    int tileid = (int)(ph * gridHeight * spriteInfo.PatternHeight + (spriteInfo.PatternHeight - 1 - h) * gridHeight + (pw * spriteInfo.PatternWidth) + (spriteInfo.PatternWidth - 1 - w) + 1);
                                    int index = LegacyAppearance.GetSpriteIndex(frameGroup, w, h, layer, pw, ph, mount, (int)SprFramesSlider.Value);
                                    int spriteId = (int)spriteInfo.SpriteId[index];
                                    SetImageInGrid(SpriteViewerGrid, gridHeight, Utils.ResizeForUI(MainWindow.MainSprStorage.getSpriteStream((uint)spriteId)), tileid, spriteId, index);
                                    counter++;
                                }
                            }
                        }
                    }
                }
            } catch (Exception) {
                MainWindow.Log("Unable to view appearance id " + CurrentObjectAppearance.Id + ", invalid texture ids.");
            }
        }

        private void SprOutfitChanged(object sender, RoutedEventArgs e)
        {
            ForceSliderChange();
        }

        private void SprLayerPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (isPageLoaded)
                ForceSliderChange();
        }
        private void OutfitXml(object sender, RoutedEventArgs e)
        {
            int typeValue = (int)CurrentObjectAppearance.Id;
            int headValue = 0;
            int bodyValue = 0;
            int legsValue = 0;
            int feetValue = 0;
            int corpseValue = 0;

            foreach (var color in SprLayerHeadPicker.AvailableColors)
            {
                if (color.Color.Value.ToString() == SprLayerHeadPicker.SelectedColor.ToString())
                    headValue = int.Parse(color.Name);
                if (color.Color.Value.ToString() == SprLayerBodyPicker.SelectedColor.ToString())
                    bodyValue = int.Parse(color.Name);
                if (color.Color.Value.ToString() == SprLayerLegsPicker.SelectedColor.ToString())
                    legsValue = int.Parse(color.Name);
                if (color.Color.Value.ToString() == SprLayerFeetPicker.SelectedColor.ToString())
                    feetValue = int.Parse(color.Name);
            }

            string xml = $"<look type=\"{typeValue}\" head=\"{headValue}\" body=\"{bodyValue}\" legs=\"{legsValue}\" feet=\"{feetValue}\" corpse=\"{corpseValue}\"/>";
            Dispatcher.Invoke(() => {
                ClipboardManager.CopyText(xml, "xml", StatusBar);
            });
        }
        private void SetImageInGrid(Grid grid, int gridHeight, BitmapImage image, int id, int spriteId, int index)
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
                existingImage.Width = 32;
                existingImage.Height = 32;
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

        private void SprGroupSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ChangeGroupType((int)SprGroupSlider.Value);
        }


        private void ChangeDirection(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Button _dir = (Button)sender;

            CurrentSprDir = int.Parse(_dir.Uid);
            InternalUpdateThingPreview();
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
        private void Spr_Drop(object sender, DragEventArgs e)
        {
            Image img = e.Source as Image;
            ShowList data = (ShowList)e.Data.GetData(typeof(ShowList));
            img.Source = data.Image;
            img.ToolTip = data.Id.ToString();
            CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.SpriteId[(int)img.Tag] = uint.Parse((string)img.ToolTip);
        }

        private void SprMount_Click(object sender, RoutedEventArgs e)
        {
            InternalUpdateThingPreview();
        }

        private void SprBlendLayer_Click(object sender, RoutedEventArgs e)
        {
            InternalUpdateThingPreview();
        }

        private void SprAddonSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            InternalUpdateThingPreview();
        }

        private void FixSpritesCount()
        {
            SpriteInfo spriteInfo = CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo;
            int NumSprites = (int)(spriteInfo.PatternWidth * spriteInfo.PatternHeight * spriteInfo.PatternLayers * spriteInfo.PatternX * spriteInfo.PatternY * spriteInfo.PatternZ * spriteInfo.PatternFrames);

            if (spriteInfo.SpriteId.Count > NumSprites)
            {
                // Remove excess sprites from spriteInfo.SpriteId until it's equal to NumSprites
                int excessCount = spriteInfo.SpriteId.Count - NumSprites;
                for (int i = 0; i < excessCount; i++)
                {
                    spriteInfo.SpriteId.RemoveAt(spriteInfo.SpriteId.Count - 1);
                }
            }
            else if (spriteInfo.SpriteId.Count < NumSprites)
            {
                // Add child id 0 to spriteInfo.SpriteId until it's equal to NumSprites
                int missingCount = NumSprites - spriteInfo.SpriteId.Count;
                for (int i = 0; i < missingCount; i++)
                {
                    spriteInfo.SpriteId.Add(0);
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
                    CurrentObjectAppearance.FrameGroup[0].FixedFrameGroup = FIXED_FRAME_GROUP.OutfitMoving;
                }
                else if (A_SprGroups.Value == 2 && CurrentObjectAppearance.FrameGroup.Count == 1)
                {
                    FrameGroup newFrameGroup = CurrentObjectAppearance.FrameGroup[0].Clone();
                    CurrentObjectAppearance.FrameGroup.Add(newFrameGroup);
                    CurrentObjectAppearance.FrameGroup[0].FixedFrameGroup = FIXED_FRAME_GROUP.OutfitIdle;
                }
            }
            else if (frameworkElement.Name == "A_SprWidth")
            {
                spriteInfo.PatternWidth = (uint)A_SprWidth.Value;
                FixSpritesCount();
                ForceSliderChange();
            }
            else if (frameworkElement.Name == "A_SprHeight")
            {
                spriteInfo.PatternHeight = (uint)A_SprHeight.Value;
                FixSpritesCount();
                ForceSliderChange();
            }
            else if (frameworkElement.Name == "A_SprLayers")
            {
                spriteInfo.PatternLayers = (uint)A_SprLayers.Value;
                FixSpritesCount();
                ForceSliderChange();
                SprBlendLayer.IsEnabled = A_SprLayers.Value > 1 ? true : false;
            }
            else if (frameworkElement.Name == "A_SprPaternX")
            {
                spriteInfo.PatternX = (uint)A_SprPaternX.Value;
                FixSpritesCount();
                ForceSliderChange();
            }
            else if (frameworkElement.Name == "A_SprPaternY")
            {
                spriteInfo.PatternY = (uint)A_SprPaternY.Value;
                FixSpritesCount();
                ForceSliderChange();
            }
            else if (frameworkElement.Name == "A_SprPaternZ")
            {
                spriteInfo.PatternZ = (uint)A_SprPaternZ.Value;
                FixSpritesCount();
                ForceSliderChange();
                SprMount.IsEnabled = A_SprPaternZ.Value > 1 ? true : false;
            }
            else if (frameworkElement.Name == "A_SprAnimation")
            {
                spriteInfo.PatternFrames = (uint)A_SprAnimation.Value;
                if (spriteInfo.PatternFrames == 1)
                {
                    spriteInfo.Animation = null;
                }
                else
                {
                    SpriteAnimation spriteAnimation = new();
                    for (int i = 0; i < spriteInfo.PatternFrames; i++)
                        spriteAnimation.SpritePhase.Add(new SpritePhase() { DurationMin = 100, DurationMax = 100 });
                    spriteInfo.Animation = spriteAnimation;
                }

                FixSpritesCount();
                ForceSliderChange();
                SpriteAnimationGroup.IsEnabled = A_SprAnimation.Value > 1 ? true : false;
                SprFramesSlider.Maximum = (double)A_SprAnimation.Value - 1;
            }
        }

        private void ObjectSave_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var flags = CurrentObjectAppearance.Flags;

            if (!string.IsNullOrWhiteSpace(A_FlagName.Text))
                CurrentObjectAppearance.Name = A_FlagName.Text;

            if (!string.IsNullOrWhiteSpace(A_FlagDescription.Text))
                CurrentObjectAppearance.Description = A_FlagDescription.Text;

            if ((bool)A_FlagGround.IsChecked)
            {
                flags.Bank = new AppearanceFlagBank
                {
                    Waypoints = (uint)A_FlagGroundSpeed.Value
                };
            }
            else
                flags.Bank = null;

            if ((bool)A_FlagClip.IsChecked)
                flags.Clip = true;
            else if (flags.HasClip)
                flags.ClearClip();

            if ((bool)A_FlagBottom.IsChecked)
                flags.Bottom = true;
            else if (flags.HasBottom)
                flags.ClearBottom();

            if ((bool)A_FlagTop.IsChecked)
                flags.Top = true;
            else if (flags.HasTop)
                flags.ClearTop();

            if ((bool)A_FlagContainer.IsChecked)
                flags.Container = true;
            else if (flags.HasContainer)
                flags.ClearContainer();

            if ((bool)A_FlagCumulative.IsChecked)
                flags.Cumulative = true;
            else if (flags.HasCumulative)
                flags.ClearCumulative();

            if ((bool)A_FlagUsable.IsChecked)
                flags.Usable = true;
            else if (flags.HasUsable)
                flags.ClearUsable();

            if ((bool)A_FlagForceuse.IsChecked)
                flags.Forceuse = true;
            else if (flags.HasForceuse)
                flags.ClearForceuse();

            if ((bool)A_FlagMultiuse.IsChecked)
                flags.Multiuse = true;
            else if (flags.HasMultiuse)
                flags.ClearMultiuse();

            if ((bool)A_FlagWrite.IsChecked)
            {
                flags.Write = new AppearanceFlagWrite
                {
                    MaxTextLength = (uint)A_FlagMaxTextLength.Value
                };
            }
            else
                flags.Write = null;

            if ((bool)A_FlagWriteOnce.IsChecked)
            {
                flags.WriteOnce = new AppearanceFlagWriteOnce
                {
                    MaxTextLengthOnce = (uint)A_FlagMaxTextLengthOnce.Value
                };
            }
            else flags.WriteOnce = null;

            if ((bool)A_FlagLiquidpool.IsChecked)
                flags.Liquidpool = true;
            else if (flags.HasLiquidpool)
                flags.ClearLiquidpool();

            if ((bool)A_FlagUnpass.IsChecked)
                flags.Unpass = true;
            else if (flags.HasUnpass)
                flags.ClearUnpass();

            if ((bool)A_FlagUnmove.IsChecked)
                flags.Unmove = true;
            else if (flags.HasUnmove)
                flags.ClearUnmove();

            if ((bool)A_FlagUnsight.IsChecked)
                flags.Unsight = true;
            else if (flags.HasUnsight)
                flags.ClearUnsight();

            if ((bool)A_FlagAvoid.IsChecked)
                flags.Avoid = true;
            else if (flags.HasAvoid)
                flags.ClearAvoid();

            if ((bool)A_FlagNoMoveAnimation.IsChecked)
                flags.NoMovementAnimation = true;

            else if (flags.HasNoMovementAnimation)
                flags.ClearNoMovementAnimation();

            if ((bool)A_FlagTake.IsChecked)
                flags.Take = true;
            else if (flags.HasTake)
                flags.ClearTake();

            if ((bool)A_FlagLiquidcontainer.IsChecked)
                flags.Liquidcontainer = true;
            else if (flags.HasLiquidcontainer)
                flags.ClearLiquidcontainer();

            if ((bool)A_FlagHang.IsChecked)
                flags.Hang = true;
            else if (flags.HasHang)
                flags.ClearHang();

            if ((bool)A_FlagHookSouth.IsChecked)
                flags.HookSouth = true;
            else if (flags.HasHookSouth)
                flags.ClearHookSouth();

            if ((bool)A_FlagHookEast.IsChecked)
                flags.HookEast = true;
            else if (flags.HasHookEast)
                flags.ClearHookEast();

            if ((bool)A_FlagRotate.IsChecked)
                flags.Rotate = true;
            else if (flags.HasRotate)
                flags.ClearRotate();

            if ((bool)A_FlagLight.IsChecked)
            {
                flags.Light = new AppearanceFlagLight
                {
                    Brightness = (uint)A_FlagLightBrightness.Value,
                    Color = (uint)A_FlagLightColor.Value
                };
            }
            else
                flags.Light = null;


            if ((bool)A_FlagDontHide.IsChecked)
                flags.DontHide = true;
            else if (flags.HasDontHide)
                flags.ClearDontHide();

            if ((bool)A_FlagTranslucent.IsChecked)
                flags.Translucent = true;
            else if (flags.HasTranslucent)
                flags.ClearTranslucent();

            if ((bool)A_FlagShift.IsChecked)
            {
                flags.Shift = new AppearanceFlagShift
                {
                    X = (int)A_FlagShiftX.Value,
                    Y = (int)A_FlagShiftY.Value,
                    A = (int)A_FlagShiftA.Value,
                    B = (int)A_FlagShiftB.Value
                };
            }
            else
                flags.Shift = null;

            if ((bool)A_FlagHeight.IsChecked)
            {
                flags.Height = new AppearanceFlagHeight
                {
                    Elevation = (uint)A_FlagElevation.Value,
                };
            }
            else flags.Height = null;

            if ((bool)A_FlagLyingObject.IsChecked)
                flags.LyingObject = true;
            else if (flags.HasLyingObject)
                flags.ClearLyingObject();

            if ((bool)A_FlagAnimateAlways.IsChecked)
                flags.AnimateAlways = true;
            else if (flags.HasAnimateAlways)
                flags.ClearAnimateAlways();

            if ((bool)A_FlagAutomap.IsChecked)
            {
                flags.Automap = new AppearanceFlagAutomap
                {
                    Color = (uint)A_FlagAutomapColor.Value,
                };
            }
            else flags.Automap = null;

            if ((bool)A_FlagLenshelp.IsChecked)
            {
                flags.Lenshelp = new AppearanceFlagLenshelp
                {
                    Id = (uint)A_FlagLenshelpId.SelectedIndex + 1100
                };
            }
            else flags.Lenshelp = null;

            if ((bool)A_FlagFullGround.IsChecked)
                flags.Fullbank = true;
            else if (flags.HasFullbank)
                flags.ClearFullbank();

            if ((bool)A_FlagIgnoreLook.IsChecked)
                flags.IgnoreLook = true;
            else if (flags.HasIgnoreLook)
                flags.ClearIgnoreLook();

            if ((bool)A_FlagClothes.IsChecked)
            {
                flags.Clothes = new AppearanceFlagClothes
                {
                    Slot = (uint)A_FlagClothesSlot.SelectedIndex
                };
            }
            else flags.Clothes = null;

            if ((bool)A_FlagDefaultAction.IsChecked)
            {
                flags.DefaultAction = new AppearanceFlagDefaultAction
                {
                    Action = (PLAYER_ACTION)A_FlagDefaultActionType.SelectedIndex
                };
            }
            else flags.DefaultAction = null;

            if ((bool)A_FlagMarket.IsChecked)
            {
                flags.Market = new AppearanceFlagMarket
                {
                    Category = (ITEM_CATEGORY)(A_FlagMarketCategory.SelectedIndex + 1),
                    TradeAsObjectId = (uint)A_FlagMarketTrade.Value,
                    ShowAsObjectId = (uint)A_FlagMarketShow.Value,
                    MinimumLevel = (uint)A_FlagMarketlevel.Value,
                };

                flags.Market.Vocation = (VOCATION)A_FlagMarketProfession.SelectedIndex;
            }
            else
                flags.Market = null;

            if ((bool)A_FlagWrap.IsChecked)
                flags.Wrap = true;
            else if (flags.HasWrap)
                flags.ClearWrap();

            if ((bool)A_FlagUnwrap.IsChecked)
                flags.Unwrap = true;
            else if (flags.HasUnwrap)
                flags.ClearUnwrap();

            if ((bool)A_FlagTopeffect.IsChecked)
                flags.Topeffect = true;
            else if (flags.HasTopeffect)
                flags.ClearTopeffect();

            if ((bool)A_FlagWearout.IsChecked)
                flags.Wearout = true;
            else if ((flags.HasWearout))
                flags.ClearWearout();

            if ((bool)A_FlagWingsOffset.IsChecked) {
                flags.WingsOffset = new() {
                    NorthX = (int)A_FlagWingsNorthX.Value,
                    NorthY = (int)A_FlagWingsNorthY.Value,
                    EastX = (int)A_FlagWingsEastX.Value,
                    EastY = (int)A_FlagWingsEastY.Value,
                    SouthX = (int)A_FlagWingsSouthX.Value,
                    SouthY = (int)A_FlagWingsSouthY.Value,
                    WestX = (int)A_FlagWingsWestX.Value,
                    WestY = (int)A_FlagWingsWestY.Value,
                };
            } else
                flags.WingsOffset = null;

            if (ObjectMenu.SelectedIndex == 0)
                MainWindow.appearances.Outfit[ObjListView.SelectedIndex] = CurrentObjectAppearance.Clone();
            else if (ObjectMenu.SelectedIndex == 1)
                MainWindow.appearances.Object[ObjListView.SelectedIndex] = CurrentObjectAppearance.Clone();
            else if (ObjectMenu.SelectedIndex == 2)
                MainWindow.appearances.Effect[ObjListView.SelectedIndex] = CurrentObjectAppearance.Clone();
            else if (ObjectMenu.SelectedIndex == 3)
                MainWindow.appearances.Missile[ObjListView.SelectedIndex] = CurrentObjectAppearance.Clone();

            ShowList showList = ObjListView.SelectedItem as ShowList;
            AnimateSelectedListItem(showList);

            StatusBar.MessageQueue?.Enqueue($"Saved Current Object.", null, null, null, false, true, TimeSpan.FromSeconds(2));
        }

        private void CopyObjectFlags(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            CurrentFlags = CurrentObjectAppearance.Flags.Clone();
            StatusBar.MessageQueue?.Enqueue($"Copied Current Object Flags.", null, null, null, false, true, TimeSpan.FromSeconds(2));
        }
        private void PasteObjectFlags(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (CurrentFlags != null)
            {
                CurrentObjectAppearance.Flags = CurrentFlags.Clone();
                LoadCurrentObjectAppearances();
                StatusBar.MessageQueue?.Enqueue($"Pasted Object Flags.", null, null, null, false, true, TimeSpan.FromSeconds(2));
            }
            else
                StatusBar.MessageQueue?.Enqueue($"Copy Flags First.", null, null, null, false, true, TimeSpan.FromSeconds(2));
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
            ComppileDialogHost.IsOpen = true;
            CompileBox.IsEnabled = true;
        }

        private async void CompileClient(object sender, RoutedEventArgs e)
        {
            bool isDatEditable = false;
            bool isSprEditable = false;
            string datfile = MainWindow._assetsPath + A_CompileName.Text + ".dat";
            string sprfile = MainWindow._assetsPath + A_CompileName.Text + ".spr";
            string otfile = MainWindow._assetsPath + A_CompileName.Text + ".otfi";

            try {
                using var fileStream = new FileStream(datfile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                isDatEditable = true;
            } catch (IOException) {
                isDatEditable = false;
            }

            try {
                using var fileStream = new FileStream(sprfile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                isSprEditable = true;
            } catch (IOException) {
                isSprEditable = false;
            }

            if (isDatEditable && isSprEditable) {
                LegacyAppearance.WriteLegacyDat(datfile, MainWindow.DatSignature, MainWindow.appearances, MainWindow.GetCurrentLoadedVersion());
                var progress = new Progress<int>(percent => {
                    LoadProgress.Value = percent;
                });
                CompileBox.IsEnabled = false;

                await Sprite.CompileSpritesAsync(sprfile, MainWindow.MainSprStorage, (bool)C_Transparent.IsChecked, MainWindow.SprSignature, progress, MainWindow.GetCurrentPreset()?.Extended ?? true);
            } else {
                StatusBar.MessageQueue?.Enqueue($".dat or .spr file is being used by another process or is not accessible.", null, null, null, false, true, TimeSpan.FromSeconds(2));
            }

            // write otfi (optional)
            try {
                PresetSettings? preset = MainWindow.GetCurrentPreset();
                if (preset != null) {
                    Utils.WritePresetToOtfi(otfile, in preset, datfile, sprfile, C_Transparent.IsChecked ?? false);
                }
            } catch {
                // ...
            }

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
                SaveFileDialog saveFileDialog = new()
                {
                    Filter = "Bitmap Image (.bmp)|*.bmp|Gif Image (.gif)|*.gif|JPEG Image (.jpeg)|*.jpeg|Png Image (.png)|*.png",
                    FileName = " ",
                    ClientGuid = Globals.GUID_LegacyDatEditor1
                };
                if (saveFileDialog.ShowDialog() == true)
                {
                    foreach (var item in selectedItems)
                    {
                        if (item.Image != null)
                        {
                            System.Drawing.Bitmap targetImg = new System.Drawing.Bitmap((int)item.Image.Width, (int)item.Image.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                            System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(targetImg);
                            if(saveFileDialog.FilterIndex != 4)
                                g.Clear(System.Drawing.Color.FromArgb(255, 255, 0, 255));
                            System.Drawing.Image image = System.Drawing.Image.FromStream(MainWindow.MainSprStorage.getSpriteStream(item.Id));
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

        private void ImportSprite_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            OpenFileDialog openFileDialog = new() {
                ClientGuid = Globals.GUID_LegacyDatEditor2,
                Filter = "Bitmap Image (.bmp)|*.bmp|Gif Image (.gif)|*.gif|JPEG Image (.jpeg)|*.jpeg|Png Image (.png)|*.png",
                Multiselect = true
            };
            int imported = 0;
            if (openFileDialog.ShowDialog() == true)
            {
                string[] selectedFiles = openFileDialog.FileNames;
                foreach (string filePath in selectedFiles)
                {
                    using (System.Drawing.Image originalImage = System.Drawing.Image.FromFile(filePath))
                    {
                        System.Drawing.Color magentaColor = System.Drawing.Color.Magenta;

                        int cropsX = originalImage.Width / 32;
                        int cropsY = originalImage.Height / 32;

                        for (int i = 0; i < cropsY; i++)
                        {
                            for (int j = 0; j < cropsX; j++)
                            {
                                System.Drawing.Rectangle cropRect = new System.Drawing.Rectangle(j * 32, i * 32, 32, 32);
                                using (System.Drawing.Bitmap croppedBitmap = new System.Drawing.Bitmap(32, 32, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                                {
                                    using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(croppedBitmap))
                                    {
                                        g.Clear(System.Drawing.Color.Transparent);
                                        ImageAttributes imageAttributes = new ImageAttributes();
                                        imageAttributes.SetColorKey(magentaColor, magentaColor);

                                        g.DrawImage(
                                            originalImage,
                                            new System.Drawing.Rectangle(0, 0, 32, 32),
                                            cropRect.X, cropRect.Y, cropRect.Width, cropRect.Height,
                                            System.Drawing.GraphicsUnit.Pixel,
                                            imageAttributes
                                        );
                                    }

                                    MemoryStream imgMemory = new MemoryStream();
                                    croppedBitmap.Save(imgMemory, ImageFormat.Png);
                                    imgMemory.Position = 0;

                                    int sprId = MainWindow.SprLists.Count;
                                    MainWindow.SprLists[sprId] = imgMemory;
                                    MainWindow.AllSprList.Add(new ShowList() { Id = (uint)sprId });
                                    CollectionViewSource.GetDefaultView(SprListView.ItemsSource).Refresh();

                                    imported++;
                                }
                            }
                        }
                    }
                }

                if (imported > 0)
                {
                    StatusBar.MessageQueue?.Enqueue($"Successfully imported {imported} image(s).", null, null, null, false, true, TimeSpan.FromSeconds(2));
                }
                else
                {
                    StatusBar.MessageQueue?.Enqueue("No images imported.", null, null, null, false, true, TimeSpan.FromSeconds(2));
                }
            }
        }
        private void DeleteSprite_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ShowList data = (ShowList)SprListView.SelectedItem;
            if (data != null && data.Image != null)
            {
                if(data.Id < MainWindow.SprLists.Count - 1)
                {
                    MainWindow.SprLists[(int)data.Id].Position = 0;
                    using System.Drawing.Bitmap emptyBitmap = new System.Drawing.Bitmap(32, 32, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    emptyBitmap.Save(MainWindow.SprLists[(int)data.Id], ImageFormat.Png);
                    CollectionViewSource.GetDefaultView(SprListView.ItemsSource).Refresh();
                    StatusBar.MessageQueue?.Enqueue($"Sprite successfully removed.", null, null, null, false, true, TimeSpan.FromSeconds(2));
                }
                else
                {
                    bool delete = true;
                    List<IEnumerable<FrameGroup>> frameGroupCollections = new List<IEnumerable<FrameGroup>>
                    {
                        MainWindow.appearances.Outfit.SelectMany(outfit => outfit.FrameGroup),
                        MainWindow.appearances.Object.SelectMany(obj => obj.FrameGroup),
                        MainWindow.appearances.Effect.SelectMany(effect => effect.FrameGroup),
                        MainWindow.appearances.Missile.SelectMany(missile => missile.FrameGroup),
    
                    };

                    foreach (var frameGroup in frameGroupCollections.SelectMany(collection => collection))
                    {
                        if (frameGroup.SpriteInfo.SpriteId.Contains(data.Id))
                        {
                            delete = false;
                        }
                    }
                    if (delete)
                    {
                        if (MainWindow.SprLists.TryRemove((int)data.Id, out MemoryStream removedStream))
                        {
                            removedStream.Dispose();
                            MainWindow.AllSprList.RemoveAt((int)data.Id);
                            CollectionViewSource.GetDefaultView(SprListView.ItemsSource).Refresh();
                            StatusBar.MessageQueue?.Enqueue($"Sprite successfully removed.", null, null, null, false, true, TimeSpan.FromSeconds(2));
                        }
                        else
                            StatusBar.MessageQueue?.Enqueue($"Unable to delete sprite.", null, null, null, false, true, TimeSpan.FromSeconds(2));
                    }
                    else
                    {
                        StatusBar.MessageQueue?.Enqueue($"Unable to delete sprite. The sprite is currently in use by one or more objects", null, null, null, false, true, TimeSpan.FromSeconds(2));
                    }
                }

            }
        }
        private void ReplaceSprite_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            OpenFileDialog openFileDialog = new() {
                Filter = "Bitmap Image (.bmp)|*.bmp|Gif Image (.gif)|*.gif|JPEG Image (.jpeg)|*.jpeg|Png Image (.png)|*.png",
                ClientGuid = Globals.GUID_LegacyDatEditor3
            };
            if (openFileDialog.ShowDialog() == true)
            {
                using (System.Drawing.Image originalImage = System.Drawing.Image.FromFile(openFileDialog.FileName))
                {
                    System.Drawing.Color magentaColor = System.Drawing.Color.Magenta;
                    System.Drawing.Rectangle cropRect = new System.Drawing.Rectangle(0, 0, 32, 32);
                    using (System.Drawing.Bitmap croppedBitmap = new System.Drawing.Bitmap(32, 32, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                    {
                        using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(croppedBitmap))
                        {
                            g.Clear(System.Drawing.Color.Transparent);
                            ImageAttributes imageAttributes = new ImageAttributes();
                            imageAttributes.SetColorKey(magentaColor, magentaColor);

                            g.DrawImage(
                                originalImage,
                                new System.Drawing.Rectangle(0, 0, 32, 32),
                                cropRect.X, cropRect.Y, cropRect.Width, cropRect.Height,
                                System.Drawing.GraphicsUnit.Pixel,
                                imageAttributes
                            );
                        }

                        MemoryStream imgMemory = new MemoryStream();
                        croppedBitmap.Save(imgMemory, ImageFormat.Png);
                        imgMemory.Position = 0;

                        int sprId = SprListView.SelectedIndex;
                        MainWindow.SprLists[sprId] = imgMemory;
                        CollectionViewSource.GetDefaultView(SprListView.ItemsSource).Refresh();
                    }
                }
                StatusBar.MessageQueue?.Enqueue($"Sprite successfully replaced.", null, null, null, false, true, TimeSpan.FromSeconds(2));
            }
        }
        private void ExportObject_PreviewMouseLeftButtonDown(object sender, RoutedEventArgs e)
        {

            List<ShowList> selectedItems = ObjListView.SelectedItems.Cast<ShowList>().ToList();
            if (selectedItems.Any())
            {
                ObjListViewSelectedIndex.Value = (int)selectedItems.Last().Id;
                List<Appearance> appearances = new List<Appearance>();
                foreach (var item in selectedItems)
                {
                    if (ObjectMenu.SelectedIndex == 0)
                        appearances.Add(MainWindow.appearances.Outfit[(int)item.Id - 1]);
                    else if (ObjectMenu.SelectedIndex == 1)
                        appearances.Add(MainWindow.appearances.Object[(int)item.Id - 100]);
                    else if (ObjectMenu.SelectedIndex == 2)
                        appearances.Add(MainWindow.appearances.Effect[(int)item.Id - 1]);
                    else if (ObjectMenu.SelectedIndex == 3)
                        appearances.Add(MainWindow.appearances.Missile[(int)item.Id - 1]);
                }
                if(ObdDecoder.Export(appearances))
                    StatusBar.MessageQueue?.Enqueue($"Successfully exported objects.", null, null, null, false, true, TimeSpan.FromSeconds(2));
            }
        }
        
        private void OTBEditor_Click(object sender, RoutedEventArgs e)
        {
            OTBEditor oTBEditor = new OTBEditor(this, true);
            oTBEditor.Show();
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            ImportManager importerManager = new ImportManager(this);
            importerManager.Show();
        }

        private void ObjectClone_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            List<ShowList> selectedItems = [.. ObjListView.SelectedItems.Cast<ShowList>()];
            if (selectedItems.Count != 0)
            {
                ObjListViewSelectedIndex.Value = (int)selectedItems.Last().Id;

                if (ObjectMenu.SelectedIndex is not >= 0 and <= 3)
                    return;

                var (group, targetList) = ObjectMenu.SelectedIndex switch {
                    0 => (MainWindow.appearances?.Outfit, ThingsOutfit),
                    1 => (MainWindow.appearances?.Object, ThingsItem),
                    2 => (MainWindow.appearances?.Effect, ThingsEffect),
                    3 => (MainWindow.appearances?.Missile, ThingsMissile),
                    _ => throw new InvalidOperationException()
                };

                if (group is null)
                    return;

                uint newId = (uint)group.Max(a => a.Id) + 1;
                foreach (var item in selectedItems) {
                    var origItem = group.First(o => o.Id == item.Id);
                    var clonedItem = origItem.Clone();
                    clonedItem.Id = newId++;

                    // update market/cyclopedia item ids if item was referencing to self
                    Utils.OnAppearanceCloned(origItem, ref clonedItem);

                    group.Add(clonedItem);
                    targetList.Add(new ShowList { Id = clonedItem.Id });
                }

                // update the ui
                var src = ObjListView.ItemsSource;
                ObjListView.ItemsSource = null;
                ObjListView.ItemsSource = src;

                // move the cursor to the recently cloned item
                ObjListView.SelectedItem = ObjListView.Items[^1];

                // scroll to the duplicated item
                Dispatcher.BeginInvoke(new Action(() => {
                    ObjListView.ScrollIntoView(ObjListView.Items[^1]);
                }), System.Windows.Threading.DispatcherPriority.Background);

                StatusBar.MessageQueue?.Enqueue($"Successfully duplicated {selectedItems.Count} {(selectedItems.Count == 1 ? "object" : "objects")}.", null, null, null, false, true, TimeSpan.FromSeconds(2));
            }
        }

        private void SearchItem_Click(object sender, RoutedEventArgs e)
        {
            SearchWindow searchWindow = new SearchWindow(this, true);
            searchWindow.Show();
        }

        private void ObjListView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e) {
            // C# 9.0: listView = sender (if null: return)
            if (sender is not ListView listView) return;

            // Perform a hit test to find the clicked item
            var hit = e.OriginalSource as DependencyObject;
            while (hit != null && hit is not ListViewItem)
                hit = VisualTreeHelper.GetParent(hit);

            if (hit is ListViewItem item) {
                var showList = (ShowList)listView.ItemContainerGenerator.ItemFromContainer(item);
                if (showList != null) {
                    Dispatcher.Invoke(() => {
                        ClipboardManager.CopyText(showList.Id.ToString(), "Object ID", StatusBar);
                    });
                }
            }
        }

        private void DeleteObject_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if(ObjListView.SelectedIndex == ObjListView.Items.Count - 1)
            {
                if (ObjectMenu.SelectedIndex == 0)
                {
                    MainWindow.appearances.Outfit.RemoveAt(ObjListView.SelectedIndex);
                    ThingsOutfit.Remove((ShowList)ObjListView.SelectedItem);
                }
                else if (ObjectMenu.SelectedIndex == 1)
                {
                    MainWindow.appearances.Object.RemoveAt(ObjListView.SelectedIndex);
                    ThingsItem.Remove((ShowList)ObjListView.SelectedItem);
                }
                else if (ObjectMenu.SelectedIndex == 2)
                {
                    MainWindow.appearances.Effect.RemoveAt(ObjListView.SelectedIndex);
                    ThingsEffect.Remove((ShowList)ObjListView.SelectedItem);

                }
                else if (ObjectMenu.SelectedIndex == 3)
                {
                    MainWindow.appearances.Missile.RemoveAt(ObjListView.SelectedIndex);
                    ThingsMissile.Remove((ShowList)ObjListView.SelectedItem);

                }
            }else
            {
                Appearance selectedObject = new Appearance();

                if (ObjectMenu.SelectedIndex == 0)
                {
                    selectedObject = MainWindow.appearances.Outfit[ObjListView.SelectedIndex];

                }
                else if (ObjectMenu.SelectedIndex == 1)
                {
                    selectedObject = MainWindow.appearances.Object[ObjListView.SelectedIndex];
                }
                else if (ObjectMenu.SelectedIndex == 2)
                {
                    selectedObject = MainWindow.appearances.Effect[ObjListView.SelectedIndex];

                }
                else if (ObjectMenu.SelectedIndex == 3)
                {
                    selectedObject = MainWindow.appearances.Missile[ObjListView.SelectedIndex];

                }

                selectedObject.Flags = new AppearanceFlags();

                FrameGroup frameGroup = new() {
                    SpriteInfo = new(),
                    FixedFrameGroup = FIXED_FRAME_GROUP.OutfitIdle
                };

                var spriteInfo = frameGroup.SpriteInfo;
                spriteInfo.PatternWidth = 1;
                spriteInfo.PatternHeight = 1;
                spriteInfo.PatternSize = 32;

                spriteInfo.PatternLayers = 1;
                spriteInfo.PatternX = 1;
                spriteInfo.PatternY = 1;
                spriteInfo.PatternZ = 1;
                spriteInfo.PatternFrames = 1;

                spriteInfo.SpriteId.Add(0);
                selectedObject.FrameGroup.Clear();
                selectedObject.FrameGroup.Add(frameGroup);

                ShowList selectedShowList = (ShowList)ObjListView.SelectedItem;
                selectedShowList.Image = Utils.BitmapToBitmapImage(MainWindow.SprLists[0]);
            }
            StatusBar.MessageQueue?.Enqueue($"Object successfully deleted.", null, null, null, false, true, TimeSpan.FromSeconds(2));

        }

        private void NewObject_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Appearance newObject = new() {
                Flags = new()
            };

            FrameGroup frameGroup = new() {
                SpriteInfo = new(),
                FixedFrameGroup = FIXED_FRAME_GROUP.OutfitIdle
            };

            var spriteInfo = frameGroup.SpriteInfo;
            spriteInfo.PatternWidth = 1;
            spriteInfo.PatternHeight = 1;
            spriteInfo.PatternSize = 32;

            spriteInfo.PatternLayers = 1;
            spriteInfo.PatternX = 1;
            spriteInfo.PatternY = 1;
            spriteInfo.PatternZ = 1;
            spriteInfo.PatternFrames = 1;

            spriteInfo.SpriteId.Add(0);

            newObject.FrameGroup.Add(frameGroup);

            if (ObjectMenu.SelectedIndex == 0)
            {
                newObject.AppearanceType = APPEARANCE_TYPE.AppearanceOutfit;
                newObject.Id = (uint)(MainWindow.appearances.Outfit.Count + 1);
                MainWindow.appearances.Outfit.Add(newObject);
                ThingsOutfit.Add(new ShowList() { Id = newObject.Id });
            }
            else if (ObjectMenu.SelectedIndex == 1)
            {
                newObject.AppearanceType = APPEARANCE_TYPE.AppearanceObject;
                newObject.Id = (uint)(MainWindow.appearances.Object.Count + 100);
                MainWindow.appearances.Object.Add(newObject);
                ThingsItem.Add(new ShowList() { Id = newObject.Id });
            }
            else if (ObjectMenu.SelectedIndex == 2)
            {
                newObject.AppearanceType = APPEARANCE_TYPE.AppearanceEffect;
                newObject.Id = (uint)(MainWindow.appearances.Effect.Count + 1);
                MainWindow.appearances.Effect.Add(newObject);
                ThingsEffect.Add(new ShowList() { Id = newObject.Id });

            }
            else if (ObjectMenu.SelectedIndex == 3)
            {
                newObject.AppearanceType = APPEARANCE_TYPE.AppearanceMissile;
                newObject.Id = (uint)(MainWindow.appearances.Missile.Count + 1);
                MainWindow.appearances.Missile.Add(newObject);
                ThingsMissile.Add(new ShowList() { Id = newObject.Id });

            }

            ObjListView.SelectedItem = ObjListView.Items[^1];
            StatusBar.MessageQueue?.Enqueue($"Object successfully created.", null, null, null, false, true, TimeSpan.FromSeconds(2));
        }

        private SpriteAnimation? GetCurrentAnimation()
        {
            return CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.Animation;
        }

        private void SprSynchronized_Click(object sender, RoutedEventArgs e)
        {
            bool syncChecked = SprSynchronized.IsChecked ?? false;
            GetCurrentAnimation()?.AnimationMode = syncChecked ? ANIMATION_ANIMATION_MODE.AnimationSynchronized : ANIMATION_ANIMATION_MODE.AnimationAsynchronized;
        }

        private void SprDefaultPhase_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            GetCurrentAnimation()?.DefaultStartPhase = (uint)(SprDefaultPhase.Value ?? 100);
        }

        private void SprLoopCount_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            GetCurrentAnimation()?.LoopCount = (uint)(SprLoopCount.Value ?? 100);
        }

        private void SprPhaseMin_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            GetCurrentAnimation()?.SpritePhase[(int)(SprFramesSlider.Value / SprFramesSlider.TickFrequency)].DurationMin = (uint)(SprPhaseMin.Value ?? 100);
        }

        private void SprPhaseMax_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            GetCurrentAnimation()?.SpritePhase[(int)(SprFramesSlider.Value / SprFramesSlider.TickFrequency)].DurationMax = (uint)(SprPhaseMax.Value ?? 100);
        }

        public void AnimateSelectedListItem(ShowList showList)
        {
            // Find the ListViewItem for the selected item
            try {
                var listViewItem = ObjListView.ItemContainerGenerator.ContainerFromItem(showList) as ListViewItem;
                if (listViewItem != null) {
                    // Find the Image control within the ListViewItem
                    var imageControl = Utils.FindVisualChild<Image>(listViewItem);
                    if (imageControl != null) {
                        showList.Images.Clear();

                        Appearance appearance = null;

                        if (ObjectMenu.SelectedIndex == 0)
                            appearance = MainWindow.appearances.Outfit.FirstOrDefault(o => o.Id == showList.Id);
                        else if (ObjectMenu.SelectedIndex == 1)
                            appearance = MainWindow.appearances.Object.FirstOrDefault(o => o.Id == showList.Id);
                        else if (ObjectMenu.SelectedIndex == 2)
                            appearance = MainWindow.appearances.Effect.FirstOrDefault(o => o.Id == showList.Id);
                        else if (ObjectMenu.SelectedIndex == 3)
                            appearance = MainWindow.appearances.Missile.FirstOrDefault(o => o.Id == showList.Id);

                        for (int i = 0; i < appearance.FrameGroup[0].SpriteInfo.PatternFrames; i++) {
                            BitmapImage imageFrame = Utils.ResizeForUI(LegacyAppearance.GetObjectImage(appearance, MainWindow.MainSprStorage, i));
                            showList.Images.Add(imageFrame);
                        }

                        showList.StartAnimation();
                    }
                }
            } catch (Exception e) {
                MainWindow.Log(e.Message, "Error");
            }
        }

        private void ObjListView_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(ShowList)))
            {
                Point dropPosition = e.GetPosition(ObjListView);
                var result = VisualTreeHelper.HitTest(ObjListView, dropPosition);

                if (result != null)
                {
                    ListViewItem listViewItem = Utils.FindAncestorOrSelf<ListViewItem>(result.VisualHit);

                    if (listViewItem != null)
                    {
                        listViewItem.Opacity = 1;
                        ShowList data = (ShowList)listViewItem.DataContext;
                        ObjListView.SelectedItem = data;
                        if (ObjectMenu.SelectedIndex == 0)
                            ReplaceObjectAppearance = MainWindow.appearances.Outfit.FirstOrDefault(o => o.Id == data.Id);
                        else if (ObjectMenu.SelectedIndex == 1)
                            ReplaceObjectAppearance = MainWindow.appearances.Object.FirstOrDefault(o => o.Id == data.Id);
                        else if (ObjectMenu.SelectedIndex == 2)
                            ReplaceObjectAppearance = MainWindow.appearances.Effect.FirstOrDefault(o => o.Id == data.Id);
                        else if (ObjectMenu.SelectedIndex == 3)
                            ReplaceObjectAppearance = MainWindow.appearances.Missile.FirstOrDefault(o => o.Id == data.Id);
                    }
                }

            }
        }
        private void ObjListView_DragOver(object sender, DragEventArgs e)
        {
            ListViewItem listViewItem = Utils.FindAncestorOrSelf<ListViewItem>(e.OriginalSource as DependencyObject);
            if (listViewItem != null)
            {
                listViewItem.Opacity = 0.5;
            }
        }
        private void ObjListView_DragLeave(object sender, DragEventArgs e)
        {
            ListViewItem listViewItem = Utils.FindAncestorOrSelf<ListViewItem>(e.OriginalSource as DependencyObject);
            if (listViewItem != null)
            {
                listViewItem.Opacity = 1;
            }
        }
        private void About_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.Show();
        }

        private void ShowLogger(object sender, RoutedEventArgs e)
        {
            MainWindow.logView.Show();
        }

        private void OpenLuaWindow_Click(object sender, RoutedEventArgs e) {
            LuaWindow luaWindow = new();
            luaWindow.Show();
        }

        private void ResetZoom_Click(object sender, RoutedEventArgs e) {
            ZoomSlider.Value = 1;
        }
    }
    public class ArithmeticConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
            {
                return null;
            }

            double doubleValue = System.Convert.ToDouble(value);
            double doubleParameter = System.Convert.ToDouble(parameter);

            // Perform arithmetic operations here based on the parameter
            if (doubleParameter > 0)
            {
                // Addition
                double result = doubleValue + doubleParameter;
                return result;
            }
            else if (doubleParameter < 0)
            {
                // Subtraction
                double result = doubleValue - Math.Abs(doubleParameter);
                return result;
            }
            else
            {
                // Multiplication
                double result = doubleValue * doubleParameter;
                return result;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
            {
                return null;
            }

            double doubleValue = System.Convert.ToDouble(value);
            double doubleParameter = System.Convert.ToDouble(parameter);

            // Perform inverse arithmetic operations here based on the parameter
            if (doubleParameter > 0)
            {
                // Subtraction (inverse of addition)
                double result = doubleValue - doubleParameter;
                return result;
            }
            else if (doubleParameter < 0)
            {
                // Addition (inverse of subtraction)
                double result = doubleValue + Math.Abs(doubleParameter);
                return result;
            }
            else
            {
                // Division (inverse of multiplication)
                if (doubleValue == 0)
                {
                    return 0;
                }
                else
                {
                    double result = doubleValue / doubleParameter;
                    return result;
                }
            }
        }
    }
    public class NullableColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return Brushes.Transparent;
            }

            return new SolidColorBrush((Color)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BooleanInverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool booleanValue)
            {
                return !booleanValue;
            }

            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("BooleanInverter can only be used for one-way binding");
        }
    }
}
