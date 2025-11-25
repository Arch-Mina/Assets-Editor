using System;

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
            ErrorManager.ShowException(e.Exception);

            e.Handled = true; // Prevent WPF crashing dialog

            Shutdown();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) {
            Exception? ex = e.ExceptionObject as Exception;
            if (ex != null) {
                ErrorManager.ShowException(ex);
            }

            // Runtime will force shutdown anyway
            Environment.Exit(1);
        }
    }
}
