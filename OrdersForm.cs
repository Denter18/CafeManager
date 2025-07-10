// OrdersForm.cs — оформление заказов
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;

namespace CafeManager
{
    public partial class OrdersForm : Form
    {
        private ComboBox dishSelector;
        private NumericUpDown quantityBox;
        private Button addBtn, confirmBtn, refreshBtn;
        private ListBox orderList;
        private List<(int dishId, string name, int qty, double price)> items = new();
        private Label totalLabel;
        private string currentUser;

        public OrdersForm(string user)
        {
            currentUser = user;
            InitializeComponent();
            LoadMenu();
        }

        private void InitializeComponent()
        {
            this.Text = "Создание заказа";
            this.Size = new System.Drawing.Size(450, 400); // Увеличили высоту
            this.StartPosition = FormStartPosition.CenterScreen;

            // Главный контейнер
            var mainContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(10)
            };
            mainContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F)); // Панель выбора
            mainContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));    // Список заказа
            mainContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));   // Итог
            mainContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));   // Кнопки

            // Панель выбора блюда
            var dishPanel = new Panel { Dock = DockStyle.Fill };
            
            var dishLabel = new Label 
            { 
                Text = "Блюдо:", 
                Left = 5, 
                Top = 10,
                AutoSize = true
            };
            
            dishSelector = new ComboBox 
            { 
                Left = 50, 
                Top = 8, 
                Width = 180,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            
            quantityBox = new NumericUpDown 
            { 
                Left = 240, 
                Top = 8, 
                Width = 50, 
                Minimum = 1, 
                Value = 1 
            };
            
            addBtn = new Button 
            { 
                Text = "Добавить", 
                Left = 300, 
                Top = 7, 
                Width = 80,
                Height = 25
            };
            
            dishPanel.Controls.Add(dishLabel);
            dishPanel.Controls.Add(dishSelector);
            dishPanel.Controls.Add(quantityBox);
            dishPanel.Controls.Add(addBtn);

            // Список заказа с прокруткой
            var orderContainer = new Panel 
            { 
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle
            };
            
            var orderLabel = new Label 
            { 
                Text = "Состав заказа:", 
                Dock = DockStyle.Top,
                Height = 20,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            
            orderList = new ListBox 
            { 
                Dock = DockStyle.Fill,
                Font = new Font("Arial", 9),
                IntegralHeight = false,
                Height = 150
            };
            
            orderContainer.Controls.Add(orderList);
            orderContainer.Controls.Add(orderLabel);

            // Итоговая сумма
            totalLabel = new Label
            {
                Text = "Итого: 0 руб.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Arial", 10, FontStyle.Bold),
                BackColor = Color.LightYellow
            };
            
            // Панель кнопок
            var buttonPanel = new Panel { Dock = DockStyle.Fill };
            
            refreshBtn = new Button 
            { 
                Text = "Обновить",
                Size = new Size(80, 30),
                Location = new Point(10, 10)
            };
            
            confirmBtn = new Button 
            { 
                Text = "Подтвердить", 
                Size = new Size(100, 30),
                Location = new Point(300, 10),
                BackColor = Color.LightGreen,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            
            buttonPanel.Controls.Add(refreshBtn);
            buttonPanel.Controls.Add(confirmBtn);
            
            // Сборка интерфейса
            mainContainer.Controls.Add(dishPanel, 0, 0);
            mainContainer.Controls.Add(orderContainer, 0, 1);
            mainContainer.Controls.Add(totalLabel, 0, 2);
            mainContainer.Controls.Add(buttonPanel, 0, 3);
            
            this.Controls.Add(mainContainer);
            
            // Обработчики событий
            addBtn.Click += AddBtn_Click;
            confirmBtn.Click += ConfirmBtn_Click;
            refreshBtn.Click += (s, e) => 
            {
                dishSelector.Items.Clear();
                LoadMenu();
            };
        }

        private void LoadMenu()
        {
            try
            {
                dishSelector.BeginUpdate();
                dishSelector.Items.Clear();
                
                using var conn = new SQLiteConnection(DBHelper.ConnectionString);
                conn.Open();
                using var cmd = new SQLiteCommand("SELECT Id, Name, Price FROM Dishes ORDER BY Name", conn);
                using var reader = cmd.ExecuteReader();
                
                while (reader.Read())
                {
                    dishSelector.Items.Add(new DishItem
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Price = reader.GetDouble(2)
                    });
                }
                
                dishSelector.DisplayMember = "Display";
                
                if (dishSelector.Items.Count > 0)
                    dishSelector.SelectedIndex = 0;
                    
                dishSelector.EndUpdate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки меню: {ex.Message}", "Ошибка");
            }
        }

        private void AddBtn_Click(object sender, EventArgs e)
        {
            if (dishSelector.SelectedItem is DishItem dish)
            {
                int qty = (int)quantityBox.Value;
                items.Add((dish.Id, dish.Name, qty, dish.Price));
                orderList.Items.Add($"{dish.Name} x{qty} = {dish.Price * qty:C2}");
                
                // Прокрутка к последнему добавленному элементу
                if (orderList.Items.Count > 0)
                {
                    orderList.TopIndex = orderList.Items.Count - 1;
                }
                
                UpdateTotal();
            }
            else
            {
                MessageBox.Show("Выберите блюдо из списка", "Ошибка");
            }
        }

        private void UpdateTotal()
        {
            double total = 0;
            foreach (var item in items) total += item.qty * item.price;
            totalLabel.Text = $"Итого: {total:C2}";
        }

        private void ConfirmBtn_Click(object sender, EventArgs e)
        {
            if (items.Count == 0)
            {
                MessageBox.Show("Добавьте хотя бы одно блюдо в заказ", "Ошибка");
                return;
            }

            using var conn = new SQLiteConnection(DBHelper.ConnectionString);
            conn.Open();
            using var txn = conn.BeginTransaction();

            try
            {
                var total = 0.0;
                foreach (var item in items) total += item.qty * item.price;

                // Создание заказа
                var cmd = new SQLiteCommand(
                    "INSERT INTO Orders (User, Date, Total, Status) VALUES (@user, @date, @total, 'Создан')",
                    conn, txn);
                cmd.Parameters.AddWithValue("@user", currentUser);
                cmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@total", total);
                cmd.ExecuteNonQuery();

                long orderId = conn.LastInsertRowId;

                // Добавление позиций заказа
                foreach (var item in items)
                {
                    var itemCmd = new SQLiteCommand(
                        "INSERT INTO OrderItems (OrderId, DishId, Quantity) VALUES (@oid, @did, @qty)",
                        conn, txn);
                    itemCmd.Parameters.AddWithValue("@oid", orderId);
                    itemCmd.Parameters.AddWithValue("@did", item.dishId);
                    itemCmd.Parameters.AddWithValue("@qty", item.qty);
                    itemCmd.ExecuteNonQuery();

                    // Списание ингредиентов
                    DeductIngredients(item.dishId, item.qty, conn, txn);
                }

                txn.Commit();
                MessageBox.Show($"Заказ #{orderId} сохранён!\nСумма: {total:C2}", "Успех");
                AuditLogger.LogAction(
                    currentUser,
                    "Создание заказа",
                    $"Заказ #{orderId} на сумму {total:C2}");
                
                // Сброс формы после успешного создания
                items.Clear();
                orderList.Items.Clear();
                UpdateTotal();
            }
            catch (Exception ex)
            {
                txn.Rollback();
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка списания");
            }
        }

        private void DeductIngredients(int dishId, int quantity, SQLiteConnection conn, SQLiteTransaction txn)
        {
            var recipeCmd = new SQLiteCommand(
                "SELECT IngredientId, Amount FROM Recipes WHERE DishId = @dishId",
                conn, txn);
            recipeCmd.Parameters.AddWithValue("@dishId", dishId);

            using (var reader = recipeCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    int ingId = reader.GetInt32(0);
                    double amount = reader.GetDouble(1);
                    double totalNeeded = amount * quantity;

                    // Проверка наличия
                    var checkCmd = new SQLiteCommand(
                        "SELECT Quantity FROM Ingredients WHERE Id = @id",
                        conn, txn);
                    checkCmd.Parameters.AddWithValue("@id", ingId);
                    double currentQty = Convert.ToDouble(checkCmd.ExecuteScalar());

                    if (currentQty < totalNeeded)
                    {
                        throw new Exception($"Недостаточно ингредиента: {GetIngredientName(ingId)}. " +
                                            $"Требуется: {totalNeeded}, в наличии: {currentQty}");
                    }

                    // Списание
                    var updateCmd = new SQLiteCommand(
                        "UPDATE Ingredients SET Quantity = Quantity - @needed WHERE Id = @id",
                        conn, txn);
                    updateCmd.Parameters.AddWithValue("@needed", totalNeeded);
                    updateCmd.Parameters.AddWithValue("@id", ingId);
                    updateCmd.ExecuteNonQuery();
                }
            }
        }

        private string GetIngredientName(int id)
        {
            try
            {
                using var conn = new SQLiteConnection(DBHelper.ConnectionString);
                conn.Open();
                var cmd = new SQLiteCommand("SELECT Name FROM Ingredients WHERE Id = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                return cmd.ExecuteScalar()?.ToString() ?? $"Ингредиент #{id}";
            }
            catch
            {
                return $"Ингредиент #{id}";
            }
        }

        class DishItem
        {
            public int Id;
            public string Name;
            public double Price;
            public string Display => $"{Name} ({Price:C2})";
        }
    }
}