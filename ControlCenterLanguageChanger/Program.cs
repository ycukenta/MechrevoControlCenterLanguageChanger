using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text.Json;
using System.Threading;

namespace ControlCenterPatcher
{
    class Program
    {
        static string backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
        static string translationsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Translations");
        static string localesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Locales");
        
        static Dictionary<string, string> L = new Dictionary<string, string>();
        
        static string T(string key) => L.ContainsKey(key) ? L[key] : $"[{key}]";

        static void Main(string[] args)
        {
            Console.Title = "Control Center Universal Patcher";

            if (!IsAdministrator())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[ERROR] Please run as Administrator! / Запустите от имени Администратора!");
                Console.ReadLine();
                return;
            }
            
            if (!SelectUILanguage()) return;
            
            while (true)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("==================================================");
                Console.WriteLine($"    {T("Title")}");
                Console.WriteLine("==================================================\n");
                Console.ResetColor();

                Console.WriteLine(T("MenuInstall"));
                Console.WriteLine(T("MenuRestore"));
                Console.WriteLine(T("MenuExit") + "\n");
                Console.Write(T("PromptAction"));

                string choice = Console.ReadLine();
                if (choice == "1") InstallTranslation();
                else if (choice == "2") RestoreBackup();
                else if (choice == "3") return;
            }
        }

        static bool SelectUILanguage()
        {
            if (!Directory.Exists(localesDir)) Directory.CreateDirectory(localesDir);
            var localeFiles = Directory.GetFiles(localesDir, "*.json");

            if (localeFiles.Length == 0)
            {
                Console.WriteLine("No localization files found in /Locales/. Please add UI language JSON files.");
                Console.ReadLine();
                return false;
            }

            Console.WriteLine("Choose utility language / Выберите язык утилиты:");
            for (int i = 0; i < localeFiles.Length; i++)
            {
                Console.WriteLine($"{i + 1}. {Path.GetFileNameWithoutExtension(localeFiles[i])}");
            }

            Console.Write("\nSelect / Выбор: ");

            if (!int.TryParse(Console.ReadLine(), out int langIndex) || langIndex < 1 || langIndex > localeFiles.Length)
            {
                Console.WriteLine("Invalid choice / Неверный выбор.");
                Console.ReadLine();
                return false;
            }

            try
            {
                string jsonString = File.ReadAllText(localeFiles[langIndex - 1]);
                L = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading language file: {ex.Message}");
                Console.ReadLine();
                return false;
            }
        }

        static void InstallTranslation()
        {
            Console.Clear();
            string targetDir = FindWindowsAppsFolder();
            if (targetDir == null) return;

            Console.Write($"\n{T("PromptTargetLang")}");
            string targetLangCode = Console.ReadLine()?.Trim().ToLower();
            if (string.IsNullOrEmpty(targetLangCode)) return;

            if (!Directory.Exists(translationsDir)) Directory.CreateDirectory(translationsDir);
            var availableTranslations = Directory.GetDirectories(translationsDir);

            if (availableTranslations.Length == 0)
            {
                PrintError(T("ErrorNoTranslations"));
                Pause();
                return;
            }

            Console.WriteLine($"\n{T("AvailableTrans")}");
            for (int i = 0; i < availableTranslations.Length; i++)
            {
                Console.WriteLine($"{i + 1}. {Path.GetFileName(availableTranslations[i])}");
            }

            Console.Write($"\n{T("PromptSelectTrans")}");
            if (!int.TryParse(Console.ReadLine(), out int transIndex) || transIndex < 1 ||
                transIndex > availableTranslations.Length)
            {
                PrintError(T("InvalidChoice"));
                Pause();
                return;
            }

            string selectedTranslationPath = availableTranslations[transIndex - 1];
            string newJsonPath = Directory.GetFiles(selectedTranslationPath, "*.json").FirstOrDefault();
            string newResxPath = Directory.GetFiles(selectedTranslationPath, "*.resx").FirstOrDefault();

            if (newJsonPath == null || newResxPath == null)
            {
                PrintError(T("FileMissing"));
                Pause();
                return;
            }
            
            string jsonFolder = Directory.Exists(Path.Combine(targetDir, "Strings_Json")) ? "Strings_Json" : "languages";

            string sysJsonPath = Path.Combine(targetDir, jsonFolder, $"{targetLangCode}.json");
            string sysResxPath = Path.Combine(targetDir, "Win32", "Resources_String", $"{targetLangCode}.resx");

            string etalonJson = Path.Combine(targetDir, jsonFolder, "en-US.json");
            string etalonResx = Path.Combine(targetDir, "Win32", "Resources_String", "en-US.resx");

            Console.WriteLine($"\n[*] {T("CheckCompat")}");
            bool isMismatch = false;
            
            if (File.Exists(etalonJson))
            {
                int etalonKeys = File.ReadAllLines(etalonJson).Count(l => l.Contains("\":"));
                int newKeys = File.ReadAllLines(newJsonPath).Count(l => l.Contains("\":"));
                
                if (Math.Abs(etalonKeys - newKeys) > 5)
                {
                    PrintWarning($"{T("WarningMismatch")} (JSON Etalon Keys: {etalonKeys}, New: {newKeys})");
                    isMismatch = true;
                }
            }
            
            if (File.Exists(etalonResx))
            {
                int etalonKeys = File.ReadAllLines(etalonResx).Count(l => l.Contains("<data name="));
                int newKeys = File.ReadAllLines(newResxPath).Count(l => l.Contains("<data name="));
                
                if (Math.Abs(etalonKeys - newKeys) > 5)
                {
                    PrintWarning($"{T("WarningMismatch")} (RESX Etalon Keys: {etalonKeys}, New: {newKeys})");
                    isMismatch = true;
                }
            }

            if (isMismatch)
            {
                Console.Write($"{T("WarningProceed")}");
                if (Console.ReadLine()?.ToLower() != "y") return;
            }

            Console.WriteLine($"\n[*] {T("StoppingServices")}");
            KillProcesses();
            GrantAccessToFolder(targetDir);

            if (!Directory.Exists(backupDir)) Directory.CreateDirectory(backupDir);

            if (File.Exists(sysJsonPath))
            {
                string backupJson = Path.Combine(backupDir, $"{targetLangCode}_backup.json");
                if (!File.Exists(backupJson)) File.Copy(sysJsonPath, backupJson);
            }

            if (File.Exists(sysResxPath))
            {
                string backupResx = Path.Combine(backupDir, $"{targetLangCode}_backup.resx");
                if (!File.Exists(backupResx)) File.Copy(sysResxPath, backupResx);
            }

            Console.WriteLine($"[*] {T("BackupSuccess")}");

            try
            {
                File.Copy(newJsonPath, sysJsonPath, true);
                File.Copy(newResxPath, sysResxPath, true);
                RestoreAccessToFolder(targetDir);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n[OK] {T("SuccessInstall")}");
            }
            catch (Exception ex)
            {
                PrintError($"{T("ErrorCopy")} {ex.Message}");
            }

            Pause();
        }

        static void RestoreBackup()
        {
            Console.Clear();
            if (!Directory.Exists(backupDir) || Directory.GetFiles(backupDir).Length == 0)
            {
                PrintError(T("EmptyBackup"));
                Pause();
                return;
            }

            string targetDir = FindWindowsAppsFolder();
            if (targetDir == null) return;

            Console.WriteLine($"[*] {T("StoppingServices")}");
            KillProcesses();
            GrantAccessToFolder(targetDir);
            
            string jsonFolder = Directory.Exists(Path.Combine(targetDir, "Strings_Json")) ? "Strings_Json" : "languages";

            var backups = Directory.GetFiles(backupDir);
            foreach (var backup in backups)
            {
                string fileName = Path.GetFileName(backup).Replace("_backup", "");
                
                string destPath = fileName.EndsWith(".json")
                    ? Path.Combine(targetDir, jsonFolder, fileName)
                    : Path.Combine(targetDir, "Win32", "Resources_String", fileName);

                try
                {
                    File.Copy(backup, destPath, true);
                }
                catch
                {
                }
            }
            RestoreAccessToFolder(targetDir);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n[OK] {T("SuccessRestore")}");
            Pause();
        }

        static void PrintError(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] {msg}");
            Console.ResetColor();
        }

        static void PrintWarning(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(msg);
            Console.ResetColor();
        }

        static void Pause()
        {
            Console.ResetColor();
            Console.WriteLine($"\n{T("PressAnyKey")}");
            Console.ReadKey();
        }

        static string FindWindowsAppsFolder()
        {
            string windowsAppsPath = @"C:\Program Files\WindowsApps";
            try
            {
                var dirs = Directory.GetDirectories(windowsAppsPath);
                
                var targetDir = dirs.FirstOrDefault(d => 
                {
                    string folderName = Path.GetFileName(d);
                    bool isTargetApp = folderName.StartsWith("CCU.WinUI_") || folderName.StartsWith("ControlCenter3_");
                    bool isNotJunk = !folderName.Contains("neutral") && !folderName.Contains("split") && !folderName.Contains("~");

                    return isTargetApp && isNotJunk;
                });

                if (targetDir == null)
                {
                    PrintError("Главная папка программы (CCU.WinUI / ControlCenter3) не найдена в WindowsApps!");
                    Pause();
                    return null;
                }

                return targetDir;
            }
            catch (UnauthorizedAccessException)
            {
                PrintError("Нет доступа к WindowsApps. Программа не смогла получить права.");
                Pause();
                return null;
            }
        }

        static void GrantAccessToFolder(string folderPath)
        {
            Console.WriteLine("[*] Получение прав на системную директорию...");
            RunHiddenCommand($"takeown /F \"{folderPath}\" /A /R /D Y");
            RunHiddenCommand($"icacls \"{folderPath}\" /grant Администраторы:F /T /C /Q");
            RunHiddenCommand($"icacls \"{folderPath}\" /grant Administrators:F /T /C /Q");
        }

        static void RunHiddenCommand(string arguments)
        {
            var pInfo = new ProcessStartInfo("cmd.exe", "/c " + arguments)
            {
                CreateNoWindow = true, UseShellExecute = false
            };
            using (var process = Process.Start(pInfo))
            {
                process.WaitForExit();
            }
        }

        static void KillProcesses()
        {
            string[] targets = { "ControlCenter", "SystrayComponent", "UniwillService" };
            foreach (var t in targets)
            {
                foreach (var p in Process.GetProcessesByName(t))
                {
                    try
                    {
                        p.Kill();
                    }
                    catch
                    {
                    }
                }
            }

            Thread.Sleep(1500);
        }

        static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        static void RestoreAccessToFolder(string folderPath)
        {
            Console.WriteLine($"[*] {T("RestoringPermissions")}");
            RunHiddenCommand($"icacls \"{folderPath}\" /setowner \"NT SERVICE\\TrustedInstaller\" /T /C /Q");
            
            RunHiddenCommand($"icacls \"{folderPath}\" /remove Администраторы /T /C /Q");
            RunHiddenCommand($"icacls \"{folderPath}\" /remove Administrators /T /C /Q");
        }
    }
}