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
            var exitCode = CliExportCommand.RunAsync(e.Args).GetAwaiter().GetResult();
            Shutdown(exitCode);
            return;
        }

        base.OnStartup(e);
        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        mainWindow.Show();
    }
}
