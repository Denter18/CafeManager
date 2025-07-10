// ReportsForm.cs — отчёты с возможностью продажи заказов
using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;

namespace CafeManager
{
    public partial class ReportsForm : Form
    {
        private TabControl tabControl;
        private DataGridView salesGrid;
        private DataGridView stockGrid;
        private DataGridView auditGrid;
        private ContextMenuStrip orderContextMenu;

        public ReportsForm()
        {
            InitializeComponent();
            InitializeContextMenu();
            LoadReports();
        }

        private void InitializeContextMenu()
        {
            orderContextMenu = new ContextMenuStrip();
            
            var markAsSoldItem = new ToolStripMenuItem("Отметить как проданный");
            markAsSoldItem.Click += MarkAsSold_Click;
            orderContextMenu.Items.Add(markAsSoldItem);
            
            var cancelOrderItem = new ToolStripMenuItem("Отменить заказ");
            cancelOrderItem.Click += CancelOrder_Click;
            orderContextMenu.Items.Add(cancelOrderItem);
        }

        private void InitializeComponent()
        {
            this.Text = "Отчетность и управление заказами";
            this.Size = new System.Drawing.Size(900, 600);
            
            tabControl = new TabControl { Dock = DockStyle.Fill };
            this.Controls.Add(tabControl);
        }

        private void LoadReports()
        {
            // Отчет по продажам
            var salesTab = new TabPage("Заказы");
            var salesPanel = new Panel { Dock = DockStyle.Fill };
            salesTab.Controls.Add(salesPanel);
            
            var salesRefreshBtn = new Button 
            { 
                Text = "Обновить данные",
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = Color.LightBlue
            };
            
            salesGrid = new DataGridView 
            { 
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            
            salesRefreshBtn.Click += (s, e) => LoadSalesData();
            salesGrid.MouseClick += SalesGrid_MouseClick;
            
            salesPanel.Controls.Add(salesGrid);
            salesPanel.Controls.Add(salesRefreshBtn);
            
            // Отчет по остаткам
            var stockTab = new TabPage("Остатки ингредиентов");
            var stockPanel = new Panel { Dock = DockStyle.Fill };
            stockTab.Controls.Add(stockPanel);
            
            var stockRefreshBtn = new Button 
            { 
                Text = "Обновить данные",
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = Color.LightBlue
            };
            
            stockGrid = new DataGridView 
            { 
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            
            stockRefreshBtn.Click += (s, e) => LoadStockData();
            stockPanel.Controls.Add(stockGrid);
            stockPanel.Controls.Add(stockRefreshBtn);
            
            // Журнал аудита
            var auditTab = new TabPage("Журнал аудита");
            var auditPanel = new Panel { Dock = DockStyle.Fill };
            auditTab.Controls.Add(auditPanel);
            
            var auditRefreshBtn = new Button 
            { 
                Text = "Обновить данные",
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = Color.LightBlue
            };
            
            auditGrid = new DataGridView 
            { 
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            
            auditRefreshBtn.Click += (s, e) => LoadAuditData();
            auditPanel.Controls.Add(auditGrid);
            auditPanel.Controls.Add(auditRefreshBtn);
            
            tabControl.TabPages.Add(salesTab);
            tabControl.TabPages.Add(stockTab);
            tabControl.TabPages.Add(auditTab);
            
            // Загрузка данных
            LoadSalesData();
            LoadStockData();
            LoadAuditData();
        }

        private void SalesGrid_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var hti = salesGrid.HitTest(e.X, e.Y);
                if (hti.RowIndex >= 0 && !salesGrid.Rows[hti.RowIndex].IsNewRow)
                {
                    salesGrid.Rows[hti.RowIndex].Selected = true;
                    orderContextMenu.Show(salesGrid, e.Location);
                }
            }
        }

        private void MarkAsSold_Click(object sender, EventArgs e)
        {
            if (salesGrid.SelectedRows.Count == 0) return;
            
            var selectedRow = salesGrid.SelectedRows[0];
            int orderId = Convert.ToInt32(selectedRow.Cells["Номер заказа"].Value);
            string currentStatus = selectedRow.Cells["Статус заказа"].Value.ToString();
            
            if (currentStatus == "Оплачен")
            {
                MessageBox.Show("Заказ уже оплачен", "Информация");
                return;
            }
            
            if (MessageBox.Show($"Подтвердить продажу заказа #{orderId}?", "Подтверждение продажи", 
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                UpdateOrderStatus(orderId, "Оплачен");
                LoadSalesData();
            }
        }

        private void CancelOrder_Click(object sender, EventArgs e)
        {
            if (salesGrid.SelectedRows.Count == 0) return;
            
            var selectedRow = salesGrid.SelectedRows[0];
            int orderId = Convert.ToInt32(selectedRow.Cells["Номер заказа"].Value);
            string currentStatus = selectedRow.Cells["Статус заказа"].Value.ToString();
            
            if (currentStatus == "Отменен")
            {
                MessageBox.Show("Заказ уже отменен", "Информация");
                return;
            }
            
            if (MessageBox.Show($"Отменить заказ #{orderId}?", "Подтверждение отмены", 
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                UpdateOrderStatus(orderId, "Отменен");
                LoadSalesData();
            }
        }

        private void UpdateOrderStatus(int orderId, string newStatus)
        {
            using (var conn = new SQLiteConnection("Data Source=cafedb.sqlite;Version=3;"))
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
                    Environment.UserName,
                    action,
                    $"Заказ #{orderId} изменен на статус '{newStatus}'");
            }
        }

        private void LoadSalesData()
        {
            using (var conn = new SQLiteConnection("Data Source=cafedb.sqlite;Version=3;"))
            {
                conn.Open();
                string query = @"
                    SELECT 
                        o.Id AS 'Номер заказа',
                        o.Date AS 'Дата/время',
                        o.Total AS 'Сумма заказа',
                        o.Status AS 'Статус заказа',
                        u.Login AS 'Официант',
                        (SELECT GROUP_CONCAT(d.Name, ', ') 
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
                salesGrid.DataSource = dt;
                
                // Форматирование
                if (salesGrid.Columns.Contains("Сумма заказа"))
                {
                    salesGrid.Columns["Сумма заказа"].DefaultCellStyle.Format = "C2";
                    salesGrid.Columns["Сумма заказа"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                
                if (salesGrid.Columns.Contains("Дата/время"))
                {
                    salesGrid.Columns["Дата/время"].DefaultCellStyle.Format = "g";
                }
                
                // Подсветка статусов
                salesGrid.CellFormatting += (s, e) =>
                {
                    if (e.ColumnIndex == salesGrid.Columns["Статус заказа"].Index)
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

        private void LoadStockData()
        {
            using (var conn = new SQLiteConnection("Data Source=cafedb.sqlite;Version=3;"))
            {
                conn.Open();
                SQLiteDataAdapter adapter = new SQLiteDataAdapter(
                    "SELECT Name AS 'Наименование ингредиента', Quantity AS 'Количество', Unit AS 'Единица измерения' FROM Ingredients", 
                    conn);
                
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                stockGrid.DataSource = dt;
                
                // Подсветка низких остатков
                stockGrid.CellFormatting += (s, e) => 
                {
                    if (e.ColumnIndex == stockGrid.Columns["Количество"].Index)
                    {
                        try
                        {
                            double quantity = Convert.ToDouble(e.Value);
                            if (quantity < 10)
                            {
                                e.CellStyle.BackColor = Color.LightPink;
                                e.CellStyle.Font = new Font(e.CellStyle.Font, FontStyle.Bold);
                            }
                            else if (quantity < 20)
                            {
                                e.CellStyle.BackColor = Color.LightYellow;
                            }
                        }
                        catch
                        {
                            // Игнорируем ошибки преобразования
                        }
                    }
                };
            }
        }

        private void LoadAuditData()
        {
            using (var conn = new SQLiteConnection("Data Source=cafedb.sqlite;Version=3;"))
            {
                conn.Open();
                string query = @"
                    SELECT 
                        Timestamp AS 'Дата и время события',
                        User AS 'Пользователь',
                        Action AS 'Выполненное действие',
                        Details AS 'Детали действия'
                    FROM AuditLog
                    ORDER BY Timestamp DESC
                ";
                
                SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                auditGrid.DataSource = dt;
                
                // Форматирование даты
                if (auditGrid.Columns.Contains("Дата и время события"))
                {
                    auditGrid.Columns["Дата и время события"].DefaultCellStyle.Format = "g";
                }
            }
        }
    }
}