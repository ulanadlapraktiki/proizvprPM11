using CartridgeAccounting.Data;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace CartridgeAccounting.Windows
{
    public partial class ReferencesWindow : Window
    {
        public ReferencesWindow()
        {
            InitializeComponent();
            LoadModels();
            LoadDepartments();
        }

        private void LoadModels()
        {
            string query = "SELECT ModelID, ModelName, CompatiblePrinters, StandardResourse, ColorType FROM CartridgeModels ORDER BY ModelName";
            DataTable dt = DatabaseHelper.ExecuteQuery(query);
            dgModels.ItemsSource = dt.DefaultView;
        }

        private void BtnAddModel_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddModelDialog();
            if (dialog.ShowDialog() == true)
            {
                LoadModels();
            }
        }

        private void BtnEditModel_Click(object sender, RoutedEventArgs e)
        {
            if (dgModels.SelectedItem == null)
            {
                MessageBox.Show("Выберите модель для редактирования.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DataRowView selected = (DataRowView)dgModels.SelectedItem;
            int modelId = Convert.ToInt32(selected["ModelID"]);
            string modelName = selected["ModelName"].ToString();
            string compatiblePrinters = selected["CompatiblePrinters"]?.ToString();
            int? standardResource = selected["StandardResourse"] != DBNull.Value ? Convert.ToInt32(selected["StandardResourse"]) : (int?)null;
            string colorType = selected["ColorType"]?.ToString();

            var dialog = new AddModelDialog(modelId, modelName, compatiblePrinters, standardResource, colorType);
            if (dialog.ShowDialog() == true)
            {
                LoadModels();
            }
        }

        private void BtnDeleteModel_Click(object sender, RoutedEventArgs e)
        {
            if (dgModels.SelectedItem == null)
            {
                MessageBox.Show("Выберите модель для удаления.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DataRowView selected = (DataRowView)dgModels.SelectedItem;
            int modelId = Convert.ToInt32(selected["ModelID"]);
            string modelName = selected["ModelName"].ToString();

            // Проверка, есть ли картриджи с этой моделью
            string checkQuery = "SELECT COUNT(*) FROM Cartridges WHERE ModelID = @ModelID";
            var checkParam = new SqlParameter("@ModelID", modelId);
            int count = Convert.ToInt32(DatabaseHelper.ExecuteScalar(checkQuery, new[] { checkParam }));
            if (count > 0)
            {
                MessageBox.Show($"Невозможно удалить модель '{modelName}', так как есть {count} картридж(ев), связанных с ней. Сначала удалите или переназначьте картриджи.",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Удалить модель '{modelName}'?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                string deleteQuery = "DELETE FROM CartridgeModels WHERE ModelID = @ModelID";
                DatabaseHelper.ExecuteNonQuery(deleteQuery, new[] { checkParam });
                LoadModels();
            }
        }

        private void BtnRefreshModels_Click(object sender, RoutedEventArgs e)
        {
            LoadModels();
        }

        private void LoadDepartments()
        {
            string query = "SELECT DepartmentsID, DepartmentName, Building, ResponsiblePerson FROM Departments ORDER BY DepartmentName";
            DataTable dt = DatabaseHelper.ExecuteQuery(query);
            dgDepartments.ItemsSource = dt.DefaultView;
        }

        private void BtnAddDepartment_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddDepartmentDialog();
            if (dialog.ShowDialog() == true)
            {
                LoadDepartments();
            }
        }

        private void BtnEditDepartment_Click(object sender, RoutedEventArgs e)
        {
            if (dgDepartments.SelectedItem == null)
            {
                MessageBox.Show("Выберите отдел для редактирования.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DataRowView selected = (DataRowView)dgDepartments.SelectedItem;
            int deptId = Convert.ToInt32(selected["DepartmentsID"]);
            string deptName = selected["DepartmentName"].ToString();
            string building = selected["Building"]?.ToString();
            string responsible = selected["ResponsiblePerson"]?.ToString();

            var dialog = new AddDepartmentDialog(deptId, deptName, building, responsible);
            if (dialog.ShowDialog() == true)
            {
                LoadDepartments();
            }
        }

        private void BtnDeleteDepartment_Click(object sender, RoutedEventArgs e)
        {
            if (dgDepartments.SelectedItem == null)
            {
                MessageBox.Show("Выберите отдел для удаления.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DataRowView selected = (DataRowView)dgDepartments.SelectedItem;
            int deptId = Convert.ToInt32(selected["DepartmentsID"]);
            string deptName = selected["DepartmentName"].ToString();

            // Проверка, есть ли картриджи
            string checkQuery = "SELECT COUNT(*) FROM Cartridges WHERE CurrentDepartmentID = @DeptID";
            var checkParam = new SqlParameter("@DeptID", deptId);
            int countCartridges = Convert.ToInt32(DatabaseHelper.ExecuteScalar(checkQuery, new[] { checkParam }));
            if (countCartridges > 0)
            {
                MessageBox.Show($"Невозможно удалить отдел '{deptName}', так как есть {countCartridges} картридж(ей), числящихся за ним. Сначала верните картриджи на склад.",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверка, есть ли движения с этим отделом в истории
            string checkHistory = "SELECT COUNT(*) FROM MovementHistory WHERE DepartmentID = @DeptID";
            int countHistory = Convert.ToInt32(DatabaseHelper.ExecuteScalar(checkHistory, new[] { checkParam }));
            if (countHistory > 0)
            {
                MessageBox.Show($"Невозможно удалить отдел '{deptName}', так как он фигурирует в {countHistory} операциях истории. Исторические данные нельзя потерять.",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Удалить отдел '{deptName}'?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                string deleteQuery = "DELETE FROM Departments WHERE DepartmentsID = @DeptID";
                DatabaseHelper.ExecuteNonQuery(deleteQuery, new[] { checkParam });
                LoadDepartments();
            }
        }

        private void BtnRefreshDepartments_Click(object sender, RoutedEventArgs e)
        {
            LoadDepartments();
        }
    }
}
