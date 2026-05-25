using CartridgeAccounting.Data;
using System.Data;
using System.Windows;
using CartridgeAccounting.Models;
using System.Windows.Controls;
using System;
using System.Data.SqlClient;
using System.Linq;

namespace CartridgeAccounting.Windows
{
    public partial class IssuedCartridgesWindow : Window
    {
        public IssuedCartridgesWindow()
        {
            InitializeComponent();
            dpIssueDate.SelectedDate = DateTime.Now;
            LoadCartridgesForComboBox();
            LoadDepartmentsForComboBox();
            LoadIssuedHistory();

        }

        private void LoadCartridgesForComboBox()
        {
            string query = @"
                SELECT c.CartridgesID, 
                       c.ModelID,
                       c.InventoryNumber, 
                       cm.ModelName
                FROM Cartridges c
                JOIN CartridgeModels cm ON c.ModelID = cm.ModelID
                WHERE c.CurrentStatusID = 1
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

        // Загрузка отделений
        private void LoadDepartmentsForComboBox()
        {
            string query = "SELECT DepartmentsID, DepartmentName FROM Departments ORDER BY DepartmentName";
            DataTable dt = DatabaseHelper.ExecuteQuery(query);
            cmbDepartment.ItemsSource = dt.DefaultView;
            cmbDepartment.SelectedValuePath = "DepartmentsID";
            cmbDepartment.DisplayMemberPath = "DepartmentName";
            if (dt.Rows.Count > 0) cmbDepartment.SelectedIndex = 0;
        }

        // Загрузка истории выдачи
        private void LoadIssuedHistory()
        {
            string query = @"
        SELECT 
            mh.MovementID,
            cm.ModelID,
            c.InventoryNumber,
            cm.ModelName,
            d.DepartmentName,
            mh.MovementDateTime,
            mh.IssuedByEmployee,
            mh.ReceivedByEmployee,
            mh.Notes
        FROM MovementHistory mh
        JOIN Cartridges c ON mh.CartridgeID = c.CartridgesID
        JOIN CartridgeModels cm ON c.ModelID = cm.ModelID
        LEFT JOIN Departments d ON mh.DepartmentID = d.DepartmentsID
WHERE mh.OperationsTypeID = (SELECT OperationsTypeID FROM OperationsType WHERE OperationsCode = 'SEND_TO_REFILL')        ORDER BY mh.MovementDateTime DESC";
            DataTable dt = DatabaseHelper.ExecuteQuery(query);
            dgIssued.ItemsSource = dt.DefaultView;
        }

        // Выдать картридж
        private void BtnIssue_Click(object sender, RoutedEventArgs e)
        {
            // Проверки
            if (cmbCartridge.SelectedValue == null)
            {
                MessageBox.Show("Выберите картридж.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (cmbDepartment.SelectedValue == null)
            {
                MessageBox.Show("Выберите отделение.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtIssuedBy.Text))
            {
                MessageBox.Show("Укажите, кто выдал картридж.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtReceivedBy.Text))
            {
                MessageBox.Show("Укажите, кто принял картридж в отделении.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Дата и время
            DateTime movementDate = dpIssueDate.SelectedDate ?? DateTime.Now;
            TimeSpan time;
            if (!TimeSpan.TryParse(txtIssueTime.Text, out time))
                time = DateTime.Now.TimeOfDay;
            DateTime fullDateTime = movementDate.Date + time;

            // Получаем MovementID
            int newMovementID = DatabaseHelper.GetNextMovementID();

            // Получаем OperationsTypeID 
            string getOpTypeIdQuery = "SELECT OperationsTypeID FROM OperationsType WHERE OperationsCode = 'ISSUE'";
            object opTypeIdObj = DatabaseHelper.ExecuteScalar(getOpTypeIdQuery);
            if (opTypeIdObj == null)
            {
                MessageBox.Show("В справочнике операций отсутствует тип 'ISSUE'. Добавьте его в таблицу OperationsType.",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            int operationsTypeId = Convert.ToInt32(opTypeIdObj);
            int cartridgeId = (int)cmbCartridge.SelectedValue;
            int departmentId = (int)cmbDepartment.SelectedValue;
            string issuedBy = txtIssuedBy.Text.Trim();
            string receivedBy = txtReceivedBy.Text.Trim();
            string notes = txtNotes.Text.Trim();

            // Запрос INSERT
            string insertMovement = @"
        INSERT INTO MovementHistory 
        (MovementID, CartridgeID, MovementDateTime, OperationsTypeID, DepartmentID, IssuedByEmployee, ReceivedByEmployee, Notes)
        VALUES 
        (@MovementID, @CartridgeID, @DateTime, @OpTypeID, @DeptID, @IssuedBy, @ReceivedBy, @Notes)";

            var parameters = new SqlParameter[]
            {
        new SqlParameter("@MovementID", newMovementID),
        new SqlParameter("@CartridgeID", cartridgeId),
        new SqlParameter("@DateTime", fullDateTime),
        new SqlParameter("@OpTypeID", operationsTypeId),
        new SqlParameter("@DeptID", departmentId),
        new SqlParameter("@IssuedBy", issuedBy),
        new SqlParameter("@ReceivedBy", receivedBy),
        new SqlParameter("@Notes", string.IsNullOrEmpty(notes) ? (object)DBNull.Value : notes)
            };
            DatabaseHelper.ExecuteNonQuery(insertMovement, parameters);

            // Обновляем статус картриджа на "Выдан в отделение"
            string updateCartridge = "UPDATE Cartridges SET CurrentStatusID = 2, CurrentDepartmentID = @DeptID WHERE CartridgesID = @CartridgeID";
            var updateParams = new SqlParameter[]
            {
        new SqlParameter("@DeptID", departmentId),
        new SqlParameter("@CartridgeID", cartridgeId)
            };
            DatabaseHelper.ExecuteNonQuery(updateCartridge, updateParams);

            // Обновляем интерфейс
            LoadCartridgesForComboBox();
            LoadIssuedHistory();
            ClearInputFields();
            MessageBox.Show("Картридж успешно выдан в отделение.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void ClearInputFields()
        {
            txtIssuedBy.Text = "";
            txtReceivedBy.Text = "";
            txtNotes.Text = "";
            dpIssueDate.SelectedDate = DateTime.Now;
            txtIssueTime.Text = DateTime.Now.ToString("HH:mm");
            if (cmbCartridge.HasItems) cmbCartridge.SelectedIndex = 0;
            if (cmbDepartment.HasItems) cmbDepartment.SelectedIndex = 0;
        }
        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            // Получаем DataTable из источника данных DataGrid
            DataTable dt = ((DataView)dgIssued.ItemsSource).Table;
            DatabaseHelper.ExportToExcel(dt, "Выданные_картриджи");
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadCartridgesForComboBox();
            LoadDepartmentsForComboBox();
            LoadIssuedHistory();
            MessageBox.Show("Данные обновлены.", "Обновление", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}