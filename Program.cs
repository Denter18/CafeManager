using System;
using System.IO;
using System.Windows.Forms;

namespace CafeManager
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            // Создание резервной копии при запуске
            CreateBackup();
            
            // Инициализация БД
            DBHelper.InitializeDatabase();
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // Запуск авторизации
            LoginForm login = new LoginForm();
            if (login.ShowDialog() == DialogResult.OK)
            {
                Application.Run(new MainForm(login.CurrentUser, login.CurrentRole));
            }
        }

        private static void CreateBackup()
        {
            try
            {
                string backupDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
                    "CafeBackups");
                
                Directory.CreateDirectory(backupDir);
                string backupFile = Path.Combine(
                    backupDir, 
                    $"cafedb_backup_{DateTime.Now:yyyyMMdd_HHmmss}.sqlite");
                
                DBHelper.BackupDatabase(backupFile);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось создать резервную копию: {ex.Message}", "Ошибка");
            }
        }
    }
}