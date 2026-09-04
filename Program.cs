using System;
using System.Diagnostics;
using System.Security.Principal;

namespace SynezSAB
{
    internal static class Program
    {
        static int Main(string[] args)
        {
            if (!IsAdmin())
            {
                RelaunchAsAdmin(args);
                return 0;
            }

            string arg = args.Length > 0 ? args[0].ToLowerInvariant() : "";
            bool killExplorer = Array.Exists(args, a => a.Equals("--explorer", StringComparison.OrdinalIgnoreCase));
            bool silent       = Array.Exists(args, a => a.Equals("--s",        StringComparison.OrdinalIgnoreCase));

            if (arg == "--a") return Activate(killExplorer, silent);
            if (arg == "--r") return Remove(FindBackupArg(args), killExplorer, silent);

            Console.WriteLine("SynezSAB — StartAllBack patcher");
            Console.WriteLine();
            Console.WriteLine("  --a [--explorer] [--s]              Patch StartAllBackX64.dll");
            Console.WriteLine("  --r [backup.bak] [--explorer] [--s] Restore from backup (auto-detect if omitted)");
            Console.WriteLine();
            Console.WriteLine("  --explorer  Kill processes holding the DLL and restart explorer.");
            Console.WriteLine("              Without it the patcher attempts a direct write (may fail if DLL is locked).");
            Console.WriteLine("  --s         Silent — no console output (errors still go to stderr).");
            return 0;
        }

        private static string FindBackupArg(string[] args)
        {
            for (int i = 1; i < args.Length; i++)
                if (!args[i].Equals("--explorer", StringComparison.OrdinalIgnoreCase))
                    return args[i];
            return null;
        }

        private static int Activate(bool killExplorer, bool silent)
        {
            var log     = new ConsoleLog(silent);
            var patcher = new Patcher(log);

            patcher.ResetTrialReminder();
            patcher.Checkup();

            if (!patcher.CheckupIsValid)
            {
                if (!silent) Console.Error.WriteLine("Checkup failed — aborting.");
                return 1;
            }

            patcher.Patch(doBackup: true, killExplorer: killExplorer);
            return 0;
        }

        private static bool IsAdmin()
        {
            using var identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static void RelaunchAsAdmin(string[] args)
        {
            string exe = Process.GetCurrentProcess().MainModule.FileName;
            string arguments = args.Length > 0 ? string.Join(" ", args) : "";
            try
            {
                Process.Start(new ProcessStartInfo(exe, arguments)
                {
                    Verb = "runas",
                    UseShellExecute = true,
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to elevate: {ex.Message}");
            }
        }

        private static int Remove(string backupPath, bool killExplorer, bool silent)
        {
            var log     = new ConsoleLog(silent);
            var patcher = new Patcher(log);

            if (backupPath != null)
            {
                patcher.SetBackupPath(backupPath);
            }
            else
            {
                string latest = patcher.FindLatestBackup();
                if (latest == null)
                {
                    if (!silent)
                    {
                        Console.Error.WriteLine("No backup (.bak) found next to the DLL, next to this EXE, or in the current directory.");
                        Console.Error.WriteLine("Either run --a first (it auto-creates a backup), or supply the path:");
                        Console.Error.WriteLine("  --r \"C:\\Program Files\\StartAllBack\\StartAllBackX64.dll.YYYY-MM-DD_HH-MM-SS.bak\"");
                    }
                    return 1;
                }
                log.Info("Auto-detected backup: ");
                log.Done(latest);
                patcher.SetBackupPath(latest);
            }

            patcher.Restore(killExplorer: killExplorer);
            return 0;
        }
    }
}
