using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace Assets_Editor
{
    /// <summary>
    /// Interaction logic for LogView.xaml
    /// </summary>
    public partial class LogView : Window
    {
        public class LogEntry
        {
            public DateTime Timestamp { get; set; }
            public string Level { get; set; }
            public string Message { get; set; }
        }
        public ObservableCollection<LogEntry> LogEntries { get; } = new ObservableCollection<LogEntry>();
        public LogView()
        {
            InitializeComponent();
            LogListView.ItemsSource = LogEntries;
        }
        public void AddLogEntry(LogEntry entry)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => AddLogEntry(entry));
                return;
            }
            LogEntries.Add(entry);
        }
    }
}
