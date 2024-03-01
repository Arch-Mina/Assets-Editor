using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tibia.Protobuf.Appearances;
using static Assets_Editor.OTB;

namespace Assets_Editor
{
    /// <summary>
    /// Interaction logic for OTBEditor.xaml
    /// </summary>
    public partial class OTBEditor : Window
    {
        private dynamic _editor;
        private bool _legacy = false;
        public OTBEditor(dynamic editor, bool legacy)
        {
            InitializeComponent();
            _editor = editor;
            _legacy = legacy;
        }
        private Dictionary<uint, Appearance> appearanceByClientId = new Dictionary<uint, Appearance>();
        private List<ServerItem> OTBItems = new List<ServerItem>();
        private bool OTBLoaded = false;
        private uint MajorVersion;
        private uint MinorVersion;
        private uint BuildNumber;
        private uint ClientVersion;
        private void OpenOTBButton_Click(object sender, RoutedEventArgs e)
        {

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "OTB Files (*.otb)|*.otb";
            if (openFileDialog.ShowDialog() == true)
            {
                appearanceByClientId.Clear();
                ItemListView.ItemsSource = null;
                OtbPathText.Text = openFileDialog.FileName;
                string selectedFilePath = openFileDialog.FileName;
                OTBReader otbReader = new OTBReader();
                OTBItems = new List<ServerItem>();
                bool stats = otbReader.Read(selectedFilePath);
                if (stats == true)
                {
                    OTBLoaded = true;
                    MajorVersion = otbReader.MajorVersion;
                    MinorVersion = otbReader.MinorVersion;
                    BuildNumber = otbReader.BuildNumber;
                    ClientVersion = otbReader.ClientVersion;
                    foreach (Appearance appearance in MainWindow.appearances.Object)
                    {
                        appearanceByClientId.Add(appearance.Id, appearance); // Replace '0' with the desired value for each Appearance key
                    }

                    foreach (var item in otbReader.Items)
                    {
                        OTBItems.Add(item);
                        ItemListView.Items.Add(new ShowList() { Id = item.ServerId, Cid = item.ClientId });
                    }
                }
            }
        }

        private void ItemListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ServerItem item = OTBItems[ItemListView.SelectedIndex];
            I_ServerId.Value = item.ServerId;
            I_ClientId.Value = item.ClientId;
            I_Name.Text = item.Name;
            I_StackOrder.SelectedIndex = (int)item.StackOrder;
            I_Type.SelectedIndex = (int)item.Type;
            I_GroundSpeed.Value = item.GroundSpeed;
            I_Unpassable.IsChecked = item.Unpassable;
            I_Movable.IsChecked = item.Movable;
            I_BlockMissiles.IsChecked = item.BlockMissiles;
            I_BlockPath.IsChecked = item.BlockPathfinder;
            I_Pickable.IsChecked = item.Pickupable;
            I_Stackable.IsChecked = item.Stackable;
            I_ForceUse.IsChecked = item.ForceUse;
            I_MultiUse.IsChecked = item.MultiUse;
            I_Rotateable.IsChecked = item.Rotatable;
            I_Hangable.IsChecked = item.Hangable;
            I_HookSouth.IsChecked = item.HookSouth;
            I_HookEast.IsChecked = item.HookEast;
            I_Elevation.IsChecked = item.HasElevation;
            I_IgnoreLook.IsChecked = item.IgnoreLook;
            I_Readable.IsChecked = item.Readable;
            I_FullGround.IsChecked = item.FullGround;
            I_MiniMapColor.Value = item.MinimapColor;
            I_ShowAs.Value = item.TradeAs;
            I_LightLevel.Value = item.LightLevel;
            I_LightColor.Value = item.LightColor;
            I_MaxReadWriteChars.Value = item.MaxReadWriteChars;
            I_MaxReadChars.Value = item.MaxReadChars;

            _editor.ObjectMenu.SelectedIndex = 1;

            foreach (ShowList showlist in _editor.ObjListView.Items)
            {
                if (showlist.Id == item.ClientId)
                {
                    _editor.ObjListView.SelectedItem = showlist;
                }
            }
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
                        if (_legacy)
                        {
                            ShowList item = (ShowList)ItemListView.Items[i];
                            if (appearanceByClientId.ContainsKey(item.Cid))
                            {
                                item.Image = Utils.BitmapToBitmapImage(LegacyAppearance.GetObjectImage(appearanceByClientId[item.Cid], MainWindow.MainSprStorage));
                            }
                        }
                        else
                        {
                            ShowList item = (ShowList)ItemListView.Items[i];
                            if (appearanceByClientId.ContainsKey(item.Cid))
                            {
                                item.Image = Utils.BitmapToBitmapImage(MainWindow.getSpriteStream((int)appearanceByClientId[item.Cid].FrameGroup[0].SpriteInfo.SpriteId[0]));
                            }
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

        private void CreateMissingItems(object sender, RoutedEventArgs e)
        {
            if (!OTBLoaded)
                return;
            ushort lastCid = 0;
            int lastServerId = 0;
            uint maxCid = MainWindow.appearances.Object.Last().Id;

            foreach (ServerItem item in OTBItems)
            {
                if (item.ClientId > lastCid)
                {
                    lastCid = item.ClientId;
                }
                if (item.ServerId > lastServerId)
                {
                    lastServerId = item.ServerId;
                }
            }
            lastServerId++;
            ushort newItemCounter = 0;

            for (ushort i = 100; i <= maxCid; i++)
            {
                ServerItem itemExist = OTBItems.FirstOrDefault(x => x.ClientId == i);
                if (itemExist == null && appearanceByClientId.ContainsKey(i))
                {
                    ServerItem item = new ServerItem(appearanceByClientId[i]);
                    if (BitConverter.ToString(item.SpriteHash) != "4B-1B-1C-88-FF-2F-AF-29-0E-BC-39-2B-11-6D-10-1C" || i > lastCid)
                    {
                        item.ClientId = i;
                        item.ServerId = (ushort)lastServerId;
                        OTBItems.Add(item);
                        ItemListView.Items.Add(new ShowList() { Id = item.ServerId, Cid = item.ClientId });
                        if (_legacy)
                            NewItemListView.Items.Add(new ShowList() { Id = item.ServerId, Cid = item.ClientId, Image = Utils.BitmapToBitmapImage(LegacyAppearance.GetObjectImage(appearanceByClientId[item.ClientId], MainWindow.MainSprStorage)) });
                        else
                            NewItemListView.Items.Add(new ShowList() { Id = item.ServerId, Cid = item.ClientId, Image = Utils.BitmapToBitmapImage(MainWindow.getSpriteStream((int)appearanceByClientId[item.ClientId].FrameGroup[0].SpriteInfo.SpriteId[0])) });
                        lastServerId++;
                        newItemCounter++;
                    }
                }
            }
        }
        private void CheckMisMatcheditems(object sender, RoutedEventArgs e)
        {
            if (!OTBLoaded)
                return;
            NewItemListView.Items.Clear();
            foreach (ServerItem item in OTBItems)
            {
                if (!appearanceByClientId.ContainsKey(item.ClientId))
                {
                    continue;
                }

                ServerItem citem = new ServerItem(appearanceByClientId[item.ClientId]);
                if (!Utils.ByteArrayCompare(item.SpriteHash,citem.SpriteHash))
                {
                    if (_legacy)
                        NewItemListView.Items.Add(new ShowList() { Id = item.ServerId, Cid = item.ClientId, Image = Utils.BitmapToBitmapImage(LegacyAppearance.GetObjectImage(appearanceByClientId[item.ClientId], MainWindow.MainSprStorage)) });
                    else
                        NewItemListView.Items.Add(new ShowList() { Id = item.ServerId, Cid = item.ClientId, Image = Utils.BitmapToBitmapImage(MainWindow.getSpriteStream((int)appearanceByClientId[item.ClientId].FrameGroup[0].SpriteInfo.SpriteId[0])) });
                }
            }

        }
        private void NewItemListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            if (!OTBLoaded)
                return;
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "OTB files (*.otb)|*.otb";
            dialog.Title = "Save OTB File";

            if (dialog.ShowDialog() == true)
            {
                if (dialog.FileName.Length == 0)
                {
                    return;
                }

                try
                {
                    OTBWriter writer = new OTBWriter(OTBItems);
                    writer.MajorVersion = MajorVersion;
                    writer.MinorVersion = MinorVersion;
                    writer.BuildNumber = BuildNumber;
                    writer.ClientVersion = ClientVersion;
                    if (writer.Write(dialog.FileName))
                    {
                        Debug.WriteLine("OTB saved");
                    }
                }
                catch (UnauthorizedAccessException exception)
                {
                    MessageBox.Show(exception.Message);
                }
            }
        }

        private void ReloadItemsAttr(object sender, RoutedEventArgs e)
        {
            foreach (ShowList selectedItem in ItemListView.SelectedItems)
            {
                ServerItem item = OTBItems[ItemListView.Items.IndexOf(selectedItem)];
                ServerItem itemAttr = new ServerItem(appearanceByClientId[item.ClientId]);
                itemAttr.ServerId = item.ServerId;
                itemAttr.ClientId = item.ClientId;
                OTBItems[ItemListView.Items.IndexOf(selectedItem)] = itemAttr;
            }
        }

        private void SaveItemButton_Click(object sender, RoutedEventArgs e)
        {
            ServerItem item = OTBItems[ItemListView.SelectedIndex];
            item.ServerId = (ushort)I_ServerId.Value;
            item.ClientId = (ushort)I_ClientId.Value;
            item.Name = I_Name.Text;
            item.StackOrder = (TileStackOrder)I_StackOrder.SelectedIndex;
            item.Type = (ServerItemType)I_Type.SelectedIndex;
            item.GroundSpeed = (ushort)I_GroundSpeed.Value;
            item.Unpassable = (bool)I_Unpassable.IsChecked;
            item.Movable = (bool)I_Movable.IsChecked;
            item.BlockMissiles = (bool)I_BlockMissiles.IsChecked;
            item.BlockPathfinder = (bool)I_BlockPath.IsChecked;
            item.Pickupable = (bool)I_Pickable.IsChecked;
            item.Stackable = (bool)I_Stackable.IsChecked;
            item.ForceUse = (bool)I_ForceUse.IsChecked;
            item.MultiUse = (bool)I_MultiUse.IsChecked;
            item.Rotatable = (bool)I_Rotateable.IsChecked;
            item.Hangable = (bool)I_Hangable.IsChecked;
            item.HookSouth = (bool)I_HookSouth.IsChecked;
            item.HookEast = (bool)I_HookEast.IsChecked;
            item.HasElevation = (bool)I_Elevation.IsChecked;
            item.IgnoreLook = (bool)I_IgnoreLook.IsChecked;
            item.Readable = (bool)I_Readable.IsChecked;
            item.FullGround = (bool)I_FullGround.IsChecked;
            item.MinimapColor = (ushort)I_MiniMapColor.Value;
            item.TradeAs = (ushort)I_ShowAs.Value;
            item.LightLevel = (ushort)I_LightLevel.Value;
            item.LightColor = (ushort)I_LightColor.Value;
            item.MaxReadWriteChars = (ushort)I_MaxReadWriteChars.Value;
            item.MaxReadChars = (ushort)I_MaxReadChars.Value;
        }
    }
}
