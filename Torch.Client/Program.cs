using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using NLog;
using Torch.Utils;
using MessageBox = System.Windows.MessageBox;

namespace Torch.Client
{
    public static class Program
    {
#if SPACE
        private const string InstallAliasName = "SpaceEngineersAlias";
        private const string ProgramName = "SpaceEngineers";
#endif
#if MEDIEVAL
        private const string InstallAliasName = "MedievalEngineersAlias";
        private const string ProgramName = "MedievalEngineers";
#endif
        public const string BinariesDirName = "Bin64";
        private static string _installAlias = null;

        public static string InstallAlias
        {
            get
            {
                // ReSharper disable once ConvertIfStatementToNullCoalescingExpression
                if (_installAlias == null)
                {
                    // ReSharper disable once AssignNullToNotNullAttribute
                    _installAlias = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location),
                        InstallAliasName);
                }
                return _installAlias;
            }
        }

        private static readonly string _steamDirectory = $"steamapps\\common\\{ProgramName}\\";
        private static readonly string _steamVerifyFile = BinariesDirName + $"\\{ProgramName}.exe";

        public const string ConfigName = "Torch.cfg";

        private static Logger _log = LogManager.GetLogger("Torch");

#if DEBUG
        [DllImport("kernel32.dll")]
        private static extern void AllocConsole();
        [DllImport("kernel32.dll")]
        private static extern void FreeConsole();
#endif
        public static void Main(string[] args)
        {
#if DEBUG
            try
            {
                AllocConsole();
#endif
                if (!TorchLauncher.IsTorchWrapped())
                {
                    AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

                    // Early config: Resolve install directory.
                    if (!File.Exists(Path.Combine(InstallAlias, _steamVerifyFile)))
                        SetupInstallAlias();

                    TorchLauncher.Launch(Assembly.GetEntryAssembly().FullName, args,
                        Path.Combine(InstallAlias, BinariesDirName));
                    return;
                }

                RunClient();
#if DEBUG
            }
            finally
            {
                FreeConsole();
            }
#endif
        }

        private static void SetupInstallAlias()
        {
            string installDirectory = null;

            // TODO look at Steam/config/Config.VDF?  Has alternate directories.
            var steamDir =
                Microsoft.Win32.Registry.GetValue("HKEY_CURRENT_USER\\SOFTWARE\\Valve\\Steam", "SteamPath",
                    null) as string;
            if (steamDir != null)
            {
                installDirectory = Path.Combine(steamDir, _steamDirectory);
                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (File.Exists(Path.Combine(installDirectory, _steamVerifyFile)))
                    _log.Debug("Found "+ ProgramName+" in {0}", installDirectory);
                else
                {
                    _log.Debug("Couldn't find  " + ProgramName + " in {0}", installDirectory);
                    installDirectory = null;
                }
            }
            if (installDirectory == null)
            {
                var dialog = new System.Windows.Forms.FolderBrowserDialog
                {
                    Description = "Please select the "+ProgramName+" installation folder"
                };
                do
                {
                    if (dialog.ShowDialog() != DialogResult.OK)
                    {
                        var ex = new FileNotFoundException(
                            "Unable to find the " + ProgramName + " install directory, aborting");
                        _log.Fatal(ex);
                        LogManager.Flush();
                        throw ex;
                    }
                    installDirectory = dialog.SelectedPath;
                    if (File.Exists(Path.Combine(installDirectory, _steamVerifyFile)))
                        break;
                    if (MessageBox.Show(
                            $"Unable to find {0} in {1}.  Are you sure it's the " + ProgramName + " install directory?",
                            "Invalid " + ProgramName + " Directory", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        break;
                } while (true); // Repeat until they confirm.
            }
            if (!JunctionLink(InstallAlias, installDirectory))
            {
                var ex = new IOException(
                    $"Failed to create junction link {InstallAlias} => {installDirectory}. Aborting.");
                _log.Fatal(ex);
                LogManager.Flush();
                throw ex;
            }
            string junctionVerify = Path.Combine(InstallAlias, _steamVerifyFile);
            if (!File.Exists(junctionVerify))
            {
                var ex = new FileNotFoundException(
                    $"Junction link is not working.  File {junctionVerify} does not exist");
                _log.Fatal(ex);
                LogManager.Flush();
                throw ex;
            }
        }

        private static bool JunctionLink(string linkName, string targetDir)
        {
            var junctionLinkProc = new ProcessStartInfo("cmd.exe", $"/c mklink /J \"{linkName}\" \"{targetDir}\"")
            {
                WorkingDirectory = Directory.GetCurrentDirectory(),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                StandardOutputEncoding = Encoding.ASCII
            };
            Process cmd = Process.Start(junctionLinkProc);
            // ReSharper disable once PossibleNullReferenceException
            while (!cmd.HasExited)
            {
                string line = cmd.StandardOutput.ReadLine();
                if (!string.IsNullOrWhiteSpace(line))
                    _log.Info(line);
                Thread.Sleep(100);
            }
            if (cmd.ExitCode != 0)
                _log.Error("Unable to create junction link {0} => {1}", linkName, targetDir);
            return cmd.ExitCode == 0;
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = (Exception) e.ExceptionObject;
            _log.Error(ex);
            LogManager.Flush();
            MessageBox.Show(ex.StackTrace, ex.Message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RunClient()
        {
            var client = new TorchClient();

            try
            {
                client.Init();
            }
            catch (Exception e)
            {
                _log.Fatal("Torch encountered an error trying to initialize the game.");
                _log.Fatal(e);
                return;
            }

            client.Start();
        }
    }
}