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

            if (arg == "--a") return Activate();
            if (arg == "--r") return Remove(args.Length > 1 ? args[1] : null);

            Console.WriteLine("SynezSAB — StartAllBack patcher");
            Console.WriteLine();
            Console.WriteLine("  --a              Patch StartAllBackX64.dll");
            Console.WriteLine("  --r [backup.bak] Restore from backup (auto-detect if omitted)");
            return 0;
        }

        private static int Activate()
        {
            var log     = new ConsoleLog();
            var patcher = new Patcher(log);

            patcher.ResetTrialReminder();
            patcher.Checkup();

            if (!patcher.CheckupIsValid)
            {
                Console.Error.WriteLine("Checkup failed — aborting.");
                return 1;
            }

            patcher.Patch(doBackup: true);
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

        private static int Remove(string backupPath)
        {
            var log     = new ConsoleLog();
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
                    Console.Error.WriteLine("No backup (.bak) found next to the DLL, next to this EXE, or in the current directory.");
                    Console.Error.WriteLine("Either run --activate-patch first (it auto-creates a backup), or supply the path:");
                    Console.Error.WriteLine("  --remove-patch \"C:\\Program Files\\StartAllBack\\StartAllBackX64.dll.YYYY-MM-DD_HH-MM-SS.bak\"");
                    return 1;
                }
                log.Info("Auto-detected backup: ");
                log.Done(latest);
                patcher.SetBackupPath(latest);
            }

            patcher.Restore();
            return 0;
        }
    }
}
