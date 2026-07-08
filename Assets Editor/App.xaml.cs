namespace Assets_Editor;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application {
    public App() { }

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        if (CliExportCommand.IsCli(e.Args))
        {
            ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;
            var synchronizationContext = System.Threading.SynchronizationContext.Current;
            try
            {
                System.Threading.SynchronizationContext.SetSynchronizationContext(null);
                var exitCode = CliExportCommand.RunAsync(e.Args).GetAwaiter().GetResult();
                Shutdown(exitCode);
            }
            finally
            {
                System.Threading.SynchronizationContext.SetSynchronizationContext(synchronizationContext);
            }

            return;
        }

        base.OnStartup(e);
        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        mainWindow.Show();
    }
}
