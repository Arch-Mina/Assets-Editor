using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using Tibia.Protobuf.Appearances;

namespace Assets_Editor
{
    public partial class NpcExportWindow : Window
    {
        private bool isExporting = false;

        public NpcExportWindow()
        {
            InitializeComponent();
            LoadDefaultPath();
        }

        private void LoadDefaultPath()
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            FolderPathTextBox.Text = Path.Combine(desktopPath, "NPC_Exports");
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select destination folder",
                InitialDirectory = FolderPathTextBox.Text
            };

            if (dialog.ShowDialog() == true)
            {
                FolderPathTextBox.Text = dialog.FolderName;
                ExportButton.IsEnabled = true;
            }
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (isExporting) return;

            string exportPath = FolderPathTextBox.Text;
            if (string.IsNullOrWhiteSpace(exportPath))
            {
                MessageBox.Show("Please select a destination folder.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                isExporting = true;
                ExportButton.IsEnabled = false;
                CancelButton.IsEnabled = false;
                StatusText.Text = "Starting export...";

                await Task.Run(() => ExportNpcData(exportPath));

                StatusText.Text = "Export completed successfully!";
                Dispatcher.Invoke(() => this.Close());
            }
            catch (Exception ex)
            {
                StatusText.Text = "Error during export";
                MessageBox.Show($"An error occurred during export:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                isExporting = false;
                ExportButton.IsEnabled = true;
                CancelButton.IsEnabled = true;
            }
        }

        private void ExportNpcData(string exportPath)
        {
            Directory.CreateDirectory(exportPath);

            var npcData = new Dictionary<string, Dictionary<uint, NpcItemInfo>>();

            if (MainWindow.appearances?.Object != null)
            {
                int totalItems = MainWindow.appearances.Object.Count;
                int processedItems = 0;

                foreach (var item in MainWindow.appearances.Object)
                {
                    if (item.Flags?.Npcsaledata != null)
                    {
                        foreach (var npcSale in item.Flags.Npcsaledata)
                        {
                            if (npcSale != null && !string.IsNullOrEmpty(npcSale.Name))
                            {
                                string npcName = npcSale.Name.ToLower().Replace(" ", "_");

                                if (!npcData.ContainsKey(npcName))
                                {
                                    npcData[npcName] = new Dictionary<uint, NpcItemInfo>();
                                }

                                if (!npcData[npcName].ContainsKey(item.Id))
                                {
                                    npcData[npcName][item.Id] = new NpcItemInfo
                                    {
                                        ItemName = item.HasName ? item.Name : $"Item {item.Id}",
                                        ClientId = item.Id,
                                        SellPrice = npcSale.BuyPrice,
                                        BuyPrice = npcSale.SalePrice,
                                        Location = npcSale.Location ?? ""
                                    };
                                }
                            }
                        }
                    }

                    processedItems++;
                    int progress = (int)((double)processedItems / totalItems * 100);

                    Dispatcher.Invoke(() =>
                    {
                        ProgressBar.Value = progress;
                        ProgressText.Text = $"{progress}%";
                        StatusText.Text = $"Processing item {processedItems} of {totalItems}...";
                    });
                }
            }

            int totalNpcs = npcData.Count;
            int processedNpcs = 0;

            foreach (var npc in npcData)
            {
                string fileName = $"{npc.Key}.txt";
                string filePath = Path.Combine(exportPath, fileName);

                using (var writer = new StreamWriter(filePath))
                {
                    writer.WriteLine($"-- NPC: {npc.Key.Replace("_", " ").ToUpper()}");
                    writer.WriteLine($"-- Location: {npc.Value.Values.FirstOrDefault()?.Location ?? "Unknown"}");
                    writer.WriteLine($"-- Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    writer.WriteLine();

                    writer.WriteLine("npcConfig.shop = {");

                    foreach (var item in npc.Value.Values.OrderBy(i => i.ItemName))
                    {
                        var fields = new List<string>
                        {
                            $"itemName = \"{item.ItemName}\"",
                            $"clientId = {item.ClientId}"
                        };

                        if (item.SellPrice > 0)
                            fields.Add($"sell = {item.SellPrice}");

                        if (item.BuyPrice > 0)
                            fields.Add($"buy = {item.BuyPrice}");

                        writer.WriteLine($"\t{{ {string.Join(", ", fields)} }},");
                    }

                    writer.WriteLine("}");
                }

                processedNpcs++;
                int progress = (int)((double)processedNpcs / totalNpcs * 100);

                Dispatcher.Invoke(() =>
                {
                    ProgressBar.Value = progress;
                    ProgressText.Text = $"{progress}%";
                    StatusText.Text = $"Generating file {processedNpcs} of {totalNpcs}...";
                });
            }

            Dispatcher.Invoke(() =>
            {
                ProgressBar.Value = 100;
                ProgressText.Text = "100%";
                StatusText.Text = $"Export completed! {totalNpcs} NPCs exported.";
            });
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isExporting)
            {
                this.Close();
            }
        }
    }

    public class NpcItemInfo
    {
        public string ItemName { get; set; } = "";
        public uint ClientId { get; set; }
        public uint SellPrice { get; set; }
        public uint BuyPrice { get; set; }
        public string Location { get; set; } = "";
    }
}
