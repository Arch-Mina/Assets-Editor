using MaterialDesignThemes.Wpf;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using Tibia.Protobuf.Appearances;
using static Assets_Editor.LogView;
using MessageBox = System.Windows.Forms.MessageBox;

namespace Assets_Editor;

public class PresetSettings {
    public string? Name { get; set; }
    public int Version { get; set; }
    public bool Extended { get; set; }
    public bool Transparent { get; set; }
    public bool FrameDurations { get; set; }
    public bool FrameGroups { get; set; }
    public string? ServerPath { get; set; }
    public string? ClientPath { get; set; }
}

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

    // directory pickers
    // to generate more use tools -> create GUID in visual studio
    private static readonly Guid GUID_assetsPicker = new("43A0E8FA-B129-4DB4-AD2D-0C44C23CE222");

    // to do
    // private static readonly Guid GUID_serverPicker = new("C01637F9-8C7B-4610-BBA2-530487BC57A2");

    private static readonly Guid GUID_export1 = new("820617D2-0ECA-4632-B62D-42F740BD731A");
    private static readonly Guid Guid_export2 = new("4FC8F7A5-4840-4840-A68B-26DFD955D224");
    private static readonly Guid Guid_export3 = new("1A5860A3-5722-4FFC-B6F2-FCE4E9FE255F");

    public static Dictionary<uint, Sprite> sprites = [];
    public static SpriteStorage MainSprStorage;
    private BackgroundWorker worker;
    private static PresetSettings? currentPreset;
    public Settings SettingsList = new();
    public static OTBReader ServerOTB = new();
    public static LogView logView = new();
    public static readonly DatStructure datStructure = new();
    private static readonly string settingsFilePath = "Settings.json";
    public MainWindow()
    {
        // initialize WPF dialog
        InitializeComponent();

        // load settings.json
        LoadEditorSettings();

        // clear selection
        A_SavedVersion.SelectedIndex = -1;

        // register event
        A_SavedVersion.SelectionChanged += A_SavedVersion_SelectionChanged;

        logView.Closing += (sender, e) => {
            e.Cancel = true;
            logView.Hide();
        };

        // select some preset
        A_SavedVersion.SelectedIndex = Math.Min(SettingsList.LastPresetChoice, A_SavedVersion.Items.Count - 1);
    }

    public class Settings
    {
        public bool DarkMode { get; set; }
        public int LastPresetChoice { get; set; }
        public List<PresetSettings>? Presets { get; set; }
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
    
    public static List<Catalog> catalog = [];

    public static Appearances? appearances;
    public static List<ShowList> AllSprList = [];
    public static ConcurrentDictionary<int, MemoryStream> SprLists = [];
    public static bool LegacyClient = false;
    public static uint DatSignature { get; set; }
    public static uint SprSignature { get; set; }
    private static bool Loaded = false;

    private static readonly PresetSettings defaultPreset = new() {
        Name = "Default",
        Version = 1,
        ServerPath = "",
        ClientPath = "~\\Tibia\\packages\\Tibia\\assets"
    };

    public static PresetSettings? GetCurrentPreset() {
        return currentPreset;
    }

    private void CreateSettingsChoices()
    {
        if (SettingsList.Presets == null) {
            LoadDefaultSettings();
            MessageBox.Show(
                $"Unable to create presets! Settings not loaded.",
                "Warning",
                (MessageBoxButtons)MessageBoxButton.OK,
                (MessageBoxIcon)MessageBoxImage.Warning
            );
            return;
        }

        // add preset choices
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        foreach (var server in SettingsList.Presets) {
            if (server.ServerPath.StartsWith(@"~\"))
                server.ServerPath = Path.Combine(localAppData, server.ServerPath[2..]);

            if (server.ClientPath.StartsWith(@"~\"))
                server.ClientPath = Path.Combine(localAppData, server.ClientPath[2..]);

            // add preset to dropdown
            A_SavedVersion.Items.Add(server.Name + " " + server.Version);
        }
    }

    private void LoadDefaultSettings()
    {
        Settings defaultSettings = new() {
            DarkMode = false,
            Presets = [ defaultPreset ]
        };
        SettingsList = defaultSettings;
    }

    private void LoadEditorSettings()
    {
        if (File.Exists(settingsFilePath)) {
            // load settings from file
            try {
                string json = File.ReadAllText(settingsFilePath);
                Settings? jsonSettings = JsonConvert.DeserializeObject<Settings>(json);
                if (jsonSettings == null) {
                    LoadDefaultSettings();
                    return;
                }

                // if the json does not have any presets, add default preset to the list
                if (jsonSettings.Presets == null || jsonSettings.Presets.Count == 0) {
                    jsonSettings.Presets = [defaultPreset];
                }

                // apply settings
                SettingsList = jsonSettings;
            } catch (Exception e) {
                LoadDefaultSettings();
                MessageBox.Show(
                    $"Unable to read settings! Using defaults.\n\n{e.Message}",
                    "Warning",
                    (MessageBoxButtons)MessageBoxButton.OK,
                    (MessageBoxIcon)MessageBoxImage.Warning
                );
            }
        } else {
            // load defaults
            LoadDefaultSettings();
        }

        // apply theme
        DarkModeToggle.IsChecked = SettingsList.DarkMode;

        // add settings to presets list
        CreateSettingsChoices();
    }

    private void LoadCatalogJson()
    {
        using StreamReader r = new(_assetsPath + "catalog-content.json");
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
        using FileStream stream = File.OpenRead(_datPath);
        using BinaryReader r = new(stream);
        DatInfo info = DatStructure.ReadAppearanceInfo(r);
        DatSignature = info.Signature;
        ObjectsCount.Content = info.ObjectCount;
        OutfitsCount.Content = info.OutfitCount;
        EffectsCount.Content = info.EffectCount;
        MissilesCount.Content = info.MissileCount;
    }

    private void ReadSprSignature()
    {
        using FileStream stream = File.OpenRead(_sprPath);
        using BinaryReader r = new(stream);
        SprSignature = r.ReadUInt32();
    }

    private void LoadLegacySpr()
    {
        bool transparency = false;
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            transparency = (bool)DatTransparentToggle.IsChecked;
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
        FolderBrowserDialog _assets = new() {
            ClientGuid = GUID_assetsPicker
        };

        // If _assetsPath is valid, set it as the starting folder
        if (!string.IsNullOrEmpty(_assetsPath) && Directory.Exists(_assetsPath)) {
            _assets.SelectedPath = _assetsPath;
        }

        if (_assets.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _assetsPath = _assets.SelectedPath;
            if (_assetsPath.EndsWith("\\") == false)
                _assetsPath += "\\";
            AssetsPath.Text = _assetsPath;

            // remember most recent accessed location
            currentPreset?.ClientPath = _assetsPath;
            SaveEditorSettings();
        }

        InternalSelectAssets();
    }

    /// <summary>
    /// Copies current assets path to clipboard
    /// </summary>
    private void CopyAssetsPath(object sender, RoutedEventArgs e)
    {
        Dispatcher.Invoke(() => {
            ClipboardManager.CopyText(AssetsPath.Text, "Path", StatusBar, 1);
        });
    }

    /// <summary>
    /// Saves current preset immediately to json
    /// </summary>
    private void SaveEditorSettings() {
        try {
            var json = JsonConvert.SerializeObject(SettingsList, Formatting.Indented);
            File.WriteAllText(settingsFilePath, json);
        } catch (Exception e) {
            LoadDefaultSettings();
            MessageBox.Show(
                $"Unable to save settings!\n\n{e.Message}",
                "Error",
                (MessageBoxButtons)MessageBoxButton.OK,
                (MessageBoxIcon)MessageBoxImage.Error
            );
        }
    }

    private bool InternalTryLoadDat()
    {
        foreach (VersionInfo version in datStructure.GetAllVersions().Reverse()) {
            try {
                //currentPreset.FrameDurations;
                //currentPreset.FrameGroups;
                LegacyAppearance Dat = new();
                Dat.ReadLegacyDat(_datPath, version.Structure);

                // dat was found, update the current preset
                currentPreset?.Version = version.Structure;
                appearances = Dat.Appearances;
                return true;
            } catch {
                // not the dat we are looking for, try another one
            }
        }

        return false;
    }

    private bool InternalTryLoadSpr()
    {
        try {
            LoadLegacySpr();
            return true;
        } catch {
            // ...
        }

        return false;
    }

    private void RefreshUIToggles()
    {
        // update the ui with found values
        Dispatcher.Invoke(() => {
            if (currentPreset == null) {
                return;
            }

            DatExtendedToggle.IsChecked = currentPreset.Extended;
            DatTransparentToggle.IsChecked = currentPreset.Transparent;
            DatAnimationsToggle.IsChecked = currentPreset.FrameDurations;
            DatFrameGroupsToggle.IsChecked = currentPreset.FrameGroups;
        });
    }

    private void Worker_DoWork(object sender, DoWorkEventArgs e)
    {
        if (currentPreset == null) {
            SetLoadingStatus(AssetsLoadingStatus.ASSETS_LOADING_ERROR, "No preset selected!");
            return;
        }

        if (LegacyClient == false) {
            LoadSprSheet();
            Loaded = true;
        } else {
            bool datFound = InternalTryLoadDat();
            if (!datFound) {
                if (!currentPreset.Extended) {
                    currentPreset.Extended = true;
                    datFound = InternalTryLoadDat();
                }

                if (!datFound && !currentPreset.FrameDurations) {
                    currentPreset.FrameDurations = true;
                    datFound = InternalTryLoadDat();
                }

                if (!datFound && !currentPreset.FrameGroups) {
                    currentPreset.FrameGroups = true;
                    datFound = InternalTryLoadDat();
                }
            }

            SetLoadingStatus(AssetsLoadingStatus.ASSETS_LOADING_WIP, "Loading dat file ...");
            if (!datFound) {
                SetLoadingStatus(AssetsLoadingStatus.ASSETS_LOADING_ERROR, "Unable to load dat!");
                RefreshUIToggles();
                return;
            }

            SetLoadingStatus(AssetsLoadingStatus.ASSETS_LOADING_WIP, "Loading spr file ...");
            if (!InternalTryLoadSpr()) {
                SetLoadingStatus(AssetsLoadingStatus.ASSETS_LOADING_ERROR, "Unable to load spr!");
                RefreshUIToggles();
                return;
            }

            /*
            if (!String.IsNullOrEmpty(currentPreset.ServerPath)) {
                string otbPath = Path.Combine(currentPreset.ServerPath, "data/items/items.otb");
                ServerOTB.Read(otbPath);
            }
            */

            RefreshUIToggles();
            Loaded = true;
        }
    }
    private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
    {
        LoadProgress.Value = e.ProgressPercentage;
    }
    private void Worker_Completed(object sender, RunWorkerCompletedEventArgs e)
    {
        if (appearances == null) {
            SetLoadingStatus(AssetsLoadingStatus.ASSETS_LOADING_ERROR, "No suitable version was found.");
            return;
        }

        if (!Loaded) {
            LoadAssets.IsEnabled = true;
            return;
        }

        if (LegacyClient == false)
        {
            DatEditor datEditor = new(appearances);
            datEditor.Show();
        }
        else
        {
            LegacyDatEditor legacyDatEditor = new(appearances);
            legacyDatEditor.Show();
        }
        Hide();
    }
    private void LoadAssets_Click(object sender, RoutedEventArgs e)
    {
        if (worker == null || !worker.IsBusy)
        {
            worker = new BackgroundWorker {
                WorkerReportsProgress = true
            };

            worker.WorkerReportsProgress = true;
            worker.ProgressChanged += Worker_ProgressChanged;
            worker.DoWork += Worker_DoWork;
            worker.RunWorkerCompleted += Worker_Completed;

            worker.RunWorkerAsync();
        }
    }

    private static void GenerateTileSetImageList(Bitmap bitmap, Catalog sheet)
    {
        using Bitmap tileSetImage = new Bitmap(bitmap);
        int tileCount = sheet.LastSpriteid - sheet.FirstSpriteid;
        int sprCount = 0;
        Image tile;

        var sprType = sheet.SpriteType;
        if (sprType >= 0) {
            var layout = DatEditor.GetSpriteLayout(sprType);
            int tWidth = layout.SpriteSizeX;
            int tHeight = layout.SpriteSizeY;
            int xCols = layout.Cols;
            int yCols = layout.Rows;

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

    /// <summary>
    /// attempts to load assets, the return value determines if spr/dat should be attempted instead
    /// </summary>
    /// <returns>true if assets were found, false if assets were not found</returns>
    private bool TryLoadAssets()
    {
        try {
            if (!File.Exists(Path.Combine(_assetsPath, "catalog-content.json"))) {
                SetVersionDescription("No version loaded");
                SetLoadingStatus(AssetsLoadingStatus.ASSETS_LOADING_ERROR, "No assets found in selected directory");
                return false;
            }

            LoadCatalogJson();
            LoadAppearances();
            LegacyClient = false;
            SetLoadingStatus(AssetsLoadingStatus.ASSETS_LOADING_INFO, "FOUND: Assets directory");
            return true;
        } catch (Exception e) {
            SetLoadingStatus(AssetsLoadingStatus.ASSETS_LOADING_ERROR, $"Unable to load assets\n\n{e.Message}");
        }

        return false;
    }

    private static string? SearchAssetFile(string? defaultFileName, string fallbackExtension) {
        // try default file name first
        if (defaultFileName != null && File.Exists(Path.Combine(_assetsPath, defaultFileName))) {
            return Path.Combine(_assetsPath, defaultFileName);
        }

        // try Tibia.(ext)
        defaultFileName = "Tibia" + fallbackExtension;
        if (File.Exists(Path.Combine(_assetsPath, defaultFileName))) {
            return Path.Combine(_assetsPath, defaultFileName);
        }

        // try the first match of *.(ext)
        string[] found = Directory.GetFiles(_assetsPath, $"*{fallbackExtension}");
        return found.Length > 0 ? Path.Combine(_assetsPath, found[0]) : null;
    }

    /// <summary>
    /// attempts to load spr/dat
    /// </summary>
    /// <returns>bool - true if loading should be interrupted</returns>
    private bool TryLoadLegacyAssets()
    {
        // TO DO:
        // project settings: otfi, dat, spr file name
        // update ui

        string? otfiDatFile = null;
        string? otfiSprFile = null;
        bool has_otfi = false;
        bool extended = false;
        bool transparency = false;
        bool frameDurations = false;
        bool frameGroups = false;

        SetLoadingStatus(AssetsLoadingStatus.ASSETS_LOADING_WIP, $"Searching for OTFI ...");

        try {
            // search order:
            // 1. Tibia.otfi
            // 2. any .otfi in the directory
            string? otfiPath = SearchAssetFile(null, ".otfi");
            if (File.Exists(otfiPath)) {
                // read from otfi
                var doc = OTMLDocument.Parse(otfiPath);
                OTMLNode root = doc.At("DatSpr");
                extended = root.Get("extended")?.ValueAs<bool>() ?? false;
                transparency = root.Get("transparency")?.ValueAs<bool>() ?? false;
                frameDurations = root.Get("frame-durations")?.ValueAs<bool>() ?? false;
                frameGroups = root.Get("frame-groups")?.ValueAs<bool>() ?? false;

                // otfi standard 1: spr/dat written separately
                otfiDatFile = root.Get("metadata-file")?.ValueAs<string>();
                otfiSprFile = root.Get("sprites-file")?.ValueAs<string>();

                // otfi standard 2: spr/dat written as a single word
                string? assetsName = root.Get("assets-name")?.ValueAs<string>();
                if (assetsName != null) {
                    otfiDatFile ??= assetsName + ".dat";
                    otfiSprFile ??= assetsName + ".spr";
                }

                has_otfi = true;
            }
        } catch (Exception e) {
            MessageBox.Show(
                $"Unable to read otfi!\n\n{e.Message}",
                "Warning",
                (MessageBoxButtons)MessageBoxButton.OK,
                (MessageBoxIcon)MessageBoxImage.Warning
            );
        }

        SetLoadingStatus(AssetsLoadingStatus.ASSETS_LOADING_WIP, $"Searching for DAT file ...");

        // search order:
        // 1. otfi provided dat
        // 2. Tibia.dat
        // 3. any .dat file in the directory
        string? datPath = SearchAssetFile(otfiDatFile, ".dat");
        if (!File.Exists(datPath)) {
            if (has_otfi) {
                SetLoadingStatus(AssetsLoadingStatus.ASSETS_LOADING_ERROR, "DAT file not found!");
                return true;
            }

            SetLoadingStatus(AssetsLoadingStatus.ASSETS_LOADING_ERROR, "Invalid assets directory!");
            return false;
        }

        // search order:
        // 1. otfi provided spr
        // 2. Tibia.spr
        // 3. any .spr file in the directory
        string? sprPath = SearchAssetFile(otfiSprFile, ".spr");
        if (!File.Exists(sprPath)) {
            SetLoadingStatus(AssetsLoadingStatus.ASSETS_LOADING_ERROR, "SPR file not found!");
            return false;
        }

        // update toggles
        if (currentPreset != null) {
            currentPreset.Extended = extended;
            currentPreset.Transparent = transparency;
            currentPreset.FrameDurations = frameDurations;
            currentPreset.FrameGroups = frameGroups;
            RefreshUIToggles();
        }

        _datPath = datPath;
        _sprPath = sprPath;

        // attempt to load dat file
        try {
            LoadLegacyDat();
        } catch (Exception ex) {
            MessageBox.Show(
                $"Unable to read dat file!\n\n{ex.Message}",
                "Warning",
                (MessageBoxButtons)MessageBoxButton.OK,
                (MessageBoxIcon)MessageBoxImage.Warning
            );
            SetLoadingStatus(AssetsLoadingStatus.ASSETS_LOADING_ERROR, "Unable to load dat file!");
            return true;
        }

        // attempt to read spr signature
        try {
            ReadSprSignature();
        } catch (Exception ex) {
            MessageBox.Show(
                $"Unable to read spr file!\n\n{ex.Message}",
                "Warning",
                (MessageBoxButtons)MessageBoxButton.OK,
                (MessageBoxIcon)MessageBoxImage.Warning
            );
            SetLoadingStatus(AssetsLoadingStatus.ASSETS_LOADING_ERROR, "Unable to load spr file!");
            return true;
        }

        LoadAssets.IsEnabled = true;
        SetVersionDescription(AssetsVersionInfo.Text = $"DAT: 0x{DatSignature:X}\nSPR: 0x{SprSignature:X}");
        // DatSignature
        // 0A 93 01 08 - consistent first 4 bytes for appearances.dat (version 12 and newer)
        if (DatSignature == 0x0801930A) {
            MessageBox.Show(
                $"Detected DAT signature is matching appearances.dat, but catalog-content.json was not detected in the selected directory.",
                "Warning",
                (MessageBoxButtons)MessageBoxButton.OK,
                (MessageBoxIcon)MessageBoxImage.Warning
            );
            SetLoadingStatus(AssetsLoadingStatus.ASSETS_LOADING_WARNING, "Possibly missing catalog-content.json!");
        } else {
            SetLoadingStatus(AssetsLoadingStatus.ASSETS_LOADING_INFO, "FOUND: Legacy SPR/DAT");
        }

        return true;
    }

    private void OnAssetsLoaded()
    {
        // assets found
        LegacyClient = false;

        // OTFI does not work with assets
        OTFIOverrideChk.IsChecked = true;

        // Assets have all features natively
        // this cannot be changed
        DatExtendedToggle.IsChecked = true;
        DatTransparentToggle.IsChecked = true;
        DatAnimationsToggle.IsChecked = true;
        DatFrameGroupsToggle.IsChecked = true;
        LoadAssets.IsEnabled = true;

        // set version info to the UI
        string version = "No data";
        try {
            // Find package.json in parent directory
            string assetsDir = Path.GetDirectoryName(_assetsPath)!;
            string parentDir = Directory.GetParent(assetsDir)!.FullName;
            string packageJsonPath = Path.Combine(parentDir, "package.json");

            if (File.Exists(packageJsonPath)) {
                string json = File.ReadAllText(packageJsonPath);

                // Parse JSON (using System.Text.Json)
                using JsonDocument doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("version", out JsonElement versionElement)) {
                    string? v = versionElement.GetString();
                    if (!string.IsNullOrWhiteSpace(v)) {
                        version = v;
                    }
                }
            }
        } catch {
            // version reading is optional, no error to display
        }

        SetVersionDescription($"Assets Version:\n{version}");
    }

    private void InternalSelectAssets()
    {
        if (_assetsPath == "") {
            SetLoadingStatus(AssetsLoadingStatus.ASSETS_LOADING_INFO, "No assets path selected");
            return;
        }

        LegacyClient = true;
        if (TryLoadAssets()) {
            OnAssetsLoaded();
            return;
        }

        if (TryLoadLegacyAssets()) {
            // handle success

            return;
        }

        // no assets found in current directory
        // common user mistake is selecting the parent directory
        // try to find assets folder inside current dir
        string oldAssetsPath = _assetsPath;
        _assetsPath = Path.Combine(_assetsPath, "assets\\");
        if (Directory.Exists(_assetsPath) && TryLoadAssets()) {
            OnAssetsLoaded();

            // send the directory change to the ui
            AssetsPath.Text = _assetsPath;
            return;
        }

        // revert the attempt to search "assets" subdirectory
        _assetsPath = oldAssetsPath;

        SetVersionDescription("No version loaded");
        SetLoadingStatus(AssetsLoadingStatus.ASSETS_LOADING_INFO, "No assets found in selected directory.");
        return;
    }

    private void A_SavedVersion_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        int presetId = A_SavedVersion.SelectedIndex;

        // another fallback for invalid server lists
        if (SettingsList.Presets == null || SettingsList.Presets.Count == 0) {
            SettingsList.Presets = [defaultPreset];
            presetId = 0;
        }

        PresetSettings preset = SettingsList.Presets[presetId];
        if (preset == null) {
            return;
        }

        // update currently selected preset
        currentPreset = preset;

        _assetsPath = preset.ClientPath;
        if (_assetsPath.EndsWith("\\") == false)
            _assetsPath += "\\";
        AssetsPath.Text = _assetsPath;

        InternalSelectAssets();
    }

    public static int GetCurrentLoadedVersion() => currentPreset.Version;

    private void DatExtended_Changed(object sender, RoutedEventArgs e) {
        currentPreset?.Extended = DatExtendedToggle.IsChecked ?? false;
    }

    private void DatTransparent_Changed(object sender, RoutedEventArgs e) {
        currentPreset?.Transparent = DatTransparentToggle.IsChecked ?? false;
    }

    private void DatAnimations_Changed(object sender, RoutedEventArgs e) {
        currentPreset?.FrameDurations = DatAnimationsToggle.IsChecked ?? false;
    }

    private void DatFrameGroup_Changed(object sender, RoutedEventArgs e) {
        currentPreset?.FrameGroups = DatFrameGroupsToggle.IsChecked ?? false;
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

    private void DarkModeToggle_Checked(object sender, RoutedEventArgs e) {
        PaletteHelper palette = new();

        ITheme theme = palette.GetTheme();
        if (DarkModeToggle.IsChecked ?? false) {
            theme.SetBaseTheme(Theme.Dark);
            SettingsList.DarkMode = true;
        } else {
            theme.SetBaseTheme(Theme.Light);
            SettingsList.DarkMode = false;
        }
        palette.SetTheme(theme);
        SaveEditorSettings();
    }

    private void SetVersionDescription(string text) {
        Dispatcher.Invoke(() => {
            AssetsVersionInfo.Text = text;
        });
    }

    enum AssetsLoadingStatus {
        ASSETS_LOADING_NONE = 0,
        ASSETS_LOADING_WIP = 1,
        ASSETS_LOADING_INFO = 2,
        ASSETS_LOADING_WARNING = 3,
        ASSETS_LOADING_ERROR = 4,
    }
    private void SetLoadingStatus(AssetsLoadingStatus newStatus, string text) {
        Dispatcher.Invoke(() =>
        {
            // hide everything if status none
            if (newStatus == AssetsLoadingStatus.ASSETS_LOADING_NONE) {
                AssetsPathLoadingMessage.Visibility = Visibility.Hidden;
                AssetsPathLoadingIcon.Visibility = Visibility.Hidden;
                AssetsPathBaseIcon.Visibility = Visibility.Hidden;
                return;
            }

            // set and show text
            AssetsPathLoadingMessage.Text = text;
            AssetsPathLoadingMessage.Visibility = Visibility.Visible;

            // display icon according to status
            if (newStatus == AssetsLoadingStatus.ASSETS_LOADING_WIP) {
                AssetsPathLoadingIcon.Visibility = Visibility.Visible;
                AssetsPathBaseIcon.Visibility = Visibility.Collapsed;
            } else {
                AssetsPathLoadingIcon.Visibility = Visibility.Collapsed;
                AssetsPathBaseIcon.Visibility = Visibility.Visible;

                switch (newStatus) {
                    case AssetsLoadingStatus.ASSETS_LOADING_INFO:
                        AssetsPathBaseIcon.Kind = PackIconKind.Info;
                        AssetsPathBaseIcon.Foreground = new SolidColorBrush(Colors.DeepSkyBlue);
                        break;
                    case AssetsLoadingStatus.ASSETS_LOADING_WARNING:
                        AssetsPathBaseIcon.Kind = PackIconKind.Alert;
                        AssetsPathBaseIcon.Foreground = new SolidColorBrush(Colors.Yellow);
                        break;
                    case AssetsLoadingStatus.ASSETS_LOADING_ERROR:
                        AssetsPathBaseIcon.Kind = PackIconKind.Error;
                        AssetsPathBaseIcon.Foreground = new SolidColorBrush(Colors.Tomato);
                        break;
                }
            }
        });
    }
}
