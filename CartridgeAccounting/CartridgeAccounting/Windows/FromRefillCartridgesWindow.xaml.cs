using CartridgeAccounting.Data;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;

namespace CartridgeAccounting.Windows
{
    public partial class FromRefillCartridgesWindow : Window
    {
        public FromRefillCartridgesWindow()
        {
            InitializeComponent();
            dpReceiveDate.SelectedDate = DateTime.Now;
            LoadCartridgesForComboBox();
            LoadFromRefillHistory();
        }

        // Загружаем картриджи со статусом "На заправке" 
        private void LoadCartridgesForComboBox()
        {
            string query = @"
                SELECT c.CartridgesID, 
                       c.ModelID,
                       c.InventoryNumber, 
                       cm.ModelName
                FROM Cartridges c
                JOIN CartridgeModels cm ON c.ModelID = cm.ModelID
                WHERE c.CurrentStatusID = 3
                  AND (c.IsActive = 1 OR c.IsActive IS NULL)
                ORDER BY c.InventoryNumber";
            DataTable dt = DatabaseHelper.ExecuteQuery(query);
            dt.Columns.Add("DisplayText", typeof(string));
            foreach (DataRow row in dt.Rows)
            {
                int modelId = Convert.ToInt32(row["ModelID"]);
                string invNumber = row["InventoryNumber"].ToString();
                string modelName = row["ModelName"].ToString();
                row["DisplayText"] = $"{modelId} | {invNumber} - {modelName}";
            }
            cmbCartridge.ItemsSource = dt.DefaultView;
            cmbCartridge.SelectedValuePath = "CartridgesID";
            cmbCartridge.DisplayMemberPath = "DisplayText";
            if (dt.Rows.Count > 0) cmbCartridge.SelectedIndex = 0;
        }

        // Загружаем историю приёма с заправки 
        private void LoadFromRefillHistory()
        {
            string query = @"
                SELECT 
                    cm.ModelID,
                    c.InventoryNumber,
                    cm.ModelName,
                    mh.MovementDateTime,
                    mh.ReceivedByEmployee,
                    mh.Notes
                FROM MovementHistory mh
                JOIN Cartridges c ON mh.CartridgeID = c.CartridgesID
                JOIN CartridgeModels cm ON c.ModelID = cm.ModelID
                WHERE mh.OperationsTypeID = (SELECT OperationsTypeID FROM OperationsType WHERE OperationsCode = 'RECEIVE_FROM_REFILL')
                ORDER BY mh.MovementDateTime DESC";
            DataTable dt = DatabaseHelper.ExecuteQuery(query);
            dgFromRefill.ItemsSource = dt.DefaultView;
        }

        // Принять картридж с заправки
        private void BtnReceive_Click(object sender, RoutedEventArgs e)
        {
            if (cmbCartridge.SelectedValue == null)
            {
                MessageBox.Show("Выберите картридж для приёма с заправки.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtReceivedBy.Text))
            {
                MessageBox.Show("Укажите, кто принимает картридж.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime receiveDate = dpReceiveDate.SelectedDate ?? DateTime.Now;
            TimeSpan time;
            if (!TimeSpan.TryParse(txtReceiveTime.Text, out time))
                time = DateTime.Now.TimeOfDay;
            DateTime fullDateTime = receiveDate.Date + time;

            int newMovementID = DatabaseHelper.GetNextMovementID();

            string getOpTypeIdQuery = "SELECT OperationsTypeID FROM OperationsType WHERE OperationsCode = 'RECEIVE_FROM_REFILL'";
            object opTypeIdObj = DatabaseHelper.ExecuteScalar(getOpTypeIdQuery);
            if (opTypeIdObj == null)
            {
                MessageBox.Show("В справочнике операций отсутствует тип 'RECEIVE_FROM_REFILL'.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            int operationsTypeId = Convert.ToInt32(opTypeIdObj);
            int cartridgeId = (int)cmbCartridge.SelectedValue;
            string receivedBy = txtReceivedBy.Text.Trim();
            string notes = txtNotes.Text.Trim();

            // Вставка в MovementHistory
            string insertMovement = @"
                INSERT INTO MovementHistory 
                (MovementID, CartridgeID, MovementDateTime, OperationsTypeID, ReceivedByEmployee, Notes)
                VALUES 
                (@MovementID, @CartridgeID, @DateTime, @OpTypeID, @ReceivedBy, @Notes)";
            var parameters = new SqlParameter[]
            {
                new SqlParameter("@MovementID", newMovementID),
                new SqlParameter("@CartridgeID", cartridgeId),
                new SqlParameter("@DateTime", fullDateTime),
                new SqlParameter("@OpTypeID", operationsTypeId),
                new SqlParameter("@ReceivedBy", receivedBy),
                new SqlParameter("@Notes", string.IsNullOrEmpty(notes) ? (object)DBNull.Value : notes)
            };
            DatabaseHelper.ExecuteNonQuery(insertMovement, parameters);

            // Обновляем статус картриджа на "На складе
            string updateCartridge = "UPDATE Cartridges SET CurrentStatusID = 1, CurrentDepartmentID = NULL WHERE CartridgesID = @CartridgeID";
            var updateParams = new SqlParameter[] { new SqlParameter("@CartridgeID", cartridgeId) };
            DatabaseHelper.ExecuteNonQuery(updateCartridge, updateParams);

            LoadCartridgesForComboBox();
            LoadFromRefillHistory();
            ClearInputFields();
            MessageBox.Show("Картридж принят с заправки.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ClearInputFields()
        {
            txtReceivedBy.Text = "";
            txtNotes.Text = "";
            dpReceiveDate.SelectedDate = DateTime.Now;
            txtReceiveTime.Text = DateTime.Now.ToString("HH:mm");
            if (cmbCartridge.HasItems) cmbCartridge.SelectedIndex = 0;
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadCartridgesForComboBox();
            LoadFromRefillHistory();
            MessageBox.Show("Данные обновлены.", "Обновление", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnReport_Click(object sender, RoutedEventArgs e)
        {
            DataTable dt = ((DataView)dgFromRefill.ItemsSource)?.Table;
            if (dt == null || dt.Rows.Count == 0)
            {
                MessageBox.Show("Нет данных для отчёта.", "Отчёт", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int totalCount = dt.Rows.Count;
            DateTime? firstDate = null, lastDate = null;
            foreach (DataRow row in dt.Rows)
            {
                DateTime movementDate = Convert.ToDateTime(row["MovementDateTime"]);
                if (firstDate == null || movementDate < firstDate) firstDate = movementDate;
                if (lastDate == null || movementDate > lastDate) lastDate = movementDate;
            }

            string report = $"📊 ОТЧЁТ ПО КАРТРИДЖАМ, ВЕРНУВШИМСЯ С ЗАПРАВКИ\n\n" +
                            $"Всего возвратов: {totalCount}\n" +
                            $"Период: с {firstDate:dd.MM.yyyy} по {lastDate:dd.MM.yyyy}\n\n" +
                            $"Статус картриджей обновлён на 'На складе (заправлен)'.";
            MessageBox.Show(report, "Статистика возвратов с заправки", MessageBoxButton.OK, MessageBoxImage.Information);


        }
        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            // Получаем DataTable из источника данных DataGrid
            DataTable dt = ((DataView)dgFromRefill.ItemsSource).Table;
            DatabaseHelper.ExportToExcel(dt, "Выданные_картриджи");
        }
    }
}