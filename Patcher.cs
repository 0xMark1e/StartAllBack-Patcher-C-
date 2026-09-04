using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace SynezSAB
{
    internal class FileInfo
    {
        public bool IsOriginal { get; set; }
        public bool IsPatched  { get; set; }
        public string Version      { get; set; } = "";
        public string OriginalHash { get; set; } = "";
        public string PatchedHash  { get; set; } = "";
        public int CheckLicenseOffset    { get; set; } = -2;
        public int CompareFileTimeOffset { get; set; } = -2;

        public bool HasFuncsOffset =>
            CheckLicenseOffset >= 0 && CompareFileTimeOffset >= 0;
    }

    internal class Patcher
    {
        private string _filePath = Data.DllPath;
        private string _backupFilePath;
        private readonly ConsoleLog _log;

        public bool CheckupIsValid { get; private set; }
        public FileInfo CheckResult { get; private set; } = new FileInfo();

        [DllImport("kernel32.dll")]
        private static extern uint GetSystemFirmwareTable(uint sig, uint id, IntPtr buf, uint size);
        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalAlloc(uint flags, UIntPtr bytes);

        private const uint RSMB = 0x52534D42;

        public Patcher(ConsoleLog log) { _log = log; }

        public void SetBackupPath(string path) => _backupFilePath = path;

        public string FindLatestBackup()
        {
            string dllName = Path.GetFileName(_filePath);

            // match both "StartAllBackX64.dll.bak" (Python patcher) and "StartAllBackX64.dll.*.bak" (timestamped)
            string[] patterns = { dllName + ".bak", dllName + ".*.bak" };

            // search: DLL dir, exe dir, current dir
            var searchDirs = new[]
            {
                Path.GetDirectoryName(_filePath),
                AppContext.BaseDirectory,
                Directory.GetCurrentDirectory(),
            };

            string best = null;
            foreach (string dir in searchDirs)
            {
                if (dir == null) continue;
                foreach (string pattern in patterns)
                {
                    try
                    {
                        string[] files = Directory.GetFiles(dir, pattern);
                        if (files.Length == 0) continue;
                        Array.Sort(files);
                        string candidate = files[files.Length - 1];
                        if (best == null || string.Compare(candidate, best, StringComparison.Ordinal) > 0)
                            best = candidate;
                    }
                    catch { }
                }
            }
            return best;
        }

        public void Checkup()
        {
            CheckupIsValid = false;
            _log.Banner("Checkup");
            _log.Info($"Searching for {Path.GetFileName(_filePath)}... ");

            if (!File.Exists(_filePath))
            {
                _log.Error($"Not found at {_filePath}");
                return;
            }

            _log.Done("Found");
            _log.Info("Checking hash... ");
            CheckResult = GetFileInfo(_filePath, printHash: true);

            if (CheckResult.IsOriginal)
            {
                _log.Warning("Not patched");
                CheckupIsValid = true;
            }
            else if (CheckResult.IsPatched)
            {
                _log.Done("Already patched");
            }
            else
            {
                _log.Warning("Not matching");
                _log.Banner("Check with functions footprint");

                byte[] content = ReadFile(_filePath);

                _log.Info("Checking CheckLicense... ");
                int clOff = IndexOf(content, Data.FootprintCheckLicense);
                CheckResult.CheckLicenseOffset = clOff;
                if (clOff == -1)
                {
                    int already = IndexOf(content, Data.PatchedFootprintCheckLicense);
                    _log.Done(already >= 0 ? "Patched" : "Not found");
                }
                else _log.Warning($"Found at 0x{clOff:X}");

                _log.Info("Checking CompareFileTime... ");
                int cftOff = IndexOf(content, Data.FootprintCompareFileTime);
                CheckResult.CompareFileTimeOffset = cftOff;
                if (cftOff == -1)
                {
                    int already = IndexOf(content, Data.PatchedFootprintCompareFileTime);
                    _log.Done(already >= 0 ? "Patched" : "Not found");
                }
                else _log.Warning($"Found at 0x{cftOff:X}");

                CheckupIsValid = CheckResult.HasFuncsOffset;
                if (CheckupIsValid) _log.Done("All functions found");
            }
        }

        public void Patch(bool doBackup, bool killExplorer = false)
        {
            _log.Banner("Patching");
            if (!CheckupIsValid) { _log.Error("Checkup not valid"); return; }

            if (doBackup) CreateBackup();

            byte[] data = ReadFile(_filePath);
            if (killExplorer) KillDllUsers();
            _log.Info("Patching... ");

            try
            {
                if (CheckResult.IsOriginal)
                {
                    foreach (var entry in Data.Repository)
                    {
                        if (entry.Version != CheckResult.Version) continue;
                        foreach (var patch in entry.Patches)
                            ApplyPatch(ref data, patch.Offset, patch.Bytes);
                        break;
                    }
                }
                else
                {
                    ApplyPatch(ref data, CheckResult.CheckLicenseOffset,    Data.PatchedFootprintCheckLicense);
                    ApplyPatch(ref data, CheckResult.CompareFileTimeOffset, Data.PatchedFootprintCompareFileTime);
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Error: {ex.Message}");
                if (doBackup) Restore(killExplorer);
                if (killExplorer) StartExplorer();
                return;
            }

            WriteFile(_filePath, data, killExplorer);

            if (CheckResult.IsOriginal)
            {
                _log.Info("Verifying hash... ");
                string newHash = ComputeHash(_filePath);
                if (newHash == CheckResult.PatchedHash) _log.Done("OK");
                else { _log.Error("Hash mismatch"); if (doBackup) Restore(killExplorer); }
            }

            if (killExplorer) StartExplorer();
        }

        public void Restore(bool killExplorer = false)
        {
            _log.Banner("Restore backup");
            if (_backupFilePath == null)
            {
                _log.Error("No backup path set.");
                return;
            }
            if (killExplorer) KillDllUsers();
            _log.Info("Restoring... ");
            WriteFile(_filePath, ReadFile(_backupFilePath), killExplorer);
            if (killExplorer) StartExplorer();
        }

        public void ResetTrialReminder()
        {
            _log.Banner("Reset trial reminder");
            _log.Info("Deleting registry keys... ");
            string keyHead = GetFirmwareKeyHead();
            string pattern = @"\{" + Regex.Escape(keyHead) + @"\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{11}\}";

            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(Data.TrialReminderKey);
                if (key == null) { _log.Warning("Key not found"); return; }
                bool deleted = false;
                foreach (string name in key.GetSubKeyNames())
                {
                    if (Regex.IsMatch(name, pattern))
                    {
                        Registry.CurrentUser.DeleteSubKeyTree(Data.TrialReminderKey + "\\" + name, false);
                        deleted = true;
                    }
                }
                _log.Done(deleted ? "Done" : "Nothing to remove");
            }
            catch (Exception ex) { _log.Error(ex.Message); }
        }

        private string GetFirmwareKeyHead()
        {
            try
            {
                uint size = GetSystemFirmwareTable(RSMB, 0, IntPtr.Zero, 0);
                if (size == 0) return "00000000-0000";
                IntPtr buf = GlobalAlloc(0x40, new UIntPtr(size + 8));
                GetSystemFirmwareTable(RSMB, 0, buf, size);
                int index = 0;
                while (index + 1 < size)
                {
                    int cur = index;
                    byte entry = Marshal.ReadByte(buf, 8 + cur);
                    if (entry == 0x01)
                    {
                        if (cur != 0)
                        {
                            ulong val    = (ulong)Marshal.ReadInt64(buf, cur + 8 + 8);
                            uint first   = (uint)(val & 0xFFFFFFFF);
                            uint second  = (uint)((val >> 0x30) & 0xFFFF);
                            return $"{first:x8}-{second:x4}";
                        }
                        break;
                    }
                    byte bLen = Marshal.ReadByte(buf, 8 + cur + 1);
                    if (bLen == 0) break;
                    int upd = cur + bLen;
                    while (true)
                    {
                        int strLen = 0;
                        while (Marshal.ReadByte(buf, 8 + upd + strLen) != 0) strLen++;
                        if (strLen == 0) break;
                        upd += strLen + 1;
                    }
                    index = upd + 1;
                }
            }
            catch { }
            return "00000000-0000";
        }

        private FileInfo GetFileInfo(string path, bool printHash)
        {
            string hash = ComputeHash(path);
            if (printHash) _log.Colored($"{hash} ", "#2682cb");
            var result = new FileInfo();
            foreach (var entry in Data.Repository)
            {
                if (hash == entry.OriginalHash)
                {
                    result.IsOriginal  = true;
                    result.Version     = entry.Version;
                    result.OriginalHash = entry.OriginalHash;
                    result.PatchedHash  = entry.PatchedHash;
                    break;
                }
                if (hash == entry.PatchedHash)
                {
                    result.IsPatched   = true;
                    result.Version     = entry.Version;
                    result.OriginalHash = entry.OriginalHash;
                    result.PatchedHash  = entry.PatchedHash;
                    break;
                }
            }
            return result;
        }

        private static string ComputeHash(string path)
        {
            byte[] bytes = SHA1.HashData(File.ReadAllBytes(path));
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }

        private static void ApplyPatch(ref byte[] data, int offset, byte[] patch)
        {
            Buffer.BlockCopy(patch, 0, data, offset, patch.Length);
        }

        private static int IndexOf(byte[] haystack, byte[] needle)
        {
            for (int i = 0; i <= haystack.Length - needle.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < needle.Length; j++)
                    if (haystack[i + j] != needle[j]) { match = false; break; }
                if (match) return i;
            }
            return -1;
        }

        private void CreateBackup()
        {
            _log.Info("Creating backup... ");
            _backupFilePath = _filePath + $".{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.bak";
            WriteFile(_backupFilePath, ReadFile(_filePath));
        }

        private void KillDllUsers()
        {
            string dllName = Path.GetFileName(_filePath);
            _log.Info($"Scanning for processes using {dllName}... ");

            var toKill = new System.Collections.Generic.List<Process>();
            foreach (var proc in Process.GetProcesses())
            {
                try
                {
                    foreach (ProcessModule mod in proc.Modules)
                    {
                        if (Path.GetFileName(mod.FileName).Equals(dllName, StringComparison.OrdinalIgnoreCase))
                        {
                            toKill.Add(proc);
                            break;
                        }
                    }
                }
                catch { }
            }

            if (toKill.Count == 0) { _log.Warning("None found"); return; }

            _log.Done($"Found {toKill.Count}");
            foreach (var proc in toKill)
            {
                _log.Info($"  Killing {proc.ProcessName} (pid {proc.Id})... ");
                try { proc.Kill(); _log.Done("OK"); }
                catch (Exception ex) { _log.Error(ex.Message); }
            }
            // No WaitForExit — proceed immediately so we write before any
            // auto-restarted process (e.g. explorer) can reload the DLL.
        }

        private static void StartExplorer()
        {
            Process.Start(new ProcessStartInfo("explorer.exe") { UseShellExecute = true });
        }

        private byte[] ReadFile(string path)
        {
            try { return File.ReadAllBytes(path); }
            catch (Exception ex) { _log.Error($"Read error: {ex.Message}"); throw; }
        }

        private void WriteFile(string path, byte[] data, bool killOnConflict = false)
        {
            try
            {
                if (File.Exists(path))
                {
                    var attr = File.GetAttributes(path);
                    if ((attr & FileAttributes.ReadOnly) != 0)
                        File.SetAttributes(path, attr & ~FileAttributes.ReadOnly);
                }
            }
            catch { }

            string dllName = Path.GetFileName(path);

            for (int attempt = 0; attempt < 15; attempt++)
            {
                try
                {
                    using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                    fs.Write(data, 0, data.Length);
                    _log.Done("Done");
                    return;
                }
                catch (IOException)
                {
                    if (!killOnConflict)
                    {
                        _log.Error("Write failed — file is locked. Add --explorer to kill processes holding the DLL.");
                        return;
                    }
                    foreach (var proc in Process.GetProcesses())
                    {
                        try
                        {
                            foreach (ProcessModule mod in proc.Modules)
                            {
                                if (Path.GetFileName(mod.FileName).Equals(dllName, StringComparison.OrdinalIgnoreCase))
                                {
                                    try { proc.Kill(); } catch { }
                                    break;
                                }
                            }
                        }
                        catch { }
                    }
                    System.Threading.Thread.Sleep(200);
                }
                catch (Exception ex)
                {
                    _log.Error($"Write error: {ex.Message}");
                    return;
                }
            }
            _log.Error("Write failed — file still locked after 15 attempts.");
        }
    }
}
