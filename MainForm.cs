// MainForm.cs — главное окно системы
using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;

namespace CafeManager
{
    public partial class MainForm : Form
    {
        private string currentUser;
        private string currentRole;
        private DataGridView ordersGrid;
        private Button refreshOrdersBtn;
        private ContextMenuStrip orderContextMenu;

        public MainForm(string user, string role)
        {
            currentUser = user;
            currentRole = role;
            InitializeComponent();
            LoadOrders();
        }

        private void InitializeComponent()
        {
            this.Text = $"Кафе: Главная панель ({currentRole})";
            this.Size = new System.Drawing.Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Главный контейнер
            var mainContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 100 // Уменьшили верхнюю панель
            };

            // Верхняя панель: приветствие и меню
            var topPanel = new Panel { Dock = DockStyle.Fill };
            
            MenuStrip menu = new MenuStrip();
            ToolStripMenuItem ordersItem = new ToolStripMenuItem("Заказы");
            ToolStripMenuItem menuItem = new ToolStripMenuItem("Меню");
            ToolStripMenuItem inventoryItem = new ToolStripMenuItem("Склад");
            ToolStripMenuItem reportsItem = new ToolStripMenuItem("Отчёты");
            ToolStripMenuItem recipesItem = new ToolStripMenuItem("Рецепты");
            ToolStripMenuItem usersItem = new ToolStripMenuItem("Пользователи");
            ToolStripMenuItem exitItem = new ToolStripMenuItem("Выход");

            // Обработчики меню
            ordersItem.Click += (s, e) => new OrdersForm(currentUser).ShowDialog();
            menuItem.Click += (s, e) => new MenuForm().ShowDialog();
            inventoryItem.Click += (s, e) => new InventoryForm().ShowDialog();
            reportsItem.Click += (s, e) => new ReportsForm().ShowDialog();
            recipesItem.Click += (s, e) => 
            {
                if (currentRole == "Администратор")
                    new RecipeForm().ShowDialog();
                else
                    MessageBox.Show("Доступно только администраторам", "Ошибка доступа");
            };
            usersItem.Click += (s, e) => 
            {
                if (currentRole == "Администратор")
                    new UsersForm().ShowDialog();
                else
                    MessageBox.Show("Доступно только администраторам", "Ошибка доступа");
            };
            exitItem.Click += (s, e) => Application.Exit();

            // Добавление пунктов меню
            menu.Items.Add(ordersItem);
            menu.Items.Add(menuItem);
            menu.Items.Add(inventoryItem);
            menu.Items.Add(reportsItem);
            menu.Items.Add(recipesItem);
            menu.Items.Add(usersItem);
            menu.Items.Add(exitItem);

            // Настройка видимости по ролям
            menuItem.Visible = (currentRole == "Администратор");
            inventoryItem.Visible = (currentRole == "Администратор");
            recipesItem.Visible = (currentRole == "Администратор");
            reportsItem.Visible = (currentRole == "Администратор" || currentRole == "Кассир");
            usersItem.Visible = (currentRole == "Администратор");

            Label welcome = new Label
            {
                Text = $"Добро пожаловать, {currentUser}! Ваша роль: {currentRole}",
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Arial", 12, FontStyle.Bold),
                BackColor = Color.Lavender
            };

            topPanel.Controls.Add(menu);
            topPanel.Controls.Add(welcome);
            
            // Нижняя панель: таблица заказов
            var ordersPanel = new Panel { Dock = DockStyle.Fill };
            
            refreshOrdersBtn = new Button 
            { 
                Text = "Обновить список заказов",
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = Color.LightSkyBlue
            };
            refreshOrdersBtn.Click += (s, e) => LoadOrders();
            
            ordersGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false
            };
            
            ordersPanel.Controls.Add(ordersGrid);
            ordersPanel.Controls.Add(refreshOrdersBtn);
            
            // Инициализация контекстного меню
            InitializeOrderContextMenu();
            ordersGrid.MouseClick += OrdersGrid_MouseClick;
            
            mainContainer.Panel1.Controls.Add(topPanel);
            mainContainer.Panel2.Controls.Add(ordersPanel);
            
            this.Controls.Add(mainContainer);
            this.MainMenuStrip = menu;
        }

        private void InitializeOrderContextMenu()
        {
            orderContextMenu = new ContextMenuStrip();
            
            var markAsSoldItem = new ToolStripMenuItem("Отметить как оплаченный");
            markAsSoldItem.Click += MarkAsSold_Click;
            orderContextMenu.Items.Add(markAsSoldItem);
            
            var cancelOrderItem = new ToolStripMenuItem("Отменить заказ");
            cancelOrderItem.Click += CancelOrder_Click;
            orderContextMenu.Items.Add(cancelOrderItem);
            
            var viewDetailsItem = new ToolStripMenuItem("Просмотр деталей");
            viewDetailsItem.Click += ViewDetails_Click;
            orderContextMenu.Items.Add(viewDetailsItem);
        }

        private void OrdersGrid_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var hti = ordersGrid.HitTest(e.X, e.Y);
                if (hti.RowIndex >= 0 && !ordersGrid.Rows[hti.RowIndex].IsNewRow)
                {
                    ordersGrid.Rows[hti.RowIndex].Selected = true;
                    orderContextMenu.Show(ordersGrid, e.Location);
                }
            }
        }

        private void LoadOrders()
        {
            try
            {
                using (var conn = new SQLiteConnection(DBHelper.ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT 
                            o.Id AS 'Номер заказа',
                            o.Date AS 'Дата создания',
                            o.Total AS 'Сумма',
                            o.Status AS 'Статус',
                            u.Login AS 'Официант',
                            (SELECT GROUP_CONCAT(d.Name || ' x' || oi.Quantity, ', ') 
                             FROM OrderItems oi
                             JOIN Dishes d ON oi.DishId = d.Id
                             WHERE oi.OrderId = o.Id) AS 'Состав заказа'
                        FROM Orders o
                        JOIN Users u ON o.User = u.Login
                        ORDER BY o.Date DESC
                    ";
                    
                    SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, conn);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    ordersGrid.DataSource = dt;
                    
                    // Форматирование
                    if (ordersGrid.Columns.Contains("Сумма"))
                    {
                        ordersGrid.Columns["Сумма"].DefaultCellStyle.Format = "C2";
                        ordersGrid.Columns["Сумма"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    }
                    
                    if (ordersGrid.Columns.Contains("Дата создания"))
                    {
                        ordersGrid.Columns["Дата создания"].DefaultCellStyle.Format = "g";
                    }
                    
                    // Подсветка статусов
                    ordersGrid.CellFormatting += (s, e) =>
                    {
                        if (e.ColumnIndex == ordersGrid.Columns["Статус"].Index)
                        {
                            string status = e.Value?.ToString() ?? "";
                            
                            if (status == "Создан")
                            {
                                e.CellStyle.BackColor = Color.LightYellow;
                            }
                            else if (status == "Оплачен")
                            {
                                e.CellStyle.BackColor = Color.LightGreen;
                            }
                            else if (status == "Отменен")
                            {
                                e.CellStyle.BackColor = Color.LightCoral;
                            }
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заказов: {ex.Message}", "Ошибка");
            }
        }

        private void MarkAsSold_Click(object sender, EventArgs e)
        {
            if (ordersGrid.SelectedRows.Count == 0) return;
            
            var selectedRow = ordersGrid.SelectedRows[0];
            int orderId = Convert.ToInt32(selectedRow.Cells["Номер заказа"].Value);
            string currentStatus = selectedRow.Cells["Статус"].Value.ToString();
            
            if (currentStatus == "Оплачен")
            {
                MessageBox.Show("Заказ уже оплачен", "Информация");
                return;
            }
            
            if (MessageBox.Show($"Подтвердить продажу заказа #{orderId}?", "Подтверждение продажи", 
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                UpdateOrderStatus(orderId, "Оплачен");
                LoadOrders();
            }
        }

        private void CancelOrder_Click(object sender, EventArgs e)
        {
            if (ordersGrid.SelectedRows.Count == 0) return;
            
            var selectedRow = ordersGrid.SelectedRows[0];
            int orderId = Convert.ToInt32(selectedRow.Cells["Номер заказа"].Value);
            string currentStatus = selectedRow.Cells["Статус"].Value.ToString();
            
            if (currentStatus == "Отменен")
            {
                MessageBox.Show("Заказ уже отменен", "Информация");
                return;
            }
            
            if (MessageBox.Show($"Отменить заказ #{orderId}?", "Подтверждение отмены", 
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                UpdateOrderStatus(orderId, "Отменен");
                LoadOrders();
            }
        }

        private void ViewDetails_Click(object sender, EventArgs e)
        {
            if (ordersGrid.SelectedRows.Count == 0) return;
            
            var selectedRow = ordersGrid.SelectedRows[0];
            int orderId = Convert.ToInt32(selectedRow.Cells["Номер заказа"].Value);
            
            string details = $"Заказ #{orderId}\n";
            details += $"Дата: {selectedRow.Cells["Дата создания"].Value}\n";
            details += $"Сумма: {selectedRow.Cells["Сумма"].Value:C2}\n";
            details += $"Статус: {selectedRow.Cells["Статус"].Value}\n";
            details += $"Официант: {selectedRow.Cells["Официант"].Value}\n\n";
            details += $"Состав заказа:\n{selectedRow.Cells["Состав заказа"].Value}";
            
            MessageBox.Show(details, "Детали заказа", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void UpdateOrderStatus(int orderId, string newStatus)
        {
            try
            {
                using (var conn = new SQLiteConnection(DBHelper.ConnectionString))
                {
                    conn.Open();
                    var cmd = new SQLiteCommand(
                        "UPDATE Orders SET Status = @status WHERE Id = @id", 
                        conn);
                    
                    cmd.Parameters.AddWithValue("@status", newStatus);
                    cmd.Parameters.AddWithValue("@id", orderId);
                    cmd.ExecuteNonQuery();
                    
                    // Логирование
                    string action = newStatus == "Оплачен" ? "Продажа заказа" : "Отмена заказа";
                    AuditLogger.LogAction(
                        currentUser,
                        action,
                        $"Заказ #{orderId} изменен на статус '{newStatus}'");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления статуса: {ex.Message}", "Ошибка");
            }
        }
    }
}