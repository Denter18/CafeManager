// InventoryForm.cs — управление запасами ингредиентов
using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;

namespace CafeManager
{
    public partial class InventoryForm : Form
    {
        private DataGridView dgv;
        private Button addBtn, delBtn, saveBtn, refreshBtn;

        public InventoryForm()
        {
            InitializeComponent();
            LoadIngredients();
        }

        private void InitializeComponent()
        {
            this.Text = "Склад — Ингредиенты";
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
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            addBtn = new Button { Text = "Добавить", Width = 80, Top = 10, Left = 20 };
            delBtn = new Button { Text = "Удалить", Width = 80, Top = 10, Left = 110 };
            saveBtn = new Button { Text = "Сохранить", Width = 80, Top = 10, Left = 200 };
            refreshBtn = new Button { Text = "Обновить", Width = 80, Top = 10, Left = 290 };

            addBtn.Click += AddBtn_Click;
            delBtn.Click += DelBtn_Click;
            saveBtn.Click += SaveBtn_Click;
            refreshBtn.Click += (s, e) => LoadIngredients();

            buttonPanel.Controls.Add(addBtn);
            buttonPanel.Controls.Add(delBtn);
            buttonPanel.Controls.Add(saveBtn);
            buttonPanel.Controls.Add(refreshBtn);

            this.Controls.Add(dgv);
            this.Controls.Add(buttonPanel);
        }

        private void LoadIngredients()
        {
            using var conn = new SQLiteConnection("Data Source=cafedb.sqlite;Version=3;");
            conn.Open();
            SQLiteDataAdapter adapter = new SQLiteDataAdapter(
                "SELECT Id AS 'Номер', Name AS 'Наименование', Quantity AS 'Количество', Unit AS 'Единица измерения' FROM Ingredients", 
                conn);
            
            DataTable dt = new DataTable();
            adapter.Fill(dt);
            dgv.DataSource = dt;
            
            // Форматирование
            if (dgv.Columns.Contains("Количество"))
            {
                dgv.Columns["Количество"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
        }

        private void AddBtn_Click(object sender, EventArgs e)
        {
            DataTable dt = dgv.DataSource as DataTable;
            dt.Rows.Add(dt.NewRow());
        }

        private void DelBtn_Click(object sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count > 0)
            {
                dgv.Rows.RemoveAt(dgv.SelectedRows[0].Index);
            }
        }

        private void SaveBtn_Click(object sender, EventArgs e)
        {
            using var conn = new SQLiteConnection("Data Source=cafedb.sqlite;Version=3;");
            conn.Open();
            SQLiteDataAdapter adapter = new SQLiteDataAdapter("SELECT * FROM Ingredients", conn);
            SQLiteCommandBuilder builder = new SQLiteCommandBuilder(adapter);
            DataTable dt = (DataTable)dgv.DataSource;
            
            try
            {
                adapter.Update(dt);
                
                // Логирование
                AuditLogger.LogAction(
                    Environment.UserName,
                    "Изменение склада",
                    $"Обновлено {dt.Rows.Count} ингредиентов");
                
                MessageBox.Show("Данные ингредиентов обновлены", "Успешно");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}