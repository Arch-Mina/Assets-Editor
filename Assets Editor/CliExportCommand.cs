using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Assets_Editor
{
    public static class CliExportCommand
    {
        public static bool IsCli(string[] args)
        {
            return args.Length > 0 &&
                (string.Equals(args[0], "export-legacy", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(args[0], "--list-profiles", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(args[0], "--help", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(args[0], "-h", StringComparison.OrdinalIgnoreCase));
        }

        public static async Task<int> RunAsync(string[] args)
        {
            ConsoleBridge.AttachParentConsole();

            try
            {
                if (args.Length == 0 || string.Equals(args[0], "--help", StringComparison.OrdinalIgnoreCase) || string.Equals(args[0], "-h", StringComparison.OrdinalIgnoreCase))
                {
                    PrintHelp();
                    return 0;
                }

                if (string.Equals(args[0], "--list-profiles", StringComparison.OrdinalIgnoreCase))
                {
                    PrintProfiles();
                    return 0;
                }

                if (!string.Equals(args[0], "export-legacy", StringComparison.OrdinalIgnoreCase))
                {
                    Console.Error.WriteLine($"Unknown command '{args[0]}'.");
                    PrintHelp();
                    return 2;
                }

                var values = ParseOptions(args);
                if (values.ContainsKey("list-profiles"))
                {
                    PrintProfiles();
                    return 0;
                }

                var profile = LegacyAssetExportProfiles.Get(GetValue(values, "profile"));
                var options = new LegacyAssetExportOptions
                {
                    InputPath = GetValue(values, "input"),
                    OutputPath = GetValue(values, "output"),
                    Profile = profile,
                    Overwrite = values.ContainsKey("overwrite"),
                    Backup = !values.ContainsKey("no-backup"),
                };

                var exporter = new LegacyAssetExporter();
                var result = await exporter.ExportAsync(
                    options,
                    new InlineProgress<string>(message => Console.WriteLine(message)),
                    new InlineProgress<int>(percent => Console.WriteLine($"sprite-slicing {percent}%")),
                    new InlineProgress<int>(percent => Console.WriteLine($"spr-writing {percent}%")));

                Console.WriteLine($"exported profile={result.Profile.Id}");
                Console.WriteLine($"dat={result.DatPath}");
                Console.WriteLine($"spr={result.SprPath}");
                foreach (var backup in result.Backups)
                {
                    Console.WriteLine($"backup={backup}");
                }

                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception.Message);
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static Dictionary<string, string> ParseOptions(string[] args)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 1; index < args.Length; index++)
            {
                var arg = args[index];
                if (!arg.StartsWith("--", StringComparison.Ordinal))
                {
                    throw new ArgumentException($"Unexpected positional argument '{arg}'.");
                }

                var key = arg[2..];
                if (key is "overwrite" or "no-backup" or "list-profiles")
                {
                    values[key] = null;
                    continue;
                }

                if (index + 1 >= args.Length)
                {
                    throw new ArgumentException($"Missing value for option '{arg}'.");
                }

                values[key] = args[++index];
            }

            return values;
        }

        private static string GetValue(Dictionary<string, string> values, string key)
        {
            if (!values.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"Missing required option '--{key}'.");
            }

            return value;
        }

        private static void PrintProfiles()
        {
            foreach (var profile in LegacyAssetExportProfiles.All)
            {
                Console.WriteLine($"{profile.Id}: {profile.DisplayName}");
                Console.WriteLine($"  {profile.Description}");
            }
        }

        private static void PrintHelp()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  Assets Editor.exe --list-profiles");
            Console.WriteLine("  Assets Editor.exe export-legacy --profile cip860-extended --input <assets-or-bin-path> --output <client-path> [--overwrite] [--no-backup]");
            Console.WriteLine();
            Console.WriteLine("Profile notes:");
            Console.WriteLine("  cip860-extended keeps the CipSoft 8.60 dat object layout and writes extended uint32 sprite ids.");
            Console.WriteLine("  It intentionally does not write modern dat flags such as Clothes/attr 32, Market, DefaultAction, Wrap, or TopEffect.");
            Console.WriteLine("  Outfit colors still come from the game protocol lookHead/lookBody/lookLegs/lookFeet/lookAddons fields and the classic outfit sprite layout.");
        }

        private sealed class InlineProgress<T> : IProgress<T>
        {
            private readonly Action<T> report;

            public InlineProgress(Action<T> report)
            {
                this.report = report;
            }

            public void Report(T value)
            {
                report(value);
            }
        }

        private static class ConsoleBridge
        {
            private const int AttachParentProcess = -1;

            [DllImport("kernel32.dll")]
            private static extern bool AttachConsole(int dwProcessId);

            [DllImport("kernel32.dll")]
            private static extern IntPtr GetConsoleWindow();

            public static void AttachParentConsole()
            {
                if (GetConsoleWindow() == IntPtr.Zero)
                {
                    AttachConsole(AttachParentProcess);
                }

                var stdout = Console.OpenStandardOutput();
                var stderr = Console.OpenStandardError();
                Console.SetOut(new StreamWriter(stdout) { AutoFlush = true });
                Console.SetError(new StreamWriter(stderr) { AutoFlush = true });
            }
        }
    }
}
