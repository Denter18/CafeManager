// RecipeForm.cs — управление рецептами
using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;

namespace CafeManager
{
    public partial class RecipeForm : Form
    {
        private ComboBox dishCombo;
        private DataGridView recipeGrid;
        private Button saveBtn, refreshBtn;
        private int currentDishId;
        private DataTable recipeTable;

        public RecipeForm()
        {
            InitializeComponent();
            LoadDishes();
        }

        private void InitializeComponent()
        {
            this.Text = "Управление рецептами";
            this.Size = new System.Drawing.Size(700, 500);
            
            // Панель для элементов управления
            var mainPanel = new Panel { Dock = DockStyle.Fill };
            
            // Панель для кнопок
            var buttonPanel = new Panel 
            { 
                Dock = DockStyle.Bottom, 
                Height = 50 
            };
            
            dishCombo = new ComboBox 
            { 
                Dock = DockStyle.Top, 
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(10)
            };
            
            recipeGrid = new DataGridView 
            { 
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = true,
                AllowUserToDeleteRows = true
            };
            
            saveBtn = new Button 
            { 
                Text = "Сохранить рецепт",
                Width = 120,
                Top = 10,
                Left = 20
            };
            
            refreshBtn = new Button 
            { 
                Text = "Обновить данные",
                Width = 120,
                Top = 10,
                Left = 150
            };
            
            dishCombo.SelectedIndexChanged += (s, e) => LoadRecipe();
            saveBtn.Click += SaveRecipe;
            refreshBtn.Click += (s, e) => 
            {
                dishCombo.Items.Clear();
                LoadDishes();
            };
            
            buttonPanel.Controls.Add(saveBtn);
            buttonPanel.Controls.Add(refreshBtn);
            
            mainPanel.Controls.Add(recipeGrid);
            mainPanel.Controls.Add(dishCombo);
            
            this.Controls.Add(mainPanel);
            this.Controls.Add(buttonPanel);
        }

        private void LoadDishes()
        {
            using (var conn = new SQLiteConnection("Data Source=cafedb.sqlite;Version=3;"))
            {
                conn.Open();
                using var cmd = new SQLiteCommand("SELECT Id, Name FROM Dishes ORDER BY Name", conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    dishCombo.Items.Add(new DishItem
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1)
                    });
                }
                dishCombo.DisplayMember = "Name";
                dishCombo.ValueMember = "Id";
                
                if (dishCombo.Items.Count > 0)
                    dishCombo.SelectedIndex = 0;
            }
        }

        private void LoadRecipe()
        {
            if (dishCombo.SelectedItem == null) return;
            
            var dish = (DishItem)dishCombo.SelectedItem;
            currentDishId = dish.Id;
            
            using (var conn = new SQLiteConnection("Data Source=cafedb.sqlite;Version=3;"))
            {
                conn.Open();
                string query = @"
                    SELECT 
                        i.Id AS 'Id',
                        i.Name AS 'Ингредиент',
                        r.Amount AS 'Количество',
                        i.Unit AS 'Единица измерения'
                    FROM Ingredients i
                    LEFT JOIN Recipes r ON r.IngredientId = i.Id AND r.DishId = @dishId
                ";
                
                SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, conn);
                adapter.SelectCommand.Parameters.AddWithValue("@dishId", currentDishId);
                recipeTable = new DataTable();
                adapter.Fill(recipeTable);
                
                recipeGrid.DataSource = recipeTable;
                recipeGrid.Columns["Id"].Visible = false;
                
                // Настройка столбца количества
                if (recipeGrid.Columns.Contains("Количество"))
                {
                    recipeGrid.Columns["Количество"].ValueType = typeof(double);
                    recipeGrid.Columns["Количество"].DefaultCellStyle.Format = "N2";
                }
            }
        }

        private void SaveRecipe(object sender, EventArgs e)
        {
            if (recipeGrid.DataSource == null) return;
            
            using (var conn = new SQLiteConnection("Data Source=cafedb.sqlite;Version=3;"))
            {
                conn.Open();
                using var transaction = conn.BeginTransaction();
                
                try
                {
                    // Удалить старые записи рецепта
                    var deleteCmd = new SQLiteCommand(
                        "DELETE FROM Recipes WHERE DishId = @dishId", 
                        conn, transaction);
                    deleteCmd.Parameters.AddWithValue("@dishId", currentDishId);
                    deleteCmd.ExecuteNonQuery();
                    
                    // Добавить новые записи
                    foreach (DataRow row in recipeTable.Rows)
                    {
                        if (row["Количество"] != DBNull.Value && 
                            Convert.ToDouble(row["Количество"]) > 0)
                        {
                            var insertCmd = new SQLiteCommand(
                                "INSERT INTO Recipes (DishId, IngredientId, Amount) " +
                                "VALUES (@dishId, @ingId, @amount)",
                                conn, transaction);
                            
                            insertCmd.Parameters.AddWithValue("@dishId", currentDishId);
                            insertCmd.Parameters.AddWithValue("@ingId", row["Id"]);
                            insertCmd.Parameters.AddWithValue("@amount", row["Количество"]);
                            insertCmd.ExecuteNonQuery();
                        }
                    }
                    
                    transaction.Commit();
                    MessageBox.Show("Рецепт успешно сохранён", "Успех");
                    
                    // Логирование действия
                    AuditLogger.LogAction(
                        Environment.UserName,
                        "Изменение рецепта",
                        $"Изменен рецепт блюда: {dishCombo.Text}");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка");
                }
            }
        }
        
        private class DishItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}