namespace Assets_Editor;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application {
    public App() { }

    protected override async void OnStartup(System.Windows.StartupEventArgs e)
    {
        if (CliExportCommand.IsCli(e.Args))
        {
            ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;
            var exitCode = await CliExportCommand.RunAsync(e.Args);
            Shutdown(exitCode);
            return;
        }

        base.OnStartup(e);
    }
}
