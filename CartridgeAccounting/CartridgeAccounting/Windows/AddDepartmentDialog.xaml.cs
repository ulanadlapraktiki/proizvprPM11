using CartridgeAccounting.Data;
using System;
using System.Data.SqlClient;
using System.Windows;

namespace CartridgeAccounting.Windows
{
    public partial class AddDepartmentDialog : Window
    {
        private int? _editDepartmentId = null;

        public AddDepartmentDialog()
        {
            InitializeComponent();
            Title = "Добавление отдела";
            btnSave.Content = "💾 Добавить";
        }

        public AddDepartmentDialog(int deptId, string deptName, string building, string responsible)
        {
            InitializeComponent();
            _editDepartmentId = deptId;
            Title = "Редактирование отдела";
            btnSave.Content = "💾 Сохранить";
            txtDepartmentName.Text = deptName;
            txtBuilding.Text = building;
            txtResponsiblePerson.Text = responsible;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string deptName = txtDepartmentName.Text.Trim();
            if (string.IsNullOrWhiteSpace(deptName))
            {
                MessageBox.Show("Введите название отдела.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string building = txtBuilding.Text.Trim();
            if (string.IsNullOrWhiteSpace(building)) building = null;

            string responsible = txtResponsiblePerson.Text.Trim();
            if (string.IsNullOrWhiteSpace(responsible)) responsible = null;

            if (_editDepartmentId.HasValue)
            {
                string updateQuery = @"
                    UPDATE Departments 
                    SET DepartmentName = @DeptName, 
                        Building = @Building, 
                        ResponsiblePerson = @Responsible
                    WHERE DepartmentsID = @DeptID";
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@DeptName", deptName),
                    new SqlParameter("@Building", building ?? (object)DBNull.Value),
                    new SqlParameter("@Responsible", responsible ?? (object)DBNull.Value),
                    new SqlParameter("@DeptID", _editDepartmentId.Value)
                };
                DatabaseHelper.ExecuteNonQuery(updateQuery, parameters);
            }
            else
            {
                string insertQuery = @"
                    INSERT INTO Departments (DepartmentName, Building, ResponsiblePerson)
                    VALUES (@DeptName, @Building, @Responsible)";
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@DeptName", deptName),
                    new SqlParameter("@Building", building ?? (object)DBNull.Value),
                    new SqlParameter("@Responsible", responsible ?? (object)DBNull.Value)
                };
                DatabaseHelper.ExecuteNonQuery(insertQuery, parameters);
            }

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}