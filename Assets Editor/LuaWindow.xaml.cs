using Assets_editor;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using MaterialDesignThemes.Wpf;
using MoonSharp.Interpreter;
using SkiaSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using Tibia.Protobuf.Appearances;

namespace Assets_Editor;

/// <summary>
/// Interaction logic for LuaWindow.xaml
/// </summary>
public partial class LuaWindow : Window, INotifyPropertyChanged {
    private Script? _luaScript;
    private ObservableCollection<dynamic> _dataItems = [];
    private ListCollectionView? _gridView;
    private string? _results;
    private ObservableCollection<ISeries> _chartSeries = [];
    private List<string> _availableColumns = [];
    private string? _selectedXAxis;
    private string? _selectedYAxis;
    private string? _selectedSecondYAxis;
    private List<string> _selectedColumns = [];
    private string _chartMode = "Single Line";
    private readonly ScriptManager _scriptManager;
    private string? _currentScriptName;
    private CancellationTokenSource _luaCancellation;
    private readonly ConcurrentQueue<string> _printQueue = new();

    private const string LightSyntaxFile = @"lua-syntax-light.xshd";
    private const string DarkSyntaxFile = @"lua-syntax-dark.xshd";


    public ObservableCollection<dynamic> DataItems {
        get => _dataItems;
        set {
            _dataItems = value;
            OnPropertyChanged(nameof(DataItems));
            // Keep a fresh view for DataGrid to avoid dynamic shape caching
            _gridView = _dataItems != null ? new ListCollectionView(_dataItems) : null;
            OnPropertyChanged(nameof(GridView));
        }
    }

    public ListCollectionView? GridView => _gridView;

    public ObservableCollection<ISeries> ChartSeries {
        get => _chartSeries ??= [];
        set {
            _chartSeries = value;
            OnPropertyChanged(nameof(ChartSeries));
        }
    }

    public List<string> AvailableColumns {
        get => _availableColumns;
        set {
            _availableColumns = value;
            OnPropertyChanged(nameof(AvailableColumns));
            UpdateComboBoxes();
        }
    }

    public string? SelectedXAxis {
        get => _selectedXAxis;
        set {
            _selectedXAxis = value;
            OnPropertyChanged(nameof(SelectedXAxis));
        }
    }

    public string? SelectedYAxis {
        get => _selectedYAxis;
        set {
            _selectedYAxis = value;
            OnPropertyChanged(nameof(SelectedYAxis));
        }
    }

    public string? SelectedSecondYAxis {
        get => _selectedSecondYAxis;
        set {
            _selectedSecondYAxis = value;
            OnPropertyChanged(nameof(SelectedSecondYAxis));
        }
    }

    public List<string> SelectedColumns {
        get => _selectedColumns;
        set {
            _selectedColumns = value;
            OnPropertyChanged(nameof(SelectedColumns));
        }
    }

    public string ChartMode {
        get => _chartMode;
        set {
            _chartMode = value;
            OnPropertyChanged(nameof(ChartMode));
        }
    }

    public string? Results {
        get => _results;
        set {
            _results = value;
            OnPropertyChanged(nameof(Results));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnBackgroundChanged(object? sender, EventArgs e) {
        if (LuaCodeEditor.Background is SolidColorBrush brush) {
            LoadSyntaxForTheme(IsDarkMode());
        }
    }

    private void ListenForThemeChanges() {
        var descriptor = DependencyPropertyDescriptor.FromProperty(
            TextEditor.BackgroundProperty, typeof(TextEditor));

        descriptor?.AddValueChanged(LuaCodeEditor, OnBackgroundChanged);
    }

    public LuaWindow() {
        InitializeComponent();
        DataContext = this;
        InitializeLuaEngine();
        InitializeTextEditor();
        ListenForThemeChanges();
        Results = "Lua Script Editor ready. Write your code and click Execute.";

        // Initialize script manager
        _scriptManager = new ScriptManager();
        _currentScriptName = null;

        LuaCodeEditor.Text = @"-- Lua Script Editor
-- Sample script to demonstrate chart functionality

function generateChartData()
    local result = {}
    for i = 1, 15 do
        table.insert(result, {
            Level = i,
            Experience = i * i * 100,
            Health = 100 + (i * 10),
            Mana = 50 + (i * 5),
            Damage = 20 + (i * 2.5),
            Defense = 10 + (i * 1.5)
        })
    end
    return result
end

return generateChartData()
";
        RefreshScriptList();
    }

    // helper to register userdata constructors
    private void RegisterLuaType<T>(string? luaName = null) where T : new() {
        // name in Lua (defaults to the class name)
        luaName ??= typeof(T).Name;

        // 1) Register userdata
        UserData.RegisterType<T>();

        // 2) Register constructor: Foo()
        _luaScript!.Globals[luaName] = DynValue.NewCallback((ctx, args) =>
        {
            return UserData.Create(new T());
        });
    }

    private void InitializeLuaEngine() {
        try {
            _luaScript = new Script();

            // Register common .NET types for Lua access
            UserData.RegisterType<Dictionary<string, object>>();
            UserData.RegisterType<List<object>>();

            // Add some utility functions to Lua
            _luaScript.Globals["print"] = DynValue.NewCallback(PrintToResults);

            // make the objects possible to index
            RegisterLuaType<Appearance>();
            RegisterLuaType<AppearanceFlags>();
            RegisterLuaType<AppearanceFlagCyclopedia>();
            RegisterLuaType<AppearanceFlagMarket>();
            RegisterLuaType<AppearanceFlagNPC>();
            RegisterLuaType<FrameGroup>();
            RegisterLuaType<SpriteInfo>();
            RegisterLuaType<SpriteAnimation>();
            RegisterLuaType<SpritePhase>();
            RegisterLuaType<Box>();

            // make it possible to use but without ability to construct
            UserData.RegisterType<LuaThings>();

            // access repeated field
            UserData.RegisterType<List<AppearanceFlagNPC>>();

            // g_things
            Table g_things = new(_luaScript);
            _luaScript.Globals["g_things"] = g_things;
            g_things["getOutfits"] = DynValue.NewCallback(LuaThings.Lua_getOutfits);
            g_things["getItems"] = DynValue.NewCallback(LuaThings.Lua_getItems);
            g_things["getEffects"] = DynValue.NewCallback(LuaThings.Lua_getEffects);
            g_things["getMissiles"] = DynValue.NewCallback(LuaThings.Lua_getMissiles);
            g_things["getOutfitById"] = DynValue.NewCallback(LuaThings.Lua_getOutfitById);
            g_things["getItemById"] = DynValue.NewCallback(LuaThings.Lua_getItemById);
            g_things["getEffectById"] = DynValue.NewCallback(LuaThings.Lua_getEffectById);
            g_things["getMissileById"] = DynValue.NewCallback(LuaThings.Lua_getMissileById);

        } catch (Exception ex) {
            ErrorManager.ShowError($"Failed to initialize Lua engine: {ex.Message}");
        }
    }

    private static bool IsDarkMode() {
        var palette = new PaletteHelper();
        ITheme theme = palette.GetTheme();
        return theme.GetBaseTheme() == BaseTheme.Dark;
    }

    private void LoadSyntaxForTheme(bool darkMode) {
        string xshdFile = darkMode ? DarkSyntaxFile : LightSyntaxFile;

        if (!File.Exists(xshdFile))
            return;

        try {
            using XmlTextReader reader = new(xshdFile);
            var definition = HighlightingLoader.Load(reader, HighlightingManager.Instance);
            LuaCodeEditor.SyntaxHighlighting = definition;
        } catch (Exception ex) {
            // Optional: log error
            Console.WriteLine($"Failed to load XSHD syntax: {ex.Message}");
        }
    }

    private void InitializeTextEditor() {
        LoadSyntaxForTheme(IsDarkMode());
    }

    private void FlushPrintInternal() {
        if (_printQueue.IsEmpty) return;

        StringBuilder sb = new();

        while (_printQueue.TryDequeue(out string? line))
            sb.AppendLine(line);

        // Update the bound property ONCE per frame
        Results += sb.ToString();

        ResultsTextBox.ScrollToEnd();
    }
    private void FlushPrintQueue(object? _, EventArgs __) {
        FlushPrintInternal();
    }

    private void ExecuteButton_Click(object sender, RoutedEventArgs e) {

        if (_luaScript == null) {
            Results = "Error: Lua engine not initialized.";
            return;
        }

        string luaCode = LuaCodeEditor.Text;
        if (string.IsNullOrWhiteSpace(luaCode)) {
            Results = "No Lua code to execute.";
            return;
        }

        // Clear previous results
        Results = "";

        // Set up cancellation
        _luaCancellation = new CancellationTokenSource();

        CancelButton.IsEnabled = true;
        ExecuteButton.IsEnabled = false;

        // Capture a snapshot of DataItems for Lua
        var dataForLua = DataItems.OfType<IDictionary<string, object>>()
                                   .Select(d => new Dictionary<string, object>(d))
                                   .ToList();

        // Run Lua on a background thread
        Task.Run(() => {
            try {
                var lua = _luaScript;

                // lua env is not possible to stop in this library unless it's the debugger triggering it
                CancelDebugger debugger = new() { CancellationToken = _luaCancellation.Token };
                lua.AttachDebugger(debugger);

                lua.Globals["data"] = dataForLua;

                DynValue func = lua.LoadString(luaCode);   // compiles but does NOT run
                DynValue coroutine = lua.CreateCoroutine(func);
                coroutine.Coroutine.AutoYieldCounter = 500; // yield every 500 instructions

                DynValue? result = null;

                // start tracking output
                DispatcherTimer? logTimer = null;
                Dispatcher.Invoke(() =>
                {
                    logTimer = new DispatcherTimer {
                        Interval = TimeSpan.FromMilliseconds(1000)
                    };
                    logTimer.Tick += FlushPrintQueue;
                    logTimer.Start();
                });

                while (coroutine.Coroutine.State != CoroutineState.Dead) {
                    try {
                        result = coroutine.Coroutine.Resume();
                    } catch (ScriptRuntimeException ex) {
                        Dispatcher.Invoke(() => {
                            FlushPrintInternal();
                            Results += $"\nLua Runtime Error: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
                            CancelButton.IsEnabled = false;
                            ExecuteButton.IsEnabled = true;
                        });
                        return;
                    }

                    if (_luaCancellation.Token.IsCancellationRequested) {
                        Dispatcher.Invoke(() => {
                            FlushPrintInternal();
                            Results += "\nScript execution cancelled by user.";
                            CancelButton.IsEnabled = false;
                            ExecuteButton.IsEnabled = true;
                        });
                        return;
                    }
                }

                // stop tracking output
                Dispatcher.Invoke(() =>
                {
                    if (logTimer != null) {
                        logTimer.Stop();
                        logTimer.Tick -= FlushPrintQueue;
                    }
                });

                // Execution finished successfully, update UI
                Dispatcher.Invoke(() =>
                {
                    CancelButton.IsEnabled = false;
                    ExecuteButton.IsEnabled = true;

                    FlushPrintInternal();

                    if (result != null && result.Type != DataType.Void) {
                        Results += $"\nScript executed successfully! (Result: {result.Type})";

                        if (result.Type == DataType.Table && (ShowInGridCheckBox?.IsChecked ?? false))
                            UpdateDataGridFromLuaResult(result);
                        else
                            Results += $"\n\nResult not sent to DataGrid (type: {result.Type}, ShowInGrid={ShowInGridCheckBox?.IsChecked}).";
                    } else
                        Results += "\nScript executed successfully (no return value).";
                });
            } catch (ScriptRuntimeException ex) {
                Dispatcher.Invoke(() => {
                    CancelButton.IsEnabled = false;
                    ExecuteButton.IsEnabled = true;
                    FlushPrintInternal();
                    Results += $"\nLua Runtime Error: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
                });
            } catch (Exception ex) {
                Dispatcher.Invoke(() => {
                    CancelButton.IsEnabled = false;
                    ExecuteButton.IsEnabled = true;
                    FlushPrintInternal();
                    Results += $"\nError executing Lua script: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
                });
            }

        });
    }

    private async void CancelButton_Click(object sender, RoutedEventArgs e) {
        Results += "\nStopping...\n";
        _luaCancellation?.Cancel();
        CancelButton.IsEnabled = false;
    }

    private void UpdateDataGridFromLuaResult(DynValue result) {
        try {
            var resultObject = result.ToObject();

            // Handle MoonSharp Table type directly
            if (result.Type == DataType.Table) {
                // Process MoonSharp table
                var table = result.Table;
                var newDataItems = new ObservableCollection<dynamic>();

                // Convert table to list of dictionaries
                foreach (var kvp in table.Pairs) {
                    if (kvp.Value.Type == DataType.Table) {
                        var itemDict = new Dictionary<string, object>();
                        var itemTable = kvp.Value.Table;

                        foreach (var itemKvp in itemTable.Pairs) {
                            var rawKey = itemKvp.Key.Type == DataType.String ? itemKvp.Key.String : itemKvp.Key.ToString();
                            var key = SanitizeKey(rawKey);
                            var value = ConvertMoonSharpValue(itemKvp.Value);
                            itemDict[key] = value;

                        }

                        var anonymousObject = CreateAnonymousObject(itemDict);
                        newDataItems.Add(anonymousObject);
                    }
                }

                // Replace collection and view to trigger DataGrid regeneration
                DataItems = newDataItems;
                Results += $"\n\nDataGrid updated with {newDataItems.Count} items.";

                // Update available columns for chart
                UpdateAvailableColumns();

                // Force DataGrid refresh: rebuild columns for current property set
                Dispatcher.BeginInvoke(new Action(() => {
                    DataGrid.Columns.Clear();
                    if (DataItems.Count > 0 && DataItems[0] is IDictionary<string, object> first) {
                        foreach (var key in first.Keys) {
                            var col = new MaterialDesignThemes.Wpf.DataGridTextColumn {
                                Header = key,
                                Binding = new Binding() {
                                    Path = new PropertyPath($"[{key}]")
                                }
                            };
                            DataGrid.Columns.Add(col);
                        }
                    }
                    DataGrid.ItemsSource = GridView;
                    DataGrid.UpdateLayout();
                }), System.Windows.Threading.DispatcherPriority.Background);
            } else if (resultObject is List<object> list) {

                var newDataItems = new ObservableCollection<dynamic>();
                foreach (var item in list) {

                    if (item is Dictionary<string, object> dict) {
                        // Convert dictionary to anonymous object for DataGrid display
                        var anonymousObject = CreateAnonymousObject(dict);
                        newDataItems.Add(anonymousObject);
                    } else {
                        newDataItems.Add(item);
                    }
                }
                DataItems = newDataItems;
                Results += "\n\nDataGrid updated.";

                // Update available columns for chart
                UpdateAvailableColumns();

                // Force DataGrid refresh using binding
                Dispatcher.BeginInvoke(new Action(() => {
                    DataGrid.ClearValue(DataGrid.ItemsSourceProperty);
                    DataGrid.SetBinding(DataGrid.ItemsSourceProperty, new Binding("DataItems") { Source = this });
                    DataGrid.UpdateLayout();
                }), System.Windows.Threading.DispatcherPriority.Background);
            } else if (resultObject is Dictionary<string, object> dict) {

                var newDataItems = new ObservableCollection<dynamic>();
                var anonymousObject = CreateAnonymousObject(dict);
                newDataItems.Add(anonymousObject);
                DataItems = newDataItems;
                Results += "\n\nDataGrid updated with 1 item.";

                // Update available columns for chart
                UpdateAvailableColumns();

                // Force DataGrid refresh using binding
                Dispatcher.BeginInvoke(new Action(() => {
                    DataGrid.ClearValue(DataGrid.ItemsSourceProperty);
                    DataGrid.SetBinding(DataGrid.ItemsSourceProperty, new Binding("DataItems") { Source = this });
                    DataGrid.UpdateLayout();
                }), System.Windows.Threading.DispatcherPriority.Background);
            } else {
                Results += $"\nUnsupported result type: {resultObject?.GetType().Name}";
            }
        } catch (Exception ex) {
            Results += $"\n\nError updating DataGrid: {ex.Message}\n{ex.StackTrace}";
        }
    }

    private static object ConvertMoonSharpValue(DynValue value) {
        switch (value.Type) {
            case DataType.Number:
                return value.Number;
            case DataType.String:
                return value.String;
            case DataType.Boolean:
                return value.Boolean;
            case DataType.Table:
                var dict = new Dictionary<string, object>();
                foreach (var kvp in value.Table.Pairs) {
                    var rawKey = kvp.Key.Type == DataType.String ? kvp.Key.String : kvp.Key.ToString();
                    dict[SanitizeKey(rawKey)] = ConvertMoonSharpValue(kvp.Value);
                }
                return dict;
            default:
                return value.ToString();
        }
    }

    private static string SanitizeKey(string key) {
        if (string.IsNullOrEmpty(key)) return key;
        // Remove surrounding quotes that MoonSharp sometimes adds when ToString() is used
        if (key.Length >= 2 && ((key[0] == '"' && key[^1] == '"') || (key[0] == '\'' && key[^1] == '\'')))
            key = key.Substring(1, key.Length - 2);
        return key;
    }

    // Return a plain dictionary so WPF can bind with indexer syntax [key]
    private static Dictionary<string, object> CreateAnonymousObject(Dictionary<string, object> dict) => new(dict);

    private readonly object _printLock = new();
    private int _tokens = 200;            // burst capacity
    private const int MaxTokens = 200;    // max prints in a burst
    private const int RefillRate = 20;    // prints per second restored
    private DateTime _lastRefill = DateTime.Now;
    private int _printCounter = 0;
    private DynValue PrintToResults(ScriptExecutionContext context, CallbackArguments args) {
        lock (_printLock) {
            // Refill tokens based on time passed
            var now = DateTime.Now;
            double seconds = (now - _lastRefill).TotalSeconds;
            if (seconds > 0) {
                _tokens = Math.Min(MaxTokens, _tokens + (int)(seconds * RefillRate));
                _lastRefill = now;
            }

            // Too much printing -> drop outputs to prevent UI flood
            if (_tokens <= 0) {
                if (_printCounter % 10000 == 0)
                    _printQueue.Enqueue("Too many print calls! Consider dumping script output to a file.");

                _printCounter++;
                return DynValue.Nil; // no yield needed
            }

            // Consume a token and print
            _tokens--;

            // Format message
            List<string> messages = new();
            for (int i = 0; i < args.Count; i++)
                messages.Add(args[i].ToPrintString());

            _printQueue.Enqueue(string.Join("\t", messages));

            // Yield every ~50 actual prints
            if (++_printCounter % 50 == 0)
                return DynValue.NewYieldReq([]);

            return DynValue.Nil;
        }
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e) {
        LuaCodeEditor.Text = "-- Lua Script Editor\n-- Write your Lua code here\n-- Access data through the 'data' variable\n\n";
        Results = "Script cleared.";
    }

    private void UpdateComboBoxes() {
        if (XAxisComboBox != null && AvailableColumnsListBox != null) {
            XAxisComboBox.ItemsSource = AvailableColumns;
            AvailableColumnsListBox.ItemsSource = AvailableColumns;

            // Set default selections if available
            if (AvailableColumns.Count > 0) {
                if (string.IsNullOrEmpty(SelectedXAxis) && AvailableColumns.Count > 0)
                    SelectedXAxis = AvailableColumns[0];
                if (string.IsNullOrEmpty(SelectedYAxis) && AvailableColumns.Count > 1)
                    SelectedYAxis = AvailableColumns[1];
                if (string.IsNullOrEmpty(SelectedSecondYAxis) && AvailableColumns.Count > 2)
                    SelectedSecondYAxis = AvailableColumns[2];
            }
        }
    }

    private void XAxisComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
        if (XAxisComboBox.SelectedItem is string selected) {
            SelectedXAxis = selected;
            RefreshChart();
        }
    }

    private void ChartModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
        if (ChartModeComboBox.SelectedItem is ComboBoxItem selectedItem) {
            ChartMode = selectedItem?.Content?.ToString() ?? "Single Line";

            // Show/hide custom selection panel
            if (CustomSelectionPanel != null) {
                CustomSelectionPanel.Visibility = ChartMode == "Custom Selection" ?
                    Visibility.Visible : Visibility.Collapsed;
            }

            RefreshChart();
        }
    }

    private void SelectAllColumnsButton_Click(object sender, RoutedEventArgs e) {
        if (AvailableColumnsListBox != null) {
            AvailableColumnsListBox.SelectAll();
            RefreshChart();
        }
    }

    private void ClearSelectionButton_Click(object sender, RoutedEventArgs e) {
        if (AvailableColumnsListBox != null) {
            AvailableColumnsListBox.UnselectAll();
            RefreshChart();
        }
    }

    private void RefreshChartButton_Click(object sender, RoutedEventArgs e) {
        RefreshChart();
    }

    private void ExportCsvButton_Click(object sender, RoutedEventArgs e) {
        try {
            if (DataItems == null || DataItems.Count == 0) {
                Results += "\nNo data to export.";
                return;
            }

            // Build CSV in memory
            var sb = new StringBuilder();

            // Collect headers from first item (dictionary keys)
            if (DataItems[0] is IDictionary<string, object> first) {
                var headers = first.Keys.ToList();
                sb.AppendLine(string.Join(",", headers.Select(EscapeCsv)));

                // Rows
                var invariant = CultureInfo.InvariantCulture;
                foreach (var item in DataItems) {
                    if (item is IDictionary<string, object> dict) {
                        var row = headers.Select(h => {
                            dict.TryGetValue(h, out var value); // value may be null
                            return FormatCsvValue(value ?? string.Empty, invariant);
                        });
                        sb.AppendLine(string.Join(",", row));
                    }
                }
            } else {
                Results += "\nData format is not exportable (expected dictionary rows).";
                return;
            }

            // Prompt for file path
            var sfd = new Microsoft.Win32.SaveFileDialog {
                Title = "Export CSV",
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                FileName = "export.csv",
                OverwritePrompt = true,
                ClientGuid = Globals.GUID_LuaWindowExportCSV
            };
            var ok = sfd.ShowDialog(this);
            if (ok == true) {
                System.IO.File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                Results += $"\nExported CSV to: {sfd.FileName}";
            }
        } catch (Exception ex) {
            Results += $"\nError exporting CSV: {ex.Message}";
        }
    }

    private static string EscapeCsv(string s) {
        if (s == null) return "";
        var needsQuotes = s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r');
        if (s.Contains('"')) s = s.Replace("\"", "\"\"");
        return needsQuotes ? "\"" + s + "\"" : s;
    }

    private static string FormatCsvValue(object value, CultureInfo invariant) {
        if (value == null) return "";
        return value switch {
            IFormattable f => EscapeCsv(f.ToString(null, invariant)),
            _ => EscapeCsv(value.ToString() ?? ""),
        };
    }

    private void RefreshChart() {
        if (DataItems == null || DataItems.Count == 0 || string.IsNullOrEmpty(SelectedXAxis)) {
            if (ChartSeries != null)
                ChartSeries.Clear();
            return;
        }

        try {
            if (ChartSeries == null)
                ChartSeries = [];

            ChartSeries.Clear();

            // Get list of columns to chart based on mode
            var columnsToChart = GetColumnsToChart();

            if (columnsToChart.Count == 0) {
                return;
            }

            // Define colors for different lines
            var colors = new SKColor[]
            {
                SKColors.Blue, SKColors.Red, SKColors.Green, SKColors.Orange,
                SKColors.Purple, SKColors.Brown, SKColors.Pink, SKColors.Cyan,
                SKColors.Magenta, SKColors.Yellow, SKColors.Gray, SKColors.Lime
            };

            // Create a line series for each selected column
            for (int i = 0; i < columnsToChart.Count; i++) {
                var columnName = columnsToChart[i];
                var points = new List<ObservablePoint>();
                var color = colors[i % colors.Length];

                foreach (var item in DataItems) {
                    if (item is IDictionary<string, object> dict) {
                        if (dict.TryGetValue(SelectedXAxis, out var xValue) &&
                            dict.TryGetValue(columnName, out var yValue)) {
                            // Convert X value to double
                            double xDouble = 0;
                            if (double.TryParse(xValue?.ToString(), out double parsedX)) {
                                xDouble = parsedX;
                            } else {
                                xDouble = points.Count;
                            }

                            // Convert Y value to double
                            double yDouble = 0;
                            if (double.TryParse(yValue?.ToString(), out double parsedY)) {
                                yDouble = parsedY;
                            }

                            points.Add(new ObservablePoint(xDouble, yDouble));
                        }
                    }
                }

                if (points.Count > 0) {
                    ChartSeries.Add(new LineSeries<ObservablePoint> {
                        Name = columnName,
                        Values = points,
                        Stroke = new SolidColorPaint(color, 2),
                        Fill = null,
                        GeometryStroke = new SolidColorPaint(color, 2),
                        GeometryFill = new SolidColorPaint(color),
                        GeometrySize = 6
                    });
                }
            }
        } catch (Exception ex) {
            Results += $"\nError refreshing chart: {ex.Message}";
        }
    }

    private List<string> GetColumnsToChart() {
        var columnsToChart = new List<string>();

        switch (ChartMode) {
            case "Single Line":
                if (!string.IsNullOrEmpty(SelectedYAxis)) {
                    columnsToChart.Add(SelectedYAxis);
                }
                break;

            case "All Numeric Columns":
                // Add all columns except the X-axis column
                foreach (var column in AvailableColumns) {
                    if (column != SelectedXAxis && IsNumericColumn(column)) {
                        columnsToChart.Add(column);
                    }
                }
                break;

            case "Custom Selection":
                // Get selected columns from the listbox
                if (AvailableColumnsListBox != null) {
                    foreach (var selectedItem in AvailableColumnsListBox.SelectedItems) {
                        if (selectedItem is string columnName && columnName != SelectedXAxis) {
                            columnsToChart.Add(columnName);
                        }
                    }
                }
                break;
        }

        return columnsToChart;
    }

    private bool IsNumericColumn(string columnName) {
        if (DataItems.Count == 0) return false;

        // Check first few items to see if they contain numeric values
        int sampleSize = Math.Min(5, DataItems.Count);
        for (int i = 0; i < sampleSize; i++) {
            if (DataItems[i] is IDictionary<string, object> dict) {
                if (dict.TryGetValue(columnName, out var value)) {
                    if (!double.TryParse(value?.ToString(), out _)) {
                        return false;
                    }
                }
            }
        }
        return true;
    }

    private void UpdateAvailableColumns() {
        var columns = new List<string>();

        if (DataItems.Count > 0 && DataItems[0] is IDictionary<string, object> firstItem) {
            columns.AddRange(firstItem.Keys);
        }

        AvailableColumns = columns;
    }

    protected virtual void OnPropertyChanged(string propertyName) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #region Script Management

    private void RefreshScriptList() {
        if (ScriptListComboBox != null) {
            ScriptListComboBox.ItemsSource = _scriptManager.Scripts;
            ScriptListComboBox.DisplayMemberPath = "Name";
            ScriptListComboBox.SelectedValuePath = "Name";
        }
    }

    private void ScriptListComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
        if (ScriptListComboBox.SelectedItem is ScriptData selectedScript) {
            LoadScript(selectedScript);
        }
    }

    private void NewScriptButton_Click(object sender, RoutedEventArgs e) {
        var scriptName = ShowInputDialog("Enter script name:", "New Script", "Untitled Script");

        if (!string.IsNullOrWhiteSpace(scriptName)) {
            try {
                var newScript = new ScriptData {
                    Name = scriptName,
                    Code = "-- New Lua Script\n-- Write your code here\n\n",
                    Description = "New script"
                };

                _scriptManager.AddScript(newScript);
                RefreshScriptList();
                ScriptListComboBox.SelectedItem = newScript;
                _currentScriptName = scriptName;
                Results += $"\nCreated new script: {scriptName}";
            } catch (Exception ex) {
                Results += $"\nError creating script: {ex.Message}";
            }
        }
    }

    private void SaveScriptButton_Click(object sender, RoutedEventArgs e) {
        var scriptName = ShowInputDialog("Enter script name:", "Save Script", _currentScriptName ?? "Untitled Script");

        if (!string.IsNullOrWhiteSpace(scriptName)) {
            try {
                var currentCode = LuaCodeEditor.Text;

                if (_scriptManager.ScriptExists(scriptName)) {
                    _scriptManager.UpdateScript(scriptName, currentCode);
                    Results += $"\nUpdated script: {scriptName}";
                } else {
                    var newScript = new ScriptData {
                        Name = scriptName,
                        Code = currentCode,
                        Description = "Saved script"
                    };
                    _scriptManager.AddScript(newScript);
                    Results += $"\nSaved new script: {scriptName}";
                }

                RefreshScriptList();
                ScriptListComboBox.SelectedItem = _scriptManager.GetScript(scriptName);
                _currentScriptName = scriptName;
            } catch (Exception ex) {
                Results += $"\nError saving script: {ex.Message}";
            }
        }
    }

    private void DeleteScriptButton_Click(object sender, RoutedEventArgs e) {
        if (ScriptListComboBox.SelectedItem is ScriptData selectedScript) {
            var result = MessageBox.Show(
                $"Are you sure you want to delete the script '{selectedScript.Name}'?",
                "Delete Script",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes) {
                try {
                    _scriptManager.DeleteScript(selectedScript.Name);
                    RefreshScriptList();
                    LuaCodeEditor.Text = "-- Lua Script Editor\n-- Write your Lua code here\n\n";
                    _currentScriptName = null;
                    Results += $"\nDeleted script: {selectedScript.Name}";
                } catch (Exception ex) {
                    Results += $"\nError deleting script: {ex.Message}";
                }
            }
        } else {
            Results += "\nNo script selected to delete.";
        }
    }

    private void RefreshScriptsButton_Click(object sender, RoutedEventArgs e) {
        _scriptManager.LoadScripts();
        RefreshScriptList();
        Results += "\nScript list refreshed.";
    }

    private void LoadScript(ScriptData script) {
        if (script != null) {
            LuaCodeEditor.Text = script.Code;
            _currentScriptName = script.Name;
            Results += $"\nLoaded script: {script.Name}";
        }
    }

    private string ShowInputDialog(string message, string title, string defaultValue = "") {
        var inputDialog = new Window() {
            Title = title,
            Width = 400,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            ResizeMode = ResizeMode.NoResize
        };

        var stackPanel = new StackPanel { Margin = new Thickness(20) };

        var textBlock = new TextBlock {
            Text = message,
            Margin = new Thickness(0, 0, 0, 10),
            TextWrapping = TextWrapping.Wrap
        };

        var textBox = new TextBox {
            Text = defaultValue,
            Margin = new Thickness(0, 0, 0, 20),
            Height = 25
        };

        var buttonPanel = new StackPanel {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var okButton = new Button {
            Content = "OK",
            Width = 75,
            Height = 25,
            Margin = new Thickness(0, 0, 10, 0),
            IsDefault = true
        };

        var cancelButton = new Button {
            Content = "Cancel",
            Width = 75,
            Height = 25,
            IsCancel = true
        };

        string? result = null;

        okButton.Click += (s, e) => {
            result = textBox.Text;
            inputDialog.Close();
        };

        cancelButton.Click += (s, e) => {
            result = null;
            inputDialog.Close();
        };

        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);

        stackPanel.Children.Add(textBlock);
        stackPanel.Children.Add(textBox);
        stackPanel.Children.Add(buttonPanel);

        inputDialog.Content = stackPanel;

        textBox.Focus();
        textBox.SelectAll();

        inputDialog.ShowDialog();

        return result ?? "";
    }

    #endregion

    protected override void OnClosed(EventArgs e) {
        base.OnClosed(e);
    }
}
