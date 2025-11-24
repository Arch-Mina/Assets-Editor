using System;
using System.Windows.Forms;

namespace Assets_Editor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application {
        public App() {
            // Catch UI thread exceptions
            DispatcherUnhandledException += App_DispatcherUnhandledException;

            // Catch background-thread exceptions
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e) {
            MessageBox.Show(
                e.Exception.ToString(),
                "Unexpected Error (UI Thread)",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            e.Handled = true; // Prevent WPF crashing dialog

            Shutdown();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) {
            var ex = e.ExceptionObject as Exception;

            MessageBox.Show(
                ex?.ToString() ?? "Unknown error",
                "Unexpected Error (Non-UI Thread)",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            // Runtime will force shutdown anyway
            Environment.Exit(1);
        }
    }
}
