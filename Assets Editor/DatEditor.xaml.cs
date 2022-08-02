using Efundies;
using Google.Protobuf;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Tibia.Protobuf.Appearances;
using Tibia.Protobuf.Shared;

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
        private int CurrentSprDir = 1;
        private ObservableCollection<ShowList> ObjectSprList = new ObservableCollection<ShowList>();

        private ObservableCollection<ShowList> ObjectOutfitSprList = new ObservableCollection<ShowList>();
        private ObservableCollection<ShowList> ObjectAddonSprList = new ObservableCollection<ShowList>();
        private ObservableCollection<ShowList> ObjectMountSprList = new ObservableCollection<ShowList>();
        private ObservableCollection<ShowList> ObjectAddonMountSprList = new ObservableCollection<ShowList>();

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Application.Current.Shutdown();
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
            UpdateShowList(ObjectMenu.SelectedIndex);
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
                    if (i >= offset && i < Math.Min(offset + 14, SprListView.Items.Count) && MainWindow.SprLists.ContainsKey(i))
                        MainWindow.AllSprList[i].Image = Utils.BitmapToBitmapImage(MainWindow.SprLists[i]);
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
                if(nIndex - maxOffset == offset)
                    scrollViewer.ScrollToVerticalOffset(offset+1);
                else if(nIndex + 1 == offset)
                    scrollViewer.ScrollToVerticalOffset(offset - 1);
                else if (nIndex >= offset + maxOffset || nIndex < offset)
                    scrollViewer.ScrollToVerticalOffset(SprListView.SelectedIndex);
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
                    if (i >= offset && i < Math.Min(offset + 14, ObjListView.Items.Count))
                    {
                        if (ObjectMenu.SelectedIndex == 0)
                            ThingsOutfit[i].Image = Utils.BitmapToBitmapImage(MainWindow.SprLists[(int)MainWindow.appearances.Outfit[i].FrameGroup[0].SpriteInfo.SpriteId[0]]);
                        else if (ObjectMenu.SelectedIndex == 1)
                            ThingsItem[i].Image = Utils.BitmapToBitmapImage(MainWindow.SprLists[(int)MainWindow.appearances.Object[i].FrameGroup[0].SpriteInfo.SpriteId[0]]);
                        else if (ObjectMenu.SelectedIndex == 2)
                            ThingsEffect[i].Image = Utils.BitmapToBitmapImage(MainWindow.SprLists[(int)MainWindow.appearances.Effect[i].FrameGroup[0].SpriteInfo.SpriteId[0]]);
                        else if (ObjectMenu.SelectedIndex == 3)
                            ThingsMissile[i].Image = Utils.BitmapToBitmapImage(MainWindow.SprLists[(int)MainWindow.appearances.Missile[i].FrameGroup[0].SpriteInfo.SpriteId[0]]);
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
                {
                    LoadSelectedObjectAppearances(MainWindow.appearances.Object[ObjListView.SelectedIndex]);
                }
                else if (ObjectMenu.SelectedIndex == 2)
                    LoadSelectedObjectAppearances(MainWindow.appearances.Effect[ObjListView.SelectedIndex]);
                else if (ObjectMenu.SelectedIndex == 3)
                    LoadSelectedObjectAppearances(MainWindow.appearances.Missile[ObjListView.SelectedIndex]);

                ObjListViewSelectedIndex.Value = (int)showList.Id;
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
            A_SprGroups.Value = CurrentObjectAppearance.FrameGroup.Count;
            SprFramesSlider.Value = 0;

            SprGroupSlider.Value = 0;
            SprGroupType.Content = "Idle";

            LoadSprPatterns((int)SprGroupSlider.Value);
            SprGroupSlider.ValueChanged += SprGroupSlider_ValueChanged;
            CurrentSprDir = 1;
            SprBlendLayer.IsChecked = false;
            SprMount.IsChecked = false;
            SprFramesSliderCounter.Content = 1;
            ChangeObjectSprImg(CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value], true);
            
        }
        private void LoadCurrentObjectAppearances()
        {
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
            if (CurrentObjectAppearance.Flags.Market != null && CurrentObjectAppearance.Flags.Market.RestrictToProfession.Count > 0)
            {
                foreach (var profession in CurrentObjectAppearance.Flags.Market.RestrictToProfession)
                {
                    if (profession == PLAYER_PROFESSION.Any)
                        A_FlagProfessionAny.IsChecked = true;
                    else if (profession == PLAYER_PROFESSION.None)
                        A_FlagProfessionNone.IsChecked = true;
                    else if (profession == PLAYER_PROFESSION.Knight)
                        A_FlagProfessionKnight.IsChecked = true;
                    else if (profession == PLAYER_PROFESSION.Paladin)
                        A_FlagProfessionPaladin.IsChecked = true;
                    else if (profession == PLAYER_PROFESSION.Sorcerer)
                        A_FlagProfessionSorcerer.IsChecked = true;
                    else if (profession == PLAYER_PROFESSION.Druid)
                        A_FlagProfessionDruid.IsChecked = true;
                    else if (profession == PLAYER_PROFESSION.Promoted)
                        A_FlagProfessionPromoted.IsChecked = true;
                }
            }
            A_FlagMarketlevel.Value = (CurrentObjectAppearance.Flags.Market != null && CurrentObjectAppearance.Flags.Market.HasMinimumLevel) ? (int)CurrentObjectAppearance.Flags.Market.MinimumLevel : 0;
            A_FlagName.Text = CurrentObjectAppearance.HasName ? CurrentObjectAppearance.Name : null;
            A_FlagDescription.Text = CurrentObjectAppearance.HasDescription ? CurrentObjectAppearance.Description : null;
            A_FlagWrap.IsChecked = CurrentObjectAppearance.Flags.HasWrap;
            A_FlagUnwrap.IsChecked = CurrentObjectAppearance.Flags.HasUnwrap;
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


        private void Spr_Drop(object sender, DragEventArgs e)
        {
            Image img = e.Source as Image;
            ShowList data = (ShowList)e.Data.GetData(typeof(ShowList));
            ObjectSprList[(int)SprFramesSlider.Value + (int)img.Tag].Id = data.Id;
            ObjectSprList[(int)SprFramesSlider.Value + (int)img.Tag].Image = data.Image;
            img.Source = data.Image;

            uint patternHeight = CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.HasPatternHeight ? CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.PatternHeight : 1;
            uint patternWidth = CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.HasPatternWidth ? CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.PatternWidth : 1;
            ObjectSprImgPanel.Width = ObjectMenu.SelectedIndex == 0 ? data.Image.Width : data.Image.Width * patternWidth;
            ObjectSprImgPanel.Height = ObjectMenu.SelectedIndex == 0 ? data.Image.Height : data.Image.Height * patternHeight;
        }


        private void SprFramesSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SprFramesSliderCounter.Content = (int)(SprFramesSlider.Value / SprFramesSlider.TickFrequency) + 1;
            SetSpriteFrame();
        }

        private void SetSpriteFrame()
        {
            if (CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.Animation != null)
            {
                SpriteAnimationGroup.IsEnabled = true;
                SprPhaseMin.Value = (int)CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.Animation.SpritePhase[(int)(SprFramesSlider.Value / SprFramesSlider.TickFrequency)].DurationMin;
                SprPhaseMax.Value = (int)CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.Animation.SpritePhase[(int)(SprFramesSlider.Value / SprFramesSlider.TickFrequency)].DurationMax;

                List<Image> images = Utils.GetLogicalChildCollection<Image>(ObjectSprImgPanel);
                foreach (Image child in images)
                {
                    child.Source = ObjectSprList[(int)child.Tag + ((int)SprFramesSlider.Value * images.Count)].Image;
                }
            }
            else
                SpriteAnimationGroup.IsEnabled = false;
        }
        private void SprGroupSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SaveSprPatterns(SprGroupSlider.Value == 0 ? 1 : 0);
            LoadSprPatterns((int)SprGroupSlider.Value);
            SprGroupType.Content = SprGroupSlider.Value == 0 ? "Idle" : "Walking";
            StorePreviousGroup(SprGroupSlider.Value == 0 ? 1 : 0);
            ChangeObjectSprImg(CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value], true);
        }
        private void LoadSprPatterns(int groupId)
        {
            A_SprLayers.Value = CurrentObjectAppearance.FrameGroup[groupId].SpriteInfo.HasLayers ? (int)CurrentObjectAppearance.FrameGroup[groupId].SpriteInfo.Layers : 1;
            A_SprPaternX.Value = CurrentObjectAppearance.FrameGroup[groupId].SpriteInfo.HasPatternWidth ? (int)CurrentObjectAppearance.FrameGroup[groupId].SpriteInfo.PatternWidth : 1;
            A_SprPaternY.Value = CurrentObjectAppearance.FrameGroup[groupId].SpriteInfo.HasPatternHeight ? (int)CurrentObjectAppearance.FrameGroup[groupId].SpriteInfo.PatternHeight : 1;
            A_SprPaternZ.Value = CurrentObjectAppearance.FrameGroup[groupId].SpriteInfo.HasPatternDepth ? (int)CurrentObjectAppearance.FrameGroup[groupId].SpriteInfo.PatternDepth : 1;
            AnimationTab.IsEnabled = CurrentObjectAppearance.FrameGroup[groupId].SpriteInfo.Animation != null;
            if (CurrentObjectAppearance.FrameGroup[groupId].SpriteInfo.Animation != null)
            {
                SprDefaultPhase.Value = CurrentObjectAppearance.FrameGroup[groupId].SpriteInfo.Animation.HasDefaultStartPhase ? (int)CurrentObjectAppearance.FrameGroup[groupId].SpriteInfo.Animation.DefaultStartPhase : 0;
                SprRandomPhase.IsChecked = CurrentObjectAppearance.FrameGroup[groupId].SpriteInfo.Animation.HasRandomStartPhase ? CurrentObjectAppearance.FrameGroup[groupId].SpriteInfo.Animation.RandomStartPhase : false;
                SprSynchronized.IsChecked = CurrentObjectAppearance.FrameGroup[groupId].SpriteInfo.Animation.HasSynchronized ? CurrentObjectAppearance.FrameGroup[groupId].SpriteInfo.Animation.Synchronized : false;

                if (CurrentObjectAppearance.FrameGroup[groupId].SpriteInfo.Animation.HasLoopType)
                {
                    if (CurrentObjectAppearance.FrameGroup[groupId].SpriteInfo.Animation.LoopType == ANIMATION_LOOP_TYPE.Pingpong)
                        SprLoopType.SelectedIndex = 0;
                    else if (CurrentObjectAppearance.FrameGroup[groupId].SpriteInfo.Animation.LoopType == ANIMATION_LOOP_TYPE.Infinite)
                        SprLoopType.SelectedIndex = 1;
                    else if (CurrentObjectAppearance.FrameGroup[groupId].SpriteInfo.Animation.LoopType == ANIMATION_LOOP_TYPE.Counted)
                        SprLoopType.SelectedIndex = 2;
                    else
                        SprLoopType.SelectedIndex = -1;
                }
                SprLoopCount.Value = CurrentObjectAppearance.FrameGroup[groupId].SpriteInfo.Animation.HasLoopCount ? (int)CurrentObjectAppearance.FrameGroup[groupId].SpriteInfo.Animation.LoopCount : 0;
            }
            A_SprOpaque.IsChecked = CurrentObjectAppearance.FrameGroup[groupId].SpriteInfo.IsOpaque;
            A_SprBounding.Value = CurrentObjectAppearance.FrameGroup[groupId].SpriteInfo.HasBoundingSquare ? (int)CurrentObjectAppearance.FrameGroup[groupId].SpriteInfo.BoundingSquare : 0;

            if (CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.BoundingBoxPerDirection != null)
            {
                BoundingBoxList.Clear();
                foreach (var box in CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.BoundingBoxPerDirection)
                    BoundingBoxList.Add(new Box() { X = box.X, Y = box.Y, Width = box.Width, Height = box.Height });
                BoxPerDirection.ItemsSource = null;
                BoxPerDirection.ItemsSource = BoundingBoxList;

            }
        }
        private void SaveSprPatterns(int groupId)
        {
            CurrentObjectAppearance.FrameGroup[groupId].SpriteInfo.IsOpaque = (bool)A_SprOpaque.IsChecked;
            A_SprOpaque.IsChecked = CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.IsOpaque;

            CurrentObjectAppearance.FrameGroup[groupId].SpriteInfo.BoundingSquare = (uint)A_SprBounding.Value;
            A_SprBounding.Value = CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.HasBoundingSquare ? (int)CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.BoundingSquare : 0;


            if (CurrentObjectAppearance.FrameGroup[groupId].SpriteInfo.Animation != null)
            {
                CurrentObjectAppearance.FrameGroup[groupId].SpriteInfo.Animation.DefaultStartPhase = (uint)SprDefaultPhase.Value;
                CurrentObjectAppearance.FrameGroup[groupId].SpriteInfo.Animation.RandomStartPhase = (bool)SprRandomPhase.IsChecked;
                CurrentObjectAppearance.FrameGroup[groupId].SpriteInfo.Animation.Synchronized = (bool)SprSynchronized.IsChecked;

                if (SprLoopType.SelectedIndex > -1)
                    CurrentObjectAppearance.FrameGroup[groupId].SpriteInfo.Animation.LoopType = (ANIMATION_LOOP_TYPE)(SprLoopType.SelectedIndex - 1);
                if (SprLoopCount.Value > 0)
                    CurrentObjectAppearance.FrameGroup[groupId].SpriteInfo.Animation.LoopCount = (uint)SprLoopCount.Value;
            }

            if (BoundingBoxList.Count > 0)
            {
                CurrentObjectAppearance.FrameGroup[groupId].SpriteInfo.BoundingBoxPerDirection.Clear();
                foreach (var box in BoundingBoxList)
                    CurrentObjectAppearance.FrameGroup[groupId].SpriteInfo.BoundingBoxPerDirection.Add(box);
            }
        }
        private void StorePreviousGroup(int groupId)
        {

            foreach (ShowList showList in ObjectOutfitSprList)
            {
                CurrentObjectAppearance.FrameGroup[groupId].SpriteInfo.SpriteId[showList.Pos] = showList.Id;
            }
            foreach (ShowList showList in ObjectMountSprList)
            {
                CurrentObjectAppearance.FrameGroup[groupId].SpriteInfo.SpriteId[showList.Pos] = showList.Id;
            }
            foreach (ShowList showList in ObjectAddonSprList)
            {
                CurrentObjectAppearance.FrameGroup[groupId].SpriteInfo.SpriteId[showList.Pos] = showList.Id;
            }
            foreach (ShowList showList in ObjectAddonMountSprList)
            {
                CurrentObjectAppearance.FrameGroup[groupId].SpriteInfo.SpriteId[showList.Pos] = showList.Id;
            }
        }
        private void ChangeDirection(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Button _dir = (Button)sender;

            CurrentSprDir = int.Parse(_dir.Uid);
            ChangeSprDirection();
        }

        private void ChangeSprDirection()
        {
            uint patternLayers = CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.HasLayers ? CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.Layers : 1;
            uint patternHeight = CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.HasPatternHeight ? CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.PatternHeight : 1;
            uint patternWidth = CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.HasPatternWidth ? CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.PatternWidth : 1;
            uint patternDepth = CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.HasPatternDepth ? CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.PatternDepth : 1;
            int SprPreFrame = (int)(patternWidth * patternHeight * patternDepth * patternLayers);
            int AnimationCount = CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value].SpriteInfo.SpriteId.Count / SprPreFrame;

            SprFramesSlider.Minimum = CurrentSprDir;
            SprFramesSlider.Maximum = ObjectSprList.Count - ((4 - CurrentSprDir) * patternLayers);
            if (patternLayers > 1)
            {
                if (CurrentSprDir > 0)
                {
                    SprFramesSlider.Minimum += (CurrentSprDir * 1);
                }

                if (SprBlendLayer.IsChecked == true)
                {
                    SprFramesSlider.Minimum += 1;
                    if (A_SprAnimation.Value > 1)
                        SprFramesSlider.Maximum += 1;
                }
            }

            SprFramesSlider.TickFrequency = patternWidth * patternLayers;
            if (SprAddonSlider.Value > 0)
            {
                SprFramesSlider.TickFrequency = (SprFramesSlider.TickFrequency * 2);
                if (SprAddonSlider.Value == 1)
                    SprFramesSlider.Maximum -= (patternWidth * patternLayers);
                else if (SprAddonSlider.Value == 2)
                    SprFramesSlider.Minimum += (patternWidth * patternLayers);
            }

            SprFramesSlider.Value = SprFramesSlider.Minimum;

            if (ObjectMenu.SelectedIndex != 0)
            {
                SprFramesSlider.Maximum = (int)A_SprAnimation.Value - 1;
                SprFramesSlider.TickFrequency = 1;
            }

            List<Image> images = Utils.GetLogicalChildCollection<Image>(ObjectSprImgPanel);
            foreach (Image child in images)
            {
                ObjectSprImgPanel.Children.Remove(child);
            }

            int ObjectSprImgCount = ObjectMenu.SelectedIndex == 0 ? 1 : (int)(patternWidth * patternHeight);
            int ImgPanelWidth = 32;
            int ImgPanelHeight = 32;

            if (ObjectSprList.Count == 1)
            {
                SprFramesSlider.Minimum = 0;
                SprFramesSlider.Maximum = 0;
            }

            for (int i = 0; i < (ObjectSprImgCount); i++)
            {
                Image img = new Image
                {
                    Stretch = Stretch.None,
                    MinWidth = 32,
                    MinHeight = 32,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    AllowDrop = true
                };
                img.Drop += Spr_Drop;
                img.PreviewMouseLeftButtonDown += Img_PreviewMouseLeftButtonDown;
                img.Source = ObjectSprList[(int)SprFramesSlider.Minimum + i].Image;
                img.Tag = i;
                ObjectSprImgPanel.Children.Add(img);
                if (ObjectSprList[i].Image.Width > ImgPanelWidth)
                    ImgPanelWidth = (int)ObjectSprList[i].Image.Width;
                if (ObjectSprList[i].Image.Height > ImgPanelHeight)
                    ImgPanelHeight = (int)ObjectSprList[i].Image.Height;

            }
            ObjectSprImgPanel.Width = ObjectMenu.SelectedIndex == 0 ? ImgPanelWidth : ImgPanelWidth * patternWidth;
            ObjectSprImgPanel.Height = ObjectMenu.SelectedIndex == 0 ? ImgPanelHeight : ImgPanelHeight * patternHeight;
        }

        private void Img_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Image img = e.Source as Image;
            List<Image> images = Utils.GetLogicalChildCollection<Image>(ObjectSprImgPanel);
            SprListView.SelectedIndex = (int)ObjectSprList[((int)SprFramesSlider.Value * images.Count) + (int)img.Tag].Id;
            ScrollViewer scrollViewer = Utils.FindVisualChild<ScrollViewer>(SprListView);
            scrollViewer.ScrollToVerticalOffset(SprListView.SelectedIndex);
        }

        private void ChangeObjectSprImg(FrameGroup frameGroup, bool reset)
        {
            uint patternWidth = frameGroup.SpriteInfo.HasPatternWidth ? frameGroup.SpriteInfo.PatternWidth : 1;
            uint patternHeight = frameGroup.SpriteInfo.HasPatternHeight ? frameGroup.SpriteInfo.PatternHeight : 1;
            uint patternDepth = frameGroup.SpriteInfo.HasPatternDepth ? frameGroup.SpriteInfo.PatternDepth : 1;
            uint patternLayers = frameGroup.SpriteInfo.HasLayers ? frameGroup.SpriteInfo.Layers : 1;
            int SprPreFrame = (int)(patternWidth * patternHeight * patternDepth * patternLayers);
            int AnimationCount = frameGroup.SpriteInfo.SpriteId.Count / SprPreFrame;

            SprAddonSlider.Maximum = patternHeight - 1;
            A_SprAnimation.Value = AnimationCount;

            CurrentSprDir = 0;
            if (reset)
            {
                ObjectOutfitSprList.Clear();
                ObjectAddonSprList.Clear();
                ObjectMountSprList.Clear();
                ObjectAddonMountSprList.Clear();
                ObjectSprList.Clear();

                for (int x = 0; x < A_SprAnimation.Value; x++)
                {
                    for (int i = SprPreFrame * x; i < SprPreFrame * (x + 1); i++)
                    {
                        if ((i < (patternWidth * patternLayers) + (SprPreFrame * x)) || ObjectMenu.SelectedIndex != 0)
                        {
                            if (i < ((patternWidth * patternHeight * patternLayers) + (SprPreFrame * x)))
                                ObjectOutfitSprList.Add(new ShowList() { Id = frameGroup.SpriteInfo.SpriteId[i], Image = Utils.BitmapToBitmapImage(MainWindow.SprLists[(int)frameGroup.SpriteInfo.SpriteId[i]]), Pos = i });
                            else
                                ObjectMountSprList.Add(new ShowList() { Id = frameGroup.SpriteInfo.SpriteId[i], Image = Utils.BitmapToBitmapImage(MainWindow.SprLists[(int)frameGroup.SpriteInfo.SpriteId[i]]), Pos = i });
                        }
                        else if ((i >= (patternWidth * patternLayers) + (SprPreFrame * x) && i < (patternWidth * patternHeight * patternLayers) + (SprPreFrame * x)))
                            ObjectAddonSprList.Add(new ShowList() { Id = frameGroup.SpriteInfo.SpriteId[i], Image = Utils.BitmapToBitmapImage(MainWindow.SprLists[(int)frameGroup.SpriteInfo.SpriteId[i]]), Pos = i });
                        else if (i >= ((patternWidth * patternHeight * patternLayers) + (SprPreFrame * x)) && i < (patternWidth * patternHeight * patternDepth * patternLayers - (patternWidth * patternDepth * patternLayers) + (SprPreFrame * x)))
                            ObjectMountSprList.Add(new ShowList() { Id = frameGroup.SpriteInfo.SpriteId[i], Image = Utils.BitmapToBitmapImage(MainWindow.SprLists[(int)frameGroup.SpriteInfo.SpriteId[i]]), Pos = i });
                        else if (i >= (patternWidth * patternHeight * patternDepth * patternLayers - (patternWidth * patternDepth * patternLayers) + (SprPreFrame * x)))
                            ObjectAddonMountSprList.Add(new ShowList() { Id = frameGroup.SpriteInfo.SpriteId[i], Image = Utils.BitmapToBitmapImage(MainWindow.SprLists[(int)frameGroup.SpriteInfo.SpriteId[i]]), Pos = i });
                    }

                }
            }

            if (SprAddonSlider.Value == 0 && SprMount.IsChecked == false)
                ObjectSprList = ObjectOutfitSprList;
            else if (SprAddonSlider.Value == 0 && SprMount.IsChecked == true)
                ObjectSprList = ObjectMountSprList;
            else if (SprAddonSlider.Value > 0 && SprMount.IsChecked == false)
                ObjectSprList = ObjectAddonSprList;
            else if (SprAddonSlider.Value > 0 && SprMount.IsChecked == true)
                ObjectSprList = ObjectAddonMountSprList;

            ChangeSprDirection();
            SetSpriteFrame();

        }
        private void SprMount_Click(object sender, RoutedEventArgs e)
        {
            ChangeObjectSprImg(CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value], false);
        }

        private void SprBlendLayer_Click(object sender, RoutedEventArgs e)
        {
            ChangeObjectSprImg(CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value], false);
        }

        private void SprAddonSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ChangeObjectSprImg(CurrentObjectAppearance.FrameGroup[(int)SprGroupSlider.Value], false);
        }

        private void BoxPerDirection_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (BoundingBoxList.Count > 4)
                BoundingBoxList.RemoveAt(BoundingBoxList.Count - 1);
        }

        private void A_SprGroups_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            SprGroupSlider.Maximum = (int)A_SprGroups.Value - 1;
        }

        private void A_SprLayers_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (A_SprLayers.Value > 1)
                SprBlendLayer.IsEnabled = true;
            else
                SprBlendLayer.IsEnabled = false;
        }

        private void A_SprPaternZ_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (A_SprPaternZ.Value > 1)
                SprMount.IsEnabled = true;
            else
                SprMount.IsEnabled = false;
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
                CurrentObjectAppearance.Flags.Market.RestrictToProfession.Clear();
                if ((bool)A_FlagProfessionAny.IsChecked) CurrentObjectAppearance.Flags.Market.RestrictToProfession.Add(PLAYER_PROFESSION.Any);
                if ((bool)A_FlagProfessionNone.IsChecked) CurrentObjectAppearance.Flags.Market.RestrictToProfession.Add(PLAYER_PROFESSION.None);
                if ((bool)A_FlagProfessionKnight.IsChecked) CurrentObjectAppearance.Flags.Market.RestrictToProfession.Add(PLAYER_PROFESSION.Knight);
                if ((bool)A_FlagProfessionPaladin.IsChecked) CurrentObjectAppearance.Flags.Market.RestrictToProfession.Add(PLAYER_PROFESSION.Paladin);
                if ((bool)A_FlagProfessionSorcerer.IsChecked) CurrentObjectAppearance.Flags.Market.RestrictToProfession.Add(PLAYER_PROFESSION.Sorcerer);
                if ((bool)A_FlagProfessionDruid.IsChecked) CurrentObjectAppearance.Flags.Market.RestrictToProfession.Add(PLAYER_PROFESSION.Druid);
                if ((bool)A_FlagProfessionPromoted.IsChecked) CurrentObjectAppearance.Flags.Market.RestrictToProfession.Add(PLAYER_PROFESSION.Promoted);
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

            SaveSprPatterns((int)SprGroupSlider.Value);
            StorePreviousGroup((int)SprGroupSlider.Value);
            if (ObjectMenu.SelectedIndex == 0)
                MainWindow.appearances.Outfit[ObjListView.SelectedIndex] = CurrentObjectAppearance.Clone();
            else if (ObjectMenu.SelectedIndex == 1)
                MainWindow.appearances.Object[ObjListView.SelectedIndex] = CurrentObjectAppearance.Clone();
            else if (ObjectMenu.SelectedIndex == 2)
                MainWindow.appearances.Effect[ObjListView.SelectedIndex] = CurrentObjectAppearance.Clone();
            else if (ObjectMenu.SelectedIndex == 3)
                MainWindow.appearances.Missile[ObjListView.SelectedIndex] = CurrentObjectAppearance.Clone();
            StatusBar.MessageQueue.Enqueue($"Saved Current Object.", null, null, null, false, true, TimeSpan.FromSeconds(2));
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
            }else
                StatusBar.MessageQueue.Enqueue($"Copy Flags First.", null, null, null, false, true, TimeSpan.FromSeconds(2));
        }

        private void OpenSpriteManager_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SprEditor sprEditor = new SprEditor();
            SprEditor.CustomSheetsList.Clear();
            foreach (MainWindow.Catalog sheet in MainWindow.catalog)
            {
                string _sprPath = String.Format("{0}{1}", MainWindow._assetsPath, sheet.File);
                if (File.Exists(_sprPath))
                {
                    if (sheet.FirstSpriteid >= 250000)
                    {
                        MainWindow.CustomSprLastId = sheet.LastSpriteid;
                        SprEditor.CustomCatalog.Add(sheet);
                        using System.Drawing.Bitmap SheetM = LZMA.DecompressFileLZMA(_sprPath);
                        var lockedBitmap = new LockBitmap(SheetM);
                        lockedBitmap.LockBits();
                        for (int y = 0; y < SheetM.Height; y++)
                        {
                            for (int x = 0; x < SheetM.Width; x++)
                            {
                                if (lockedBitmap.GetPixel(x, y) == System.Drawing.Color.FromArgb(255, 255, 0, 255))
                                {
                                    lockedBitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(0, 255, 0, 255));
                                }
                            }
                        }
                        lockedBitmap.UnlockBits();
                        SprEditor.CustomSheetsList.Add(new ShowList() { Id = (uint)sheet.FirstSpriteid, Image = Utils.BitmapToBitmapImage(SheetM), Name = sheet.File });
                    }
                }
            }
            sprEditor.SheetsList.ItemsSource = SprEditor.CustomSheetsList;
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
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        private void NewObjectDialogHost_OnDialogClosing(object sender, DialogClosingEventArgs eventArgs)
        {
            if (!Equals(eventArgs.Parameter, true)) return;

            Appearance newObject = new Appearance();
            newObject.Flags = new AppearanceFlags();
            FrameGroup frameIdleGroup = new FrameGroup();
            frameIdleGroup.SpriteInfo = new SpriteInfo();
            frameIdleGroup.SpriteInfo.Layers = uint.Parse(NLayers.Text);
            frameIdleGroup.SpriteInfo.PatternWidth = uint.Parse(NPatternX.Text);
            frameIdleGroup.SpriteInfo.PatternHeight = uint.Parse(NPatternY.Text);
            frameIdleGroup.SpriteInfo.PatternDepth = uint.Parse(NPatternZ.Text);

            if (NObjectType.SelectedIndex == 0)
            {
                frameIdleGroup.Id = 0;
                frameIdleGroup.FixedFrameGroup = FIXED_FRAME_GROUP.OutfitIdle;
                if (int.Parse(NIdleGroupCount.Text) > 1)
                {
                    SpriteAnimation spriteAnimation = new SpriteAnimation();
                    for (int i = 0; i < (int.Parse(NIdleGroupCount.Text)); i++)
                        spriteAnimation.SpritePhase.Add(new SpritePhase() { DurationMin = 100, DurationMax = 100 });
                    frameIdleGroup.SpriteInfo.Animation = spriteAnimation;
                }
                int spritecount = (int)(frameIdleGroup.SpriteInfo.Layers * frameIdleGroup.SpriteInfo.PatternWidth * frameIdleGroup.SpriteInfo.PatternHeight * frameIdleGroup.SpriteInfo.PatternDepth * int.Parse(NIdleGroupCount.Text));
                for (int i = 0; i < spritecount; i++)
                    frameIdleGroup.SpriteInfo.SpriteId.Add(0);

                newObject.Id = (uint)(MainWindow.appearances.Outfit[^1].Id + 1);
                newObject.FrameGroup.Add(frameIdleGroup);
                if (NMoveGroup.IsChecked == true)
                {
                    FrameGroup frameMoveGroup = new FrameGroup();
                    frameMoveGroup.Id = 1;
                    frameMoveGroup.FixedFrameGroup = FIXED_FRAME_GROUP.OutfitMoving;
                    frameMoveGroup.SpriteInfo = new SpriteInfo();
                    frameMoveGroup.SpriteInfo.Layers = uint.Parse(NLayers.Text);
                    frameMoveGroup.SpriteInfo.PatternWidth = uint.Parse(NPatternX.Text);
                    frameMoveGroup.SpriteInfo.PatternHeight = uint.Parse(NPatternY.Text);
                    frameMoveGroup.SpriteInfo.PatternDepth = uint.Parse(NPatternZ.Text);
                    SpriteAnimation spriteMoveAnimation = new SpriteAnimation();
                    for (int i = 0; i < (int.Parse(NMoveGroupCount.Text)); i++)
                        spriteMoveAnimation.SpritePhase.Add(new SpritePhase() { DurationMin = 100, DurationMax = 100 });
                    frameMoveGroup.SpriteInfo.Animation = spriteMoveAnimation;
                    spritecount = (int)(frameMoveGroup.SpriteInfo.Layers * frameMoveGroup.SpriteInfo.PatternWidth * frameMoveGroup.SpriteInfo.PatternHeight * frameMoveGroup.SpriteInfo.PatternDepth * int.Parse(NMoveGroupCount.Text));
                    for (int i = 0; i < spritecount; i++)
                        frameMoveGroup.SpriteInfo.SpriteId.Add(0);
                    newObject.FrameGroup.Add(frameMoveGroup);
                }
                MainWindow.appearances.Outfit.Add(newObject);
                ThingsOutfit.Add(new ShowList() { Id = newObject.Id});

            }
            else if (NObjectType.SelectedIndex == 1)
            {
                frameIdleGroup.FixedFrameGroup = FIXED_FRAME_GROUP.ObjectInitial;
                if (int.Parse(NIdleGroupCount.Text) > 1)
                {
                    SpriteAnimation spriteAnimation = new SpriteAnimation();
                    for (int i = 0; i < (int.Parse(NIdleGroupCount.Text)); i++)
                        spriteAnimation.SpritePhase.Add(new SpritePhase() { DurationMin = 100, DurationMax = 100 });
                    frameIdleGroup.SpriteInfo.Animation = spriteAnimation;
                }
                int spritecount = (int)(frameIdleGroup.SpriteInfo.Layers * frameIdleGroup.SpriteInfo.PatternWidth * frameIdleGroup.SpriteInfo.PatternHeight * frameIdleGroup.SpriteInfo.PatternDepth * int.Parse(NIdleGroupCount.Text));
                for (int i = 0; i < spritecount; i++)
                    frameIdleGroup.SpriteInfo.SpriteId.Add(0);

                newObject.Id = (uint)(MainWindow.appearances.Object[^1].Id + 1);
                newObject.FrameGroup.Add(frameIdleGroup);
                MainWindow.appearances.Object.Add(newObject);
                ThingsItem.Add(new ShowList() { Id = newObject.Id});

            }
            else if (NObjectType.SelectedIndex == 2)
            {
                frameIdleGroup.FixedFrameGroup = FIXED_FRAME_GROUP.ObjectInitial;
                if (int.Parse(NIdleGroupCount.Text) > 1)
                {
                    SpriteAnimation spriteAnimation = new SpriteAnimation();
                    for (int i = 0; i < (int.Parse(NIdleGroupCount.Text)); i++)
                        spriteAnimation.SpritePhase.Add(new SpritePhase() { DurationMin = 100, DurationMax = 100 });
                    frameIdleGroup.SpriteInfo.Animation = spriteAnimation;
                }
                int spritecount = (int)(frameIdleGroup.SpriteInfo.Layers * frameIdleGroup.SpriteInfo.PatternWidth * frameIdleGroup.SpriteInfo.PatternHeight * frameIdleGroup.SpriteInfo.PatternDepth * int.Parse(NIdleGroupCount.Text));
                for (int i = 0; i < spritecount; i++)
                    frameIdleGroup.SpriteInfo.SpriteId.Add(0);

                newObject.Id = (uint)(MainWindow.appearances.Effect[^1].Id + 1);
                newObject.FrameGroup.Add(frameIdleGroup);
                MainWindow.appearances.Effect.Add(newObject);
                ThingsEffect.Add(new ShowList() { Id = newObject.Id});

            }
            else if (NObjectType.SelectedIndex == 3)
            {
                frameIdleGroup.FixedFrameGroup = FIXED_FRAME_GROUP.ObjectInitial;
                if (int.Parse(NIdleGroupCount.Text) > 1)
                {
                    SpriteAnimation spriteAnimation = new SpriteAnimation();
                    for (int i = 0; i < (int.Parse(NIdleGroupCount.Text)); i++)
                        spriteAnimation.SpritePhase.Add(new SpritePhase() { DurationMin = 100, DurationMax = 100 });
                    frameIdleGroup.SpriteInfo.Animation = spriteAnimation;
                }
                int spritecount = (int)(frameIdleGroup.SpriteInfo.Layers * frameIdleGroup.SpriteInfo.PatternWidth * frameIdleGroup.SpriteInfo.PatternHeight * frameIdleGroup.SpriteInfo.PatternDepth * int.Parse(NIdleGroupCount.Text));
                for (int i = 0; i < spritecount; i++)
                    frameIdleGroup.SpriteInfo.SpriteId.Add(0);

                newObject.Id = (uint)(MainWindow.appearances.Missile[^1].Id + 1);
                newObject.FrameGroup.Add(frameIdleGroup);
                MainWindow.appearances.Missile.Add(newObject);
                ThingsMissile.Add(new ShowList() { Id = newObject.Id});

            }
        }

        private void NObjectType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                if (NObjectType.SelectedIndex == 0)
                    NMoveGroup.IsEnabled = true;
                else
                    NMoveGroup.IsEnabled = false;
            }
        }

        private void SpriteExport_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ShowList data = (ShowList)SprListView.SelectedItem;
            if (data != null && data.Image != null)
            {
                System.Drawing.Bitmap targetImg = new System.Drawing.Bitmap((int)data.Image.Width, (int)data.Image.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(targetImg);
                g.Clear(System.Drawing.Color.FromArgb(255, 255, 0, 255));
                System.Drawing.Image image = System.Drawing.Image.FromStream(MainWindow.SprLists[(int)data.Id]);
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.DrawImage(image, new System.Drawing.Rectangle(0, 0, targetImg.Width, targetImg.Height), new System.Drawing.Rectangle(0, 0, targetImg.Width, targetImg.Height), System.Drawing.GraphicsUnit.Pixel);
                g.Dispose();
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Bitmap Image (.bmp)|*.bmp|Gif Image (.gif)|*.gif|JPEG Image (.jpeg)|*.jpeg|Png Image (.png)|*.png"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
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
                    }
                    targetImg.Dispose();
                }

            }
        }
    }
}
