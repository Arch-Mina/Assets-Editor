using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.IO;
using Newtonsoft.Json;
using MessageBox = System.Windows.Forms.MessageBox;
using Tibia.Protobuf.Appearances;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using static Assets_Editor.LogView;

namespace Assets_Editor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static  string _assetsPath = "";
        public static string _datPath = "";
        public static string _sprPath = "";
        public static string _imgExportPath = "";
        public static ushort ObjectCount { get; set; }
        public static ushort OutfitCount { get; set; }
        public static ushort EffectCount { get; set; }
        public static ushort MissileCount { get; set; }

        public static Dictionary<uint, Sprite> sprites = new Dictionary<uint, Sprite>();
        public static SpriteStorage MainSprStorage;
        readonly BackgroundWorker worker = new BackgroundWorker();
        public static ServerSetting serverSetting = new ServerSetting();
        public Settings SettingsList = new Settings();
        public static OTBReader ServerOTB = new OTBReader();
        public static LogView logView = new LogView();
        public static DatStructure datStructure = new DatStructure();
        public MainWindow()
        {
            InitializeComponent();
            worker.WorkerReportsProgress = true;
            worker.ProgressChanged += Worker_ProgressChanged;
            worker.DoWork += Worker_DoWork;
            worker.RunWorkerCompleted += Worker_Completed;
            LoadEditorSettings();
            var dat = new DatStructure();
            var allVersions = dat.GetAllVersions();
            foreach (var version in allVersions)
            {
                A_ClientVersion.Items.Add(version.Number);
            }
            logView.Closing += (sender, e) =>
            {
                e.Cancel = true;
                logView.Hide();
            };
        }
        public class ServerSetting
        {
            public string Name { get; set; }
            public int Version { get; set; }
            public bool Transparent { get; set; }
            public string ServerPath { get; set; }
            public string ClientPath { get; set; }
        }
        public class Settings
        {
            public List<ServerSetting> Servers { get; set; }
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            System.Windows.Application.Current.Shutdown();
        }

        public class Catalog
        {
            public Catalog()
            {
                SpriteType = int.MinValue;
                FirstSpriteid = int.MinValue;
                LastSpriteid = int.MinValue;
                Area = int.MinValue;
                Version = int.MinValue;
            }
            public string Type { get; set; }
            public string File { get; set; }
            [DefaultValue(int.MinValue)]
            public int SpriteType { get; set; }
            [DefaultValue(int.MinValue)]
            public int FirstSpriteid { get; set; }
            [DefaultValue(int.MinValue)]
            public int LastSpriteid { get; set; }
            [DefaultValue(int.MinValue)]
            public int Area { get; set; }
            [DefaultValue(int.MinValue)]
            public int Version { get; set; }
        }
        
        public static List<Catalog> catalog;

        public static Appearances appearances;
        public static List<ShowList> AllSprList = new List<ShowList>();
        public static ConcurrentDictionary<int, MemoryStream> SprLists = new ConcurrentDictionary<int, MemoryStream>();
        // Track sprite sizes for proper SpriteType assignment during export
        public static ConcurrentDictionary<int, System.Drawing.Size> SpriteSizes = new ConcurrentDictionary<int, System.Drawing.Size>();
        public static bool LegacyClient = false;
        public static uint DatSignature { get; set; }
        public static uint SprSignature { get; set; }
        private void LoadEditorSettings()
        {
            string settingFilePath = "Settings.json";
            if (File.Exists(settingFilePath))
            {
                string json = File.ReadAllText(settingFilePath);
                SettingsList = JsonConvert.DeserializeObject<Settings>(json);
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                foreach (var server in SettingsList.Servers)
                {
                    if (server.ServerPath.StartsWith(@"~\"))
                        server.ServerPath = Path.Combine(localAppData, server.ServerPath.Substring(2));

                    if (server.ClientPath.StartsWith(@"~\"))
                        server.ClientPath = Path.Combine(localAppData, server.ClientPath.Substring(2));

                    A_SavedVersion.Items.Add(server.Name + " " + server.Version);
                }
            }
        }
        private void LoadCatalogJson()
        {
            using StreamReader r = new StreamReader(_assetsPath + "catalog-content.json");
            string json = r.ReadToEnd();
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };
            catalog = JsonConvert.DeserializeObject<List<Catalog>>(json, settings);
        }

        private void LoadAppearances()
        {
            _datPath = String.Format("{0}{1}", _assetsPath, catalog[0].File);
            if (File.Exists(_datPath) == false)
                return;
            FileStream appStream;
            using (appStream = new FileStream(_datPath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
            {
                appearances = Tibia.Protobuf.Appearances.Appearances.Parser.ParseFrom(appStream);
                ObjectCount = (ushort)appearances.Object[^1].Id;
                OutfitCount = (ushort)appearances.Outfit[^1].Id;
                EffectCount = (ushort)appearances.Effect[^1].Id;
                MissileCount = (ushort)appearances.Missile[^1].Id;

                ObjectsCount.Content = ObjectCount;
                OutfitsCount.Content = OutfitCount;
                EffectsCount.Content = EffectCount;
                MissilesCount.Content = MissileCount;
            }
        }

        private void LoadLegacyDat()
        {
            using var stream = File.OpenRead(_datPath);
            using var r = new BinaryReader(stream);
            {
                DatInfo info = DatStructure.ReadAppearanceInfo(r);
                DatSignature = info.Signature;
                ObjectsCount.Content = info.ObjectCount;
                OutfitsCount.Content = info.OutfitCount;
                EffectsCount.Content = info.EffectCount;
                MissilesCount.Content = info.MissileCount;
            }
        }
        private void LoadLegacySpr()
        {
            bool transparency = false;
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                transparency = (bool)SprTransparent.IsChecked;
            });
            var progressReporter = new Progress<int>(value =>
            {
                worker.ReportProgress(value);
            });

            MainSprStorage = new SpriteStorage(_sprPath, transparency, progressReporter);
            SprSignature = MainSprStorage.Signature;
            SprLists = MainSprStorage.SprLists;
            sprites = MainSprStorage.Sprites;
            for (uint i = 0; i < sprites.Count; i++)
            {
                AllSprList.Add(new ShowList() { Id = i });
            }

        }

        private void SelectAssetsFolder(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog _assets = new FolderBrowserDialog();
            if (_assets.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _assetsPath = _assets.SelectedPath;
                if (_assetsPath.EndsWith("\\") == false)
                    _assetsPath += "\\";
                AssetsPath.Text = _assetsPath;
            }
            if (_assetsPath != "" && File.Exists(_assetsPath + "catalog-content.json") == true)
            {
                LegacyClient = false;
                LoadCatalogJson();
                LoadAppearances();
                LoadAssets.IsEnabled = true;
                SprTransparent.Visibility = Visibility.Hidden;
                A_ClientVersion.Visibility = Visibility.Hidden;
            }
            else if (_assetsPath != "" && File.Exists(_assetsPath + "Tibia.dat") == true && File.Exists(_assetsPath + "Tibia.spr") == true)
            {
                LegacyClient = true;
                _datPath = String.Format("{0}{1}", _assetsPath, "Tibia.dat");
                _sprPath = String.Format("{0}{1}", _assetsPath, "Tibia.spr");
                LoadLegacyDat();
                LoadAssets.IsEnabled = true;
                SprTransparent.Visibility = Visibility.Visible;
                A_ClientVersion.Visibility = Visibility.Visible;

            }
            else
                MessageBox.Show("You have selected a wrong assets path.");
        }
        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            if (LegacyClient == false)
                LoadSprSheet();
            else
            {
                LegacyAppearance Dat = new LegacyAppearance();
                Dat.ReadLegacyDat(_datPath, serverSetting.Version);
                appearances = Dat.Appearances;
                LoadLegacySpr();
                if (serverSetting.ServerPath != string.Empty)
                {
                    string otbPath = System.IO.Path.Combine(serverSetting.ServerPath, "data/items/items.otb");
                    bool stats = ServerOTB.Read(otbPath);
                }
            }
        }
        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            LoadProgress.Value = e.ProgressPercentage;
        }
        private void Worker_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            if (LegacyClient == false)
            {
                DatEditor datEditor = new DatEditor(appearances);
                datEditor.Show();
            }
            else
            {
                LegacyDatEditor legacyDatEditor = new LegacyDatEditor(appearances);
                legacyDatEditor.Show();
            }
            Hide();
        }
        private void LoadAssets_Click(object sender, RoutedEventArgs e)
        {
            if (worker.IsBusy != true)
            {
                worker.RunWorkerAsync();
            }
        }

        private static void GenerateTileSetImageList(Bitmap bitmap, Catalog sheet)
        {
            using Bitmap tileSetImage = new Bitmap(bitmap);
            int tileCount = sheet.LastSpriteid - sheet.FirstSpriteid;
            int sprCount = 0;
            Image tile;

            if (sheet.SpriteType >= 0)
            {
                // Calculate tile dimensions based on SpriteType
                int tWidth = 32;
                int tHeight = 32;
                
                switch (sheet.SpriteType)
                {
                    case 0: tWidth = 32; tHeight = 32; break;  // 12×12 = 144 sprites
                    case 1: tWidth = 32; tHeight = 64; break;  // 12×6 = 72 sprites
                    case 2: tWidth = 64; tHeight = 32; break;  // 6×12 = 72 sprites
                    case 3: tWidth = 64; tHeight = 64; break;  // 6×6 = 36 sprites
                    case 4: tWidth = 96; tHeight = 96; break;  // 4×4 = 16 sprites
                    case 5: tWidth = 128; tHeight = 128; break; // 3×3 = 9 sprites
                    case 6: tWidth = 192; tHeight = 192; break; // 2×2 = 4 sprites
                    case 7: tWidth = 256; tHeight = 256; break; // 1×1 = 1 sprite
                    default: tWidth = 32; tHeight = 32; break;
                }
                
                int xCols = 384 / tWidth;
                int yCols = 384 / tHeight;

                System.Drawing.Size tileSize = new System.Drawing.Size(tWidth, tHeight);
                for (int x = 0; x < yCols; x++)
                {
                    for (int y = 0; y < xCols; y++)
                    {
                        if (sprCount > tileCount)
                            break;

                        tile = new Bitmap(tileSize.Width, tileSize.Height, tileSetImage.PixelFormat);
                        Graphics g = Graphics.FromImage(tile);
                        Rectangle sourceRect = new Rectangle(y * tileSize.Width, x * tileSize.Height, tileSize.Width, tileSize.Height);
                        g.DrawImage(tileSetImage, new Rectangle(0, 0, tileSize.Width, tileSize.Height), sourceRect, GraphicsUnit.Pixel);
                        MemoryStream ms = new MemoryStream();
                        tile.Save(ms, ImageFormat.Png);
                        tile.Dispose();
                        SprLists[sheet.FirstSpriteid + sprCount] = ms;
                        g.Dispose();
                        sprCount++;
                    }
                }

            }
        }

        private void LoadSprSheet()
        {
            int progress = 0;
            SprLists = new ConcurrentDictionary<int, MemoryStream>();
            var options = new ParallelOptions()
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount * 5
            };
            Parallel.ForEach(catalog, options, (spr, state) =>
            {
                if (spr.Type == "sprite")
                {
                    for (int i = spr.FirstSpriteid; i <= spr.LastSpriteid; i++)
                    {
                        SprLists[i] = null;
                    }
                    progress++;
                    worker.ReportProgress((int)(progress * 100 / catalog.Count));
                }
            });
            uint maxSpriteId = (uint)(catalog.Max(r => r.LastSpriteid) + 1);
            for (uint i = 0; i < maxSpriteId; i++)
            {
                AllSprList.Add(new ShowList() { Id = i });
            }
        }

        public static MemoryStream getSpriteStream(int spriteId)
        {
            if (SprLists[spriteId] != null)
            {
                return SprLists[spriteId];
            }
            Catalog CatalogInfo = null;
            foreach (var SprCatalog in catalog)
            {
                if (spriteId >= SprCatalog.FirstSpriteid && spriteId <= SprCatalog.LastSpriteid)
                {
                    CatalogInfo = SprCatalog;
                    break;
                }
            }

            if (CatalogInfo != null)
            {
                string _sprPath = String.Format("{0}{1}", _assetsPath, CatalogInfo.File);
                Bitmap SheetM = LZMA.DecompressFileLZMA(_sprPath);
                GenerateTileSetImageList(SheetM, CatalogInfo);
                SheetM.Dispose();
            }

            if (SprLists[spriteId] == null)
            {
                Debug.WriteLine("Spr is null {0}, {1}", spriteId, CatalogInfo.FirstSpriteid);
                return SprLists[0];
            }
            return SprLists[spriteId];
        }
        private void A_SavedVersion_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            int serverId = A_SavedVersion.SelectedIndex;
            ServerSetting server = SettingsList.Servers[serverId];
            if (server != null)
            {
                _assetsPath = server.ClientPath;
                if (_assetsPath.EndsWith("\\") == false)
                    _assetsPath += "\\";
                AssetsPath.Text = _assetsPath;

                if (server.Version >= 1300 && File.Exists(_assetsPath + "catalog-content.json") == true)
                {
                    LegacyClient = false;
                    LoadCatalogJson();
                    LoadAppearances();
                    LoadAssets.IsEnabled = true;
                    SprTransparent.Visibility = Visibility.Hidden;
                    A_ClientVersion.Visibility = Visibility.Hidden;
                }
                else if (server.Version < 1300 && File.Exists(_assetsPath + "Tibia.dat") == true && File.Exists(_assetsPath + "Tibia.spr") == true)
                {
                    LegacyClient = true;
                    _datPath = String.Format("{0}{1}", _assetsPath, "Tibia.dat");
                    _sprPath = String.Format("{0}{1}", _assetsPath, "Tibia.spr");
                    LoadLegacyDat();
                    LoadAssets.IsEnabled = true;
                    SprTransparent.Visibility = Visibility.Hidden;
                    A_ClientVersion.Visibility = Visibility.Hidden;
                }
                else
                    MessageBox.Show("You have selected a wrong assets path.");
                serverSetting = server;
            }
        }

        private void A_ClientVersion_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            serverSetting.Version = ushort.Parse(A_ClientVersion.SelectedItem.ToString());
        }

        private void SprTransparent_Changed(object sender, RoutedEventArgs e)
        {
            serverSetting.Transparent = (bool)SprTransparent.IsChecked;
        }
        public static void Log(string message, string level = "Info")
        {
            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Message = message
            };
            logView.AddLogEntry(entry);
        }
    }
}
