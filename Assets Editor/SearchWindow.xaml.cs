using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Tibia.Protobuf.Appearances;

namespace Assets_Editor
{
    /// <summary>
    /// Interaction logic for SearchWindow.xaml
    /// </summary>
    public partial class SearchWindow : Window
    {
        private dynamic _editor;
        public SearchWindow(dynamic editor, bool legacy)
        {
            InitializeComponent();
            _editor = editor;
        }
        private void ItemListView_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            VirtualizingStackPanel panel = Utils.FindVisualChild<VirtualizingStackPanel>(ItemListView);
            if (ItemListView.Items.Count > 0 && panel != null)
            {
                int offset = (int)panel.VerticalOffset;
                for (int i = 0; i < ItemListView.Items.Count; i++)
                {
                    if (i >= offset && i < Math.Min(offset + 20, ItemListView.Items.Count))
                    {
                        ShowList item = (ShowList)ItemListView.Items[i];
                        if (MainWindow.LegacyClient)
                            item.Image = Utils.BitmapToBitmapImage(LegacyAppearance.GetObjectImage(MainWindow.appearances.Object[(int)item.Id - 100], MainWindow.MainSprStorage));
                        else
                        {
                            Appearance obj = MainWindow.appearances.Object.FirstOrDefault(o => o.Id == item.Id);
                            item.Image = Utils.BitmapToBitmapImage(MainWindow.getSpriteStream((int)obj.FrameGroup[0].SpriteInfo.SpriteId[0]));
                        }
                    }
                    else
                    {
                        ShowList item = (ShowList)ItemListView.Items[i];
                        item.Image = null;
                    }
                }
            }
        }


        private void ItemListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _editor.ObjectMenu.SelectedIndex = 1;
            ShowList showList = (ShowList)ItemListView.SelectedItem;

            if (showList != null)
            {
                _editor.ObjListView.SelectedItem = (_editor.ObjListView.Items as IEnumerable).OfType<ShowList>().FirstOrDefault(item => item.Id == showList.Id);
            }
        }

        private void SearchItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ItemListView.Items.Clear();

            foreach (Appearance item in MainWindow.appearances.Object)
            {

                if (A_FlagGround.IsChecked == true && (item.Flags.Bank == null))
                    continue;
                
                if (A_FlagClip.IsChecked == true && item.Flags.Clip != true)
                    continue;
                
                if (A_FlagBottom.IsChecked == true && item.Flags.Bottom != true)
                    continue;

                if (A_FlagTop.IsChecked == true && item.Flags.Top != true)
                    continue;

                if (A_FlagContainer.IsChecked == true && item.Flags.Container != true)
                    continue;

                if (A_FlagCumulative.IsChecked == true && item.Flags.Cumulative != true)
                    continue;

                if (A_FlagUsable.IsChecked == true && item.Flags.Usable != true)
                    continue;

                if (A_FlagForceuse.IsChecked == true && item.Flags.Forceuse != true)
                    continue;

                if (A_FlagMultiuse.IsChecked == true && item.Flags.Multiuse != true)
                    continue;

                if (A_FlagWrite.IsChecked == true && (item.Flags.Write == null))
                    continue;

                if (A_FlagWriteOnce.IsChecked == true && (item.Flags.WriteOnce == null))
                    continue;

                if (A_FlagLiquidpool.IsChecked == true && item.Flags.HasLiquidpool != true)
                    continue;

                if (A_FlagUnpass.IsChecked == true && item.Flags.HasUnpass != true)
                    continue;

                if (A_FlagUnmove.IsChecked == true && item.Flags.HasUnmove != true)
                    continue;

                if (A_FlagUnsight.IsChecked == true && item.Flags.HasUnsight != true)
                    continue;

                if (A_FlagAvoid.IsChecked == true && item.Flags.HasAvoid != true)
                    continue;

                if (A_FlagNoMoveAnimation.IsChecked == true && item.Flags.HasNoMovementAnimation != true)
                    continue;

                if (A_FlagTake.IsChecked == true && item.Flags.HasTake != true)
                    continue;

                if (A_FlagLiquidcontainer.IsChecked == true && item.Flags.HasLiquidcontainer != true)
                    continue;

                if (A_FlagHang.IsChecked == true && item.Flags.HasHang != true)
                    continue;

                if (A_FlagHookSouth.IsChecked == true && item.Flags.HookSouth != true)
                    continue;

                if (A_FlagHookEast.IsChecked == true && item.Flags.HookEast != true)
                    continue;

                if (A_FlagRotate.IsChecked == true && item.Flags.HasRotate != true)
                    continue;

                if (A_FlagLight.IsChecked == true && (item.Flags.Light == null))
                    continue;

                if (A_FlagDontHide.IsChecked == true && item.Flags.HasDontHide != true)
                    continue;

                if (A_FlagTranslucent.IsChecked == true && item.Flags.HasTranslucent != true)
                    continue;

                if (A_FlagShift.IsChecked == true && (item.Flags.Shift == null))
                    continue;

                if (A_FlagHeight.IsChecked == true && (item.Flags.Height == null))
                    continue;

                if (A_FlagLyingObject.IsChecked == true && item.Flags.HasLyingObject != true)
                    continue;

                if (A_FlagAutomap.IsChecked == true && (item.Flags.Automap == null))
                    continue;

                if (A_FlagLenshelp.IsChecked == true && (item.Flags.Lenshelp == null))
                    continue;

                if (A_FlagFullGround.IsChecked == true && item.Flags.HasFullbank != true)
                    continue;

                if (A_FlagIgnoreLook.IsChecked == true && item.Flags.HasIgnoreLook != true)
                    continue;

                if (A_FlagClothes.IsChecked == true && (item.Flags.Clothes == null))
                    continue;

                if (A_FlagDefaultAction.IsChecked == true && (item.Flags.DefaultAction == null))
                    continue;

                if (A_FlagMarket.IsChecked == true && (item.Flags.Market == null))
                    continue;

                if (A_FlagWrap.IsChecked == true && item.Flags.HasWrap != true)
                    continue;

                if (A_FlagUnwrap.IsChecked == true && item.Flags.HasUnwrap != true)
                    continue;

                if (A_FlagTopeffect.IsChecked == true && item.Flags.HasTop != true)
                    continue;

                if (A_FlagWearout.IsChecked == true && item.Flags.HasWearout != true)
                    continue;

                if (A_FlagAnimated.IsChecked == true && item.FrameGroup[0].SpriteInfo.PatternFrames == 1)
                    continue;

                    ItemListView.Items.Add(new ShowList() { Id = item.Id});
            }
        }
    }
}
