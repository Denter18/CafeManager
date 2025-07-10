// LoginForm.cs — окно авторизации
// LoginForm.cs — окно авторизации
using System;
using System.Data.SQLite;
using System.Windows.Forms;

namespace CafeManager
{
    public partial class LoginForm : Form
    {
        public string CurrentUser { get; private set; }
        public string CurrentRole { get; private set; }

        private TextBox txtUsername;
        private TextBox txtPassword;
        private Button loginButton;

        public LoginForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Вход в систему";
            this.Size = new System.Drawing.Size(300, 200);

            Label lblUsername = new Label { Text = "Логин", Top = 20, Left = 20, Width = 100 };
            txtUsername = new TextBox { Top = 40, Left = 20, Width = 240 };

            Label lblPassword = new Label { Text = "Пароль", Top = 70, Left = 20, Width = 100 };
            txtPassword = new TextBox { Top = 90, Left = 20, Width = 240, UseSystemPasswordChar = true };

            loginButton = new Button { Text = "Войти", Top = 130, Left = 20, Width = 240 };
            loginButton.Click += loginButton_Click;

            this.Controls.Add(lblUsername);
            this.Controls.Add(txtUsername);
            this.Controls.Add(lblPassword);
            this.Controls.Add(txtPassword);
            this.Controls.Add(loginButton);
        }

        private void loginButton_Click(object sender, EventArgs e)
        {
            string login = txtUsername.Text.Trim();
            string password = txtPassword.Text.Trim();

            using (SQLiteConnection conn = new SQLiteConnection("Data Source=cafedb.sqlite;Version=3;"))
            {
                conn.Open();
                string query = "SELECT Password, Role FROM Users WHERE Login = @login";
                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@login", login);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string storedHash = reader["Password"].ToString();
                            string inputHash = DBHelper.HashPassword(password);
                            
                            if (inputHash == storedHash)
                            {
                                CurrentUser = login;
                                CurrentRole = reader["Role"].ToString();
                                this.DialogResult = DialogResult.OK;
                                this.Close();
                                return;
                            }
                        }
                    }
                }
            }
            MessageBox.Show("Неверный логин или пароль", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}