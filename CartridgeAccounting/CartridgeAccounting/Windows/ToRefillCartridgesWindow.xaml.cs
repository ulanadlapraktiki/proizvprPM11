using CartridgeAccounting.Data;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;


namespace CartridgeAccounting.Windows
{
    public partial class ToRefillCartridgesWindow : Window
    {
        public ToRefillCartridgesWindow()
        {
            InitializeComponent();
            dpRefillDate.SelectedDate = DateTime.Now;
            LoadCartridgesForComboBox();
            LoadToRefillHistory();
        }

        // Загружаем картриджи для отправки 
        private void LoadCartridgesForComboBox()
        {
            string query = @"
                SELECT c.CartridgesID, 
                       c.ModelID,
                       c.InventoryNumber, 
                       cm.ModelName
                FROM Cartridges c
                JOIN CartridgeModels cm ON c.ModelID = cm.ModelID
                WHERE c.CurrentStatusID = 4
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


        // Загружаем историю отправок на заправку 
        private void LoadToRefillHistory()
        {
            string query = @"
                SELECT 
                    cm.ModelID,
                    c.InventoryNumber,
                    cm.ModelName,
                    mh.MovementDateTime,
                    mh.IssuedByEmployee,
                    mh.Notes
                FROM MovementHistory mh
                JOIN Cartridges c ON mh.CartridgeID = c.CartridgesID
                JOIN CartridgeModels cm ON c.ModelID = cm.ModelID
                WHERE mh.OperationsTypeID = (SELECT OperationsTypeID FROM OperationsType WHERE OperationsCode = 'SEND_TO_REFILL')
                ORDER BY mh.MovementDateTime DESC";
            DataTable dt = DatabaseHelper.ExecuteQuery(query);
            dgToRefill.ItemsSource = dt.DefaultView;
        }

        // Отправить картридж на заправку
        private void BtnSendToRefill_Click(object sender, RoutedEventArgs e)
        {
            if (cmbCartridge.SelectedValue == null)
            {
                MessageBox.Show("Выберите картридж для отправки на заправку.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtIssuedBy.Text))
            {
                MessageBox.Show("Укажите, кто отправляет картридж на заправку.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime movementDate = dpRefillDate.SelectedDate ?? DateTime.Now;
            TimeSpan time;
            if (!TimeSpan.TryParse(txtRefillTime.Text, out time))
                time = DateTime.Now.TimeOfDay;
            DateTime fullDateTime = movementDate.Date + time;

            int newMovementID = DatabaseHelper.GetNextMovementID();

            string getOpTypeIdQuery = "SELECT OperationsTypeID FROM OperationsType WHERE OperationsCode = 'SEND_TO_REFILL'";
            object opTypeIdObj = DatabaseHelper.ExecuteScalar(getOpTypeIdQuery);
            if (opTypeIdObj == null)
            {
                MessageBox.Show("В справочнике операций отсутствует тип 'SEND_TO_REFILL'.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            int operationsTypeId = Convert.ToInt32(opTypeIdObj);
            int cartridgeId = (int)cmbCartridge.SelectedValue;
            string issuedBy = txtIssuedBy.Text.Trim();
            string notes = txtNotes.Text.Trim();

            string insertMovement = @"
                INSERT INTO MovementHistory 
                (MovementID, CartridgeID, MovementDateTime, OperationsTypeID, IssuedByEmployee, Notes)
                VALUES 
                (@MovementID, @CartridgeID, @DateTime, @OpTypeID, @IssuedBy, @Notes)";
            var parameters = new SqlParameter[]
            {
                new SqlParameter("@MovementID", newMovementID),
                new SqlParameter("@CartridgeID", cartridgeId),
                new SqlParameter("@DateTime", fullDateTime),
                new SqlParameter("@OpTypeID", operationsTypeId),
                new SqlParameter("@IssuedBy", issuedBy),
                new SqlParameter("@Notes", string.IsNullOrEmpty(notes) ? (object)DBNull.Value : notes)
            };
            DatabaseHelper.ExecuteNonQuery(insertMovement, parameters);

            string updateCartridge = "UPDATE Cartridges SET CurrentStatusID = 3, CurrentDepartmentID = NULL WHERE CartridgesID = @CartridgeID";
            var updateParams = new SqlParameter[] { new SqlParameter("@CartridgeID", cartridgeId) };
            DatabaseHelper.ExecuteNonQuery(updateCartridge, updateParams);

            LoadCartridgesForComboBox();
            LoadToRefillHistory();
            ClearInputFields();
            MessageBox.Show("Картридж отправлен на заправку.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ClearInputFields()
        {
            txtIssuedBy.Text = "";
            txtNotes.Text = "";
            dpRefillDate.SelectedDate = DateTime.Now;
            txtRefillTime.Text = DateTime.Now.ToString("HH:mm");
            if (cmbCartridge.HasItems) cmbCartridge.SelectedIndex = 0;
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadCartridgesForComboBox();
            LoadToRefillHistory();
            MessageBox.Show("Данные обновлены.", "Обновление", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnReport_Click(object sender, RoutedEventArgs e)
        {
            DataTable dt = ((DataView)dgToRefill.ItemsSource)?.Table;
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

            string report = $"📊 ОТЧЁТ ПО КАРТРИДЖАМ, ОТПРАВЛЕННЫМ НА ЗАПРАВКУ\n\n" +
                            $"Всего отправлено: {totalCount}\n" +
                            $"Период: с {firstDate:dd.MM.yyyy} по {lastDate:dd.MM.yyyy}\n\n" +
                            $"Рекомендуется связаться с сервисным центром для контроля возврата.";
            MessageBox.Show(report, "Статистика отправок на заправку", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            // Получаем DataTable из источника данных DataGrid
            DataTable dt = ((DataView)dgToRefill.ItemsSource).Table;
            DatabaseHelper.ExportToExcel(dt, "Выданные_картриджи");
        }

    }
}