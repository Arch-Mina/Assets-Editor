using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
