// MenuForm.cs — управление блюдами кафе
using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;

namespace CafeManager
{
    public partial class MenuForm : Form
    {
        private DataGridView dgv;
        private Button addBtn, delBtn, saveBtn, refreshBtn;
        private DataTable dataTable;

        public MenuForm()
        {
            InitializeComponent();
            LoadDishes();
        }

        private void InitializeComponent()
        {
            this.Text = "Меню кафе";
            this.Size = new System.Drawing.Size(600, 400);

            // Панель для кнопок
            var buttonPanel = new Panel 
            { 
                Dock = DockStyle.Bottom, 
                Height = 50 
            };
            
            dgv = new DataGridView 
            { 
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = true,
                AllowUserToDeleteRows = true
            };

            addBtn = new Button { Text = "Добавить", Width = 80, Top = 10, Left = 20 };
            delBtn = new Button { Text = "Удалить", Width = 80, Top = 10, Left = 110 };
            saveBtn = new Button { Text = "Сохранить", Width = 80, Top = 10, Left = 200 };
            refreshBtn = new Button { Text = "Обновить", Width = 80, Top = 10, Left = 290 };

            addBtn.Click += AddBtn_Click;
            delBtn.Click += DelBtn_Click;
            saveBtn.Click += SaveBtn_Click;
            refreshBtn.Click += (s, e) => LoadDishes();

            buttonPanel.Controls.Add(addBtn);
            buttonPanel.Controls.Add(delBtn);
            buttonPanel.Controls.Add(saveBtn);
            buttonPanel.Controls.Add(refreshBtn);

            this.Controls.Add(dgv);
            this.Controls.Add(buttonPanel);
        }

        private void LoadDishes()
        {
            using (var conn = new SQLiteConnection("Data Source=cafedb.sqlite;Version=3;"))
            {
                conn.Open();
                SQLiteDataAdapter adapter = new SQLiteDataAdapter(
                    "SELECT Id AS 'Номер', Name AS 'Название блюда', Price AS 'Цена' FROM Dishes", 
                    conn);
                
                dataTable = new DataTable();
                adapter.Fill(dataTable);
                
                // Устанавливаем первичный ключ
                dataTable.PrimaryKey = new DataColumn[] { dataTable.Columns["Номер"] };
                
                dgv.DataSource = dataTable;
                
                // Форматирование
                if (dgv.Columns.Contains("Цена"))
                {
                    dgv.Columns["Цена"].DefaultCellStyle.Format = "C2";
                    dgv.Columns["Цена"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                
                // Настройка столбцов
                if (dgv.Columns.Contains("Номер"))
                {
                    dgv.Columns["Номер"].ReadOnly = true;
                    dgv.Columns["Номер"].Visible = false;
                }
            }
        }

        private void AddBtn_Click(object sender, EventArgs e)
        {
            DataRow newRow = dataTable.NewRow();
            dataTable.Rows.Add(newRow);
        }

        private void DelBtn_Click(object sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count > 0)
            {
                foreach (DataGridViewRow row in dgv.SelectedRows)
                {
                    int id = Convert.ToInt32(row.Cells["Номер"].Value);
                    DeleteRelatedRecipes(id);
                    dgv.Rows.Remove(row);
                }
            }
        }

        private void DeleteRelatedRecipes(int dishId)
        {
            using (var conn = new SQLiteConnection("Data Source=cafedb.sqlite;Version=3;"))
            {
                conn.Open();
                var cmd = new SQLiteCommand(
                    "DELETE FROM Recipes WHERE DishId = @dishId", 
                    conn);
                cmd.Parameters.AddWithValue("@dishId", dishId);
                cmd.ExecuteNonQuery();
            }
        }

        private void SaveBtn_Click(object sender, EventArgs e)
        {
            using (var conn = new SQLiteConnection("Data Source=cafedb.sqlite;Version=3;"))
            {
                conn.Open();
                SQLiteDataAdapter adapter = new SQLiteDataAdapter("SELECT * FROM Dishes", conn);
                SQLiteCommandBuilder builder = new SQLiteCommandBuilder(adapter);
                
                try
                {
                    adapter.Update(dataTable);
                    
                    // Логирование
                    AuditLogger.LogAction(
                        Environment.UserName,
                        "Изменение меню",
                        $"Обновлено {dataTable.Rows.Count} блюд");
                    
                    MessageBox.Show("Изменения сохранены", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}