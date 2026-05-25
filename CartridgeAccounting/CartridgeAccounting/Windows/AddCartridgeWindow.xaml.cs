using CartridgeAccounting.Data;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;

namespace CartridgeAccounting.Windows
{
    public partial class AddCartridgeWindow : Window
    {
        public AddCartridgeWindow()
        {
            InitializeComponent();
            LoadModels();
        }

        private void LoadModels()
        {
            string query = "SELECT ModelID, ModelName FROM CartridgeModels ORDER BY ModelName";
            DataTable dt = DatabaseHelper.ExecuteQuery(query);
            dt.Columns.Add("DisplayText", typeof(string), "ModelName");
            cmbModel.ItemsSource = dt.DefaultView;
            cmbModel.SelectedValuePath = "ModelID";
            cmbModel.DisplayMemberPath = "DisplayText";
            if (dt.Rows.Count > 0) cmbModel.SelectedIndex = 0;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string invNumber = txtInventoryNumber.Text.Trim();
            if (string.IsNullOrWhiteSpace(invNumber))
            {
                MessageBox.Show("Введите инвентарный номер картриджа.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (IsInventoryNumberExists(invNumber))
            {
                MessageBox.Show($"Картридж с инвентарным номером '{invNumber}' уже существует.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (cmbModel.SelectedValue == null)
            {
                MessageBox.Show("Выберите модель картриджа.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int modelId = (int)cmbModel.SelectedValue;
            DateTime? purchaseDate = dpPurchaseDate.SelectedDate;
            decimal? cost = null;
            if (!string.IsNullOrWhiteSpace(txtInitialCost.Text))
            {
                if (decimal.TryParse(txtInitialCost.Text, out decimal parsedCost))
                    cost = parsedCost;
                else
                    MessageBox.Show("Стоимость указана неверно. Будет сохранено как NULL.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            bool isActive = chkIsActive.IsChecked ?? true;

            // Получаем CartridgesID
            int newCartridgeID = DatabaseHelper.GetNextCartridgeID();  

            string insertQuery = @"
                INSERT INTO Cartridges (CartridgesID, InventoryNumber, ModelID, CurrentStatusID, PurchaseDate, InitialCost, IsActive)
                VALUES (@CartridgesID, @InvNumber, @ModelID, 1, @PurchaseDate, @Cost, @IsActive)";
            var parameters = new SqlParameter[]
            {
                new SqlParameter("@CartridgesID", newCartridgeID),
                new SqlParameter("@InvNumber", invNumber),
                new SqlParameter("@ModelID", modelId),
                new SqlParameter("@PurchaseDate", purchaseDate.HasValue ? (object)purchaseDate.Value : DBNull.Value),
                new SqlParameter("@Cost", cost.HasValue ? (object)cost.Value : DBNull.Value),
                new SqlParameter("@IsActive", isActive)
            };
            DatabaseHelper.ExecuteNonQuery(insertQuery, parameters);

            MessageBox.Show("Картридж успешно добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }

        private bool IsInventoryNumberExists(string invNumber)
        {
            string query = "SELECT COUNT(*) FROM Cartridges WHERE InventoryNumber = @InvNumber";
            var param = new SqlParameter("@InvNumber", invNumber);
            object result = DatabaseHelper.ExecuteScalar(query, new[] { param });
            int count = Convert.ToInt32(result);
            return count > 0;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}