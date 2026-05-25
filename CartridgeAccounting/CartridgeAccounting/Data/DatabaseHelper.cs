using CartridgeAccounting.Models;
using ClosedXML.Excel;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;

namespace CartridgeAccounting.Data
{ 
    public class DatabaseHelper
    {
        // Строка подключения к SQL Server
        private static string connectionString = @"Data Source=DESKTOP-IH53N4B\SQLEXPRESS;Initial Catalog=CartridgeAccountingDB;Integrated Security=True;TrustServerCertificate=True";

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(connectionString);
        }

        // Метод для проверки подключения
        public static bool TestConnection()
        {
            try
            {
                using (SqlConnection conn = GetConnection())
                {
                    conn.Open();
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка подключения: {ex.Message}");
                return false;
            }
        }

        // Метод для выполнения запросов без возврата данных (INSERT, UPDATE, DELETE)
        public static int ExecuteNonQuery(string query, SqlParameter[] parameters = null)
        {
            using (SqlConnection conn = GetConnection())
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    if (parameters != null)
                        cmd.Parameters.AddRange(parameters);
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        // Получение следующего доступного MovementID для таблицы MovementHistory
        // Получение следующего доступного MovementID для таблицы MovementHistory
        public static int GetNextMovementID()
        {
            string query = "SELECT ISNULL(MAX(MovementID), 0) + 1 FROM MovementHistory";
            object result = ExecuteScalar(query);
            return Convert.ToInt32(result);
        }

        public static int GetNextCartridgeID()
        {
            string query = "SELECT ISNULL(MAX(CartridgesID), 0) + 1 FROM Cartridges";
            object result = ExecuteScalar(query);
            return Convert.ToInt32(result);
        }

        public static void ExportToExcel(DataTable data, string defaultFileName)
        {
            if (data == null || data.Rows.Count == 0)
            {
                MessageBox.Show("Нет данных для экспорта.", "Экспорт", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Папка для отчётов (в каталоге приложения)
            string reportsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
            if (!Directory.Exists(reportsDir))
                Directory.CreateDirectory(reportsDir);

            // Имя файла: вид отчёта + дата-время
            string fileName = $"{defaultFileName}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            string fullPath = Path.Combine(reportsDir, fileName);

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Отчёт");
                // Заголовки столбцов
                for (int i = 0; i < data.Columns.Count; i++)
                {
                    worksheet.Cell(1, i + 1).Value = data.Columns[i].ColumnName;
                    worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                }
                // Данные
                for (int row = 0; row < data.Rows.Count; row++)
                {
                    for (int col = 0; col < data.Columns.Count; col++)
                    {
                        worksheet.Cell(row + 2, col + 1).Value = data.Rows[row][col]?.ToString() ?? "";
                    }
                }
                // Автоширина колонок
                worksheet.Columns().AdjustToContents();
                workbook.SaveAs(fullPath);
            }

            MessageBox.Show($"Отчёт сохранён:\n{fullPath}", "Экспорт выполнен", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Метод для получения данных (SELECT)
        public static DataTable ExecuteQuery(string query, SqlParameter[] parameters = null)
        {
            using (SqlConnection conn = GetConnection())
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    if (parameters != null)
                        cmd.Parameters.AddRange(parameters);

                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        DataTable table = new DataTable();
                        adapter.Fill(table);
                        return table;
                    }
                }
            }
        }

        public static object ExecuteScalar(string query, SqlParameter[] parameters = null)
        {
            using (SqlConnection conn = GetConnection())
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    if (parameters != null)
                        cmd.Parameters.AddRange(parameters);
                    return cmd.ExecuteScalar();
                }
            }
        }
    }
}