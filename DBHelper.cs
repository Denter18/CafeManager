// DBHelper.cs — подключение к SQLite и создание структуры БД
using System;
using System.Data.SQLite;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CafeManager
{
    public static class DBHelper
    {
        private static readonly string dbFile = "cafedb.sqlite";
        public static string ConnectionString => $"Data Source={dbFile};Version=3;";

        public static void InitializeDatabase()
        {
            try
            {
                if (!File.Exists(dbFile))
                {
                    SQLiteConnection.CreateFile(dbFile);
                    Console.WriteLine("Создана новая база данных.");
                }

                using (var conn = new SQLiteConnection(ConnectionString))
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = transaction;
                        
                        // Создание таблиц
                        cmd.CommandText = @"
                        PRAGMA foreign_keys = ON;

                        CREATE TABLE IF NOT EXISTS Users (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Login TEXT NOT NULL UNIQUE,
                            Password TEXT NOT NULL,
                            Role TEXT NOT NULL
                        );

                        CREATE TABLE IF NOT EXISTS Dishes (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Name TEXT NOT NULL,
                            Price REAL NOT NULL
                        );

                        CREATE TABLE IF NOT EXISTS Ingredients (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Name TEXT NOT NULL,
                            Quantity REAL NOT NULL,
                            Unit TEXT NOT NULL
                        );

                        CREATE TABLE IF NOT EXISTS Recipes (
                            DishId INTEGER,
                            IngredientId INTEGER,
                            Amount REAL,
                            FOREIGN KEY (DishId) REFERENCES Dishes(Id) ON DELETE CASCADE,
                            FOREIGN KEY (IngredientId) REFERENCES Ingredients(Id),
                            PRIMARY KEY (DishId, IngredientId)
                        );

                        CREATE TABLE IF NOT EXISTS Orders (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            User TEXT NOT NULL,
                            Date TEXT NOT NULL,
                            Total REAL NOT NULL,
                            Status TEXT NOT NULL DEFAULT 'Создан'
                        );

                        CREATE TABLE IF NOT EXISTS OrderItems (
                            OrderId INTEGER,
                            DishId INTEGER,
                            Quantity INTEGER NOT NULL,
                            FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE CASCADE,
                            FOREIGN KEY (DishId) REFERENCES Dishes(Id)
                        );
                        
                        CREATE TABLE IF NOT EXISTS AuditLog (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            User TEXT NOT NULL,
                            Action TEXT NOT NULL,
                            Timestamp TEXT NOT NULL,
                            Details TEXT
                        );
                        ";
                        cmd.ExecuteNonQuery();

                        // Проверка наличия столбца Role
                        bool hasRole = false;
                        cmd.CommandText = "PRAGMA table_info(Users);";
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string columnName = reader.GetString(1);
                                if (columnName.Equals("Role", StringComparison.OrdinalIgnoreCase))
                                {
                                    hasRole = true;
                                    break;
                                }
                            }
                        }

                        if (!hasRole)
                        {
                            cmd.CommandText = "ALTER TABLE Users ADD COLUMN Role TEXT NOT NULL DEFAULT 'user';";
                            cmd.ExecuteNonQuery();
                        }

                        // Шифрование пароля администратора
                        string adminPassword = HashPassword("admin");
                        cmd.CommandText = "INSERT OR IGNORE INTO Users (Login, Password, Role) VALUES ('admin', @password, 'Администратор')";
                        cmd.Parameters.AddWithValue("@password", adminPassword);
                        cmd.ExecuteNonQuery();

                        // Добавление начальных данных
                        cmd.CommandText = @"
                        INSERT OR IGNORE INTO Dishes (Id, Name, Price) VALUES 
                            (1, 'Борщ', 250),
                            (2, 'Стейк говяжий', 450),
                            (3, 'Салат Цезарь', 320),
                            (4, 'Кофе латте', 180),
                            (5, 'Чизкейк', 220);
                        
                        INSERT OR IGNORE INTO Ingredients (Id, Name, Quantity, Unit) VALUES 
                            (1, 'Говядина', 50, 'кг'),
                            (2, 'Картофель', 100, 'кг'),
                            (3, 'Свекла', 30, 'кг'),
                            (4, 'Салат Айсберг', 20, 'кг'),
                            (5, 'Кофейные зерна', 15, 'кг'),
                            (6, 'Сливки', 40, 'л'),
                            (7, 'Сыр', 25, 'кг'),
                            (8, 'Помидоры', 35, 'кг'),
                            (9, 'Сухарики', 10, 'кг'),
                            (10, 'Соус Цезарь', 15, 'л');
                        
                        INSERT OR IGNORE INTO Recipes (DishId, IngredientId, Amount) VALUES 
                            (1, 2, 0.3),   -- Борщ: Картофель
                            (1, 3, 0.2),   -- Борщ: Свекла
                            (2, 1, 0.4),   -- Стейк: Говядина
                            (3, 4, 0.15),  -- Цезарь: Салат Айсберг
                            (3, 8, 0.1),   -- Цезарь: Помидоры
                            (3, 9, 0.05),  -- Цезарь: Сухарики
                            (3, 10, 0.03), -- Цезарь: Соус
                            (4, 5, 0.02),  -- Кофе: Кофейные зерна
                            (4, 6, 0.1),   -- Кофе: Сливки
                            (5, 7, 0.2);   -- Чизкейк: Сыр
                        ";
                        cmd.ExecuteNonQuery();
                        
                        transaction.Commit();
                        Console.WriteLine("База данных успешно инициализирована.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка инициализации БД: {ex.Message}");
                throw;
            }
        }

        public static string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        public static void BackupDatabase(string backupPath)
        {
            try
            {
                File.Copy(dbFile, backupPath, true);
                Console.WriteLine($"Создана резервная копия: {backupPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка создания резервной копии: {ex.Message}");
            }
        }
    }
}