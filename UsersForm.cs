using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;

namespace CafeManager
{
    public partial class UsersForm : Form
    {
        private DataGridView usersGrid;
        private Button addBtn, saveBtn, refreshBtn;

        public UsersForm()
        {
            InitializeComponent();
            LoadUsers();
        }

        private void InitializeComponent()
        {
            this.Text = "Управление пользователями";
            this.Size = new Size(600, 400);
            
            // Панель для кнопок
            var buttonPanel = new Panel 
            { 
                Dock = DockStyle.Bottom, 
                Height = 50 
            };
            
            usersGrid = new DataGridView 
            { 
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = true,
                AllowUserToDeleteRows = true
            };
            
            addBtn = new Button { Text = "Добавить", Width = 100, Top = 10, Left = 20 };
            saveBtn = new Button { Text = "Сохранить", Width = 100, Top = 10, Left = 130 };
            refreshBtn = new Button { Text = "Обновить", Width = 100, Top = 10, Left = 240 };
            
            addBtn.Click += (s, e) => 
            {
                DataTable dt = usersGrid.DataSource as DataTable;
                dt.Rows.Add(dt.NewRow());
            };
            
            saveBtn.Click += SaveUsers;
            refreshBtn.Click += (s, e) => LoadUsers();
            
            buttonPanel.Controls.Add(addBtn);
            buttonPanel.Controls.Add(saveBtn);
            buttonPanel.Controls.Add(refreshBtn);
            
            this.Controls.Add(usersGrid);
            this.Controls.Add(buttonPanel);
        }

        private void LoadUsers()
        {
            using (var conn = new SQLiteConnection(DBHelper.ConnectionString))
            {
                conn.Open();
                SQLiteDataAdapter adapter = new SQLiteDataAdapter(
                    "SELECT Login AS 'Логин', Role AS 'Роль' FROM Users", 
                    conn);
                
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                usersGrid.DataSource = dt;
            }
        }

        private void SaveUsers(object sender, EventArgs e)
        {
            try
            {
                using (var conn = new SQLiteConnection(DBHelper.ConnectionString))
                {
                    conn.Open();
                    SQLiteDataAdapter adapter = new SQLiteDataAdapter("SELECT * FROM Users", conn);
                    SQLiteCommandBuilder builder = new SQLiteCommandBuilder(adapter);
                    DataTable dt = (DataTable)usersGrid.DataSource;
                    
                    adapter.Update(dt);
                    MessageBox.Show("Изменения сохранены", "Успех");
                    
                    AuditLogger.LogAction(
                        Environment.UserName,
                        "Изменение пользователей",
                        "Обновлены данные пользователей");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка");
            }
        }
    }
}