// AuditLogger.cs — журналирование действий пользователей
using System;
using System.Data; // Добавлена необходимая директива
using System.Data.SQLite;

namespace CafeManager
{
    public static class AuditLogger
    {
        public static void LogAction(string user, string action, string details = "")
        {
            try
            {
                using (var conn = new SQLiteConnection("Data Source=cafedb.sqlite;Version=3;"))
                {
                    conn.Open();
                    var cmd = new SQLiteCommand(
                        "INSERT INTO AuditLog (User, Action, Timestamp, Details) " +
                        "VALUES (@user, @action, @time, @details)", 
                        conn);
                    
                    cmd.Parameters.AddWithValue("@user", user);
                    cmd.Parameters.AddWithValue("@action", action);
                    cmd.Parameters.AddWithValue("@time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@details", details);
                    
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка записи в журнал аудита: {ex.Message}");
            }
        }

        public static DataTable GetAuditLog(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var dt = new DataTable();
            
            try
            {
                using (var conn = new SQLiteConnection("Data Source=cafedb.sqlite;Version=3;"))
                {
                    conn.Open();
                    string query = @"
                        SELECT 
                            Timestamp as 'Дата/Время',
                            User as 'Пользователь',
                            Action as 'Действие',
                            Details as 'Детали'
                        FROM AuditLog
                    ";
                    
                    if (fromDate.HasValue || toDate.HasValue)
                    {
                        query += " WHERE 1=1";
                        
                        if (fromDate.HasValue)
                            query += " AND Timestamp >= @fromDate";
                        
                        if (toDate.HasValue)
                            query += " AND Timestamp <= @toDate";
                    }
                    
                    query += " ORDER BY Timestamp DESC";
                    
                    using (var cmd = new SQLiteCommand(query, conn))
                    {
                        if (fromDate.HasValue)
                            cmd.Parameters.AddWithValue("@fromDate", fromDate.Value.ToString("yyyy-MM-dd"));
                        
                        if (toDate.HasValue)
                            cmd.Parameters.AddWithValue("@toDate", toDate.Value.ToString("yyyy-MM-dd 23:59:59"));
                        
                        using (var adapter = new SQLiteDataAdapter(cmd))
                        {
                            adapter.Fill(dt);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка чтения журнала аудита: {ex.Message}");
            }
            
            return dt;
        }
    }
}