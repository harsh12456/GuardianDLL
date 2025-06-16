using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace GuardianDLL.pages
{
    public class SafeDllBackupManager
    {
        private readonly string _backupDir;
        private readonly string _metadataFile;
        private Dictionary<string, string> _dllBackups;

        public SafeDllBackupManager()
        {
            _backupDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GuardianDLL", "SafeDllBackups"
            );
            _metadataFile = Path.Combine(_backupDir, "backups.json");
            LoadBackupMetadata();
        }

        private void LoadBackupMetadata()
        {
            if (File.Exists(_metadataFile))
            {
                try
                {
                    var json = File.ReadAllText(_metadataFile);
                    _dllBackups = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                                  ?? new Dictionary<string, string>();
                }
                catch (Exception ex) // Catch both I/O and JSON-related exceptions
                {
                    Console.WriteLine($"Error loading backup metadata: {ex.Message}");
                    _dllBackups = new Dictionary<string, string>();
                }
            }
            else
            {
                _dllBackups = new Dictionary<string, string>();
            }
        }

        private void SaveBackupMetadata()
        {
            var json = JsonSerializer.Serialize(_dllBackups, new JsonSerializerOptions { WriteIndented = true });
            Directory.CreateDirectory(_backupDir);
            File.WriteAllText(_metadataFile, json);
        }

        private string GetBackupFileName(string dllPath)
        {
            // Use SHA256 hash of the full path for uniqueness
            using var sha = System.Security.Cryptography.SHA256.Create();
            var hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(dllPath));
            return BitConverter.ToString(hash).Replace("-", "").ToLower() + ".bak";
        }

        public bool HasBackup(string dllPath)
        {
            return _dllBackups.ContainsKey(dllPath) && File.Exists(Path.Combine(_backupDir, _dllBackups[dllPath]));
        }

        public void BackupDll(string dllPath)
        {
            if (!File.Exists(dllPath))
                throw new FileNotFoundException("DLL not found", dllPath);

            Directory.CreateDirectory(_backupDir);
            string backupName = GetBackupFileName(dllPath);
            string backupPath = Path.Combine(_backupDir, backupName);

            File.Copy(dllPath, backupPath, overwrite: true);
            _dllBackups[dllPath] = backupName;
            SaveBackupMetadata();
        }

        public bool RestoreDll(string dllPath)
        {
            if (_dllBackups.TryGetValue(dllPath, out var backupName))
            {
                string backupPath = Path.Combine(_backupDir, backupName);
                if (File.Exists(backupPath))
                {
                    File.Copy(backupPath, dllPath, overwrite: true);
                    return true;
                }
            }
            return false;
        }

        public void RemoveBackup(string dllPath)
        {
            if (_dllBackups.TryGetValue(dllPath, out var backupName))
            {
                string backupPath = Path.Combine(_backupDir, backupName);
                if (File.Exists(backupPath))
                    File.Delete(backupPath);
                _dllBackups.Remove(dllPath);
                SaveBackupMetadata();
            }
        }
    }
}