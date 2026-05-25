using CartridgeAccounting.Data;
using System.Data;
using System.Windows;
using System.Data.SqlClient;

namespace CartridgeAccounting.Windows
{
    public partial class ReturnedCartridgesWindow : Window
    {
        public ReturnedCartridgesWindow()
        {
            InitializeComponent();
            LoadReturnedHistory();
        }

        private void LoadReturnedHistory()
        {
            string query = @"
        SELECT 
            cm.ModelID,
            c.InventoryNumber,
            cm.ModelName,
            ot.OperationName,
            CASE 
                WHEN ot.OperationsCode = 'Return_From_Department' THEN d.DepartmentName
                WHEN ot.OperationsCode = 'Receive_From_Refill' THEN 'Сервис заправки'
                ELSE NULL
            END AS SourceName,
            mh.MovementDateTime,
            mh.ReceivedByEmployee,
            mh.Notes
        FROM MovementHistory mh
        JOIN Cartridges c ON mh.CartridgeID = c.CartridgesID
        JOIN CartridgeModels cm ON c.ModelID = cm.ModelID
        JOIN OperationsType ot ON mh.OperationsTypeID = ot.OperationsTypeID
        LEFT JOIN Departments d ON mh.DepartmentID = d.DepartmentsID
        WHERE ot.OperationsCode IN ('Return_From_Department', 'Receive_From_Refill')
        ORDER BY mh.MovementDateTime DESC";
            DataTable dt = DatabaseHelper.ExecuteQuery(query);
            dgReturned.ItemsSource = dt.DefaultView;
        }
        // Кнопка "Обновить"
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadReturnedHistory();
            MessageBox.Show("Данные обновлены.", "Обновление", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Кнопка "Отчёт"
        private void BtnReport_Click(object sender, RoutedEventArgs e)
        {
            DataTable dt = ((DataView)dgReturned.ItemsSource).Table;
            if (dt == null || dt.Rows.Count == 0)
            {
                MessageBox.Show("Нет данных для отчёта.", "Отчёт", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Подсчёт статистики
            int totalCount = dt.Rows.Count;
            int returnCount = 0;
            int fromRefillCount = 0;
            DateTime? firstDate = null;
            DateTime? lastDate = null;

            foreach (DataRow row in dt.Rows)
            {
                string opName = row["OperationName"].ToString();
                if (opName.Contains("Возврат из отделения") || opName == "Return")
                    returnCount++;
                else if (opName.Contains("Возврат с заправки") || opName == "FromRefill")
                    fromRefillCount++;

                DateTime movementDate = Convert.ToDateTime(row["MovementDateTime"]);
                if (firstDate == null || movementDate < firstDate) firstDate = movementDate;
                if (lastDate == null || movementDate > lastDate) lastDate = movementDate;
            }

            string report = $"📊 ОТЧЁТ ПО ПРИНЯТЫМ КАРТРИДЖАМ\n\n" +
                            $"Всего операций приёма: {totalCount}\n" +
                            $"Из них:\n" +
                            $"  - Возврат из отделений: {returnCount}\n" +
                            $"  - Возврат с заправки: {fromRefillCount}\n\n" +
                            $"Период: с {firstDate:dd.MM.yyyy} по {lastDate:dd.MM.yyyy}\n\n" +
                            $"Детальную информацию можно экспортировать (функция в разработке).";

            MessageBox.Show(report, "Статистика приёма", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            // Получаем DataTable из источника данных DataGrid
            DataTable dt = ((DataView)dgReturned.ItemsSource).Table;
            DatabaseHelper.ExportToExcel(dt, "Выданные_картриджи");
        }
    }
}