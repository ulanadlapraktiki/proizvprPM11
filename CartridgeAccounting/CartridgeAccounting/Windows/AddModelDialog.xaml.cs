using CartridgeAccounting.Data;
using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace CartridgeAccounting.Windows
{
    public partial class AddModelDialog : Window
    {
        public string NewModelName { get; private set; }
        private int? _editModelId = null;

        // Конструктор для добавления новой модели
        public AddModelDialog()
        {
            InitializeComponent();
            Title = "Добавление новой модели";
            btnSave.Content = "💾 Добавить";
        }

        // Конструктор для редактирования модели
        public AddModelDialog(int modelId, string modelName, string compatiblePrinters, int? standardResource, string colorType)
        {
            InitializeComponent();
            _editModelId = modelId;
            Title = "Редактирование модели";
            btnSave.Content = "💾 Сохранить";
            txtModelName.Text = modelName;
            txtCompatiblePrinters.Text = compatiblePrinters;
            if (standardResource.HasValue) txtStandardResource.Text = standardResource.Value.ToString();
            if (!string.IsNullOrEmpty(colorType))
            {
                if (colorType.Contains("Чёрно") || colorType == "ЧБ")
                    cmbColorType.SelectedIndex = 0;
                else if (colorType.Contains("Цвет"))
                    cmbColorType.SelectedIndex = 1;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string modelName = txtModelName.Text.Trim();
            if (string.IsNullOrWhiteSpace(modelName))
            {
                MessageBox.Show("Введите название модели.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string compatiblePrinters = txtCompatiblePrinters.Text.Trim();
            if (string.IsNullOrWhiteSpace(compatiblePrinters)) compatiblePrinters = null;

            int? standardResource = null;
            if (!string.IsNullOrWhiteSpace(txtStandardResource.Text))
            {
                if (int.TryParse(txtStandardResource.Text, out int res))
                    standardResource = res;
                else
                    MessageBox.Show("Ресурс должен быть числом. Поле будет сохранено пустым.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            string colorType = (cmbColorType.SelectedItem as ComboBoxItem)?.Content.ToString();

            if (_editModelId.HasValue)
            {
                // Редактирование
                string updateQuery = @"
                    UPDATE CartridgeModels 
                    SET ModelName = @ModelName, 
                        CompatiblePrinters = @CompatiblePrinters, 
                        StandardResourse = @StandardResourse, 
                        ColorType = @ColorType
                    WHERE ModelID = @ModelID";
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@ModelName", modelName),
                    new SqlParameter("@CompatiblePrinters", string.IsNullOrEmpty(compatiblePrinters) ? (object)DBNull.Value : compatiblePrinters),
                    new SqlParameter("@StandardResourse", standardResource.HasValue ? (object)standardResource.Value : DBNull.Value),
                    new SqlParameter("@ColorType", string.IsNullOrEmpty(colorType) ? (object)DBNull.Value : colorType),
                    new SqlParameter("@ModelID", _editModelId.Value)
                };
                DatabaseHelper.ExecuteNonQuery(updateQuery, parameters);
            }
            else
            {
                // Добавление
                string insertQuery = @"
                    INSERT INTO CartridgeModels (ModelName, CompatiblePrinters, StandardResourse, ColorType)
                    VALUES (@ModelName, @CompatiblePrinters, @StandardResourse, @ColorType)";
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@ModelName", modelName),
                    new SqlParameter("@CompatiblePrinters", string.IsNullOrEmpty(compatiblePrinters) ? (object)DBNull.Value : compatiblePrinters),
                    new SqlParameter("@StandardResourse", standardResource.HasValue ? (object)standardResource.Value : DBNull.Value),
                    new SqlParameter("@ColorType", string.IsNullOrEmpty(colorType) ? (object)DBNull.Value : colorType)
                };
                DatabaseHelper.ExecuteNonQuery(insertQuery, parameters);
            }

            NewModelName = modelName;
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