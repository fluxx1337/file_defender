using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using MahApps.Metro.Controls;
using Newtonsoft.Json;
using System.Windows.Forms;
using System.Windows.Data;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog; 

namespace FileDefender
{
    public partial class MainWindow : MetroWindow
    {
        private List<FileInfo> protectedFiles = new List<FileInfo>();
        private string password = "";
        private string configPath = "config.enc";
        private readonly string staticSecretKey = "secretkey111"; 

        public class FileInfo
        {
            public string Path { get; set; }
            public bool IsEncrypted { get; set; }
            public string FileName => System.IO.Path.GetFileName(Path); 
        }

        public MainWindow()
        {
            InitializeComponent();
            LoadConfig();
        }

        private void AddFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                if (protectedFiles.Exists(f => f.Path == filePath))
                {
                    LogTextBox.AppendText($"[{DateTime.Now}] Файл {Path.GetFileName(filePath)} уже добавлен\n");
                    return;
                }

                try
                {
                    var fileInfo = new FileInfo { Path = filePath, IsEncrypted = true };
                    protectedFiles.Add(fileInfo);
                    EncryptFile(filePath);
                    UpdateFileList();
                    SaveConfig();
                    LogTextBox.AppendText($"[{DateTime.Now}] Файл {Path.GetFileName(filePath)} успешно добавлен и зашифрован\n");
                }
                catch (Exception ex)
                {
                    LogTextBox.AppendText($"[{DateTime.Now}] Ошибка при добавлении файла {Path.GetFileName(filePath)}: {ex.Message}\n");
                }
            }
        }

        private void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string folderPath = folderDialog.SelectedPath;
                    string[] files = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly);
                    foreach (string filePath in files)
                    {
                        if (protectedFiles.Exists(f => f.Path == filePath))
                        {
                            LogTextBox.AppendText($"[{DateTime.Now}] Файл {Path.GetFileName(filePath)} уже добавлен, пропуск\n");
                            continue;
                        }

                        try
                        {
                            var fileInfo = new FileInfo { Path = filePath, IsEncrypted = true };
                            protectedFiles.Add(fileInfo);
                            EncryptFile(filePath);
                            LogTextBox.AppendText($"[{DateTime.Now}] Файл {Path.GetFileName(filePath)} из папки успешно зашифрован\n");
                        }
                        catch (Exception ex)
                        {
                            LogTextBox.AppendText($"[{DateTime.Now}] Ошибка при шифровании файла {Path.GetFileName(filePath)} из папки: {ex.Message}\n");
                        }
                    }
                    UpdateFileList();
                    SaveConfig();
                    LogTextBox.AppendText($"[{DateTime.Now}] Все файлы из папки {folderPath} обработаны\n");
                }
            }
        }

        private void RemoveFile_Click(object sender, RoutedEventArgs e)
        {
            if (FileList.SelectedIndex >= 0)
            {
                try
                {
                    string filePath = protectedFiles[FileList.SelectedIndex].Path;
                    string fileName = Path.GetFileName(filePath);
                    if (protectedFiles[FileList.SelectedIndex].IsEncrypted)
                    {
                        DecryptFile(filePath);
                        LogTextBox.AppendText($"[{DateTime.Now}] Файл {fileName} расшифрован перед удалением\n");
                    }
                    protectedFiles.RemoveAt(FileList.SelectedIndex);
                    UpdateFileList();
                    SaveConfig();
                    LogTextBox.AppendText($"[{DateTime.Now}] Файл {fileName} успешно удален из списка\n");
                }
                catch (Exception ex)
                {
                    LogTextBox.AppendText($"[{DateTime.Now}] Ошибка при удалении файла: {ex.Message}\n");
                }
            }
        }

        private void Encrypt_Click(object sender, RoutedEventArgs e)
        {
            if (FileList.SelectedIndex >= 0)
            {
                string filePath = protectedFiles[FileList.SelectedIndex].Path;
                if (protectedFiles[FileList.SelectedIndex].IsEncrypted)
                {
                    LogTextBox.AppendText($"[{DateTime.Now}] Файл {Path.GetFileName(filePath)} уже зашифрован\n");
                    return;
                }

                try
                {
                    EncryptFile(filePath);
                    protectedFiles[FileList.SelectedIndex].IsEncrypted = true;
                    UpdateFileList();
                    LogTextBox.AppendText($"[{DateTime.Now}] Файл {Path.GetFileName(filePath)} успешно зашифрован\n");
                }
                catch (Exception ex)
                {
                    LogTextBox.AppendText($"[{DateTime.Now}] Ошибка шифрования файла {Path.GetFileName(filePath)}: {ex.Message}\n");
                }
            }
        }

        private void Decrypt_Click(object sender, RoutedEventArgs e)
        {
            if (FileList.SelectedIndex >= 0)
            {
                string filePath = protectedFiles[FileList.SelectedIndex].Path;
                if (!protectedFiles[FileList.SelectedIndex].IsEncrypted)
                {
                    LogTextBox.AppendText($"[{DateTime.Now}] Файл {Path.GetFileName(filePath)} не зашифрован\n");
                    return;
                }

                try
                {
                    DecryptFile(filePath);
                    protectedFiles[FileList.SelectedIndex].IsEncrypted = false;
                    UpdateFileList();
                    LogTextBox.AppendText($"[{DateTime.Now}] Файл {Path.GetFileName(filePath)} успешно расшифрован\n");
                }
                catch (Exception ex)
                {
                    LogTextBox.AppendText($"[{DateTime.Now}] Ошибка расшифровки файла {Path.GetFileName(filePath)}: {ex.Message}\n");
                }
            }
        }

        private bool GetPassword()
        {
            PasswordDialog dialog = new PasswordDialog();
            if (dialog.ShowDialog() == true)
            {
                password = dialog.Password;
                SaveConfig();
                LogTextBox.AppendText($"[{DateTime.Now}] Пароль успешно установлен\n");
                return true;
            }
            else
            {
                LogTextBox.AppendText($"[{DateTime.Now}] Ввод пароля отменен, диалог закрыт\n");
                if (password == null || password.Length == 0)
                {
                    password = "exclusivePassword";
                    LogTextBox.AppendText($"[{DateTime.Now}] Ввод пароля отменен, установлен пароль по-умолчанию");
                }
                return false;
            }
        }

        private void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            GetPassword();
        }

        private void UpdateFileList()
        {
            FileList.ItemsSource = null; 
            FileList.ItemsSource = protectedFiles; 
        }

        private void SaveConfig()
        {
            try
            {
                var config = new { Password = password, Files = protectedFiles };
                string json = JsonConvert.SerializeObject(config);
                byte[] encryptedData = EncryptConfig(Encoding.UTF8.GetBytes(json));
                File.WriteAllBytes(configPath, encryptedData);
                LogTextBox.AppendText($"[{DateTime.Now}] Конфигурация успешно сохранена\n");
            }
            catch (Exception ex)
            {
                LogTextBox.AppendText($"[{DateTime.Now}] Ошибка сохранения конфига: {ex.Message}\n");
            }
        }

        private void LoadConfig()
        {
            this.Show();
            if (File.Exists(configPath))
            {
                try
                {
                    byte[] encryptedData = File.ReadAllBytes(configPath);
                    byte[] decryptedData = DecryptConfig(encryptedData);
                    string json = Encoding.UTF8.GetString(decryptedData);
                    var config = JsonConvert.DeserializeObject<dynamic>(json);
                    password = config.Password;
                    protectedFiles = config.Files.ToObject<List<FileInfo>>();
                    UpdateFileList();
                    LogTextBox.AppendText($"[{DateTime.Now}] Конфигурация успешно загружена\n");
                }
                catch (Exception ex)
                {
                    LogTextBox.AppendText($"[{DateTime.Now}] Ошибка загрузки конфига: {ex.Message}\n");
                    File.Delete(configPath);
                    if (GetPassword())
                    {
                        protectedFiles.Clear();
                        UpdateFileList();
                    }
                }
            }
            else
            {
                GetPassword();
            }
        }

        private void EncryptFile(string filePath)
        {
            byte[] fileData = File.ReadAllBytes(filePath);
            File.WriteAllBytes(filePath, EncryptData(fileData));
        }

        private void DecryptFile(string filePath)
        {
            byte[] fileData = File.ReadAllBytes(filePath);
            File.WriteAllBytes(filePath, DecryptData(fileData));
        }

        private byte[] EncryptData(byte[] data)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = GetKey(password);
                aes.IV = new byte[16];
                using (MemoryStream ms = new MemoryStream())
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                    cs.FlushFinalBlock();
                    return ms.ToArray();
                }
            }
        }

        private byte[] DecryptData(byte[] data)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = GetKey(password);
                aes.IV = new byte[16];
                using (MemoryStream ms = new MemoryStream())
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                    cs.FlushFinalBlock();
                    return ms.ToArray();
                }
            }
        }

        private byte[] EncryptConfig(byte[] data)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = GetKey(staticSecretKey);
                aes.IV = new byte[16];
                using (MemoryStream ms = new MemoryStream())
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                    cs.FlushFinalBlock();
                    return ms.ToArray();
                }
            }
        }

        private byte[] DecryptConfig(byte[] data)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = GetKey(staticSecretKey);
                aes.IV = new byte[16];
                using (MemoryStream ms = new MemoryStream())
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                    cs.FlushFinalBlock();
                    return ms.ToArray();
                }
            }
        }

        private byte[] GetKey(string key)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
            }
        }
    }

    // Конвертер для отображения статуса (✓ или ✗)
    [ValueConversion(typeof(bool), typeof(string))]
    public class BoolToCheckConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (bool)value ? "✓" : "✗";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}